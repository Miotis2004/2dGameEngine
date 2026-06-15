using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace _2dGameEngine.Services;

/// <summary>
/// Provides slot-based JSON persistence for game progress, preferences, and other runtime data.
/// </summary>
public sealed class SaveDataService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="SaveDataService"/> class.
    /// </summary>
    /// <param name="rootDirectory">Directory where save slot files are stored.</param>
    public SaveDataService(string? rootDirectory = null)
    {
        RootDirectory = string.IsNullOrWhiteSpace(rootDirectory)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Unity2Clone", "Saves")
            : rootDirectory;
    }

    /// <summary>
    /// Gets the directory where save slot files are stored.
    /// </summary>
    public string RootDirectory { get; }

    /// <summary>
    /// Saves a strongly typed payload to a named slot.
    /// </summary>
    public SaveSlotMetadata Save<T>(string slotName, T data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slotName);
        ArgumentNullException.ThrowIfNull(data);
        Directory.CreateDirectory(RootDirectory);

        SaveEnvelope<T> envelope = new(1, slotName, DateTimeOffset.UtcNow, data);
        string path = GetSlotPath(slotName);
        File.WriteAllText(path, JsonSerializer.Serialize(envelope, JsonOptions));
        return new SaveSlotMetadata(slotName, path, envelope.SavedAtUtc);
    }

    /// <summary>
    /// Loads a strongly typed payload from a named slot, or returns <see langword="default"/> when the slot does not exist.
    /// </summary>
    public T? Load<T>(string slotName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slotName);
        string path = GetSlotPath(slotName);
        if (!File.Exists(path))
        {
            return default;
        }

        SaveEnvelope<T> envelope = JsonSerializer.Deserialize<SaveEnvelope<T>>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidDataException($"Save slot '{slotName}' is empty or invalid.");
        return envelope.Data;
    }

    /// <summary>
    /// Deletes a named save slot when it exists.
    /// </summary>
    public bool Delete(string slotName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slotName);
        string path = GetSlotPath(slotName);
        if (!File.Exists(path))
        {
            return false;
        }

        File.Delete(path);
        return true;
    }

    /// <summary>
    /// Enumerates available save slots with their last write timestamps.
    /// </summary>
    public IReadOnlyList<SaveSlotMetadata> ListSlots()
    {
        if (!Directory.Exists(RootDirectory))
        {
            return [];
        }

        List<SaveSlotMetadata> slots = [];
        foreach (string path in Directory.EnumerateFiles(RootDirectory, "*.save.json"))
        {
            string slotName = Path.GetFileName(path)[..^".save.json".Length];
            slots.Add(new SaveSlotMetadata(slotName, path, File.GetLastWriteTimeUtc(path)));
        }

        return slots;
    }

    private string GetSlotPath(string slotName)
    {
        string safeName = string.Join("_", slotName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        if (string.IsNullOrWhiteSpace(safeName))
        {
            throw new ArgumentException("Save slot name must contain at least one valid file-name character.", nameof(slotName));
        }

        return Path.Combine(RootDirectory, $"{safeName}.save.json");
    }

    private sealed record SaveEnvelope<T>(int SchemaVersion, string SlotName, DateTimeOffset SavedAtUtc, T Data);
}

/// <summary>
/// Describes a save slot on disk.
/// </summary>
public sealed record SaveSlotMetadata(string SlotName, string Path, DateTimeOffset SavedAtUtc);
