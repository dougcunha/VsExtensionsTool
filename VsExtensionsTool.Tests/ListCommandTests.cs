using System.CommandLine;
using Shouldly;
using VsExtensionsTool.Commands;
using VsExtensionsTool.Managers;
using VsExtensionsTool.Models;
using Spectre.Console.Testing;
using System.Diagnostics.CodeAnalysis;
using VsExtensionsTool.Helpers;
using NSubstitute;

namespace VsExtensionsTool.Tests;

/// <summary>
/// Test suite for ListCommand using StaticMock para mockar métodos estáticos.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListCommandTests
{
    private readonly ListCommand _command;
    private readonly IExtensionListDisplayHelper _displayHelper = Substitute.For<IExtensionListDisplayHelper>();
    private readonly IVisualStudioManager _vsManager = Substitute.For<IVisualStudioManager>();
    private readonly IExtensionManager _extensionManager = Substitute.For<IExtensionManager>();
    private readonly TestConsole _console = new();

    public ListCommandTests()
        => _command = new ListCommand
        (
            _displayHelper,
            _vsManager,
            _extensionManager,
            _console
        );

    [Fact]
    public async Task ListCommand_NoVsInstance_PrintsNoInstanceMessage()
    {
        // Arrange
        _vsManager.SelectVisualStudioInstanceAsync()
            .Returns(Task.FromResult<VisualStudioInstance?>(null));

        var root = new RootCommand { _command };

        // Act
        await root.InvokeAsync("list");

        // Assert
        var output = _console.Output;
        output.ShouldContain("No Visual Studio instance selected", Case.Insensitive);
    }

    [Fact]
    public async Task ListCommand_NoExtensions_PrintsNoExtensionsMessage()
    {
        // Arrange
        var vsInstance = new VisualStudioInstance { DisplayName = "VS2022" };

        _vsManager.SelectVisualStudioInstanceAsync()
            .Returns(Task.FromResult<VisualStudioInstance?>(vsInstance));

        _extensionManager.GetExtensions(vsInstance, null)
            .Returns([]);

        var root = new RootCommand { _command };

        // Act
        await root.InvokeAsync("list");

        // Assert
        var output = _console.Output;
        output.ShouldContain("No extensions found", Case.Insensitive);
    }

    [Fact]
    public async Task ListCommand_WithExtensions_DisplaysExtensions()
    {
        // Arrange
        var vsInstance = new VisualStudioInstance { DisplayName = "VS2022" };

        _vsManager.SelectVisualStudioInstanceAsync()
            .Returns(Task.FromResult<VisualStudioInstance?>(vsInstance));

        var extensions = new List<ExtensionInfo>
        {
            new() { Name = "Ext1", Id = "ext1" },
            new() { Name = "Ext2", Id = "ext2" }
        };

        _extensionManager.GetExtensions(vsInstance, null)
            .Returns(extensions);

        var root = new RootCommand { _command };

        // Act
        await root.InvokeAsync("list");

        // Assert
        _displayHelper.Received(1).DisplayExtensions
        (
            extensions
        );
    }

    [Fact]
    public async Task ListCommand_WithOutdated_DiplayExtensionsWithVersion()
    {
        // Arrange
        var vsInstance = new VisualStudioInstance { DisplayName = "VS2022" };

        _vsManager.SelectVisualStudioInstanceAsync()
            .Returns(Task.FromResult<VisualStudioInstance?>(vsInstance));

        var extensions = new List<ExtensionInfo>
        {
            new() { Name = "Ext1", Id = "ext1", InstalledVersion = "1.0.0.0", LatestVersion = "2.0.0.0" },
            new() { Name = "Ext2", Id = "ext2", InstalledVersion = "2.0.0.0", LatestVersion = "2.0.0.0" }
        };

        _extensionManager.GetExtensions(vsInstance, null)
            .Returns(extensions);

        var root = new RootCommand { _command };

        // Act
        await root.InvokeAsync("list --outdated");

        // Assert
        _displayHelper.Received(1).DisplayExtensions
        (
           Arg.Is<List<ExtensionInfo>>(static x => x.Count == 1 && x[0].Id == "ext1")
        );

        await _displayHelper.Received(1)
            .PopulateExtensionsInfoFromMarketplaceAsync(extensions, vsInstance);
    }
}
