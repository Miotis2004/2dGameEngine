using System;
using System.Collections.Generic;
using System.Linq;

namespace _2dGameEngine.Services;

/// <summary>
/// Aggregates player-facing platform services used by runtime gameplay code.
/// </summary>
public sealed class GameServices
{
    private readonly Dictionary<string, AchievementState> _achievements = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, long> _leaderboardScores = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="GameServices"/> class.
    /// </summary>
    public GameServices(SaveDataService? saves = null, LocalizationService? localization = null)
    {
        Saves = saves ?? new SaveDataService();
        Localization = localization ?? new LocalizationService();
    }

    /// <summary>
    /// Raised when an achievement is unlocked for the first time.
    /// </summary>
    public event EventHandler<AchievementState>? AchievementUnlocked;

    /// <summary>
    /// Gets the save data service.
    /// </summary>
    public SaveDataService Saves { get; }

    /// <summary>
    /// Gets the localization service.
    /// </summary>
    public LocalizationService Localization { get; }

    /// <summary>
    /// Unlocks an achievement and returns its current state.
    /// </summary>
    public AchievementState UnlockAchievement(string id, string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        if (_achievements.TryGetValue(id, out AchievementState? existing))
        {
            return existing;
        }

        AchievementState state = new(id, displayName, DateTimeOffset.UtcNow);
        _achievements[id] = state;
        AchievementUnlocked?.Invoke(this, state);
        return state;
    }

    /// <summary>
    /// Gets all unlocked achievements.
    /// </summary>
    public IReadOnlyList<AchievementState> GetAchievements() => _achievements.Values.OrderBy(achievement => achievement.UnlockedAtUtc).ToArray();

    /// <summary>
    /// Records a high score for a leaderboard, keeping the best value submitted so far.
    /// </summary>
    public long SubmitScore(string leaderboardId, long score)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(leaderboardId);
        if (!_leaderboardScores.TryGetValue(leaderboardId, out long existing) || score > existing)
        {
            _leaderboardScores[leaderboardId] = score;
            return score;
        }

        return existing;
    }

    /// <summary>
    /// Gets the best score for a leaderboard, or zero when no score has been submitted.
    /// </summary>
    public long GetBestScore(string leaderboardId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(leaderboardId);
        return _leaderboardScores.TryGetValue(leaderboardId, out long score) ? score : 0;
    }
}

/// <summary>
/// Represents an unlocked achievement.
/// </summary>
public sealed record AchievementState(string Id, string DisplayName, DateTimeOffset UnlockedAtUtc);
