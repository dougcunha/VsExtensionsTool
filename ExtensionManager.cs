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
    /// The version of the extension.
    /// </summary>
    public string Version { get; init; } = string.Empty;
}

/// <summary>
/// Provides methods to list, filter, and remove Visual Studio extensions for a given installation.
/// </summary>
public sealed class ExtensionManager
{
    private const string EXTENSIONS_RELATIVE_PATH = "Extensions";
    private const string VSIX_MANIFEST = "extension.vsixmanifest";
    private string _instanceId = string.Empty;
    private string _installationVersion = string.Empty;

    /// <summary>
    /// Sets the instance ID and installation version for the Visual Studio instance to manage extensions for.
    /// </summary>
    /// <param name="instanceId">The instance ID of the Visual Studio installation.</param>
    /// <param name="installationVersion">The installation version of the Visual Studio installation.</param>
    public void SetInstanceInfo(string instanceId, string installationVersion)
    {
        _instanceId = instanceId;
        _installationVersion = installationVersion;
    }

    /// <summary>
    /// Gets all extensions installed for the specified Visual Studio installation.
    /// </summary>
    /// <param name="installationPath">The installation path of Visual Studio.</param>
    /// <returns>List of ExtensionInfo objects representing each extension.</returns>
    public List<ExtensionInfo> GetExtensions(string installationPath)
    {
        var extensions = new List<ExtensionInfo>();

        foreach (var folder in GetExtensionPaths(installationPath).Where(Directory.Exists))
        {
            foreach (var dir in Directory.GetDirectories(folder))
            {
                var manifestPath = Path.Combine(dir, VSIX_MANIFEST);

                if (!File.Exists(manifestPath))
                    continue;

                var info = ReadManifest(manifestPath);

                if (info != null)
                    extensions.Add(info);
            }
        }

        return [.. extensions.OrderBy(static e => e.Name, StringComparer.CurrentCultureIgnoreCase)];
    }

    /// <summary>
    /// Removes an extension by its Id for the specified Visual Studio installation.
    /// </summary>
    /// <param name="installationPath">The installation path of Visual Studio.</param>
    /// <param name="id">The Id of the extension to remove.</param>
    public void RemoveExtensionById(string installationPath, string id)
    {
        const string VSIX_INSTALLER_RELATIVE_PATH = "Common7/IDE/VSIXInstaller.exe";
        var vsixInstallerPath = Path.Combine(installationPath, VSIX_INSTALLER_RELATIVE_PATH);

        if (!File.Exists(vsixInstallerPath))
        {
            Console.WriteLine("VSIXInstaller.exe not found in this installation.");

            return;
        }

        var extensions = GetExtensions(installationPath);
        var extension = extensions.FirstOrDefault(e => e.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

        if (extension == null)
        {
            Console.WriteLine("Extension not found.");

            return;
        }

        using var process = new Process();

        process.StartInfo = new ProcessStartInfo
        {
            FileName = vsixInstallerPath,
            Arguments = $"/uninstall:{extension.Id}",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        Console.WriteLine(output);
        Console.WriteLine($"Extension '{extension.Name}' removed (Id: {extension.Id}).");
    }

    /// <summary>
    /// Returns the possible extension folders for the current Visual Studio instance.
    /// </summary>
    /// <param name="installationPath">The installation path of Visual Studio.</param>
    /// <returns>List of possible extension folder paths.</returns>
    private List<string> GetExtensionPaths(string installationPath)
    {
        var paths = new List<string>();
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        if (!string.IsNullOrEmpty(_instanceId) && !string.IsNullOrEmpty(_installationVersion))
        {
            var mainVersion = GetMainVersion(_installationVersion); // e.g. 17.0
            var userPath = Path.Combine(localAppData, "Microsoft", "VisualStudio", $"{mainVersion}_{_instanceId}", EXTENSIONS_RELATIVE_PATH);
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
            var extensionInfo = new ExtensionInfo { Name = name, Id = id, Publisher = publisher, Version = version };
            PopulateFromManifestJson(manifestPath, extensionInfo);

            return extensionInfo;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error occurred while reading the manifest file." + ex.Message);

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
            Console.WriteLine("Error occurred while reading the manifest file." + ex.Message);
        }
    }
}
