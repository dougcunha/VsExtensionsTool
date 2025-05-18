using VsExtensionsTool.Managers;
using VsExtensionsTool.Models;

namespace VsExtensionsTool.Helpers;

/// <summary>
/// Responsible for displaying extension lists with support for progress and marketplace queries.
/// </summary>
public sealed class ExtensionListDisplayHelper(IExtensionManager extensionManager, IAnsiConsole console) : IExtensionListDisplayHelper
{

    /// <summary>
    /// Displays a list of extensions, with optional support for marketplace version queries and progress bar.
    /// </summary>
    /// <param name="extensions">List of installed extensions.</param>
    /// <param name="console">Console to use for output.</param>
    public void DisplayExtensions(List<ExtensionInfo> extensions)
    {
        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("#");
        table.AddColumn("Name");
        table.AddColumn("Publisher");
        table.AddColumn("Installed");

        var showMarketplaceVersion = extensions.Any(static ext => ext.LatestVersion != "Not found");

        if (showMarketplaceVersion)
            table.AddColumn("Marketplace");

        var index = 1;

        foreach (var ext in extensions)
        {
            string? marketplaceVersion = null;

            if (showMarketplaceVersion)
            {
                marketplaceVersion = ext.IsOutdated
                    ? $"[yellow]{Markup.Escape(ext.LatestVersion)}[/]"
                    : Markup.Escape(ext.LatestVersion);
            }

            var columns = new List<string?>
            {
                ext.IsOutdated
                    ? $"[yellow]{index}[/]"
                    : index.ToString(),
                ext.IsOutdated
                    ? $"[yellow]{Markup.Escape(ext.Name)}[/]"
                    : Markup.Escape(ext.Name),
                ext.IsOutdated
                    ? $"[yellow]{Markup.Escape(ext.Publisher)}[/]"
                    : Markup.Escape(ext.Publisher),
                ext.IsOutdated
                    ? $"[yellow]{Markup.Escape(ext.InstalledVersion)}[/]"
                    : Markup.Escape(ext.InstalledVersion),
                marketplaceVersion
            };

            table.AddRow
            (
                columns.Where(static c => c != null).ToArray()!
            );

            index++;
        }

        if (extensions.Count == 0)
            console.MarkupLine("[red]No extensions found.[/]");
        else
            console.Write(table);
    }

    /// <summary>
    /// Populates extension info from the marketplace, showing progress.
    /// </summary>
    /// <param name="extensions">Extensions to update.</param>
    /// <param name="instance">Visual Studio instance.</param>
    /// <param name="console">Console to use for output.</param>
    public async Task PopulateExtensionsInfoFromMarketplaceAsync
    (
        List<ExtensionInfo> extensions,
        VisualStudioInstance instance
    )
    {
        console.MarkupLine("[bold]Fetching extensions versions...[/]");
        var progress = console.Progress();

        await progress
            .Columns
            (
                new ProgressBarColumn
                {
                    CompletedStyle = new Style(foreground: Color.Green1, decoration: Decoration.Conceal | Decoration.Bold | Decoration.Invert),
                    RemainingStyle = new Style(decoration: Decoration.Conceal),
                    FinishedStyle = new Style(foreground: Color.Green1, decoration: Decoration.Conceal | Decoration.Bold | Decoration.Invert)
                },
                new PercentageColumn(),
                new SpinnerColumn(),
                new ElapsedTimeColumn(),
                new TaskDescriptionColumn()
            )
            .StartAsync(async progressCtx =>
            {
                var progressTask = progressCtx.AddTask("Checking Marketplace...", maxValue: extensions.Count);

                await extensionManager
                    .PopulateExtensionInfoFromMarketplaceAsync
                    (
                        instance,
                        extensions,
                        ext =>
                        {
                            progressTask.Description = $"Checking {ext.Name}...";
                            progressTask.Increment(1);
                        }).ConfigureAwait(false);

                progressTask.Description = "Done";
                progressTask.StopTask();
            }).ConfigureAwait(false);
    }
}
