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

        var outdated = await ExtensionListDisplayHelper.DisplayExtensionsAsync(
            extensions,
            context,
            showMarketplaceVersion: true,
            showOnlyOutdated: true
        ).ConfigureAwait(false);

        if (outdated.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]All extensions are up to date![/]");

            return;
        }

        var selectedExt = SelectExtension(outdated);

        if (selectedExt is null)
            return;

        await ApplyUpdateAsync(context, selectedExt).ConfigureAwait(false);
    }

    private static ExtensionInfo? SelectExtension(List<ExtensionInfo> outdated)
    {
        if (outdated.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]All extensions are up to date![/]");

            return null;
        }

        var choice = AnsiConsole.Ask<int>("Enter the number of the extension to update (0 to cancel):");

        if (choice > 0 && choice <= outdated.Count)
            return outdated[choice - 1];

        AnsiConsole.MarkupLine("[grey]Update cancelled.[/]");

        return null;
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
