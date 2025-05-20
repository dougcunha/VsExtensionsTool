using System.Text.Json.Serialization;

namespace VsExtensionsTool.Models;

/// <summary>
/// Represents information about a Visual Studio installation.
/// </summary>
public sealed class VisualStudioInstance
{
    /// <summary>
    /// The display name of the Visual Studio installation.
    /// </summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// The installation path of Visual Studio.
    /// </summary>
    [JsonPropertyName("installationPath")]
    public string? InstallationPath { get; set; }

    /// <summary>
    /// The channel ID of the Visual Studio installation.
    /// </summary>
    [JsonPropertyName("channelId")]
    public string? ChannelId { get; set; }

    /// <summary>
    /// The version of the Visual Studio installation.
    /// </summary>
    [JsonPropertyName("installationVersion")]
    public string? InstallationVersion { get; set; }

    /// <summary>
    /// The unique instance ID of the Visual Studio installation.
    /// </summary>
    [JsonPropertyName("instanceId")]
    public string? InstanceId { get; set; }
}
