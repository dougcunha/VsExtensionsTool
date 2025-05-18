using System.Diagnostics.CodeAnalysis;
using NSubstitute;
using Shouldly;
using System.IO.Abstractions;
using VsExtensionsTool.Helpers;
using VsExtensionsTool.Managers;
using VsExtensionsTool.Models;
using Spectre.Console.Testing;
using System.Xml.Linq;
using WireMock.Server;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace VsExtensionsTool.Tests;

/// <summary>
/// Unit tests for ExtensionManager.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ExtensionManagerTests
{
    private readonly TestConsole _console = new();
    private readonly IMarketplaceHelper _marketplaceHelper = Substitute.For<IMarketplaceHelper>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly IProcessRunner _processRunner = Substitute.For<IProcessRunner>();
    private readonly IXDocumentLoader _xDocumentLoader = Substitute.For<IXDocumentLoader>();
    private readonly XDocument _packageManifest;
    private readonly ExtensionManager _manager;

    public ExtensionManagerTests()
    {
        _packageManifest = XDocument.Load("Xml\\PackageManifest.xml");
        _manager = new ExtensionManager(_console, _marketplaceHelper, _fileSystem, _processRunner, _xDocumentLoader);
    }

    [Fact]
    public void GetExtensions_NoExtensions_ReturnsEmptyList()
    {
        // Arrange
        var instance = new VisualStudioInstance { InstallationPath = "C:/VS" };
        _fileSystem.Directory.Exists(Arg.Any<string>()).Returns(false);
        _fileSystem.Directory.GetDirectories(Arg.Any<string>()).Returns([]);

        // Act
        var result = _manager.GetExtensions(instance);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task RemoveExtensionById_VsixInstallerNotFound_PrintsError()
    {
        // Arrange
        var instance = new VisualStudioInstance { InstallationPath = "C:/VS" };
        _fileSystem.File.Exists(Arg.Any<string>()).Returns(false);

        // Act
        await _manager.RemoveExtensionByIdAsync(instance, "ext1");

        // Assert
        _console.Output.ShouldContain("VSIXInstaller.exe not found");
    }

    [Fact]
    public async Task RemoveExtensionById_ExtensionNotFound_PrintsError()
    {
        // Arrange
        var instance = new VisualStudioInstance { InstallationPath = "C:/VS" };
        _fileSystem.File.Exists(Arg.Any<string>()).Returns(true);

        // Act
        await _manager.RemoveExtensionByIdAsync(instance, "notfound");

        // Assert
        _console.Output.ShouldContain("Extension not found");
    }

    [Fact]
    public void GetExtensions_WithManifest_AddsExtension()
    {
        // Arrange
        var instance = new VisualStudioInstance
        {
            InstallationPath = "C:/VS",
            InstanceId = "abc",
            ChannelId = "Preview",
            InstallationVersion = "17.0"
        };

        var extDir = "C:/VS/dir1";
        var manifestPath = Path.Combine(extDir, "extension.vsixmanifest");
        _fileSystem.Directory.Exists(Arg.Any<string>()).Returns(true);
        _fileSystem.Directory.GetDirectories(Arg.Any<string>()).Returns([extDir]);
        _fileSystem.File.Exists(manifestPath).Returns(true);
        _xDocumentLoader.Load(manifestPath).Returns(_packageManifest);

        // Act
        var result = _manager.GetExtensions(instance);

        // Assert
        result.Count.ShouldBe(2); 
    }

    [Fact]
    public void GetExtensions_WithFilter_FiltersExtensions()
    {
        // Arrange
        var instance = new VisualStudioInstance
        {
            InstallationPath = "C:/VS",
            InstanceId = "abc",
            ChannelId = "Preview",
            InstallationVersion = "17.0"
        };

        var extDir = "C:/VS/dir1";
        var manifestPath = Path.Combine(extDir, "extension.vsixmanifest");
        _fileSystem.Directory.Exists(Arg.Any<string>()).Returns(true);
        _fileSystem.Directory.GetDirectories(Arg.Any<string>()).Returns([extDir]);
        _fileSystem.File.Exists(manifestPath).Returns(true);
        _xDocumentLoader.Load(manifestPath).Returns(_packageManifest);

        // Act
        var result = _manager.GetExtensions(instance, "2022");

        // Assert
        result.Count.ShouldBe(2); 
    }

    [Fact]
    public async Task RemoveExtensionByIdAsync_RemovesExtensionAndPrintsMessages()
    {
        // Arrange
        var instance = new VisualStudioInstance { InstallationPath = "C:/VS", InstanceId = "abc" };
        var extDir = "C:/VS/dir1";
        var manifestPath = Path.Combine(extDir, "extension.vsixmanifest");
        _fileSystem.Directory.Exists(Arg.Any<string>()).Returns(true);
        _fileSystem.Directory.GetDirectories(Arg.Any<string>()).Returns([extDir]);
        _fileSystem.File.Exists(manifestPath).Returns(true);
        _xDocumentLoader.Load(manifestPath).Returns(_packageManifest);

        _fileSystem.File.Exists(Arg.Is<string>(s => s.Contains("VSIXInstaller.exe"))).Returns(true);
        _processRunner.RunAsync(Arg.Any<string>(), Arg.Any<string>()).Returns("removed");

        // Act
        await _manager.RemoveExtensionByIdAsync(instance, "0c8bd9fa-77d5-4563-ab57-9e01608c3d05");

        // Assert
        _console.Output.ShouldContain("removed");
    }

    [Fact]
    public async Task UpdateExtensionAsync_VsixInstallerNotFound_PrintsErrorAndReturnsEmpty()
    {
        // Arrange       
        using var server = WireMockServer.Start(); 
        var fakeUrl = server.Url + "/vsix";
        var instance = new VisualStudioInstance { InstallationPath = "C:/VS", InstanceId = "abc" };
        var ext = new ExtensionInfo { Name = "TestExt", Id = "ext1", InstalledVersion = "1.0", VsixUrl = fakeUrl };
        _fileSystem.File.Exists(Arg.Any<string>()).Returns(false);

        server.Given(Request.Create().WithPath("/vsix").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("FAKE_VSIX"));

        // Act
        var result = await _manager.UpdateExtensionAsync(ext, instance);

        // Assert
        result.ShouldBeEmpty();
        _console.Output.ShouldContain("VSIXInstaller.exe not found");
    }

    [Fact]
    public async Task UpdateExtensionAsync_SuccessfulUpdate_DeletesVsix()
    {
        // Arrange
        using var server = WireMockServer.Start(); 
        var fakeUrl = server.Url + "/vsix";
        var instance = new VisualStudioInstance { InstallationPath = "C:/VS", InstanceId = "abc" };
        var ext = new ExtensionInfo { Name = "TestExt", Id = "ext1", InstalledVersion = "1.0", VsixUrl = fakeUrl };
        _fileSystem.File.Exists(Arg.Any<string>()).Returns(true);
        _processRunner.RunAsync(Arg.Any<string>(), Arg.Any<string>()).Returns("updated");

        server.Given(Request.Create().WithPath("/vsix").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("FAKE_VSIX"));

        // Act
        var result = await _manager.UpdateExtensionAsync(ext, instance);

        // Assert
        result.ShouldBe("updated");
    }
}
