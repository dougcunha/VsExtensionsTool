namespace VsExtensionsTool.Commands;

/// <summary>
/// Command to update outdated Visual Studio extensions.
/// </summary>
public sealed class UpdateCommand : ICommand
{
    public string Name
        => "/update";

    public string Description
        => "List all outdated extensions and update the selected one.";

    public bool NeedsVsInstance
        => true;

    public bool CanExecute(CommandContext context)
        => context.Args.Length > 0 && context.Args[0] == "/update";

    public async Task ExecuteAsync(CommandContext context)
    {
        if (ICommand.ShowHelp(context.Args))
        {
            PrintHelp();

            return;
        }

        var extensions = context.ExtensionManager.GetExtensions(context.VisualStudioInstance!.InstallationPath!);
        AnsiConsole.MarkupLine("[bold]Checking for outdated extensions...[/]");
        await ExtensionListDisplayHelper.PopulateExtensionsInfoFromMarketplaceAsync(extensions, context.ExtensionManager).ConfigureAwait(false);

        List<ExtensionInfo> outdated = [.. extensions.Where(static ext => ext.IsOutdated)];

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
                .AddChoices([.. outdated.Select(static e => ($"{e.Name} ({e.InstalledVersion} » {e.LatestVersion})", e.Id))])
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
                        var info = outdated.First(e => e.Id == ext.Id);
                        AnsiConsole.MarkupLine($"[yellow]Updating extension:[/] {ext.Item1}");
                        await ApplyUpdateAsync(context, info).ConfigureAwait(false);
                    }
                }
            );
    }

    private static async Task ApplyUpdateAsync(CommandContext context, ExtensionInfo selectedExt)
    {
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots2)
            .SpinnerStyle(Style.Parse("green"))
            .StartAsync("[blue]Running VSIXInstaller...[/]", async _ =>
            {
                var output = await ExtensionManager.UpdateExtensionAsync(selectedExt, context.VisualStudioInstance!)
                    .ConfigureAwait(false);

                AnsiConsole.WriteLine(output);
                AnsiConsole.MarkupLine($"[green]Extension '{selectedExt.Name}' updated![/]");
            }).ConfigureAwait(false);
    }

    public void PrintHelp()
        => AnsiConsole.MarkupLine($"{Name}   {Description}");
}
