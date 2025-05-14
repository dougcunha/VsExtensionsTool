namespace VsExtensionsTool.Commands;

/// <summary>
/// Command to remove an extension by Id or by selection for the selected Visual Studio instance.
/// </summary>
public sealed class RemoveCommand : ICommand
{
    /// <inheritdoc />
    public string Name
        => "/remove";

    /// <inheritdoc />
    public string Description
        => "Remove an extension by its id.";

    /// <inheritdoc />
    public bool NeedsVsInstance
        => true;

    /// <inheritdoc />
    public bool CanExecute(CommandContext context)
        => context.Args.Length > 0
            && context.Args[0] == "/remove";

    /// <inheritdoc />
    public async Task ExecuteAsync(CommandContext context)
    {
        if (ICommand.ShowHelp(context.Args))
        {
            PrintHelp();

            return;
        }

        var args = context.Args;
        (string id, bool direct, string filter) = ParseArgs(args);

        if (direct && !string.IsNullOrWhiteSpace(id))
        {
            context.ExtensionManager.RemoveExtensionById(context.VisualStudioInstance!.InstallationPath!, id, context.VisualStudioInstance!.InstanceId!);

            return;
        }

        var extensions = context.ExtensionManager.GetExtensions(context.VisualStudioInstance!.InstallationPath!, filter);

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
                        context.ExtensionManager.RemoveExtensionById(context.VisualStudioInstance!.InstallationPath!, ext.Id, context.VisualStudioInstance!.InstanceId!);
                    }
                }
            );
    }

    private static (string id, bool direct, string filter) ParseArgs(string[] args)
    {
        var id = string.Empty;
        var direct = false;
        var filter = string.Empty;

        if (args.Length <= 1)
            return (id, direct, filter);

        if (args[1] == "/id" && args.Length > 2)
        {
            id = args[2];
            direct = true;
        }
        else
        {
            filter = args[1];
        }

        return (id, direct, filter);
    }

    /// <inheritdoc />
    public void PrintHelp()
        => AnsiConsole.WriteLine(Markup.Escape($"{Name} [<filter>] [/id <id>]   {Description}"));
}
