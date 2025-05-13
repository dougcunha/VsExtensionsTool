namespace VsExtensionsTool;

/// <summary>
/// Responsible for displaying extension lists with support for progress and marketplace queries.
/// </summary>
public static class ExtensionListDisplayHelper
{
    /// <summary>
    /// Displays a list of extensions, with optional support for marketplace version queries and progress bar.
    /// </summary>
    /// <param name="extensions">List of installed extensions.</param>
    /// <param name="context">Command context.</param>
    /// <param name="showMarketplaceVersion">If true, queries and displays marketplace version.</param>
    /// <param name="showOnlyOutdated">If true, displays only outdated extensions.</param>
    /// <returns>List of displayed extensions (filtered and, if requested, enriched with marketplace data).</returns>
    public static async Task<List<ExtensionInfo>> DisplayExtensionsAsync
    (
        List<ExtensionInfo> extensions,
        CommandContext context,
        bool showMarketplaceVersion = false,
        bool showOnlyOutdated = false
    )
    {
        var displayed = new List<ExtensionInfo>();
        var total = extensions.Count;
        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("#");
        table.AddColumn("Name");
        table.AddColumn("Publisher");
        table.AddColumn("Installed");

        if (showMarketplaceVersion || showOnlyOutdated)
            table.AddColumn("Marketplace");

        var progress = AnsiConsole.Progress();

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
                var progressTask = progressCtx.AddTask(showMarketplaceVersion ? "Checking Marketplace..." : "Getting extensions...", maxValue: total);

                await ProcessExtensionsAsync(extensions, context, showMarketplaceVersion, showOnlyOutdated, displayed, table, progressTask).ConfigureAwait(false);

                progressTask.Description = "Done";
                progressTask.StopTask();
            }).ConfigureAwait(false);

        if (displayed.Count == 0)
            AnsiConsole.MarkupLine("[red]No extensions found.[/]");
        else
            AnsiConsole.Write(table);

        return displayed;
    }

    private static async Task ProcessExtensionsAsync
    (
        List<ExtensionInfo> extensions,
        CommandContext context,
        bool showMarketplaceVersion,
        bool showOnlyOutdated,
        List<ExtensionInfo> displayed,
        Table table,
        ProgressTask progressTask
    )
    {
        var index = 1;

        foreach (var ext in extensions)
        {
            progressTask.Description = Markup.Escape(ext.Name);

            if (showMarketplaceVersion || showOnlyOutdated)
                await MarketplaceHelper.PopulateExtensionInfoFromMarketplaceAsync(ext, context.VisualStudioInstance!).ConfigureAwait(false);

            if (showOnlyOutdated && !ext.IsOutdated)
            {
                progressTask.Increment(1);

                continue;
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
                showMarketplaceVersion
                    ? ext.IsOutdated
                        ? $"[yellow]{Markup.Escape(ext.LatestVersion)}[/]"
                        : Markup.Escape(ext.LatestVersion)
                    : null
            };

            table.AddRow
            (
                columns.Where(static c => c != null).ToArray()!
            );

            displayed.Add(ext);
            index++;
            progressTask.Increment(1);
        }
    }
}
