using System.Xml.Linq;

namespace VsExtensionsTool.Helpers;

/// <summary>
/// Abstraction for loading XDocument, to facilitate unit testing.
/// </summary>
public interface IXDocumentLoader
{
    /// <summary>
    /// Loads an XDocument from the specified path.
    /// </summary>
    /// <param name="path">The path to the XML file.</param>
    /// <returns>The loaded XDocument.</returns>
    XDocument Load(string path);
}
