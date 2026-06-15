using System;
using System.Collections.Generic;

namespace _2dGameEngine.Performance;

public sealed record ProfilerFrameSample(
    ulong FrameNumber,
    TimeSpan DeltaTime,
    TimeSpan FrameTime,
    int EntityCount,
    long AllocatedBytes,
    MemorySnapshot Memory,
    IReadOnlyList<KeyValuePair<string, TimeSpan>> Sections)
{
    public double FramesPerSecond => DeltaTime.TotalSeconds <= 0.0 ? 0.0 : 1.0 / DeltaTime.TotalSeconds;
}
