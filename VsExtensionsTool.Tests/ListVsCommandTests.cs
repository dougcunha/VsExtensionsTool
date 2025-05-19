using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using NSubstitute;
using VsExtensionsTool.Commands;
using VsExtensionsTool.Helpers;
using VsExtensionsTool.Managers;
using VsExtensionsTool.Models;

namespace VsExtensionsTool.Tests;

/// <summary>
/// Test suite for ListVsCommand using SMock to mock static dependencies.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListVsCommandTests
{
    private readonly IVisualStudioDisplayHelper _vsDisplayHelper = Substitute.For<IVisualStudioDisplayHelper>();
    private readonly IVisualStudioManager _vsManager = Substitute.For<IVisualStudioManager>();
    private readonly ListVsCommand _command;

    public ListVsCommandTests()
        => _command = new ListVsCommand(_vsDisplayHelper, _vsManager);

    [Fact]
    public async Task ListVsCommand_NoInstallations_PrintsNoInstallationsMessage()
    {
        // Arrange
        _vsManager.GetVisualStudioInstallationsAsync()
            .Returns(Task.FromResult(new List<VisualStudioInstance>()));

        var root = new RootCommand { _command };

        // Act
        await root.InvokeAsync("list_vs");

        // Assert
        _vsDisplayHelper.Received(1).PrintInstallationsTable(Arg.Is<List<VisualStudioInstance>>(x => x.Count == 0));
    }

    [Fact]
    public async Task ListVsCommand_OneInstallation_PrintsInstallationInfo()
    {
        // Arrange
        var installations = new List<VisualStudioInstance>
        {
            new()
            {
                DisplayName = "VS2022",
                InstallationVersion = "17.0.0",
                InstallationPath = "C:/VS2022"
            }
        };

        _vsManager.GetVisualStudioInstallationsAsync()
            .Returns(Task.FromResult(installations));

        var root = new RootCommand { _command };

        // Act
        await root.InvokeAsync("list_vs");

        // Assert
        _vsDisplayHelper.Received(1).PrintInstallationsTable(Arg.Is<List<VisualStudioInstance>>(static x => x.Count == 1));
        _vsDisplayHelper.Received(1).PrintInstallationsTable(Arg.Is<List<VisualStudioInstance>>(x => x[0] == installations[0]));
    }

    [Fact]
    public async Task ListVsCommand_MultipleInstallations_PrintsAllInstallations()
    {
        // Arrange
        var installations = new List<VisualStudioInstance>
        {
            new()
            {
                DisplayName = "VS2019",
                InstallationVersion = "16.0.0",
                InstallationPath = "C:/VS2019"
            },
            new()
            {
                DisplayName = "VS2022",
                InstallationVersion = "17.0.0",
                InstallationPath = "C:/VS2022",
                ChannelId = "VisualStudio.17.preview"
            }
        };

        _vsManager.GetVisualStudioInstallationsAsync()
            .Returns(Task.FromResult(installations));

        var root = new RootCommand { _command };

        // Act
        await root.InvokeAsync("list_vs");

        // Assert
        _vsDisplayHelper.Received(1).PrintInstallationsTable(Arg.Is<List<VisualStudioInstance>>(x => x.Count == 2));
        _vsDisplayHelper.Received(1).PrintInstallationsTable(Arg.Is<List<VisualStudioInstance>>(x => x[0] == installations[0]));
        _vsDisplayHelper.Received(1).PrintInstallationsTable(Arg.Is<List<VisualStudioInstance>>(x => x[1] == installations[1]));
    }
}
