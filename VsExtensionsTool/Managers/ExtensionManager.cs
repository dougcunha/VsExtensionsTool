using System.IO.Abstractions;
using System.Text.Json;

namespace VsExtensionsTool.Managers;

/// <inheritdoc/>
public sealed class ExtensionManager
(
    IAnsiConsole console,
    IMarketplaceHelper mktHelper,
    IFileSystem fileSystem,
    IProcessRunner processRunner,
    IXDocumentLoader xDocumentLoader
) : IExtensionManager
{
    private const string EXTENSIONS_RELATIVE_PATH = "Extensions";
    private const string VSIX_MANIFEST = "extension.vsixmanifest";

    /// <inheritdoc/>
    public List<ExtensionInfo> GetExtensions(VisualStudioInstance instance, string? filter = null)
    {
        var extensions = new List<ExtensionInfo>();

        foreach (var dir in GetExtensionPaths(instance.InstallationPath!, instance)
            .Where(fileSystem.Directory.Exists)
            .SelectMany(fileSystem.Directory.GetDirectories))
        {
            var manifestPath = Path.Combine(dir, VSIX_MANIFEST);

            if (!fileSystem.File.Exists(manifestPath))
                continue;

            var info = ReadManifest(manifestPath);

            if (info != null)
                extensions.Add(info);
        }

        if (!string.IsNullOrEmpty(filter))
        {
            extensions =
            [
                .. extensions.Where
                (e => e.Name.Contains
                (
                    filter,
                    StringComparison.CurrentCultureIgnoreCase
                )
                || e.Id.Contains
                (
                    filter,
                    StringComparison.CurrentCultureIgnoreCase
                )
                )
            ];
        }

        return [.. extensions.OrderBy(static e => e.Name, StringComparer.CurrentCultureIgnoreCase)];
    }

    /// <inheritdoc/>
    public async Task PopulateExtensionInfoFromMarketplaceAsync
    (
        VisualStudioInstance instance,
        List<ExtensionInfo> extensions,
        Action<ExtensionInfo> onPopulate
    )
    {
        foreach (var ext in extensions)
        {
            await mktHelper.PopulateExtensionInfoFromMarketplaceAsync(ext, instance).ConfigureAwait(false);
            onPopulate(ext);
        }
    }

    /// <inheritdoc/>
    public async Task RemoveExtensionByIdAsync(VisualStudioInstance instance, string id)
    {
        const string VSIX_INSTALLER_RELATIVE_PATH = "Common7/IDE/VSIXInstaller.exe";
        var vsixInstallerPath = Path.Combine(instance.InstallationPath!, VSIX_INSTALLER_RELATIVE_PATH);

        if (!fileSystem.File.Exists(vsixInstallerPath))
        {
            console.MarkupLine("[red]VSIXInstaller.exe not found in this installation.[/]");

            return;
        }

        var extensions = GetExtensions(instance, null);
        var extension = extensions.FirstOrDefault(e => e.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

        if (extension == null)
        {
            console.MarkupLine("[red]Extension not found.[/]");

            return;
        }

        var output = await processRunner.RunAsync
        (
            vsixInstallerPath,
            $"/uninstall:{extension.Id} /quiet /sp /instanceIds:{instance.InstanceId}"
        ).ConfigureAwait(false);

        console.MarkupLine(Markup.Escape(output));
        console.MarkupLineInterpolated($"Extension '{extension.Name}' removed (Id: {extension.Id}).");
    }

    /// <summary>
    /// Downloads the VSIX file from the marketplace using the provided URL.
    /// </summary>
    /// <param name="selectedExt">
    /// The extension information containing the URL to download the VSIX file.
    /// </param>
    /// <returns>
    /// The path to the downloaded VSIX file.
    /// </returns>
    private async Task<string> DownloadVsixFromMarketplaceAsync(ExtensionInfo selectedExt)
    {
        var tempVsixPath = Path.Combine(Path.GetTempPath(), $"{selectedExt.Id}_{Guid.NewGuid()}.vsix");
        console.MarkupLine("[blue]Downloading VSIX from Marketplace...[/]");
        using var http = new HttpClient();
        var vsixBytes = await http.GetByteArrayAsync(selectedExt.VsixUrl).ConfigureAwait(false);
        await fileSystem.File.WriteAllBytesAsync(tempVsixPath, vsixBytes).ConfigureAwait(false);

        return tempVsixPath;
    }

    /// <inheritdoc/>
    public async Task<string> UpdateExtensionAsync(ExtensionInfo selectedExt, VisualStudioInstance instance)
    {
        var vsixPath = await DownloadVsixFromMarketplaceAsync(selectedExt).ConfigureAwait(false);

        try
        {
            var vsixInstallerPath = Path.Combine(instance.InstallationPath!, "Common7", "IDE", "VSIXInstaller.exe");

            if (!fileSystem.File.Exists(vsixInstallerPath))
            {
                console.MarkupLine("[red]VSIXInstaller.exe not found in this installation.[/]");

                return string.Empty;
            }

            var output = await processRunner.RunAsync
            (
                vsixInstallerPath,
                $"/quiet /instanceIds:{instance.InstanceId} \"{vsixPath}\""
            ).ConfigureAwait(false);

            return output;
        }
        finally
        {
            try
            {
                fileSystem.File.Delete(vsixPath);
            }
            catch
            {
                console.MarkupLine("[red]Error deleting temporary VSIX file.[/]");
            }
        }
    }

    /// <summary>
    /// Returns the possible extension folders for the current Visual Studio instance.
    /// </summary>
    /// <param name="installationPath">The installation path of Visual Studio.</param>
    /// <param name="instance">A VisualStudioInstance para contexto do usuário.</param>
    /// <returns>List of possible extension folder paths.</returns>
    private static List<string> GetExtensionPaths(string installationPath, VisualStudioInstance? instance)
    {
        var paths = new List<string>();
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        if (instance is { InstallationVersion: not null, InstanceId: not null })
        {
            var mainVersion = GetMainVersion(instance.InstallationVersion); // e.g. 17.0
            var userPath = Path.Combine(localAppData, "Microsoft", "VisualStudio", $"{mainVersion}_{instance.InstanceId}", EXTENSIONS_RELATIVE_PATH);
            paths.Add(userPath);
        }

        var globalPath = Path.Combine(installationPath, "Common7", "IDE", EXTENSIONS_RELATIVE_PATH);
        paths.Add(globalPath);

        return paths;
    }

    /// <summary>
    /// Gets the main version (major.minor) from the full installation version string.
    /// </summary>
    /// <param name="installationVersion">The full installation version string.</param>
    /// <returns>The main version string (e.g., "17.0").</returns>
    private static string GetMainVersion(string installationVersion)
    {
        var parts = installationVersion.Split('.');

        return parts.Length > 0
            ? $"{parts[0]}.0"
            : installationVersion;
    }

    /// <summary>
    /// Reads the extension manifest and returns extension information.
    /// </summary>
    /// <param name="manifestPath">The path to the extension.vsixmanifest file.</param>
    /// <returns>ExtensionInfo object or null if not found/invalid.</returns>
    private ExtensionInfo? ReadManifest(string manifestPath)
    {
        try
        {
            var doc = xDocumentLoader.Load(manifestPath);
            var ns = doc.Root?.GetDefaultNamespace() ?? "";
            var metadata = doc.Descendants(ns + "Metadata").FirstOrDefault();

            if (metadata == null)
                return null;

            var identity = metadata.Element(ns + "Identity");
            var id = identity?.Attribute("Id")?.Value ?? "";
            var publisher = identity?.Attribute("Publisher")?.Value ?? "";
            var name = metadata.Element(ns + "DisplayName")?.Value ?? "";
            var version = identity?.Attribute("Version")?.Value ?? "";
            var extensionInfo = new ExtensionInfo { Name = name, Id = id, Publisher = publisher, InstalledVersion = version };
            PopulateFromManifestJson(manifestPath, extensionInfo);

            return extensionInfo;
        }
        catch (Exception ex)
        {
            console.MarkupLine($"[red]Error occurred while reading the manifest file. {ex.Message.EscapeMarkup()}[/]");

            return null;
        }
    }

    /// <summary>
    /// Populates the specified <see cref="ExtensionInfo"/> object with data from a manifest.json file.
    /// </summary>
    /// <remarks>This method attempts to locate and read a manifest.json file in the same directory as the
    /// specified manifest path. If the file exists, it parses the JSON content and extracts the "vsixId" property to
    /// populate the <paramref name="extension"/> object. If the file does not exist, the method returns without making
    /// any changes. If an error occurs during file reading or JSON parsing, an error message is logged to the
    /// console.</remarks>
    /// <param name="manifestPath">The file path to the manifest file directory.</param>
    /// <param name="extension">The <see cref="ExtensionInfo"/> object to populate with data from the manifest.</param>
    private void PopulateFromManifestJson(string manifestPath, ExtensionInfo extension)
    {
        var jsonManifest = Path.GetDirectoryName(manifestPath) + Path.DirectorySeparatorChar + "manifest.json";

        if (!fileSystem.File.Exists(jsonManifest))
            return;

        try
        {
            var json = fileSystem.File.ReadAllText(jsonManifest);
            var doc = JsonDocument.Parse(json);

            extension.VsixId = doc.RootElement.GetProperty("vsixId").GetString();
        }
        catch (Exception ex)
        {
            console.MarkupLine($"[red]Error occurred while reading the manifest file. {ex.Message.EscapeMarkup()}[/]");
        }
    }
}
