using VsExtensionsTool.Models;

namespace VsExtensionsTool.Managers;

/// <summary>
/// This interface is responsible for managing Visual Studio extensions.
/// </summary>
public interface IExtensionManager
{
    /// <summary>
    /// Gets the extensions for a given Visual Studio instance.
    /// </summary>
    /// <param name="instance">The Visual Studio instance.</param>
    /// <param name="filter">The filter to apply to the extensions.</param>
    /// <returns>A list of extensions.</returns>
    List<ExtensionInfo> GetExtensions(VisualStudioInstance instance, string? filter = null);

    /// <summary>
    /// Populates the extension information from the marketplace asynchronously.
    /// </summary>
    /// <param name="instance">The Visual Studio instance.</param>
    /// <param name="extensions">The list of extensions to populate.</param>
    /// <param name="onPopulate">The action to perform on each populated extension.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PopulateExtensionInfoFromMarketplaceAsync(VisualStudioInstance instance, List<ExtensionInfo> extensions, Action<ExtensionInfo> onPopulate);

    /// <summary>
    /// Removes an extension by its ID.
    /// </summary>
    /// <param name="instance">The Visual Studio instance.</param>
    /// <param name="id">The ID of the extension to remove.</param>
    Task RemoveExtensionByIdAsync(VisualStudioInstance instance, string id);

    /// <summary>
    /// Updates an extension asynchronously.
    /// </summary>
    /// <param name="selectedExt">The extension to update.</param>
    /// <param name="instance">The Visual Studio instance.</param>
    /// <returns>The string output from console.</returns>
    Task<string> UpdateExtensionAsync(ExtensionInfo selectedExt, VisualStudioInstance instance);
}
