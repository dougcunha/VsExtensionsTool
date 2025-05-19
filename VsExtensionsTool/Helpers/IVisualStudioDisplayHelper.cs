namespace VsExtensionsTool.Helpers;

/// <summary>
/// This interface is responsible for displaying visual studio installations.
/// </summary>
public interface IVisualStudioDisplayHelper
{
    /// <summary>
    /// Prints the installations table.
    /// </summary>
    /// <param name="installations">The list of Visual Studio installations.</param>
    /// <param name="showHeader">Whether to show the header.</param>
    void PrintInstallationsTable(IReadOnlyList<VisualStudioInstance>? installations, bool showHeader = true);
}
