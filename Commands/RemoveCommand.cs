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
        => "Remove extension by Id or by selection.";

    /// <inheritdoc />
    public bool CanExecute(CommandContext context)
        => context.Args.Length > 0
            && context.Args[0] == "/remove"
            && context.VisualStudioInstance != null;

    /// <inheritdoc />
    public async Task ExecuteAsync(CommandContext context)
    {
        var args = context.Args;
        (string id, bool direct, string filter) = ParseArgs(args);

        if (!direct && string.IsNullOrWhiteSpace(id))
        {
            var listCmd = new ListCommand();
            var filteredContext = SetListFilteredContext(context, filter);
            await listCmd.ExecuteAsync(filteredContext).ConfigureAwait(false);

            Console.Write("\nEnter the number of the extension to remove: ");
            var input = Console.ReadLine();

            if (!int.TryParse(input, out var index))
            {
                Console.WriteLine("Invalid number.");

                return;
            }

            var extensions = listCmd.Extensions;

            if (index < 1 || index > extensions.Count)
            {
                Console.WriteLine("Invalid extension number.");

                return;
            }

            id = extensions[index - 1].Id;
            Console.WriteLine($"Removing extension \"{extensions[index - 1].Name}\"...");
        }

        if (!string.IsNullOrWhiteSpace(id))
            context.ExtensionManager.RemoveExtensionById(context.VisualStudioInstance!.InstallationPath!, id);
        else
            Console.WriteLine("Please provide the Id of the extension to remove.");
    }

    private static CommandContext SetListFilteredContext(CommandContext context, string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return context;

        var filteredArgs = new[] { "/list", filter };
        context = new CommandContext(filteredArgs, context.VisualStudioManager, context.ExtensionManager, context.VisualStudioInstance);

        return context;
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
        => Console.WriteLine($"{Name} [<filter>] [/id <id>]   {Description}");
}
