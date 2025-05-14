namespace VsExtensionsTool;

file static class Program
{
    /// <summary>
    /// Application entry point.
    /// </summary>
    public static Task Main(string[] args)
    {
        var extManager = new ExtensionManager();
        var dispatcher = new CommandDispatcher(extManager);

        return dispatcher.DispatchAsync(args);
    }
}