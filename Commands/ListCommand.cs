namespace VsExtensionsTool.Commands;

/// <summary>
/// Command to list all extensions for the selected Visual Studio instance.
/// </summary>
public sealed class ListCommand : ICommand
{
    private List<ExtensionInfo>? _extensions;

    /// <summary>
    /// List of extensions for the selected Visual Studio instance.
    /// </summary>
    public List<ExtensionInfo> Extensions
        => _extensions ?? throw new InvalidOperationException("Extensions have not been initialized.");

    /// <inheritdoc />
    public string Name
        => "/list";

    /// <inheritdoc />
    public string Description
        => "List all extensions for the selected instance, optionally filtered by name or id. Use /version to show the latest version from the marketplace. Use /outdated to show only outdated extensions.";

    /// <inheritdoc />
    public bool CanExecute(CommandContext context)
        => context.Args.Length > 0
            && context.Args[0] == "/list"
            && context.VisualStudioInstance != null;

    private void PopulateExtensionInfos(CommandContext context)
    {
        var filter = context.Args.Skip(1).FirstOrDefault(static arg => arg != "/version" && arg != "/outdated");
        _extensions = context.ExtensionManager.GetExtensions(context.VisualStudioInstance!.InstallationPath!);

        if (string.IsNullOrWhiteSpace(filter))
            return;

        _extensions =
        [
            .. _extensions.Where
            (
                e => e.Name.Contains
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

    /// <inheritdoc />
    public async Task ExecuteAsync(CommandContext context)
    {
        const string NO_EXTENSIONS_FOUND = "No extensions found.";
        const string NAME_HEADER = "Name";
        const string PUBLISHER_HEADER = "Publisher";
        const string INSTALLED_HEADER = "Installed";
        const string MARKETPLACE_HEADER = "Marketplace";
        const string NUMBER_HEADER = "#";
        var showMarketplaceVersion = context.Args.Any(static arg => arg == "/version");
        var showOnlyOutdated = context.Args.Any(static arg => arg == "/outdated");

        PopulateExtensionInfos(context);

        if (_extensions is null || _extensions.Count == 0)
        {
            AnsiConsole.MarkupLine($"[red]{NO_EXTENSIONS_FOUND}[/]");
            return;
        }

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn(NUMBER_HEADER);
        table.AddColumn(NAME_HEADER);
        table.AddColumn(PUBLISHER_HEADER);
        table.AddColumn(INSTALLED_HEADER);
        if (showMarketplaceVersion || showOnlyOutdated)
            table.AddColumn(MARKETPLACE_HEADER);

        var index = 1;

        await AnsiConsole.Live(table)
            .StartAsync(
                async ctx =>
                {
                    foreach (var ext in _extensions)
                    {
                        await AddToTableAsync(table, showMarketplaceVersion, showOnlyOutdated, index, ext, context).ConfigureAwait(false);
                        ctx.Refresh();
                        index++;
                    }
                }
            ).ConfigureAwait(false);
    }

    private static async Task AddToTableAsync(Table table, bool showMarketplaceVersion, bool showOnlyOutdated, int index, ExtensionInfo ext, CommandContext context)
    {
        var name = Markup.Escape(ext.Name);
        var publisher = Markup.Escape(ext.Publisher);
        var version = Markup.Escape(ext.Version);

        if (!showMarketplaceVersion && !showOnlyOutdated)
        {
            table.AddRow(index.ToString(), name, publisher, version);
            return;
        }

        var latestVersion = Markup.Escape(await MarketplaceHelper.GetLatestExtensionVersionAsync(ext, context.VisualStudioInstance!).ConfigureAwait(false));
        var isOutdated = !string.Equals(ext.Version, latestVersion, StringComparison.OrdinalIgnoreCase) && latestVersion != "Not found";

        if (showOnlyOutdated && !isOutdated)
        {
            return;
        }

        if (isOutdated)
        {
            table.AddRow(
                $"[yellow]{index}[/]",
                $"[yellow]{name}[/]",
                $"[yellow]{publisher}[/]",
                $"[yellow]{version}[/]",
                $"[yellow]{latestVersion}[/]");
        }
        else if (showMarketplaceVersion)
        {
            table.AddRow(index.ToString(), name, publisher, version, latestVersion);
        }
        else
        {
            table.AddRow(index.ToString(), name, publisher, version);
        }
    }

    /// <inheritdoc />
    public void PrintHelp()
        => Console.WriteLine($"{Name} [filter] [/version] [/outdated]   {Description}");
}
