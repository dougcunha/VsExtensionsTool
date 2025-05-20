using System.Text.Json;

namespace VsExtensionsTool.Managers;

/// <inheritdoc/>
public sealed class VisualStudioManager
(
    IAnsiConsole console,
    IVisualStudioDisplayHelper vsDisplayHelper,
    IProcessRunner processRunner
) : IVisualStudioManager
{
    private const string VSWHERE_PATH = @"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe";
    private const string VSWHERE_ARGS = "-all -prerelease -format json";

    /// <inheritdoc/>
    public VisualStudioInstance? VisualStudio { get; set; }

    /// <inheritdoc/>
    public async Task<List<VisualStudioInstance>> GetVisualStudioInstallationsAsync()
    {
        var output = string.Empty;

        await console.Status()
            .Spinner(Spinner.Known.Dots2)
            .SpinnerStyle(Style.Parse("green"))
            .StartAsync("[green]Detecting Visual Studio installations...[/]", async _ =>
            {
                var vswherePath = Environment.ExpandEnvironmentVariables(VSWHERE_PATH);

                output = await processRunner.RunAsync(vswherePath, VSWHERE_ARGS).ConfigureAwait(false);
            }).ConfigureAwait(false);

        return string.IsNullOrWhiteSpace(output)
            ? []
            : JsonSerializer.Deserialize<List<VisualStudioInstance>>(output) ?? [];
    }

    /// <inheritdoc />
    public async Task<VisualStudioInstance?> SelectVisualStudioInstanceAsync(bool reprompt = true)
    {
        if (!reprompt && VisualStudio is not null)
            return VisualStudio;

        var installations = await GetVisualStudioInstallationsAsync().ConfigureAwait(false);

        if (installations.Count == 0)
        {
            vsDisplayHelper.PrintInstallationsTable(installations);

            return null;
        }

        if (installations.Count == 1)
        {
            VisualStudio = installations[0];

            return VisualStudio;
        }

        vsDisplayHelper.PrintInstallationsTable(installations, false);
        var choice = await console.AskAsync<int>("Enter the number of the desired installation (0 to cancel): ").ConfigureAwait(false);

        VisualStudio = choice > 0 && choice <= installations.Count
            ? installations[choice - 1]
            : null;

        return VisualStudio;
    }
}
