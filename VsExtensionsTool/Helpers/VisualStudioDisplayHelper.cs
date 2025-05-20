namespace VsExtensionsTool.Helpers;

/// <inheritdoc/>
public sealed class VisualStudioDisplayHelper(IAnsiConsole console) : IVisualStudioDisplayHelper
{
    private const string NUMBER_HEADER = "#";
    private const string NAME_HEADER = "Name";
    private const string VERSION_HEADER = "InstalledVersion";
    private const string PREVIEW_LABEL = " [yellow](Preview)[/]";
    private const string NO_INSTALLATION_FOUND = "No Visual Studio installations found.";
    private const string DETECTED_INSTALLATIONS = "Detected Visual Studio installations:";

    /// <inheritdoc/>
    public void PrintInstallationsTable
    (
        IReadOnlyList<VisualStudioInstance>? installations,
        bool showHeader = true
    )
    {
        if (installations == null || installations.Count == 0)
        {
            console.MarkupLine($"[red]{NO_INSTALLATION_FOUND}[/]");

            return;
        }

        if (showHeader)
            console.MarkupLine(DETECTED_INSTALLATIONS);

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(NUMBER_HEADER)
            .AddColumn(NAME_HEADER)
            .AddColumn(VERSION_HEADER);

        for (var i = 0; i < installations.Count; i++)
        {
            var displayName = installations[i].DisplayName ?? string.Empty;
            var version = installations[i].InstallationVersion ?? string.Empty;
            var preview = installations[i].ChannelId?.Contains("preview", StringComparison.CurrentCultureIgnoreCase) == true ? PREVIEW_LABEL : string.Empty;
            table.AddRow((i + 1).ToString(), displayName, version + preview);
        }

        console.Write(table);
    }
}
