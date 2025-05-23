using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace VsExtensionsTool.Helpers;

/// <inheritdoc/>
public sealed class MarketplaceHelper(IAnsiConsole console, HttpClient httpClient, string? marketplaceUrl = null) : IMarketplaceHelper
{
    private readonly string _marketplaceUrl = marketplaceUrl ?? MARKETPLACE_API_URL;
    private const string MARKETPLACE_API_URL = "https://marketplace.visualstudio.com/_apis/public/gallery/extensionquery";
    private const string MARKETPLACE_API_VERSION = "3.2-preview.1";
    private const int FILTERTYPE_VSIX_ID = 17;
    private const int FILTERTYPE_PUBLISHER = 7;

    [Flags]
    private enum ExtensionQueryOptions
    {
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
        // ReSharper restore UnusedMember.Local
    }

    /// <inheritdoc/>
    public async Task PopulateExtensionInfoFromMarketplaceAsync(ExtensionInfo extension, VisualStudioInstance vsInstance)
    {
        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json;api-version=" + MARKETPLACE_API_VERSION);
        httpClient.DefaultRequestHeaders.Add("User-Agent", $"VSIDE-{vsInstance.InstallationVersion}");
        httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");

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
            var response = await httpClient.PostAsync(_marketplaceUrl, content).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var (version, url) = await TryExtractVersionAndVsixUrlAsync(response).ConfigureAwait(false);
                extension.LatestVersion = version ?? "Not found";
                extension.VsixUrl = url;
                return;
            }

            var errorMessage = await GetResponseStringAsync(response).ConfigureAwait(false);
            console.MarkupLineInterpolated($"[red]Error occurred while fetching the latest version. {errorMessage}[/]");
        }
        catch (Exception ex)
        {
            console.MarkupLine($"[red]Error occurred while fetching the latest version. {ex.Message.EscapeMarkup()}[/]");
        }
    }

    private static readonly string[] _encodings = ["gzip", "br", "deflate"];
    private static readonly Dictionary<string, Func<Stream, Stream>> _streamFactory = new()
    {
        ["gzip"] = static res => new GZipStream(res, CompressionMode.Decompress),
        ["br"] = static res => new BrotliStream(res, CompressionMode.Decompress),
        ["deflate"] = static res => new DeflateStream(res, CompressionMode.Decompress)
    };

    /// <summary>
    /// Asynchronously retrieves the response content as a string, handling decompression if necessary.
    /// </summary>
    /// <remarks>This method checks the content encoding of the response and applies the appropriate
    /// decompression if the encoding is supported. If no decompression is required, the content is read directly as a
    /// string.</remarks>
    /// <param name="response">The <see cref="HttpResponseMessage"/> containing the response content to process.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response content as a string. If
    /// the response content is compressed, it is decompressed before being returned.</returns>
    private static async Task<string> GetResponseStringAsync(HttpResponseMessage response)
    {
        if (!response.Content.Headers.ContentEncoding.Any(static e => _encodings.Contains(e)))
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        await using var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        await using var decompressedStream = _streamFactory[response.Content.Headers.ContentEncoding.First()](contentStream);
        using var reader = new StreamReader(decompressedStream, Encoding.UTF8);

        return await reader.ReadToEndAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Attempts to extract the version and VSIX URL from the provided HTTP response.
    /// </summary>
    /// <remarks>This method parses a JSON response to extract the first available version and its associated
    /// VSIX URL. If the response does not contain the expected structure or the required data, the method returns <see
    /// langword="null"/> for the corresponding values.</remarks>
    /// <param name="response">The HTTP response containing the JSON payload to parse.</param>
    /// <returns>A tuple containing the extracted version and VSIX URL: <list type="bullet"> <item><description><c>version</c>:
    /// The version string if found; otherwise, <see langword="null"/>.</description></item>
    /// <item><description><c>vsixUrl</c>: The VSIX download URL if found; otherwise, <see
    /// langword="null"/>.</description></item> </list></returns>
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
            {
                continue;
            }

            vsixUrl = property.GetProperty("value").GetString();

            break;
        }

        return (version, vsixUrl);
    }
}