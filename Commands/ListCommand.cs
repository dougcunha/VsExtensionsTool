using System.CommandLine;
using VsExtensionsTool.Helpers;
using VsExtensionsTool.Managers;
using VsExtensionsTool.Models;

namespace VsExtensionsTool.Commands;

/// <summary>
/// Command to list all extensions for the selected Visual Studio instance.
/// </summary>
public sealed class ListCommand : Command
{   
    private readonly Func<Task<VisualStudioInstance?>> _vsInstanceFactory;

    /// <summary>
    /// Initializes the list command.
    /// </summary>
    public ListCommand(Func<Task<VisualStudioInstance?>> vsInstanceFactory) 
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
        _vsInstanceFactory = vsInstanceFactory;

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
        var vsInstance = await _vsInstanceFactory().ConfigureAwait(false);
        
        if (vsInstance is null)
        {
            AnsiConsole.MarkupLine("[red]No Visual Studio instance selected.[/]");

            return;
        }

        var extensions = ExtensionManager.GetExtensions(vsInstance, filter);

        if (version || outdated)
            await ExtensionListDisplayHelper.PopulateExtensionsInfoFromMarketplaceAsync(extensions, vsInstance).ConfigureAwait(false);

        if (extensions.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No extensions found.[/]");

            return;
        }

        ExtensionListDisplayHelper.DisplayExtensions
        (
            outdated
                ? [.. extensions.Where(static ext => ext.IsOutdated)]
                : extensions
        );
    }
}
