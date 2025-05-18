namespace VsExtensionsTool.Managers;

using VsExtensionsTool.Models;

/// <summary>
/// This interface is responsible for managing Visual Studio installations.
/// </summary>
public interface IVisualStudioManager
{
    /// <summary>
    /// Gets the Visual Studio installations asynchronously.
    /// </summary>
    /// <returns>A list of Visual Studio installations.</returns>
    Task<List<VisualStudioInstance>> GetVisualStudioInstallationsAsync();

    /// <summary>
    /// Selects a Visual Studio instance asynchronously.
    /// </summary>
    /// <param name="reprompt">Whether to reprompt the user if no instance is selected.</param>
    /// <returns>The selected Visual Studio instance.</returns>
    Task<VisualStudioInstance?> SelectVisualStudioInstanceAsync(bool reprompt = true);

    /// <summary>
    /// Gets the selected Visual Studio instance selected by the user.
    /// </summary>
    VisualStudioInstance? VisualStudio { get; }
}
