namespace VsExtensionsTool.Helpers;

/// <inheritdoc />
/// <summary>
/// Default implementation of <see cref="T:VsExtensionsTool.Helpers.IProcessRunner" /> using <see cref="T:System.Diagnostics.Process" />.
/// </summary>
public sealed class ProcessRunner : IProcessRunner
{
    /// <inheritdoc />
    public async Task<string> RunAsync(string fileName, string arguments)
    {
        using var process = new Process();

        process.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
        await process.WaitForExitAsync().ConfigureAwait(false);

        return output;
    }
}
