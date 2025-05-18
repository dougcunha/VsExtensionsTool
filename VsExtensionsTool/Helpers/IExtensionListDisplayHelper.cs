using VsExtensionsTool.Models;

namespace VsExtensionsTool.Helpers;

/// <summary>
/// Interface for displaying extension list.
/// This interface is responsible for displaying extension lists with support for progress and marketplace queries.
/// </summary>
public interface IExtensionListDisplayHelper
{
    /// <summary>
    /// Displays a list of extensions, with optional support for marketplace version queries and progress bar.
    /// </summary>
    void DisplayExtensions(List<ExtensionInfo> extensions);

    /// <summary>
    /// Populates the extension information from the marketplace.
    /// </summary>
    /// <param name="extensions">The list of extensions to populate.</param>
    /// <param name="instance">The Visual Studio instance to use.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PopulateExtensionsInfoFromMarketplaceAsync(List<ExtensionInfo> extensions, VisualStudioInstance instance);
}
