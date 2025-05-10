using System.Diagnostics;
using VsExtensionsTool.Commands;

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
        };

        commands.Add(new HelpCommand(commands));

        return commands;
    }

    /// <summary>
    /// Selects the Visual Studio instance, if necessary.
    /// </summary>
    private VisualStudioInstance? SelectVisualStudioInstance(string[] args)
    {
        var needsVsInstance = args.Length > 0 && (args[0] == "/list" || args[0] == "/remove");

        if (!needsVsInstance)
            return null;

        var installations = _vsManager.GetVisualStudioInstallations();

        if (installations.Count == 0)
        {
            VisualStudioDisplayHelper.PrintInstallationsTable(installations);

            return null;
        }

        if (installations.Count == 1)
            return installations[0];

        VisualStudioDisplayHelper.PrintInstallationsTable(installations, false);
        Console.Write("Enter the number of the desired installation: ");

        if (int.TryParse(Console.ReadLine(), out var choice) && choice > 0 && choice <= installations.Count)
            return installations[choice - 1];

        Console.WriteLine("Invalid option.");

        return null;
    }

    public async Task DispatchAsync(string[] args)
    {
        try
        {
            args = [.. args.Select(a => _aliases.GetValueOrDefault(a, a))];

            var selectedInstance = SelectVisualStudioInstance(args);

            if (selectedInstance != null)
                _extManager.SetInstanceInfo(selectedInstance.InstanceId!, selectedInstance.InstallationVersion!);

            var context = new CommandContext(args, _vsManager, _extManager, selectedInstance);
            var commands = CreateCommands().Where(command => command.CanExecute(context)).ToList();

            if (commands.Count == 0)
            {
                Console.WriteLine("No command found. Use /help to see the options.");

                return;
            }

            foreach (var command in commands)
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
