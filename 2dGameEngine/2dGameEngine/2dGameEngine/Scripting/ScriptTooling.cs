using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace _2dGameEngine.Scripting;

/// <summary>
/// Describes a script diagnostic produced by C# project tooling.
/// </summary>
public sealed record ScriptDiagnostic(ScriptDiagnosticSeverity Severity, string Message, string? FilePath = null, int? Line = null, int? Column = null)
{
    /// <summary>
    /// Formats the diagnostic for the editor console.
    /// </summary>
    public string ToConsoleMessage()
    {
        string location = string.IsNullOrWhiteSpace(FilePath) ? string.Empty : $" ({FilePath}{(Line is null ? string.Empty : $":{Line}")})";
        return $"Script {Severity}: {Message}{location}";
    }
}

/// <summary>
/// Severity levels for script diagnostics.
/// </summary>
public enum ScriptDiagnosticSeverity
{
    Info,
    Warning,
    Error,
}

/// <summary>
/// Captures the result of compiling or hot reloading the C# gameplay project.
/// </summary>
public sealed record ScriptToolingResult(bool Succeeded, IReadOnlyList<ScriptDiagnostic> Diagnostics, DateTimeOffset CompletedAt)
{
    public static ScriptToolingResult Success(IEnumerable<ScriptDiagnostic> diagnostics) => new(true, diagnostics.ToArray(), DateTimeOffset.UtcNow);

    public static ScriptToolingResult Failure(IEnumerable<ScriptDiagnostic> diagnostics) => new(false, diagnostics.ToArray(), DateTimeOffset.UtcNow);
}

/// <summary>
/// Represents a source-level breakpoint tracked by the editor before debugger integration attaches.
/// </summary>
public sealed record ScriptBreakpoint(string FilePath, int Line, bool Enabled = true, string? Condition = null);

/// <summary>
/// Stores C# script debugging settings, breakpoints, and hot-reload state for a project.
/// </summary>
public sealed class ScriptDebugSession
{
    private readonly List<ScriptBreakpoint> _breakpoints = [];

    public ScriptDebugSession(string projectDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectDirectory);
        ProjectDirectory = Path.GetFullPath(projectDirectory);
    }

    public string ProjectDirectory { get; }

    public string SettingsPath => Path.Combine(ProjectDirectory, "ProjectSettings", "ScriptTooling.json");

    public bool HotReloadEnabled { get; set; } = true;

    public string DebuggerTransport { get; set; } = "managed";

    public DateTimeOffset LastHotReloadAt { get; set; }

    public IReadOnlyList<ScriptBreakpoint> Breakpoints => _breakpoints;

    public void AddOrUpdateBreakpoint(string filePath, int line, string? condition = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (line <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(line), "Breakpoint line numbers are one-based.");
        }

        string normalized = NormalizeProjectPath(ProjectDirectory, filePath);
        int index = _breakpoints.FindIndex(breakpoint => string.Equals(breakpoint.FilePath, normalized, StringComparison.OrdinalIgnoreCase) && breakpoint.Line == line);
        ScriptBreakpoint breakpoint = new(normalized, line, Enabled: true, condition);
        if (index >= 0)
        {
            _breakpoints[index] = breakpoint;
        }
        else
        {
            _breakpoints.Add(breakpoint);
        }
    }

    public bool RemoveBreakpoint(string filePath, int line)
    {
        string normalized = NormalizeProjectPath(ProjectDirectory, filePath);
        return _breakpoints.RemoveAll(breakpoint => string.Equals(breakpoint.FilePath, normalized, StringComparison.OrdinalIgnoreCase) && breakpoint.Line == line) > 0;
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(new ScriptDebugSessionState(HotReloadEnabled, DebuggerTransport, LastHotReloadAt, _breakpoints.ToArray()), ScriptToolingJson.Options), Encoding.UTF8);
    }

    public static ScriptDebugSession Load(string projectDirectory)
    {
        ScriptDebugSession session = new(projectDirectory);
        if (!File.Exists(session.SettingsPath))
        {
            return session;
        }

        ScriptDebugSessionState? state = JsonSerializer.Deserialize<ScriptDebugSessionState>(File.ReadAllText(session.SettingsPath), ScriptToolingJson.Options);
        if (state is null)
        {
            return session;
        }

        session.HotReloadEnabled = state.HotReloadEnabled;
        session.DebuggerTransport = string.IsNullOrWhiteSpace(state.DebuggerTransport) ? "managed" : state.DebuggerTransport;
        session.LastHotReloadAt = state.LastHotReloadAt;
        session._breakpoints.AddRange(state.Breakpoints ?? []);
        return session;
    }

    internal static string NormalizeProjectPath(string projectDirectory, string path)
    {
        string fullPath = Path.IsPathRooted(path) ? Path.GetFullPath(path) : Path.GetFullPath(Path.Combine(projectDirectory, path));
        return Path.GetRelativePath(projectDirectory, fullPath).Replace(Path.DirectorySeparatorChar, '/');
    }

    private sealed record ScriptDebugSessionState(bool HotReloadEnabled, string DebuggerTransport, DateTimeOffset LastHotReloadAt, ScriptBreakpoint[]? Breakpoints);
}

