namespace VsExtensionsTool;

/// <summary>
/// Responsible for resolving and dispatching commands based on command-line arguments.
/// </summary>
public sealed class CommandDispatcher
{
    private readonly VisualStudioManager _vsManager;
    private readonly ExtensionManager _extManager;
    private readonly Dictionary<string, string> _aliases;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandDispatcher"/> class.
    /// </summary>
    public CommandDispatcher(VisualStudioManager vsManager, ExtensionManager extManager)
    {
        _vsManager = vsManager;
        _extManager = extManager;

        _aliases = new Dictionary<string, string>
        {
            { "--help", "/help" },
            { "--list", "/list" },
            { "--list_vs", "/list_vs" },
            { "--remove", "/remove" },
            { "--version", "/version" },
            { "--outdated", "/outdated" },
            { "--update", "/update" },
            { "--id", "/id" }
        };
    }

    /// <summary>
    /// Create all available commands.
    /// </summary>
    private static List<ICommand> CreateCommands()
    {
        var commands = new List<ICommand>
        {
            new ListVsCommand(),
            new ListCommand(),
            new RemoveCommand(),
            new UpdateCommand(),
        };

        commands.Add(new HelpCommand(commands));

        return commands;
    }

    /// <summary>
    /// Selects the Visual Studio instance, if necessary.
    /// </summary>
    private async Task<VisualStudioInstance?> SelectVisualStudioInstanceAsync()
    {
        var installations = await _vsManager.GetVisualStudioInstallationsAsync().ConfigureAwait(false);

        if (installations.Count == 0)
        {
            VisualStudioDisplayHelper.PrintInstallationsTable(installations);

            return null;
        }

        if (installations.Count == 1)
            return installations[0];

        VisualStudioDisplayHelper.PrintInstallationsTable(installations, false);
        var choice = AnsiConsole.Ask<int>("Enter the number of the desired installation (0 to cancel): ");

        if (choice > 0 && choice <= installations.Count)
            return installations[choice - 1];

        AnsiConsole.WriteLine("Invalid option.");
        return null;
    }

    public async Task DispatchAsync(string[] args)
    {
        try
        {
            args = [.. args.Select(a => _aliases.GetValueOrDefault(a, a))];
            var context = new CommandContext(args, _vsManager, _extManager, null);
            var commands = CreateCommands();
            var needsVsInstance = commands.Any(c => c.CanExecute(context) && c.NeedsVsInstance);

            if (needsVsInstance)
            {
                var selectedInstance = await SelectVisualStudioInstanceAsync().ConfigureAwait(false);

                if (selectedInstance == null)
                {
                    AnsiConsole.WriteLine("No Visual Studio instance selected. Exiting.");

                    return;
                }

                context.VisualStudioInstance = selectedInstance;
                _extManager.SetInstanceInfo(selectedInstance.InstanceId!, selectedInstance.InstallationVersion!);
            }

            var commandsToRun = commands.Where(command => command.CanExecute(context)).ToList();

            if (commandsToRun.Count == 0)
            {
                AnsiConsole.WriteLine("No command found. Use /help to see the options.");

                return;
            }

            foreach (var command in commandsToRun)
                await command.ExecuteAsync(context).ConfigureAwait(false);
        }
        finally
        {
            if (Debugger.IsAttached)
            {
                Console.WriteLine("Press any key to exit...");
                Console.Read();
            }
        }
    }
}
