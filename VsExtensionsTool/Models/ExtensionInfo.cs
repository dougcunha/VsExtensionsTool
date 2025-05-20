namespace VsExtensionsTool.Models;

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
    public IEnumerator<ExtensionInfo> GetEnumerator()
    {
        yield break;
    }
}
