namespace VsExtensionsTool.Commands;

/// <summary>
/// Command to print help for all registered commands.
/// </summary>
public sealed class HelpCommand : ICommand
{
    private readonly List<ICommand> _commands;
    private const string TITLE = "VsExtensionsTool";
    private const string SUBTITLE = "Visual Studio Extensions Manager";
    private const string COMMAND_HEADER = "Command";
    private const string DESCRIPTION_HEADER = "Description";

    /// <inheritdoc />
    public string Name
        => "/help";

    /// <inheritdoc />
    public string Description
        => "Show help for all commands.";

    /// <inheritdoc />
    public bool NeedsVsInstance
        => false;

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
        AnsiConsole.Clear();

        AnsiConsole.Write
        (
            new FigletText(TITLE)
                .Centered()
                .Color(Color.Green1)
        );

        AnsiConsole.MarkupLine($"[bold blue]{SUBTITLE}[/]");
        AnsiConsole.WriteLine();

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn(new TableColumn(COMMAND_HEADER).Centered());
        table.AddColumn(DESCRIPTION_HEADER);

        foreach (var cmd in _commands)
            table.AddRow($"[yellow]{Markup.Escape(cmd.Name)}[/]", cmd.Description);

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[dim]Use the specific command or /help for more details.[/]");

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void PrintHelp()
        => AnsiConsole.MarkupLine($"{Name}   {Description}");
}
