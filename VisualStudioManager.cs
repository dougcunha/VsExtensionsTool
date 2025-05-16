using System.Text.Json;

namespace VsExtensionsTool;

using System.Diagnostics;

/// <summary>
/// Provides methods to detect and retrieve Visual Studio installations using vswhere.
/// </summary>
public static class VisualStudioManager
{
    private const string VSWHERE_PATH = @"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe";
    private const string VSWHERE_ARGS = "-all -prerelease -format json";

    /// <summary>
    /// Gets all Visual Studio installations on the system, including Preview versions.
    /// </summary>
    /// <returns>List of VisualStudioInstance objects representing each installation.</returns>
    public static async Task<List<VisualStudioInstance>> GetVisualStudioInstallationsAsync()
    {
        var output = string.Empty;

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots2)
            .SpinnerStyle(Style.Parse("green"))
            .StartAsync("[green]Detecting Visual Studio installations...[/]", async _ =>
            {
                var vswherePath = Environment.ExpandEnvironmentVariables(VSWHERE_PATH);
                using var process = new Process();

                process.StartInfo = new ProcessStartInfo
                {
                    FileName = vswherePath,
                    Arguments = VSWHERE_ARGS,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                process.Start();
                output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                await process.WaitForExitAsync().ConfigureAwait(false);
            }).ConfigureAwait(false);

        return string.IsNullOrWhiteSpace(output)
            ? []
            : JsonSerializer.Deserialize<List<VisualStudioInstance>>(output) ?? [];
    }

    /// <summary>
    /// Selects the Visual Studio instance, if necessary.
    /// </summary>
    public static async Task<VisualStudioInstance?> SelectVisualStudioInstanceAsync()
    {
        var installations = await GetVisualStudioInstallationsAsync().ConfigureAwait(false);

        if (installations.Count == 0)
        {
            VisualStudioDisplayHelper.PrintInstallationsTable(installations);

            return null;
        }

        if (installations.Count == 1)
            return installations[0];

        VisualStudioDisplayHelper.PrintInstallationsTable(installations, false);
        var choice = await AnsiConsole.AskAsync<int>("Enter the number of the desired installation (0 to cancel): ").ConfigureAwait(false);

        return choice > 0 && choice <= installations.Count
            ? installations[choice - 1]
            : null;
    }
}
