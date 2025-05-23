using System.Text;

namespace VsExtensionsTool.Tests.Helpers;

[ExcludeFromCodeCoverage]
public sealed class MarketplaceHelperTests
{
    private readonly TestConsole _console = new();

    private static HttpClient CreateMockHttpClient(HttpResponseMessage response)
    {
        var handler = new MockHttpMessageHandler(response);

        return new HttpClient(handler);
    }

    private class MockHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(response);
    }

    [Fact]
    public async Task PopulateExtensionInfoFromMarketplaceAsync_SetsLatestVersionAndVsixUrl()
    {
        // Arrange
        var vsInstance = new VisualStudioInstance { InstallationVersion = "17.0.0" };
        var ext = new ExtensionInfo { Name = "TestExt", Publisher = "TestPublisher" };

        var responseJson = JsonSerializer.Serialize(new
        {
            results = new[]
            {
                new
                {
                    extensions = new[]
                    {
                        new
                        {
                            versions = new[]
                            {
                                new
                                {
                                    version = "2.1.0",
                                    properties = new[]
                                    {
                                        new { key = "DownloadUpdateUrl", value = "https://vsix-url/abc.vsix" }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        });

        var httpResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
        };

        var httpClient = CreateMockHttpClient(httpResponse);
        var helper = new MarketplaceHelper(_console, httpClient);

        // Act
        await helper.PopulateExtensionInfoFromMarketplaceAsync(ext, vsInstance);

        // Assert
        ext.LatestVersion.ShouldBe("2.1.0");
        ext.VsixUrl.ShouldBe("https://vsix-url/abc.vsix");
    }

    [Fact]
    public async Task PopulateExtensionInfoFromMarketplaceAsync_ApiError_PrintsError()
    {
        // Arrange
        var vsInstance = new VisualStudioInstance { InstallationVersion = "17.0.0" };
        var ext = new ExtensionInfo { Name = "TestExt", Publisher = "TestPublisher" };

        var httpResponse = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Internal Server Error")
        };

        var httpClient = CreateMockHttpClient(httpResponse);
        var helper = new MarketplaceHelper(_console, httpClient);

        // Act
        await helper.PopulateExtensionInfoFromMarketplaceAsync(ext, vsInstance);

        // Assert
        _console.Output.ShouldContain("Error occurred while fetching the latest version");
    }

    [Fact]
    public async Task PopulateExtensionInfoFromMarketplaceAsync_ThrowsException_PrintsError()
    {
        // Arrange
        var vsInstance = new VisualStudioInstance { InstallationVersion = "17.0.0" };
        var ext = new ExtensionInfo { Name = "TestExt", Publisher = "TestPublisher" };
        var handler = new ThrowingHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var helper = new MarketplaceHelper(_console, httpClient);

        // Act
        await helper.PopulateExtensionInfoFromMarketplaceAsync(ext, vsInstance);

        // Assert
        _console.Output.ShouldContain("Error occurred while fetching the latest version");
    }

    [Theory]
    [InlineData("gzip")]
    [InlineData("br")]
    [InlineData("deflate")]
    public async Task GetResponseStringAsync_WithCompressedEncoding_HandlesCompressedEncodings(string encoding)
    {
        // Arrange
        var json = JsonSerializer.Serialize(new
        {
            results = new[]
            {
                new
                {
                    extensions = new[]
                    {
                        new
                        {
                            versions = new[]
                            {
                                new
                                {
                                    version = "2.1.0",
                                    properties = new[]
                                    {
                                        new { key = "DownloadUpdateUrl", value = "https://vsix-url/abc.vsix" }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        });

        var compressed = encoding switch
        {
            "gzip" => await HandleGzipEncodingAsync(json),
            "br" => await HandleBrotliEncodingAsync(json),
            "deflate" => await HandleDeflateEncodingAsync(json),
            var _ => throw new ArgumentOutOfRangeException(nameof(encoding)),
        };

        var content = new ByteArrayContent(compressed);
        content.Headers.Add("Content-Encoding", encoding);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        var httpResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = content };
        var httpClient = CreateMockHttpClient(httpResponse);
        var helper = new MarketplaceHelper(_console, httpClient);
        var vsInstance = new VisualStudioInstance { InstallationVersion = "17.0.0" };
        var ext = new ExtensionInfo { Name = "TestExt", Publisher = "TestPublisher" };

        // Act
        await helper.PopulateExtensionInfoFromMarketplaceAsync(ext, vsInstance);

        // Assert
        ext.LatestVersion.ShouldBe("2.1.0");
        ext.VsixUrl.ShouldBe("https://vsix-url/abc.vsix");
    }

    private static async Task<byte[]> HandleDeflateEncodingAsync(string json)
    {
        await using var ms = new MemoryStream();
        await using var deflate = new System.IO.Compression.DeflateStream(ms, System.IO.Compression.CompressionMode.Compress);
        await using var sw = new StreamWriter(deflate, Encoding.UTF8);
        await sw.WriteAsync(json);
        await sw.FlushAsync();
        deflate.Flush();

        return ms.ToArray();
    }

    private static async Task<byte[]> HandleBrotliEncodingAsync(string json)
    {
        await using var ms = new MemoryStream();
        await using var br = new System.IO.Compression.BrotliStream(ms, System.IO.Compression.CompressionMode.Compress);
        await using var sw = new StreamWriter(br, Encoding.UTF8);
        await sw.WriteAsync(json);
        await sw.FlushAsync();
        br.Flush();

        return ms.ToArray();
    }

    private static async Task<byte[]> HandleGzipEncodingAsync(string json)
    {
        await using var ms = new MemoryStream();
        await using var gzip = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Compress);
        await using var sw = new StreamWriter(gzip, Encoding.UTF8);
        await sw.WriteAsync(json);
        await sw.FlushAsync();
        gzip.Flush();

        return ms.ToArray();
    }

    private class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new HttpRequestException("Simulated network failure");
    }
}
