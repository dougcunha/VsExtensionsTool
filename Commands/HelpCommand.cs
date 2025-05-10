namespace VsExtensionsTool.Commands;

/// <summary>
/// Command to print help for all registered commands.
/// </summary>
public sealed class HelpCommand : ICommand
{
    private readonly List<ICommand> _commands;

    /// <inheritdoc />
    public string Name
        => "/help";

    /// <inheritdoc />
    public string Description
        => "Show this help menu.";

    /// <summary>
    /// Initializes a new instance of the <see cref="HelpCommand"/> class.
    /// </summary>
    /// <param name="commands">The list of commands to show help for.</param>
    public HelpCommand(List<ICommand> commands)
        => _commands = commands;

    /// <inheritdoc />
    public bool CanExecute(CommandContext context)
        => context.Args.Length == 0 || context.Args[0] == "/help";

    /// <inheritdoc />
    public Task ExecuteAsync(CommandContext context)
    {
        Console.WriteLine("VsExtensionsTool - Visual Studio Extensions Manager\n");
        Console.WriteLine("Available commands:");

        foreach (var cmd in _commands)
        {
            cmd.PrintHelp();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void PrintHelp()
        => Console.WriteLine($"{Name}   {Description}");
}