/// <summary>
/// Runs C# script tooling commands for compile checks, hot reload preparation, and debugger metadata.
/// </summary>
public static class ScriptTooling
{
    private static readonly Regex MsBuildDiagnosticPattern = new(@"^(?<file>.*)\((?<line>\d+),(?<column>\d+)\):\s(?<severity>warning|error)\s(?<code>[^:]+):\s(?<message>.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static ScriptToolingResult CheckScripts(string solutionPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(solutionPath);
        return RunDotNet(solutionPath, "build", "-v:minimal");
    }

    public static ScriptToolingResult PrepareHotReload(string solutionPath, ScriptDebugSession session)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(solutionPath);
        ArgumentNullException.ThrowIfNull(session);
        if (!session.HotReloadEnabled)
        {
            return ScriptToolingResult.Success([new ScriptDiagnostic(ScriptDiagnosticSeverity.Info, "Hot reload is disabled for this project.")]);
        }

        ScriptToolingResult result = RunDotNet(solutionPath, "build", "-p:HotReloadEnabled=true", "-p:DebugType=portable", "-v:minimal");
        if (result.Succeeded)
        {
            session.LastHotReloadAt = result.CompletedAt;
            session.Save();
        }

        return result;
    }

    public static string CreateLaunchSettings(string projectDirectory, string projectSafeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectSafeName);
        string propertiesDirectory = Path.Combine(projectDirectory, "src", $"{projectSafeName}.Editor", "Properties");
        Directory.CreateDirectory(propertiesDirectory);
        string path = Path.Combine(propertiesDirectory, "launchSettings.json");
        var document = new
        {
            profiles = new Dictionary<string, object>
            {
                [$"{projectSafeName}.Editor"] = new
                {
                    commandName = "Project",
                    environmentVariables = new Dictionary<string, string>
                    {
                        ["UNITY2_SCRIPT_DEBUGGING"] = "1",
                        ["DOTNET_MODIFIABLE_ASSEMBLIES"] = "debug",
                    },
                    hotReloadEnabled = true,
                },
            },
        };
        File.WriteAllText(path, JsonSerializer.Serialize(document, ScriptToolingJson.Options), Encoding.UTF8);
        return path;
    }

    private static ScriptToolingResult RunDotNet(string solutionPath, params string[] arguments)
    {
        List<ScriptDiagnostic> diagnostics = [];
        if (!File.Exists(solutionPath))
        {
            return ScriptToolingResult.Failure([new ScriptDiagnostic(ScriptDiagnosticSeverity.Error, "Solution file was not found.", solutionPath)]);
        }

        ProcessStartInfo startInfo = new("dotnet")
        {
            WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(solutionPath))!,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.ArgumentList.Add(solutionPath);
        Process? startedProcess;
        try
        {
            startedProcess = Process.Start(startInfo);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException)
        {
            return ScriptToolingResult.Failure([new ScriptDiagnostic(ScriptDiagnosticSeverity.Error, $"Unable to start dotnet tooling process: {ex.Message}")]);
        }

        if (startedProcess is null)
        {
            return ScriptToolingResult.Failure([new ScriptDiagnostic(ScriptDiagnosticSeverity.Error, "Unable to start dotnet tooling process.")]);
        }

        using Process process = startedProcess;
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        diagnostics.AddRange(ParseDiagnostics(output));
        diagnostics.AddRange(ParseDiagnostics(error));
        if (process.ExitCode != 0 && diagnostics.All(diagnostic => diagnostic.Severity != ScriptDiagnosticSeverity.Error))
        {
            diagnostics.Add(new ScriptDiagnostic(ScriptDiagnosticSeverity.Error, $"dotnet {string.Join(' ', arguments)} exited with code {process.ExitCode}."));
        }

        if (diagnostics.Count == 0)
        {
            diagnostics.Add(new ScriptDiagnostic(ScriptDiagnosticSeverity.Info, $"dotnet {string.Join(' ', arguments)} completed successfully."));
        }

        return process.ExitCode == 0 ? ScriptToolingResult.Success(diagnostics) : ScriptToolingResult.Failure(diagnostics);
    }

    private static IEnumerable<ScriptDiagnostic> ParseDiagnostics(string text)
    {
        foreach (string line in text.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries))
        {
            Match match = MsBuildDiagnosticPattern.Match(line.Trim());
            if (!match.Success)
            {
                continue;
            }

            ScriptDiagnosticSeverity severity = match.Groups["severity"].Value.Equals("error", StringComparison.OrdinalIgnoreCase) ? ScriptDiagnosticSeverity.Error : ScriptDiagnosticSeverity.Warning;
            yield return new ScriptDiagnostic(severity, $"{match.Groups["code"].Value}: {match.Groups["message"].Value}", match.Groups["file"].Value, int.Parse(match.Groups["line"].Value), int.Parse(match.Groups["column"].Value));
        }
    }
}

internal static class ScriptToolingJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web) { WriteIndented = true };
}
