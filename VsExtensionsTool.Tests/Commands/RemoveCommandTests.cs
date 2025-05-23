namespace VsExtensionsTool.Tests.Commands;

/// <summary>
/// Integration tests for RemoveCommand.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class RemoveCommandTests
{
    private readonly RemoveCommand _command;
    private readonly IVisualStudioManager _vsManager = Substitute.For<IVisualStudioManager>();
    private readonly IExtensionManager _extensionManager = Substitute.For<IExtensionManager>();
    private readonly TestConsole _console = new();

    public RemoveCommandTests()
        => _command = new RemoveCommand
        (
            _vsManager,
            _extensionManager,
            _console
        );

    [Fact]
    public async Task RemoveCommand_NoVsInstance_PrintsNoInstanceMessage()
    {
        // Arrange
        _vsManager.SelectVisualStudioInstanceAsync()
            .Returns(Task.FromResult<VisualStudioInstance?>(null));

        var root = new RootCommand { _command };

        // Act
        await root.InvokeAsync("rm");

        // Assert
        var output = _console.Output;
        output.ShouldContain("No Visual Studio instance selected", Case.Insensitive);
    }

    [Fact]
    public async Task RemoveCommand_WithId_RemovesExtensionById()
    {
        // Arrange
        var vsInstance = new VisualStudioInstance { DisplayName = "VS2022" };

        _vsManager.SelectVisualStudioInstanceAsync()
            .Returns(Task.FromResult<VisualStudioInstance?>(vsInstance));

        var root = new RootCommand { _command };

        // Act
        await root.InvokeAsync("rm --id ext1");

        // Assert
        await _extensionManager.Received(1).RemoveExtensionByIdAsync(vsInstance, "ext1");
    }

    [Fact]
    public async Task RemoveCommand_NoExtensions_PrintsNoExtensionsMessage()
    {
        // Arrange
        var vsInstance = new VisualStudioInstance { DisplayName = "VS2022" };

        _vsManager.SelectVisualStudioInstanceAsync()
            .Returns(Task.FromResult<VisualStudioInstance?>(vsInstance));

        _extensionManager.GetExtensions(vsInstance, null)
            .Returns([]);

        var root = new RootCommand { _command };

        // Act
        await root.InvokeAsync("rm");

        // Assert
        var output = _console.Output;
        output.ShouldContain("No extensions found", Case.Insensitive);
    }

    [Fact]
    public async Task RemoveCommand_NoSelection_PrintsNoExtensionsSelected()
    {
        // Arrange
        _console.Interactive();
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

        _console.Input.PushKey(ConsoleKey.Enter); 

        var root = new RootCommand { _command };

        // Act
        await root.InvokeAsync("rm");

        // Assert
        var output = _console.Output;
        output.ShouldContain("No extensions selected for removal", Case.Insensitive);
    }

    [Fact]
    public async Task RemoveCommand_WithSelection_RemovesSelectedExtensions()
    {
        // Arrange
        _console.Interactive();
        var vsInstance = new VisualStudioInstance { DisplayName = "VS2022" };

        _vsManager.SelectVisualStudioInstanceAsync()
            .Returns(Task.FromResult<VisualStudioInstance?>(vsInstance));

        var extensions = new List<ExtensionInfo>
        {
            new() { Name = "Ext1", Id = "ext1", InstalledVersion = "1.0" },
            new() { Name = "Ext2", Id = "ext2", InstalledVersion = "2.0" }
        };

        _extensionManager.GetExtensions(vsInstance, null)
            .Returns(extensions);

        _console.Input.PushKey(ConsoleKey.Spacebar);
        _console.Input.PushKey(ConsoleKey.Enter); 

        var root = new RootCommand { _command };

        // Act
        await root.InvokeAsync("rm");

        // Assert
        await _extensionManager.Received(1).RemoveExtensionByIdAsync(vsInstance, "ext1");
        await _extensionManager.Received(1).RemoveExtensionByIdAsync(vsInstance, "ext2");
    }
}
