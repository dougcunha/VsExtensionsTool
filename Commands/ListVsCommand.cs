using System.CommandLine;
using VsExtensionsTool.Helpers;
using VsExtensionsTool.Managers;

namespace VsExtensionsTool.Commands;

/// <summary>
/// Command to list all Visual Studio installations.
/// </summary>
public sealed class ListVsCommand : Command
{
    public ListVsCommand() : base("list_vs", "List all Visual Studio installations.")
    {
        this.SetHandler
        (
            HandleAsync
        );
    }    
        
    private async Task HandleAsync()
    {
        var installations = await VisualStudioManager.GetVisualStudioInstallationsAsync().ConfigureAwait(false);
        VisualStudioDisplayHelper.PrintInstallationsTable(installations);
    }
}
