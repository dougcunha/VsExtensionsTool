namespace VsExtensionsTool.Tests.Helpers;

/// <summary>
/// Unit tests for VisualStudioDisplayHelper.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class VisualStudioDisplayHelperTests
{
    private readonly TestConsole _console = new();
    private readonly VisualStudioDisplayHelper _displayHelper;

    public VisualStudioDisplayHelperTests()
        => _displayHelper = new VisualStudioDisplayHelper(_console);

    [Fact]
    public void PrintInstallationsTable_NullOrEmpty_PrintsNoInstallations()
    {
        // Act
        _displayHelper.PrintInstallationsTable(null);
        _displayHelper.PrintInstallationsTable([]);

        // Assert
        var output = _console.Output;
        output.ShouldContain("No Visual Studio installations found.");
    }

    [Fact]
    public void PrintInstallationsTable_WithInstallations_PrintsTable()
    {
        // Arrange
        var installations = new List<VisualStudioInstance>
        {
            new() { DisplayName = "VS2022", InstallationVersion = "17.0.0", ChannelId = "release" },
            new() { DisplayName = "VS2022 Preview", InstallationVersion = "17.1.0", ChannelId = "preview" }
        };

        // Act
        _displayHelper.PrintInstallationsTable(installations);

        // Assert
        var output = _console.Output;
        output.ShouldContain("Detected Visual Studio installations:");
        output.ShouldContain("VS2022");
        output.ShouldContain("17.0.0");
        output.ShouldContain("VS2022 Preview");
        output.ShouldContain("17.1.0");
        output.ShouldContain("(Preview)");
    }

    [Fact]
    public void PrintInstallationsTable_WithShowHeaderFalse_DoesNotPrintHeader()
    {
        // Arrange
        var installations = new List<VisualStudioInstance>
        {
            new() { DisplayName = "VS2022", InstallationVersion = "17.0.0" }
        };

        // Act
        _displayHelper.PrintInstallationsTable(installations, showHeader: false);

        // Assert
        var output = _console.Output;
        output.ShouldNotContain("Detected Visual Studio installations:");
        output.ShouldContain("VS2022");
    }
}
