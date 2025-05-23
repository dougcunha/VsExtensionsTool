namespace VsExtensionsTool.Tests.Commands;

/// <summary>
/// Test suite for UpdateCommand using SMock to mock static dependencies.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateCommandTests
{
    private readonly IExtensionManager _extensionManager = Substitute.For<IExtensionManager>();
    private readonly IVisualStudioManager _vsManager = Substitute.For<IVisualStudioManager>();
    private readonly IExtensionListDisplayHelper _extDisplayHelper = Substitute.For<IExtensionListDisplayHelper>();
    private readonly TestConsole _console = new();
    private readonly UpdateCommand _command;

    public UpdateCommandTests()
        => _command = new UpdateCommand
        (
            _vsManager,
            _extDisplayHelper,
            _extensionManager,
            _console
        );

    [Fact]
    public async Task UpdateCommand_NoVsInstance_PrintsNoInstanceMessage()
    {
        // Arrange
       
        var root = new RootCommand { _command };

        // Act
        await root.InvokeAsync("upd");

        // Assert
        var output = _console.Output;
        output.ShouldContain("No Visual Studio instance selected", Case.Insensitive);
    }

    [Fact]
    public async Task UpdateCommand_NoOutdatedExtensions_PrintsAllUpToDate()
    {
        // Arrange
        var vsInstance = new VisualStudioInstance { DisplayName = "VS2022" };

        _vsManager.SelectVisualStudioInstanceAsync()
            .Returns(Task.FromResult<VisualStudioInstance?>(vsInstance));

        _extensionManager.GetExtensions(vsInstance, null)
            .Returns([]);

        _extDisplayHelper.PopulateExtensionsInfoFromMarketplaceAsync
        (
            [],
            vsInstance
        ).Returns(Task.CompletedTask);

        var root = new RootCommand { _command };

        // Act
        await root.InvokeAsync("upd");

        // Assert
        var output = _console.Output;
        output.ShouldContain("All extensions are up to date", Case.Insensitive);
    }

    [Fact]
    public async Task UpdateCommand_NoSelection_PrintsNoExtensionsSelected()
    {
        // Arrange
        _console.Interactive();
        var vsInstance = new VisualStudioInstance { DisplayName = "VS2022" };

        _vsManager.SelectVisualStudioInstanceAsync()
            .Returns(Task.FromResult<VisualStudioInstance?>(vsInstance));

        var outdated = new List<ExtensionInfo>
        {
            new()
            {
                Name = "Ext1",
                Id = "ext1",
                InstalledVersion = "1.0",
                LatestVersion = "2.0"
            }
        };

        _extensionManager.GetExtensions(vsInstance, null)
            .Returns(outdated);

        _console.Input.PushKey(ConsoleKey.Enter);

        var root = new RootCommand { _command };

        // Act
        await root.InvokeAsync("upd");

        // Assert
        var output = _console.Output;
        output.ShouldContain("No extensions selected for update", Case.Insensitive);
    }

    [Fact]
    public async Task UpdateCommand_WithSelection_UpdatesExtensions()
    {
        // Arrange
        _console.Interactive();
        var vsInstance = new VisualStudioInstance { DisplayName = "VS2022" };

        _vsManager.SelectVisualStudioInstanceAsync()
            .Returns(Task.FromResult<VisualStudioInstance?>(vsInstance));

        var outdated = new List<ExtensionInfo>
        {
            new()
            {
                Name = "Ext1",
                Id = "ext1",
                InstalledVersion = "1.0",
                LatestVersion = "2.0"
            }
        };

        _extensionManager.GetExtensions(vsInstance, null)
            .Returns(outdated);

        _extensionManager.UpdateExtensionAsync(outdated[0], vsInstance)
            .Returns(Task.FromResult("Updated!"));

        _console.Input.PushKey(ConsoleKey.Spacebar);
        _console.Input.PushKey(ConsoleKey.Enter);

        var root = new RootCommand { _command };

        // Act
        await root.InvokeAsync("upd");

        // Assert
        var output = _console.Output;
        output.ShouldContain("Updating extension", Case.Insensitive);
        output.ShouldContain("Extension 'Ext1' updated", Case.Insensitive);
        output.ShouldContain("Updated!", Case.Insensitive);
    }
}
