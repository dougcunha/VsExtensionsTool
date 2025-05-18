using System.IO.Compression;
using System.Text;
using System.Text.Json;
using VsExtensionsTool.Managers;
using VsExtensionsTool.Models;

namespace VsExtensionsTool.Helpers;

public static class MarketplaceHelper
{
    private const string MARKETPLACE_API_URL = "https://marketplace.visualstudio.com/_apis/public/gallery/extensionquery";
    private const string MARKETPLACE_API_VERSION = "3.2-preview.1";
    private const int FILTERTYPE_VSIX_ID = 17;
    private const int FILTERTYPE_PUBLISHER = 7;

    [Flags]
    private enum ExtensionQueryOptions
    {
        // ReSharper disable InconsistentNaming
        // ReSharper disable UnusedMember.Local
        None = 0,
        IncludeVersions = 1,
        IncludeFiles = 2,
        IncludeCategoryAndTags = 4,
        IncludeSharedAccounts = 8,
        IncludeVersionProperties = 16,
        ExcludeNonValidated = 32,
        IncludeInstallationTargets = 64,
        IncludeAssetUri = 128,
        IncludeStatistics = 256,
        IncludeLatestVersionOnly = 512,
        Unpublished = 1024,
        IncludeNameWithPublisher = 2048,
        IncludeMarketPlace = 4096,
        IncludeMetadata = 8192,
        IncludeFrameworks = 16384
    }
    // ReSharper restore InconsistentNaming
    // ReSharper restore UnusedMember.Local

    /// <summary>
    /// Gets the latest version of a Visual Studio extension from the Marketplace by its Id.
    /// </summary>
    /// <param name="extension">The extension info.</param>
    /// <param name="vsInstance">The selected instance of Visual Studio.</param>
    /// <returns>The latest version string, or "Not found" if not available.</returns>
    public static async Task PopulateExtensionInfoFromMarketplaceAsync(ExtensionInfo extension, VisualStudioInstance vsInstance)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Accept", "application/json;api-version=" + MARKETPLACE_API_VERSION);
        client.DefaultRequestHeaders.Add("User-Agent", $"VSIDE-{vsInstance.InstallationVersion}");
        client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");

        //remove all spaces from publisher
        var publisher = extension.Publisher.Replace(" ", string.Empty);

        //remove all spaces from extension id
        var extensionId = extension.Name.Replace(" ", string.Empty);
        var useVsixId = !string.IsNullOrWhiteSpace(extension.VsixId);

        var payload = new
        {
            filters = new[]
            {
                new
                {
                    criteria = new[]
                    {
                        new
                        {
                            filterType = useVsixId ? FILTERTYPE_VSIX_ID : FILTERTYPE_PUBLISHER,
                            value = useVsixId ? $"VsixId={extension.VsixId}" : $"{publisher}.{extensionId}"
                        }
                    },
                    pageNumber = 1,
                    pageSize = 1,
                    sortBy = 0,
                    sortOrder = 0
                }
            },
            flags = ExtensionQueryOptions.IncludeLatestVersionOnly | ExtensionQueryOptions.IncludeFiles | ExtensionQueryOptions.IncludeVersions,
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await client.PostAsync(MARKETPLACE_API_URL, content).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var (version, url) = await TryExtractVersionAndVsixUrlAsync(response).ConfigureAwait(false);

                extension.LatestVersion = version ?? "Not found";
                extension.VsixUrl = url;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error occurred while fetching the latest version. {ex.Message.EscapeMarkup()}[/]");
        }
    }

    private static readonly string[] _encodings = ["gzip", "br", "deflate"];

    private static readonly Dictionary<string, Func<Stream, Stream>> _streamFactory = new()
    {
        ["gzip"] = static res => new GZipStream(res, CompressionMode.Decompress),
        ["br"] = static res => new BrotliStream(res, CompressionMode.Decompress),
        ["deflate"] = static res => new DeflateStream(res, CompressionMode.Decompress)
    };

    private static async Task<string> GetResponseStringAsync(HttpResponseMessage response)
    {
        if (!response.Content.Headers.ContentEncoding.Any(static e => _encodings.Contains(e)))
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        await using var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        await using var decompressedStream = _streamFactory[response.Content.Headers.ContentEncoding.First()](contentStream);
        using var reader = new StreamReader(decompressedStream, Encoding.UTF8);

        return await reader.ReadToEndAsync().ConfigureAwait(false);
    }

    private static async Task<(string? version, string? vsixUrl)> TryExtractVersionAndVsixUrlAsync(HttpResponseMessage response)
    {
        var responseString = await GetResponseStringAsync(response).ConfigureAwait(false);

        using var doc = JsonDocument.Parse(responseString);
        var root = doc.RootElement;
        var results = root.GetProperty("results");

        if (results.GetArrayLength() == 0)
            return (null, null);

        var extensions = results[0].GetProperty("extensions");

        if (extensions.GetArrayLength() == 0)
            return (null, null);

        var versions = extensions[0].GetProperty("versions");

        if (versions.GetArrayLength() == 0)
            return (null, null);

        var version = versions[0].GetProperty("version").GetString();
        string? vsixUrl = null;

        if (!versions[0].TryGetProperty("properties", out var properties))
            return (version, vsixUrl);

        foreach (var property in properties.EnumerateArray())
        {
            if (!property.TryGetProperty("key", out var propertyType)
                || propertyType.GetString() != "DownloadUpdateUrl")
                continue;

            vsixUrl = property.GetProperty("value").GetString();

            break;
        }

        return (version, vsixUrl);
    }
}