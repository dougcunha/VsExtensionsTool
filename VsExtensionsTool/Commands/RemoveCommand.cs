using System.CommandLine;
using VsExtensionsTool.Managers;
using VsExtensionsTool.Models;

namespace VsExtensionsTool.Commands;

/// <summary>
/// Command to remove an extension by Id or by selection for the selected Visual Studio instance.
/// </summary>
public sealed class RemoveCommand : Command
{
    private readonly Func<Task<VisualStudioInstance?>> _vsInstanceFactory;

    /// <inheritdoc />
    public RemoveCommand(Func<Task<VisualStudioInstance?>> vsInstanceFactory) : base("rm", "Remove an extension by its id.")
    {
        _vsInstanceFactory = vsInstanceFactory;

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
        var vsInstance = await _vsInstanceFactory().ConfigureAwait(false);

        if (vsInstance is null)
        {
            AnsiConsole.MarkupLine("[red]No Visual Studio instance selected.[/]");

            return;
        }

        if (!string.IsNullOrWhiteSpace(id))
        {
            ExtensionManager.RemoveExtensionById(vsInstance, id);

            return;
        }

        var extensions = ExtensionManager.GetExtensions(vsInstance, filter);

        if (extensions.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No extensions found.[/]");

            return;
        }

        var selected = await AnsiConsole.PromptAsync
        (
            new MultiSelectionPrompt<(string Name, string Id)>()
                .Title("Select the extensions to [red]remove[/]:")
                .NotRequired()
                .PageSize(20)
                .MoreChoicesText("[grey](Move up and down to reveal more extensions)[/]")
                .InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to accept)[/]")
                .AddChoices([.. extensions.Select(static e => (e.Name, e.Id))])
                .UseConverter(static e => e.Name)
        ).ConfigureAwait(false);

        if (selected.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No extensions selected for removal.[/]");

            return;
        }

        AnsiConsole
            .Status()
            .Start
            (
                "Removing selected extensions...",
                ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots2);
                    ctx.SpinnerStyle(Style.Parse("green"));

                    foreach (var ext in selected)
                    {
                        AnsiConsole.MarkupLine($"[yellow]Removing extension:[/] {ext.Name}");
                        ExtensionManager.RemoveExtensionById(vsInstance, ext.Id);
                    }
                }
            );
    }
}
