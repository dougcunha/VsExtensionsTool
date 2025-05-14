namespace VsExtensionsTool.Commands;

/// <summary>
/// Command to list all extensions for the selected Visual Studio instance.
/// </summary>
public sealed class ListCommand : ICommand
{
    /// <inheritdoc />
    public string Name
        => "/list";

    /// <inheritdoc />
    public string Description
        => "List all extensions for the selected instance, optionally filtered by name or id. Use /version to show the latest version from the marketplace. Use /outdated to show only outdated extensions.";

    /// <inheritdoc />
    public bool NeedsVsInstance
        => true;

    /// <inheritdoc />
    public bool CanExecute(CommandContext context)
        => context.Args.Length > 0 && context.Args[0] == "/list";

    private static List<ExtensionInfo> GetExtensions(CommandContext context)
    {
        var filter = context.Args
            .Skip(1)
            .FirstOrDefault(static arg => arg != "/version" && arg != "/outdated");

        return context.ExtensionManager.GetExtensions(context.VisualStudioInstance!.InstallationPath!, filter);
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(CommandContext context)
    {
        if (ICommand.ShowHelp(context.Args))
        {
            PrintHelp();

            return;
        }

        var showMarketplaceVersion = context.Args.Any(static arg => arg == "/version");
        var showOnlyOutdated = context.Args.Any(static arg => arg == "/outdated");
        var extensions = GetExtensions(context);

        if (showMarketplaceVersion || showOnlyOutdated)
            await ExtensionListDisplayHelper.PopulateExtensionsInfoFromMarketplaceAsync(extensions, context.ExtensionManager).ConfigureAwait(false);

        if (extensions.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No extensions found.[/]");

            return;
        }

        ExtensionListDisplayHelper.DisplayExtensions
        (
            showOnlyOutdated
                ? [.. extensions.Where(static ext => ext.IsOutdated)]
                : extensions
        );
    }

    /// <inheritdoc />
    public void PrintHelp()
        => AnsiConsole.MarkupLine($"{Name} [[<filter>]] [[/version]] [[/outdated]]   {Description}");
}
