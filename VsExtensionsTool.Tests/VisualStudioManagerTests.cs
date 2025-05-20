using System.Diagnostics.CodeAnalysis;
using NSubstitute;
using Shouldly;
using Spectre.Console.Testing;
using VsExtensionsTool.Helpers;
using VsExtensionsTool.Managers;
using VsExtensionsTool.Models;

namespace VsExtensionsTool.Tests;

/// <summary>
/// Unit tests for VisualStudioManager.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class VisualStudioManagerTests
{
    private readonly TestConsole _console = new();
    private readonly IVisualStudioDisplayHelper _displayHelper = Substitute.For<IVisualStudioDisplayHelper>();
    private readonly IProcessRunner _processRunner = Substitute.For<IProcessRunner>();
    private readonly VisualStudioManager _manager;

    public VisualStudioManagerTests()
        => _manager = new VisualStudioManager(_console, _displayHelper, _processRunner);

    [Fact]
    public async Task GetVisualStudioInstallationsAsync_EmptyOutput_ReturnsEmptyList()
    {
        // Arrange
        _processRunner.RunAsync("", "")
            .ReturnsForAnyArgs("");

        // Act
        var result = await _manager.GetVisualStudioInstallationsAsync();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetVisualStudioInstallationsAsync_ValidJson_ReturnsDeserializedList()
    {
        // Arrange
        const string JSON = """
        [
            {
                "displayName": "VS2022",
                "installationPath": "C:/VS",
                "channelId": "Preview",
                "installationVersion": "17.0.0",
                "instanceId": "abc"
            }
        ]
        """;

        _processRunner.RunAsync("", "")
            .ReturnsForAnyArgs(JSON);

        // Act
        var result = await _manager.GetVisualStudioInstallationsAsync();

        // Assert
        result.ShouldHaveSingleItem();
        result[0].DisplayName.ShouldBe("VS2022");
        result[0].InstallationPath.ShouldBe("C:/VS");
        result[0].ChannelId.ShouldBe("Preview");
        result[0].InstallationVersion.ShouldBe("17.0.0");
        result[0].InstanceId.ShouldBe("abc");
    }

    [Fact]
    public async Task SelectVisualStudioInstanceAsync_NoInstallations_ReturnsNullAndPrintsTable()
    {
        // Arrange
        _processRunner.RunAsync("", "")
            .ReturnsForAnyArgs("[]");

        // Act
        var result = await _manager.SelectVisualStudioInstanceAsync();

        // Assert
        result.ShouldBeNull();
        _displayHelper.Received(1).PrintInstallationsTable(Arg.Any<List<VisualStudioInstance>>());
    }

    [Fact]
    public async Task SelectVisualStudioInstanceAsync_SingleInstallation_ReturnsInstance()
    {
        // Arrange
        const string JSON = """
        [
            {
                "displayName": "VS2022",
                "installationPath": "C:/VS",
                "channelId": "Preview",
                "installationVersion": "17.0.0",
                "instanceId": "abc"
            }
        ]
        """;

        _processRunner.RunAsync("", "")
            .ReturnsForAnyArgs(JSON);

        // Act
        var result = await _manager.SelectVisualStudioInstanceAsync();

        // Assert
        result.ShouldNotBeNull();
        result.DisplayName.ShouldBe("VS2022");
    }

    [Fact]
    public async Task SelectVisualStudioInstanceAsync_RepromptFalse_ReturnsCachedInstance()
    {
        // Arrange
        const string JSON = """
        [
            {
                "displayName": "VS2022",
                "installationPath": "C:/VS",
                "channelId": "Preview",
                "installationVersion": "17.0.0",
                "instanceId": "abc"
            }
        ]
        """;

        _processRunner.RunAsync("", "")
            .ReturnsForAnyArgs(JSON);

        // Act
        var result1 = await _manager.SelectVisualStudioInstanceAsync();
        var result2 = await _manager.SelectVisualStudioInstanceAsync(false);

        // Assert
        result2.ShouldBe(result1);
    }

    [Fact]
    public async Task SelectVisualStudioInstanceAsync_MultipleInstallations_UserSelectsSecond_ReturnsSecondInstance()
    {
        // Arrange
        const string JSON = """
        [
            {
                "displayName":"VS2022",
                "installationPath":"C:/VS",
                "channelId":"Preview",
                "installationVersion":"17.0.0",
                "instanceId":"abc"
            },
            {
                "displayName":"VS2019",
                "installationPath":"C:/VS2019",
                "channelId":"Release",
                "installationVersion":"16.0.0",
                "instanceId":"def"
            }
        ]
        """;

        _processRunner.RunAsync("", "")
            .ReturnsForAnyArgs(JSON);

        _console.Interactive();
        _console.Input.PushTextWithEnter("2");

        // Act
        var result = await _manager.SelectVisualStudioInstanceAsync();

        // Assert
        result.ShouldNotBeNull();
        result.DisplayName.ShouldBe("VS2019");
    }

    [Fact]
    public async Task SelectVisualStudioInstanceAsync_MultipleInstallations_UserCancels_ReturnsNull()
    {
        // Arrange
        const string JSON = """
        [
            {
                "displayName":"VS2022",
                "installationPath":"C:/VS",
                "channelId":"Preview",
                "installationVersion":"17.0.0",
                "instanceId":"abc"
            },
            {
                "displayName":"VS2019",
                "installationPath":"C:/VS2019",
                "channelId":"Release",
                "installationVersion":"16.0.0",
                "instanceId":"def"
            }
        ]
        """;

        _processRunner.RunAsync("", "")
            .ReturnsForAnyArgs(JSON);

        _console.Interactive();
        _console.Input.PushTextWithEnter("0");

        // Act
        var result = await _manager.SelectVisualStudioInstanceAsync();

        // Assert
        result.ShouldBeNull();
    }
}
