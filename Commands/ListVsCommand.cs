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
    public bool CanExecute(CommandContext context)
        => context.Args.Length > 0 && context.Args[0] == "/list_vs";

    /// <inheritdoc />
    public Task ExecuteAsync(CommandContext context)
    {
        var installations = context.VisualStudioManager.GetVisualStudioInstallations();
        VisualStudioDisplayHelper.PrintInstallationsTable(installations);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void PrintHelp()
        => Console.WriteLine($"{Name}   {Description}");
}
