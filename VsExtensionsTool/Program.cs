using VsExtensionsTool.Commands;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Abstractions;
using System.Net.Http;
using Microsoft.Extensions.Http; // Necessário para AddHttpClient

var services = new ServiceCollection();
var console = AnsiConsole.Console;
services.AddSingleton(console);
services.AddSingleton<IExtensionListDisplayHelper, ExtensionListDisplayHelper>();
services.AddHttpClient<IMarketplaceHelper, MarketplaceHelper>();
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

var serviceProvider = services.BuildServiceProvider();

var rootCommand = new RootCommand("VsExtensionsTool - Visual Studio Extensions Manager");

rootCommand.AddCommand(serviceProvider.GetRequiredService<ListCommand>());
rootCommand.AddCommand(serviceProvider.GetRequiredService<ListVsCommand>());
rootCommand.AddCommand(serviceProvider.GetRequiredService<RemoveCommand>());
rootCommand.AddCommand(serviceProvider.GetRequiredService<UpdateCommand>());

await rootCommand.InvokeAsync(args).ConfigureAwait(false);

if (Debugger.IsAttached)
{
    Console.WriteLine("Press any key to exit...");
    Console.Read();
}