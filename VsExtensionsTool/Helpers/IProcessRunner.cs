namespace VsExtensionsTool.Helpers;

/// <summary>
/// Abstraction for running external processes, to facilitate unit testing.
/// </summary>
public interface IProcessRunner
{
    /// <summary>
    /// Runs a process asynchronously and returns the standard output.
    /// </summary>
    /// <param name="fileName">The executable to run.</param>
    /// <param name="arguments">The arguments to pass to the executable.</param>
    /// <returns>The standard output of the process.</returns>
    Task<string> RunAsync(string fileName, string arguments);
}