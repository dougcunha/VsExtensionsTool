namespace VsExtensionsTool.Commands;

/// <summary>
/// Command to update outdated Visual Studio extensions.
/// </summary>
public sealed class UpdateCommand : Command
{
    private readonly IVisualStudioManager _vsManager;
    private readonly IExtensionListDisplayHelper _extensionListDisplayHelper;
    private readonly IExtensionManager _extensionManager;
    private readonly IAnsiConsole _console;

    public UpdateCommand
    (
        IVisualStudioManager vsManager,
        IExtensionListDisplayHelper extensionListDisplayHelper,
        IExtensionManager extensionManager,
        IAnsiConsole console
    )
        : base("upd", "List all outdated extensions and update the selected one.")
    {
        _vsManager = vsManager;
        _extensionListDisplayHelper = extensionListDisplayHelper;
        _extensionManager = extensionManager;
        _console = console;

        this.SetHandler(HandleAsync);
    }

    private async Task HandleAsync()
    {
        var vsInstance = await _vsManager.SelectVisualStudioInstanceAsync().ConfigureAwait(false);

        if (vsInstance is null)
        {
            _console.MarkupLine("[red]No Visual Studio instance selected.[/]");

            return;
        }

        var extensions = _extensionManager.GetExtensions(vsInstance);
        _console.MarkupLine("[bold]Checking for outdated extensions...[/]");
        await _extensionListDisplayHelper.PopulateExtensionsInfoFromMarketplaceAsync(extensions, vsInstance).ConfigureAwait(false);

        var outdated = extensions.Where(static ext => ext.IsOutdated).ToList();

        if (outdated.Count == 0)
        {
            _console.MarkupLine("[green]All extensions are up to date![/]");

            return;
        }

        var selected = await _console.PromptAsync
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
            _console.MarkupLine("[yellow]No extensions selected for update.[/]");

            return;
        }

        await _console.Status().StartAsync
        (
            "Updating selected extensions...",
            async ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots2);
                ctx.SpinnerStyle(Style.Parse("green"));

                foreach (var ext in selected)
                {
                    var info = outdated.First(e => e.Id == ext.Item2);
                    _console.MarkupLine($"[yellow]Updating extension:[/] {ext.Item1}");
                    await ApplyUpdateAsync(vsInstance, info).ConfigureAwait(false);
                }
            }
        ).ConfigureAwait(false);
    }

    private async Task ApplyUpdateAsync
    (
        VisualStudioInstance vsInstance,
        ExtensionInfo selectedExt
    )
    {
        var output = await _extensionManager
            .UpdateExtensionAsync(selectedExt, vsInstance)
            .ConfigureAwait(false);

        _console.WriteLine(output);
        _console.MarkupLine($"[green]Extension '{selectedExt.Name}' updated![/]");
    }
}
