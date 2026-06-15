using System;

namespace _2dGameEngine.Performance;

public sealed record MemorySnapshot(
    DateTimeOffset CapturedAt,
    string Reason,
    long ManagedBytes,
    long TotalCommittedBytes,
    long HeapSizeBytes,
    long FragmentedBytes,
    int Gen0Collections,
    int Gen1Collections,
    int Gen2Collections)
{
    public static MemorySnapshot Capture(string reason)
    {
        GCMemoryInfo memoryInfo = GC.GetGCMemoryInfo();
        return new MemorySnapshot(
            DateTimeOffset.UtcNow,
            reason,
            GC.GetTotalMemory(false),
            memoryInfo.TotalCommittedBytes,
            memoryInfo.HeapSizeBytes,
            memoryInfo.FragmentedBytes,
            GC.CollectionCount(0),
            GC.CollectionCount(1),
            GC.CollectionCount(2));
    }
}
