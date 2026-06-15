using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace _2dGameEngine.Testing;

public sealed record EditorTestResult(int ExitCode, string Output, string Error)
{
    public bool Succeeded => ExitCode == 0;
}

public static class EditorTestRunner
{
    public static async Task<EditorTestResult> RunDotNetTestsAsync(string workingDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);
        using Process process = new()
        {
            StartInfo = new ProcessStartInfo("dotnet", "test --nologo")
            {
                WorkingDirectory = Directory.Exists(workingDirectory) ? workingDirectory : AppContext.BaseDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };
        StringBuilder output = new();
        StringBuilder error = new();
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) output.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) error.AppendLine(e.Data); };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();
        return new EditorTestResult(process.ExitCode, output.ToString(), error.ToString());
    }
}
