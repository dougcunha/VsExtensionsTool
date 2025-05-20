using VsExtensionsTool.Commands;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Abstractions;

var builder = Host.CreateApplicationBuilder(args);
var services = builder.Services;
var console = AnsiConsole.Console;
services.AddSingleton(console);
services.AddSingleton<IExtensionListDisplayHelper, ExtensionListDisplayHelper>();
services.AddSingleton<IMarketplaceHelper, MarketplaceHelper>();
services.AddSingleton<IVisualStudioDisplayHelper, VisualStudioDisplayHelper>();
services.AddSingleton<IVisualStudioManager, VisualStudioManager>();
services.AddSingleton<IExtensionManager, ExtensionManager>();
services.AddTransient<IFileSystem, FileSystem>();
services.AddTransient<IProcessRunner, ProcessRunner>();
services.AddTransient<ListCommand>();
services.AddTransient<ListVsCommand>();
services.AddTransient<RemoveCommand>();
services.AddTransient<UpdateCommand>();
services.AddTransient<IXDocumentLoader, XDocumentLoader>();

using var host = builder.Build();
await host.StartAsync().ConfigureAwait(false);

var rootCommand = new RootCommand("VsExtensionsTool - Visual Studio Extensions Manager");

rootCommand.AddCommand(host.Services.GetRequiredService<ListCommand>());
rootCommand.AddCommand(host.Services.GetRequiredService<ListVsCommand>());
rootCommand.AddCommand(host.Services.GetRequiredService<RemoveCommand>());
rootCommand.AddCommand(host.Services.GetRequiredService<UpdateCommand>());

await rootCommand.InvokeAsync(args).ConfigureAwait(false);

if (Debugger.IsAttached)
{
    Console.WriteLine("Press any key to exit...");
    Console.Read();
}