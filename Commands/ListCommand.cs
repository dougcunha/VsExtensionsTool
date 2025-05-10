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
        => "List all extensions for the selected instance, optionally filtered by name or id. Use /version to show the latest version from the marketplace.";

    /// <inheritdoc />
    public bool CanExecute(CommandContext context)
        => context.Args.Length > 0
            && context.Args[0] == "/list"
            && context.VisualStudioInstance != null;

    private void PopulateExtensionInfos(CommandContext context)
    {
        var filter = context.Args.Skip(1).FirstOrDefault(static arg => arg != "/version");
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

        if (showMarketplaceVersion)
            table.AddColumn(MARKETPLACE_HEADER);

        var index = 1;

        await AnsiConsole.Live(table)
            .StartAsync
            (
                async ctx =>
                {
                    foreach (var ext in _extensions)
                    {
                        await AddToTableAsync(table, showMarketplaceVersion, index, ext, context).ConfigureAwait(false);
                        ctx.Refresh();
                        index++;
                    }
                }
            ).ConfigureAwait(false);
    }

    private static async Task AddToTableAsync(Table table, bool showMarketplaceVersion, int index, ExtensionInfo ext, CommandContext context)
    {
        var name = Markup.Escape(ext.Name);
        var publisher = Markup.Escape(ext.Publisher);
        var version = Markup.Escape(ext.Version);

        if (!showMarketplaceVersion)
        {
            table.AddRow(index.ToString(), name, publisher, version);

            return;
        }

        var lastestVersion = Markup.Escape(await MarketplaceHelper.GetLatestExtensionVersionAsync(ext, context.VisualStudioInstance!).ConfigureAwait(false));
        var isUpdateAvailable = !string.Equals(ext.Version, lastestVersion, StringComparison.OrdinalIgnoreCase) && lastestVersion != "Not found";

        if (isUpdateAvailable)
        {
            table.AddRow(
                $"[yellow]{index}[/]",
                $"[yellow]{name}[/]",
                $"[yellow]{publisher}[/]",
                $"[yellow]{version}[/]",
                $"[yellow]{lastestVersion}[/]");
        }
        else
        {
            table.AddRow(index.ToString(), name, publisher, version, lastestVersion);
        }
    }

    /// <inheritdoc />
    public void PrintHelp()
        => Console.WriteLine($"{Name} [filter] [/version]   {Description}");
}
