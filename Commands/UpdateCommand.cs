using System.CommandLine;
using VsExtensionsTool.Helpers;
using VsExtensionsTool.Managers;
using VsExtensionsTool.Models;

namespace VsExtensionsTool.Commands;

/// <summary>
/// Command to update outdated Visual Studio extensions.
/// </summary>
public sealed class UpdateCommand : Command
{
    private readonly Func<Task<VisualStudioInstance?>> _vsInstanceFactory;

    public UpdateCommand(Func<Task<VisualStudioInstance?>> vsInstanceFactory)
        : base("upd", "List all outdated extensions and update the selected one.")
    {
        _vsInstanceFactory = vsInstanceFactory;

        this.SetHandler(HandleAsync);
    }

    private async Task HandleAsync()
    {
        var vsInstance = await _vsInstanceFactory().ConfigureAwait(false);

        if (vsInstance is null)
        {
            AnsiConsole.MarkupLine("[red]No Visual Studio instance selected.[/]");

            return;
        }

        var extensions = ExtensionManager.GetExtensions(vsInstance);
        AnsiConsole.MarkupLine("[bold]Checking for outdated extensions...[/]");
        await ExtensionListDisplayHelper.PopulateExtensionsInfoFromMarketplaceAsync(extensions, vsInstance).ConfigureAwait(false);

        var outdated = extensions.Where(static ext => ext.IsOutdated).ToList(); 

        if (outdated.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]All extensions are up to date![/]");
            return;
        }

        var selected = await AnsiConsole.PromptAsync
        (
            new MultiSelectionPrompt<(string Name, string Id)>()
                .Title("Select the extensions to [green]update[/]:")
                .NotRequired()
                .PageSize(20)
                .MoreChoicesText("[grey](Move up and down to reveal more extensions)[/]")
                .InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to accept)[/]")
                .AddChoiceGroup(("Select all", ""), [.. outdated.Select(static e => ($"{e.Name} ({e.InstalledVersion} » {e.LatestVersion})", e.Id))])
                .UseConverter(static e => e.Item1)
        ).ConfigureAwait(false);

        if (selected.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No extensions selected for update.[/]");

            return;
        }

        await AnsiConsole
            .Status()
            .StartAsync
            (
                "Updating selected extensions...",
                async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots2);
                    ctx.SpinnerStyle(Style.Parse("green"));

                    foreach (var ext in selected)
                    {
                        var info = outdated.First(e => e.Id == ext.Item2);
                        AnsiConsole.MarkupLine($"[yellow]Updating extension:[/] {ext.Item1}");
                        await ApplyUpdateAsync(vsInstance, info).ConfigureAwait(false);
                    }
                }
            );
    }

    private static async Task ApplyUpdateAsync(VisualStudioInstance vsInstance, ExtensionInfo selectedExt)
    {
        var output = await ExtensionManager
            .UpdateExtensionAsync(selectedExt, vsInstance)
            .ConfigureAwait(false);

        AnsiConsole.WriteLine(output);
        AnsiConsole.MarkupLine($"[green]Extension '{selectedExt.Name}' updated![/]");
    }
}
