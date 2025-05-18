using System.CommandLine;
using VsExtensionsTool.Helpers;
using VsExtensionsTool.Managers;

namespace VsExtensionsTool.Commands;

/// <summary>
/// Command to list all extensions for the selected Visual Studio instance.
/// </summary>
public sealed class ListCommand : Command
{
    private readonly IExtensionListDisplayHelper _displayHelper;
    private readonly IVisualStudioManager _vsManager;
    private readonly IExtensionManager _extensionManager;
    private readonly IAnsiConsole _console;

    /// <summary>
    /// Initializes the list command.
    /// </summary>
    public ListCommand
    (
        IExtensionListDisplayHelper displayHelper,
        IVisualStudioManager vsManager,
        IExtensionManager extensionManager,
        IAnsiConsole console
    )
        : base("list", "Lists all extensions for the selected Visual Studio instance.")
    {
        var filterOption = new Option<string?>
        (
            aliases: ["--filter", "-f", "/filter"],
            description: "Filter by extension name or id."
        );

        var versionOption = new Option<bool>
        (
            aliases: ["--version", "-v", "/version"],
            description: "Show latest marketplace version."
        );

        var outdatedOption = new Option<bool>
        (
            aliases: ["--outdated", "-o", "/outdated"],
            description: "Show only outdated extensions."
        );

        AddOption(filterOption);
        AddOption(versionOption);
        AddOption(outdatedOption);
        _displayHelper = displayHelper;
        _vsManager = vsManager;
        _extensionManager = extensionManager;
        _console = console;

        this.SetHandler
        (
            HandleAsync,
            filterOption,
            versionOption,
            outdatedOption
        );
    }

    private async Task HandleAsync(string? filter, bool version, bool outdated)
    {
        var vsInstance = await _vsManager.SelectVisualStudioInstanceAsync().ConfigureAwait(false);

        if (vsInstance is null)
        {
            _console.MarkupLine("[red]No Visual Studio instance selected.[/]");

            return;
        }

        var extensions = _extensionManager.GetExtensions(vsInstance, filter);

        if (version || outdated)
            await _displayHelper.PopulateExtensionsInfoFromMarketplaceAsync(extensions, vsInstance).ConfigureAwait(false);

        if (extensions.Count == 0)
        {
            _console.MarkupLine("[red]No extensions found.[/]");

            return;
        }

        _displayHelper.DisplayExtensions
        (
            outdated
                ? [.. extensions.Where(static ext => ext.IsOutdated)]
                : extensions            
        );
    }
}
