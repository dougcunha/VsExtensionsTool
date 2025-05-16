using System.CommandLine;
using System.Diagnostics;
using VsExtensionsTool;
using VsExtensionsTool.Commands;

var rootCommand = new RootCommand("VsExtensionsTool - Visual Studio Extensions Manager");
VisualStudioInstance? vsInstance = null;
rootCommand.AddCommand(new ListCommand(VsInstanceFactory));
rootCommand.AddCommand(new ListVsCommand());
rootCommand.AddCommand(new RemoveCommand(VsInstanceFactory));
rootCommand.AddCommand(new UpdateCommand(VsInstanceFactory));

await rootCommand.InvokeAsync(args);

if (Debugger.IsAttached)
{
    Console.WriteLine("Press any key to exit...");
    Console.Read();
}

Task<VisualStudioInstance?> VsInstanceFactory() 
    => vsInstance != null
        ? Task.FromResult<VisualStudioInstance?>(vsInstance)
        : VisualStudioManager.SelectVisualStudioInstanceAsync();