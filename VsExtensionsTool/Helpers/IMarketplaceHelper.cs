using VsExtensionsTool.Models;

namespace VsExtensionsTool.Helpers;

/// <summary>
/// Interface for marketplace helper.
/// This interface is responsible for populating extension information from the marketplace.
/// </summary>
public interface IMarketplaceHelper
{
    /// <summary>
    /// Populates the extension information from the marketplace.
    /// </summary>
    /// <param name="extension">The extension information to populate.</param>
    /// <param name="vsInstance">The Visual Studio instance to use.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PopulateExtensionInfoFromMarketplaceAsync(ExtensionInfo extension, VisualStudioInstance vsInstance);
}
