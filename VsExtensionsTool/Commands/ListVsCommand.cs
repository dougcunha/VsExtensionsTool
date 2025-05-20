namespace VsExtensionsTool.Commands;

/// <summary>
/// Command to list all Visual Studio installations.
/// </summary>
public sealed class ListVsCommand : Command
{
    private readonly IVisualStudioDisplayHelper _displayHelper;
    private readonly IVisualStudioManager _vsManager;

    public ListVsCommand
    (
        IVisualStudioDisplayHelper displayHelper,
        IVisualStudioManager vsManager
    )
        : base("list_vs", "List all Visual Studio installations.")
    {
        _displayHelper = displayHelper;
        _vsManager = vsManager;
        this.SetHandler(HandleAsync);
    }

    private async Task HandleAsync()
    {
        var installations = await _vsManager.GetVisualStudioInstallationsAsync().ConfigureAwait(false);
        _displayHelper.PrintInstallationsTable(installations);
    }
}
