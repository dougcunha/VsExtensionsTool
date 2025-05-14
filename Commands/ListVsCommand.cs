namespace VsExtensionsTool.Commands;

/// <summary>
/// Command to list all Visual Studio installations.
/// </summary>
public sealed class ListVsCommand : ICommand
{
    /// <inheritdoc />
    public string Name
        => "/list_vs";

    /// <inheritdoc />
    public string Description
        => "List all Visual Studio installations.";

    /// <inheritdoc />
    public bool NeedsVsInstance
        => false;

    /// <inheritdoc />
    public bool CanExecute(CommandContext context)
        => context.Args.Length > 0 && context.Args[0] == "/list_vs";

    /// <inheritdoc />
    public async Task ExecuteAsync(CommandContext context)
    {
        if (ICommand.ShowHelp(context.Args))
        {
            PrintHelp();

            return;
        }

        var installations = await VisualStudioManager.GetVisualStudioInstallationsAsync().ConfigureAwait(false);
        VisualStudioDisplayHelper.PrintInstallationsTable(installations);
    }

    /// <inheritdoc />
    public void PrintHelp()
        => AnsiConsole.MarkupLine($"{Name}   {Description}");
}
