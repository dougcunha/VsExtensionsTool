namespace VsExtensionsTool;

file static class Program
{
    /// <summary>
    /// Application entry point.
    /// </summary>
    public static Task Main(string[] args)
    {
        var vsManager = new VisualStudioManager();
        var extManager = new ExtensionManager();
        var dispatcher = new CommandDispatcher(vsManager, extManager);

        return dispatcher.DispatchAsync(args);
    }
}