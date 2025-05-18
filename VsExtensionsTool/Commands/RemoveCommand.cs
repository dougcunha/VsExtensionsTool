using System.CommandLine;
using VsExtensionsTool.Helpers;
using VsExtensionsTool.Managers;
using VsExtensionsTool.Models;

namespace VsExtensionsTool.Commands;

/// <summary>
/// Command to remove Visual Studio extensions.
/// </summary>
public sealed class RemoveCommand : Command
{
    private readonly IVisualStudioManager _vsManager;
    private readonly IExtensionManager _extensionManager;
    private readonly IAnsiConsole _console;

    /// <inheritdoc />
    public RemoveCommand
    (
        IVisualStudioManager vsManager,
        IExtensionManager extensionManager,
        IAnsiConsole console
    ) 
        : base("rm", "Remove an extension by its id.")
    {
        _vsManager = vsManager;
        _extensionManager = extensionManager;
        _console = console;

        var idOption = new Option<string>
        (
            aliases: ["--id", "-i", "/id"],
            description: "The id of the extension to remove."
        );

        var fileterOption = new Option<string>
        (
            aliases: ["--filter", "-f", "/filter"],
            description: "Filter by extension name or id."
        );

        AddOption(idOption);
        AddOption(fileterOption);

        this.SetHandler
        (
            HandleAsync,
            idOption,
            fileterOption
        );
    }

    
    private async Task HandleAsync(string? id, string? filter)
    {        
        var vsInstance = await _vsManager.SelectVisualStudioInstanceAsync().ConfigureAwait(false);

        if (vsInstance is null)
        {
            _console.MarkupLine("[red]No Visual Studio instance selected.[/]");

            return;
        }

        if (!string.IsNullOrWhiteSpace(id))
        {
            await _extensionManager.RemoveExtensionByIdAsync(vsInstance, id).ConfigureAwait(false);

            return;
        }

        var extensions = _extensionManager.GetExtensions(vsInstance, filter);

        if (extensions.Count == 0)
        {
            _console.MarkupLine("[red]No extensions found.[/]");

            return;
        }

        var selected = await _console.PromptAsync
        (
            new MultiSelectionPrompt<(string Name, string Id)>()
                .Title("Select the extensions to [red]remove[/]:")
                .NotRequired()
                .PageSize(20)
                .MoreChoicesText("[grey](Move up and down to reveal more extensions)[/]")
                .InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to accept)[/]")
                .AddChoiceGroup(("Select all", ""), [.. extensions.Select(static e => ($"{e.Name} ({e.InstalledVersion})", e.Id))])
                .UseConverter(static e => e.Item1)
        ).ConfigureAwait(false);

        if (selected.Count == 0)
        {
            _console.MarkupLine("[yellow]No extensions selected for removal.[/]");

            return;
        }

        await _console.Status().StartAsync
        (
            "Removing selected extensions...",
            async ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots2);
                ctx.SpinnerStyle(Style.Parse("red"));

                foreach (var ext in selected)
                {
                    _console.MarkupLine($"[yellow]Removing extension:[/] {ext.Item1}");
                    await _extensionManager.RemoveExtensionByIdAsync(vsInstance, ext.Item2).ConfigureAwait(false);
                }
            }
        ).ConfigureAwait(false);
    }
}
