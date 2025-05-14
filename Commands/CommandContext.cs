namespace VsExtensionsTool.Commands;

/// <summary>
/// Provides context for command execution, including arguments, dependencies and the selected Visual Studio instance.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CommandContext"/> class.
/// </remarks>
public sealed class CommandContext
(
    string[] args,
    ExtensionManager extManager,
    VisualStudioInstance? vsInstance
)
{
    /// <summary>
    /// Gets the command-line arguments.
    /// </summary>
    public string[] Args { get; } = args;

    /// <summary>
    /// Gets the extension manager.
    /// </summary>
    public ExtensionManager ExtensionManager { get; } = extManager;

    /// <summary>
    /// Gets the selected Visual Studio instance, if any.
    /// </summary>
    public VisualStudioInstance? VisualStudioInstance { get; set; } = vsInstance;
}
