using System.Text.Json;
using System.Text.Json.Serialization;

namespace VsExtensionsTool;

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
    /// The path to the main product executable.
    /// </summary>
    [JsonPropertyName("productPath")]
    public string? ProductPath { get; set; }

    /// <summary>
    /// The channel ID of the Visual Studio installation.
    /// </summary>
    [JsonPropertyName("channelId")]
    public string? ChannelId { get; set; }

    /// <summary>
    /// The channel URI of the Visual Studio installation.
    /// </summary>
    [JsonPropertyName("channelUri")]
    public string? ChannelUri { get; set; }

    /// <summary>
    /// The version of the Visual Studio installation.
    /// </summary>
    [JsonPropertyName("installationVersion")]
    public string? InstallationVersion { get; set; }

    /// <summary>
    /// Indicates if the installation is a prerelease (Preview).
    /// </summary>
    [JsonPropertyName("isPrerelease")]
    public bool IsPrerelease { get; set; }

    /// <summary>
    /// The unique instance ID of the Visual Studio installation.
    /// </summary>
    [JsonPropertyName("instanceId")]
    public string? InstanceId { get; set; }
}

/// <summary>
/// Provides methods to detect and retrieve Visual Studio installations using vswhere.
/// </summary>
public sealed class VisualStudioManager
{
    private const string VSWHERE_PATH = @"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe";
    private const string VSWHERE_ARGS = "-all -prerelease -format json";

    /// <summary>
    /// Gets all Visual Studio installations on the system, including Preview versions.
    /// </summary>
    /// <returns>List of VisualStudioInstance objects representing each installation.</returns>
    public List<VisualStudioInstance> GetVisualStudioInstallations()
    {
        var vswherePath = Environment.ExpandEnvironmentVariables(VSWHERE_PATH);

        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = vswherePath,
                Arguments = VSWHERE_ARGS,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        if (string.IsNullOrWhiteSpace(output))
            return [];

        return JsonSerializer.Deserialize<List<VisualStudioInstance>>(output) ?? [];
    }
}
