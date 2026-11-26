using System;
using UnityEngine;
/// <summary>
/// Global type-safe event channels for decoupled game communication.
/// Systems, Managers, UI, and Gameplay subscribe to events without direct coupling.
/// </summary>
public static class GameEvents
{
    // Game Lifecycle Events
    public static Action<GameState, GameState> OnGameStateChanged;
    public static Action<bool> OnGamePaused;

    // Progression / Gameplay Events
    public static Action<int> OnScoreChanged;
    public static Action<int, Vector3> OnScoreAddedAtPosition; // (scorePoints, worldPosition)
    public static Action<int> OnHighScoreChanged;
    public static Action OnBallDropped;
    public static Action OnNextBallUpdated;
    public static Action<int> OnRemainingDropsChanged; // remaining drops count
    public static Action<int> OnBonusDropsEarned; // extra drops awarded from special blocks
    public static Action<int> OnLevelStarted;
    public static Action<int, bool> OnLevelCompleted; // levelIndex, isSuccess

    // Merge & Block Destruction Events
    public static Action<Vector3, float, int> OnBallsMerged; // (midpoint, mergedBallRadius, mergedTier)
    public static Action<int, Vector3> OnComboAchieved; // (comboCount, worldPosition)
    public static Action<int, int> OnBlocksRemainingChanged; // (remainingTargets, totalTargets)
    public static Action<DestructibleBlock> OnBlockDestroyed;

    // Audio Events
    public static Action<string> OnPlaySFX;
    public static Action<string, float> OnPlaySFXPitched;
    public static Action<string> OnPlayMusic;

    // UI Events
    public static Action<string> OnShowToast;

    /// <summary>
    /// Resets all subscriptions. Useful on complete game resets or scene teardowns if needed.
    /// </summary>
    public static void ClearAllSubscriptions()
    {
        OnGameStateChanged = null;
        OnGamePaused = null;
        OnScoreChanged = null;
        OnScoreAddedAtPosition = null;
        OnHighScoreChanged = null;
        OnBallDropped = null;
        OnNextBallUpdated = null;
        OnRemainingDropsChanged = null;
        OnBonusDropsEarned = null;
        OnLevelStarted = null;
        OnLevelCompleted = null;
        OnBallsMerged = null;
        OnComboAchieved = null;
        OnBlocksRemainingChanged = null;
        OnBlockDestroyed = null;
        OnPlaySFX = null;
        OnPlaySFXPitched = null;
        OnPlayMusic = null;
        OnShowToast = null;
    }
}
