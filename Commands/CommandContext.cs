namespace VsExtensionsTool.Commands;

/// <summary>
/// Provides context for command execution, including arguments, dependencies and the selected Visual Studio instance.
/// </summary>
public sealed class CommandContext
{
    /// <summary>
    /// Gets the command-line arguments.
    /// </summary>
    public string[] Args { get; }

    /// <summary>
    /// Gets the Visual Studio manager.
    /// </summary>
    public VisualStudioManager VisualStudioManager { get; }

    /// <summary>
    /// Gets the extension manager.
    /// </summary>
    public ExtensionManager ExtensionManager { get; }

    /// <summary>
    /// Gets the selected Visual Studio instance, if any.
    /// </summary>
    public VisualStudioInstance? VisualStudioInstance { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandContext"/> class.
    /// </summary>
    public CommandContext(string[] args, VisualStudioManager vsManager, ExtensionManager extManager, VisualStudioInstance? vsInstance)
    {
        Args = args;
        VisualStudioManager = vsManager;
        ExtensionManager = extManager;
        VisualStudioInstance = vsInstance;
    }
}
