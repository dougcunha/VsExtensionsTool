using System.Xml.Linq;

namespace VsExtensionsTool.Helpers;

/// <inheritdoc />
/// <summary>
/// Default implementation of IXDocumentLoader using XDocument.Load.
/// </summary>
public sealed class XDocumentLoader : IXDocumentLoader
{
    /// <inheritdoc />
    public XDocument Load(string path)
        => XDocument.Load(path);
}
