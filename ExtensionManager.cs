using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;

namespace VsExtensionsTool;

/// <summary>
/// Represents information about a Visual Studio extension.
/// </summary>
public sealed class ExtensionInfo
{
    /// <summary>
    /// The unique identifier of the extension.
    /// </summary>
    public string? VsixId { get; set; }

    /// <summary>
    /// The display name of the extension.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// The unique identifier of the extension.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// The publisher of the extension.
    /// </summary>
    public string Publisher { get; init; } = string.Empty;

    /// <summary>
    /// The version of the installed extension.
    /// </summary>
    public string InstalledVersion { get; init; } = string.Empty;

    /// <summary>
    /// The version of the extension on marketplace.
    /// </summary>
    public string LatestVersion { get; set; } = "Not found";

    /// <summary>
    /// The URL to download the extension from the marketplace.
    /// </summary>
    public string? VsixUrl { get; set; }

    /// <summary>
    /// If the extension version is outdated.
    /// </summary>
    public bool IsOutdated
        => !string.Equals(InstalledVersion, LatestVersion, StringComparison.OrdinalIgnoreCase) && LatestVersion != "Not found";
}

/// <summary>
/// Provides methods to list, filter, and remove Visual Studio extensions for a given installation.
/// </summary>
public static class ExtensionManager
{
    private const string EXTENSIONS_RELATIVE_PATH = "Extensions";
    private const string VSIX_MANIFEST = "extension.vsixmanifest";

    /// <summary>
    /// Gets all extensions installed for the specified Visual Studio installation.
    /// </summary>
    /// <param name="installationPath">The installation path of Visual Studio.</param>
    /// <param name="filter">
    /// An optional filter string to search for extensions by name or Id.
    /// </param>
    /// <returns>List of ExtensionInfo objects representing each extension.</returns>
    public static List<ExtensionInfo> GetExtensions(VisualStudioInstance instance, string? filter = null)
    {
        var extensions = new List<ExtensionInfo>();

        foreach (var dir in GetExtensionPaths(instance.InstallationPath!, instance).Where(Directory.Exists).SelectMany(Directory.GetDirectories))
        {
            var manifestPath = Path.Combine(dir, VSIX_MANIFEST);

            if (!File.Exists(manifestPath))
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

    public static async Task PopulateExtensionInfoFromMarketplaceAsync(VisualStudioInstance instance, List<ExtensionInfo> extensions, Action<ExtensionInfo> onPopulate)
    {
        foreach (var ext in extensions)
        {
            await MarketplaceHelper.PopulateExtensionInfoFromMarketplaceAsync(ext, instance!);
            onPopulate(ext);
        }
    }

    /// <summary>
    /// Removes an extension by its Id for the specified Visual Studio installation.
    /// </summary>
    /// <param name="installationPath">The installation path of Visual Studio.</param>
    /// <param name="id">The Id of the extension to remove.</param>
    /// <param name="instanceId">
    /// The instance ID of the Visual Studio installation.
    /// </param>
    public static void RemoveExtensionById(VisualStudioInstance instance, string id)
    {
        const string VSIX_INSTALLER_RELATIVE_PATH = "Common7/IDE/VSIXInstaller.exe";
        var vsixInstallerPath = Path.Combine(instance.InstallationPath!, VSIX_INSTALLER_RELATIVE_PATH);

        if (!File.Exists(vsixInstallerPath))
        {
            AnsiConsole.MarkupLine("[red]VSIXInstaller.exe not found in this installation.[/]");

            return;
        }

        var extensions = GetExtensions(instance, null);
        var extension = extensions.FirstOrDefault(e => e.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

        if (extension == null)
        {
            AnsiConsole.MarkupLine("[red]Extension not found.[/]");

            return;
        }

        using var process = new Process();

        process.StartInfo = new ProcessStartInfo
        {
            FileName = vsixInstallerPath,
            Arguments = $"/uninstall:{extension.Id} /quiet /sp /instanceIds:{instance.InstanceId}",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        AnsiConsole.MarkupLine(Markup.Escape(output));
        AnsiConsole.MarkupLineInterpolated($"Extension '{extension.Name}' removed (Id: {extension.Id}).");
    }

    private static async Task<string> DownloadVsixFromMarketplaceAsync(ExtensionInfo selectedExt)
    {
        var tempVsixPath = Path.Combine(Path.GetTempPath(), $"{selectedExt.Id}_{Guid.NewGuid()}.vsix");
        AnsiConsole.MarkupLine("[blue]Downloading VSIX from Marketplace...[/]");
        using var http = new HttpClient();
        var vsixBytes = await http.GetByteArrayAsync(selectedExt.VsixUrl).ConfigureAwait(false);
        await File.WriteAllBytesAsync(tempVsixPath, vsixBytes).ConfigureAwait(false);

        return tempVsixPath;
    }

    /// <summary>
    /// Updates an extension by its path for the specified Visual Studio installation.
    /// </summary>
    /// <param name="selectedExt">
    /// The selected extension to update.
    /// </param>
    /// <param name="instance">
    /// The Visual Studio instance to update the extension for.
    /// </param>
    /// <returns>
    /// The output of the VSIXInstaller process.
    /// </returns>
    public static async Task<string> UpdateExtensionAsync(ExtensionInfo selectedExt, VisualStudioInstance instance)
    {
        var vsixPath = await DownloadVsixFromMarketplaceAsync(selectedExt).ConfigureAwait(false);

        try
        {
            var vsixInstallerPath = Path.Combine(instance.InstallationPath!, "Common7", "IDE", "VSIXInstaller.exe");

            if (!File.Exists(vsixInstallerPath))
            {
                AnsiConsole.MarkupLine("[red]VSIXInstaller.exe not found in this installation.[/]");

                return string.Empty;
            }

            using var process = new Process();

            process.StartInfo = new ProcessStartInfo
            {
                FileName = vsixInstallerPath,
                Arguments = $"/quiet /instanceIds:{instance.InstanceId} \"{vsixPath}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
            await process.WaitForExitAsync().ConfigureAwait(false);

            return output;
        }
        finally
        {
            try
            {
                File.Delete(vsixPath);
            }
            catch
            {
                AnsiConsole.MarkupLine("[red]Error deleting temporary VSIX file.[/]");
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

        if (instance?.InstallationVersion is not null && instance?.InstanceId is not null)
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
    private static ExtensionInfo? ReadManifest(string manifestPath)
    {
        try
        {
            var doc = XDocument.Load(manifestPath);
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
            AnsiConsole.MarkupLine($"[red]Error occurred while reading the manifest file. {ex.Message.EscapeMarkup()}[/]");

            return null;
        }
    }

    private static void PopulateFromManifestJson(string manifestPath, ExtensionInfo extension)
    {
        var jsonManifest = Path.GetDirectoryName(manifestPath) + Path.DirectorySeparatorChar + "manifest.json";

        if (!File.Exists(jsonManifest))
            return;

        try
        {
            var json = File.ReadAllText(jsonManifest);
            var doc = JsonDocument.Parse(json);

            extension.VsixId = doc.RootElement.GetProperty("vsixId").GetString();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error occurred while reading the manifest file. {ex.Message.EscapeMarkup()}[/]");
        }
    }
}
