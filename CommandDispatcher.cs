namespace VsExtensionsTool;

/// <summary>
/// Responsible for resolving and dispatching commands based on command-line arguments.
/// </summary>
public sealed class CommandDispatcher
{
    private readonly VisualStudioManager _vsManager;
    private readonly ExtensionManager _extManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandDispatcher"/> class.
    /// </summary>
    public CommandDispatcher(VisualStudioManager vsManager, ExtensionManager extManager)
    {
        _vsManager = vsManager;
        _extManager = extManager;
    }

    /// <summary>
    /// Normalizes command-line arguments so that any parameter can be used with '/' or '--'.
    /// </summary>
    private static string NormalizeArg(string arg)
    {
        if (arg.StartsWith("--", StringComparison.Ordinal))
            return "/" + arg[2..];

        return arg;
    }

    /// <summary>
    /// Returns a list of arguments with both '/' and '--' variants for each parameter.
    /// </summary>
    private static string[] NormalizeArgs(string[] args)
        => [..args.Select(NormalizeArg)];

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
        var choice = await AnsiConsole.AskAsync<int>("Enter the number of the desired installation (0 to cancel): ").ConfigureAwait(false);

        if (choice > 0 && choice <= installations.Count)
            return installations[choice - 1];

        return null;
    }

    public async Task DispatchAsync(string[] args)
    {
        try
        {
            args = NormalizeArgs(args);
            var context = new CommandContext(args, _vsManager, _extManager, null);
            var commands = CreateCommands();
            var needsVsInstance = !ICommand.ShowHelp(args) && commands.Any(c => c.CanExecute(context) && c.NeedsVsInstance);

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
                AnsiConsole.WriteLine("Press any key to exit...");
                Console.Read();
            }
        }
    }
}
