using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace _2dGameEngine.Services;

/// <summary>
/// Resolves localized strings from culture-specific tables with fallback support.
/// </summary>
public sealed class LocalizationService
{
    private readonly Dictionary<string, Dictionary<string, string>> _tables = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalizationService"/> class.
    /// </summary>
    public LocalizationService(string defaultCulture = "en-US")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultCulture);
        DefaultCulture = NormalizeCulture(defaultCulture);
        CurrentCulture = DefaultCulture;
    }

    /// <summary>
    /// Raised after <see cref="CurrentCulture"/> changes.
    /// </summary>
    public event EventHandler? CultureChanged;

    /// <summary>
    /// Gets the fallback culture used when a key is missing from the current culture.
    /// </summary>
    public string DefaultCulture { get; }

    /// <summary>
    /// Gets the active culture used by <see cref="GetString"/>.
    /// </summary>
    public string CurrentCulture { get; private set; }

    /// <summary>
    /// Sets the active culture for future lookups.
    /// </summary>
    public void SetCulture(string cultureName)
    {
        string normalized = NormalizeCulture(cultureName);
        if (StringComparer.OrdinalIgnoreCase.Equals(CurrentCulture, normalized))
        {
            return;
        }

        CurrentCulture = normalized;
        CultureChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Adds or replaces a localization table for a culture.
    /// </summary>
    public void AddTable(string cultureName, IReadOnlyDictionary<string, string> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        _tables[NormalizeCulture(cultureName)] = new Dictionary<string, string>(entries, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Loads a JSON object containing key/value localized strings for a culture.
    /// </summary>
    public void LoadJsonTable(string cultureName, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Dictionary<string, string> entries = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))
            ?? throw new InvalidDataException($"Localization table '{path}' is empty or invalid.");
        AddTable(cultureName, entries);
    }

    /// <summary>
    /// Gets a localized string for the current culture, falling back to the default culture and then the key itself.
    /// </summary>
    public string GetString(string key, params object[] args)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        string value = TryGetString(CurrentCulture, key)
            ?? TryGetString(DefaultCulture, key)
            ?? key;
        return args.Length == 0 ? value : string.Format(CultureInfo.GetCultureInfo(CurrentCulture), value, args);
    }

    private string? TryGetString(string cultureName, string key)
    {
        return _tables.TryGetValue(cultureName, out Dictionary<string, string>? table) && table.TryGetValue(key, out string? value)
            ? value
            : null;
    }

    private static string NormalizeCulture(string cultureName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cultureName);
        return CultureInfo.GetCultureInfo(cultureName).Name;
    }
}
