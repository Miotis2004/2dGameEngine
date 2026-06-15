using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace _2dGameEngine.Performance;

/// <summary>
/// Captures frame timing, named sample durations, and managed memory telemetry for editor diagnostics.
/// </summary>
public sealed class PerformanceProfiler
{
    private const int DefaultCapacity = 180;
    private readonly int _capacity;
    private readonly Queue<ProfilerFrameSample> _history = new();
    private readonly Stopwatch _frameStopwatch = new();
    private readonly Dictionary<string, TimeSpan> _sections = new(StringComparer.Ordinal);
    private long _frameStartAllocatedBytes;
    private MemorySnapshot _lastMemorySnapshot = MemorySnapshot.Capture("Startup");

    public PerformanceProfiler(int capacity = DefaultCapacity)
    {
        _capacity = Math.Max(1, capacity);
    }

    public IReadOnlyCollection<ProfilerFrameSample> History => _history;

    public ProfilerFrameSample? LatestFrame { get; private set; }

    public MemorySnapshot LastMemorySnapshot => _lastMemorySnapshot;

    public double AverageFrameMilliseconds => _history.Count == 0 ? 0.0 : _history.Average(sample => sample.FrameTime.TotalMilliseconds);

    public double PeakFrameMilliseconds => _history.Count == 0 ? 0.0 : _history.Max(sample => sample.FrameTime.TotalMilliseconds);

    public void BeginFrame(ulong frameNumber)
    {
        _sections.Clear();
        _frameStartAllocatedBytes = GC.GetAllocatedBytesForCurrentThread();
        _frameStopwatch.Restart();
    }

    public IDisposable Measure(string name) => new ProfilerScope(this, name);

    public ProfilerFrameSample EndFrame(ulong frameNumber, TimeSpan deltaTime, int entityCount)
    {
        _frameStopwatch.Stop();
        long allocatedBytes = Math.Max(0L, GC.GetAllocatedBytesForCurrentThread() - _frameStartAllocatedBytes);
        _lastMemorySnapshot = MemorySnapshot.Capture("Frame");
        ProfilerFrameSample sample = new(frameNumber, deltaTime, _frameStopwatch.Elapsed, entityCount, allocatedBytes, _lastMemorySnapshot, _sections.ToArray());
        _history.Enqueue(sample);
        while (_history.Count > _capacity)
        {
            _history.Dequeue();
        }

        LatestFrame = sample;
        return sample;
    }

    public MemorySnapshot CaptureMemorySnapshot(string reason)
    {
        _lastMemorySnapshot = MemorySnapshot.Capture(reason);
        return _lastMemorySnapshot;
    }

    public MemorySnapshot ForceCollection(string reason)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        return CaptureMemorySnapshot(reason);
    }

    private void RecordSection(string name, TimeSpan elapsed)
    {
        _sections[name] = _sections.TryGetValue(name, out TimeSpan existing) ? existing + elapsed : elapsed;
    }

    private sealed class ProfilerScope : IDisposable
    {
        private readonly PerformanceProfiler _profiler;
        private readonly string _name;
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private bool _disposed;

        public ProfilerScope(PerformanceProfiler profiler, string name)
        {
            _profiler = profiler;
            _name = name;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _stopwatch.Stop();
            _profiler.RecordSection(_name, _stopwatch.Elapsed);
            _disposed = true;
        }
    }
}
