namespace VsExtensionsTool.Commands;

/// <summary>
/// Represents a command that can be executed by the VsExtensionsTool.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Executes the command with the provided context.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    Task ExecuteAsync(CommandContext context);

    /// <summary>
    /// Determines whether the help option is specified in the provided arguments.
    /// </summary>
    /// <param name="args">An array of command-line arguments to check for help options.</param>
    /// <returns><see langword="true"/> if the arguments contain "/help" or "--help"; otherwise, <see langword="false"/>.</returns>
    static bool ShowHelp(string[] args)
        => args.Length > 1 && (args.Contains("/help") || args.Contains("--help"));

    /// <summary>
    /// Prints help information for the command.
    /// </summary>
    void PrintHelp();

    /// <summary>
    /// Gets the command name (e.g. /list, /remove).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a short description of the command.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Determines if this command should handle the given context.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <returns>True if the command should execute, otherwise false.</returns>
    bool CanExecute(CommandContext context);

    /// <summary>
    /// Indicates if the command requires a Visual Studio instance.
    /// </summary>
    bool NeedsVsInstance { get; }
}
