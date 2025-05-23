namespace VsExtensionsTool.Tests.Helpers;

[ExcludeFromCodeCoverage]
public sealed class ExtensionListDisplayHelperTests
{
    private readonly TestConsole _console = new();
    private readonly IExtensionManager _extensionManager = Substitute.For<IExtensionManager>();
    private readonly ExtensionListDisplayHelper _helper;

    public ExtensionListDisplayHelperTests()
        => _helper = new ExtensionListDisplayHelper(_extensionManager, _console);

    [Fact]
    public void DisplayExtensions_EmptyList_PrintsNoExtensions()
    {
        // Act
        _helper.DisplayExtensions([]);

        // Assert
        _console.Output.ShouldContain("No extensions found");
    }

    [Fact]
    public void DisplayExtensions_WithExtensions_PrintsTable()
    {
        // Arrange
        var extensions = new List<ExtensionInfo>
        {
            new() { Name = "Ext1", Publisher = "Pub1", InstalledVersion = "1.0.0" },
            new() { Name = "Ext2", Publisher = "Pub2", InstalledVersion = "2.0.0" }
        };

        // Act
        _helper.DisplayExtensions(extensions);

        // Assert
        var output = _console.Output;
        output.ShouldContain("Ext1");
        output.ShouldContain("Pub1");
        output.ShouldContain("1.0.0");
        output.ShouldContain("Ext2");
        output.ShouldContain("Pub2");
        output.ShouldContain("2.0.0");
    }

    [Fact]
    public void DisplayExtensions_WithMarketplaceVersion_PrintsMarketplaceColumn()
    {
        // Arrange
        var extensions = new List<ExtensionInfo>
        {
            new() { Name = "Ext1", Publisher = "Pub1", InstalledVersion = "1.0.0", LatestVersion = "2.0.0" },
            new() { Name = "Ext2", Publisher = "Pub2", InstalledVersion = "2.0.0", LatestVersion = "2.0.0" }
        };

        // Act
        _helper.DisplayExtensions(extensions);

        // Assert
        var output = _console.Output;
        output.ShouldContain("Marketplace");
        output.ShouldContain("2.0.0");
    }

    [Fact]
    public void DisplayExtensions_WithOutdatedExtension_PrintsExtensionInfo()
    {
        // Arrange
        var extensions = new List<ExtensionInfo>
        {
            new() { Name = "Ext1", Publisher = "Pub1", InstalledVersion = "1.0.0", LatestVersion = "2.0.0" }
        };
        // Act
        _helper.DisplayExtensions(extensions);
        // Assert
        var output = _console.Output;
        output.ShouldContain("Ext1");
        output.ShouldContain("Pub1");
        output.ShouldContain("1.0.0");
        output.ShouldContain("2.0.0");
    }

    [Fact]
    public async Task PopulateExtensionsInfoFromMarketplaceAsync_CallsExtensionManagerAndShowsProgress()
    {
        // Arrange
        var extensions = new List<ExtensionInfo>
        {
            new() { Name = "Ext1", Publisher = "Pub1" },
            new() { Name = "Ext2", Publisher = "Pub2" }
        };
        var instance = new VisualStudioInstance { DisplayName = "VS2022" };
        _extensionManager.PopulateExtensionInfoFromMarketplaceAsync(
            instance,
            extensions,
            Arg.Any<Action<ExtensionInfo>>()
        ).Returns(Task.CompletedTask);

        // Act
        await _helper.PopulateExtensionsInfoFromMarketplaceAsync(extensions, instance);

        // Assert
        _console.Output.ShouldContain("Fetching extensions versions");

        await _extensionManager.Received(1)
            .PopulateExtensionInfoFromMarketplaceAsync
            (
                instance,
                extensions,
                Arg.Any<Action<ExtensionInfo>>()
            );
    }
}
