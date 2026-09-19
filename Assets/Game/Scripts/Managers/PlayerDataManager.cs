using System;
using UnityEngine;

/// <summary>
/// Central Manager handling player data persistence:
/// - In-game Coins balance & transactions
/// - Campaign Level progression & 3-Star ratings per level
/// - Ball Kit & Theme ownership / unlock state
/// - Daily Reward streak & Spin Wheel timer persistence
/// </summary>
public class PlayerDataManager : Singleton<PlayerDataManager>
{
    private const string PREF_COINS = "Player_Coins";
    private const string PREF_UNLOCKED_LEVEL = "Highest_Unlocked_Level";
    private const string PREF_STAR_PREFIX = "Level_Stars_";
    private const string PREF_SCORE_PREFIX = "Level_Score_";
    private const string PREF_KIT_OWNED_PREFIX = "Kit_Owned_";
    private const string PREF_THEME_OWNED_PREFIX = "Theme_Owned_";
    private const string PREF_DAILY_STREAK = "Daily_Streak_Count";
    private const string PREF_LAST_CLAIM_DATE = "Last_Claim_Date";
    private const string PREF_LAST_SPIN_DATE = "Last_Spin_Date";

    [Header("Starting Player Data")]
    [SerializeField] private int startingCoins = 200;

    public static Action<int> OnCoinsChanged;
    public static Action<int, int> OnLevelStarsUpdated; // (levelNumber, stars)

    public int Coins { get; private set; }
    public int HighestUnlockedLevel { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        if (DependencyManager.HasInstance)
        {
            DependencyManager.Instance.Register(this);
        }
        LoadData();
    }

    private void LoadData()
    {
        Coins = PlayerPrefs.GetInt(PREF_COINS, startingCoins);
        HighestUnlockedLevel = Mathf.Max(1, PlayerPrefs.GetInt(PREF_UNLOCKED_LEVEL, 1));

        // Ensure default Kit & Theme are owned
        SetKitOwned("classic_kit", true);
        SetThemeOwned("dark_theme", true);
    }

    #region Coins Economy

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;

        Coins += amount;
        PlayerPrefs.SetInt(PREF_COINS, Coins);
        PlayerPrefs.Save();

        Debug.Log($"[PlayerDataManager] Added {amount} coins. New balance: {Coins}");
        OnCoinsChanged?.Invoke(Coins);
    }

    public bool DeductCoins(int amount)
    {
        if (amount <= 0) return true;
        if (Coins < amount) return false;

        Coins -= amount;
        PlayerPrefs.SetInt(PREF_COINS, Coins);
        PlayerPrefs.Save();

        Debug.Log($"[PlayerDataManager] Deducted {amount} coins. New balance: {Coins}");
        OnCoinsChanged?.Invoke(Coins);
        return true;
    }

    #endregion

    #region Level Progression & 3-Star Rating

    public int GetLevelStars(int levelNum)
    {
        return PlayerPrefs.GetInt(PREF_STAR_PREFIX + levelNum, 0);
    }

    public int GetLevelHighScore(int levelNum)
    {
        return PlayerPrefs.GetInt(PREF_SCORE_PREFIX + levelNum, 0);
    }

    public bool IsLevelUnlocked(int levelNum)
    {
        return levelNum == 1 || levelNum <= HighestUnlockedLevel;
    }

    /// <summary>
    /// Evaluates performance and saves level completion results (Stars, Unlocks, Coins).
    /// Returns calculated star rating (1, 2, or 3).
    /// </summary>
    public int SaveLevelCompletion(int levelNum, int scoreAchieved, int dropsRemaining, int maxDrops, int targetScore)
    {
        // 1. Calculate Stars
        int stars = 1; // Passed target score
        float dropsRatio = maxDrops > 0 ? (float)dropsRemaining / maxDrops : 1f;

        if (scoreAchieved >= targetScore * 1.4f || dropsRatio >= 0.5f)
        {
            stars = 3;
        }
        else if (scoreAchieved >= targetScore * 1.15f || dropsRatio >= 0.25f)
        {
            stars = 2;
        }

        // Save Stars if higher than previous attempt
        int oldStars = GetLevelStars(levelNum);
        if (stars > oldStars)
        {
            PlayerPrefs.SetInt(PREF_STAR_PREFIX + levelNum, stars);
            OnLevelStarsUpdated?.Invoke(levelNum, stars);
        }

        // Save HighScore if higher
        int oldScore = GetLevelHighScore(levelNum);
        if (scoreAchieved > oldScore)
        {
            PlayerPrefs.SetInt(PREF_SCORE_PREFIX + levelNum, scoreAchieved);
        }

        // Unlock Next Level
        int nextLevel = levelNum + 1;
        if (nextLevel > HighestUnlockedLevel)
        {
            HighestUnlockedLevel = nextLevel;
            PlayerPrefs.SetInt(PREF_UNLOCKED_LEVEL, HighestUnlockedLevel);
        }

        // Reward Coins: Base 50 + Level bonus
        int bonusCoins = 50 + (levelNum * 15) + (stars * 20);
        AddCoins(bonusCoins);

        PlayerPrefs.Save();
        return stars;
    }

    public int GetTotalStarsEarned(int totalLevels = 50)
    {
        int total = 0;
        for (int i = 1; i <= totalLevels; i++)
        {
            total += GetLevelStars(i);
        }
        return total;
    }

    #endregion

    #region Kit & Theme Ownership

    public bool IsKitOwned(string kitId)
    {
        if (string.IsNullOrEmpty(kitId) || kitId == "classic_kit") return true;
        return PlayerPrefs.GetInt(PREF_KIT_OWNED_PREFIX + kitId, 0) == 1;
    }

    public void SetKitOwned(string kitId, bool owned = true)
    {
        PlayerPrefs.SetInt(PREF_KIT_OWNED_PREFIX + kitId, owned ? 1 : 0);
        PlayerPrefs.Save();
    }

    public bool UnlockKitWithCoins(string kitId, int coinCost)
    {
        if (IsKitOwned(kitId)) return true;

        if (DeductCoins(coinCost))
        {
            SetKitOwned(kitId, true);
            Debug.Log($"[PlayerDataManager] Successfully unlocked Kit '{kitId}' for {coinCost} coins!");
            return true;
        }
        return false;
    }

    public bool IsThemeOwned(string themeId)
    {
        if (string.IsNullOrEmpty(themeId) || themeId == "dark_theme") return true;
        return PlayerPrefs.GetInt(PREF_THEME_OWNED_PREFIX + themeId, 0) == 1;
    }

    public void SetThemeOwned(string themeId, bool owned = true)
    {
        PlayerPrefs.SetInt(PREF_THEME_OWNED_PREFIX + themeId, owned ? 1 : 0);
        PlayerPrefs.Save();
    }

    public bool UnlockThemeWithCoins(string themeId, int coinCost)
    {
        if (IsThemeOwned(themeId)) return true;

        if (DeductCoins(coinCost))
        {
            SetThemeOwned(themeId, true);
            Debug.Log($"[PlayerDataManager] Successfully unlocked Theme '{themeId}' for {coinCost} coins!");
            return true;
        }
        return false;
    }

    #endregion

    #region Daily Rewards & Spin Wheel Persistence

    public int GetDailyStreak()
    {
        return PlayerPrefs.GetInt(PREF_DAILY_STREAK, 0);
    }

    public bool CanClaimDailyReward()
    {
        string lastDate = PlayerPrefs.GetString(PREF_LAST_CLAIM_DATE, string.Empty);
        string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        return lastDate != today;
    }

    public void ClaimDailyReward(int dayIndex, int coinReward)
    {
        string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        PlayerPrefs.SetString(PREF_LAST_CLAIM_DATE, today);

        int currentStreak = GetDailyStreak();
        int nextStreak = (currentStreak % 7) + 1;
        PlayerPrefs.SetInt(PREF_DAILY_STREAK, nextStreak);

        AddCoins(coinReward);
        PlayerPrefs.Save();
    }

    public bool CanFreeSpin()
    {
        string lastSpin = PlayerPrefs.GetString(PREF_LAST_SPIN_DATE, string.Empty);
        string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        return lastSpin != today;
    }

    public void RecordFreeSpin()
    {
        string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        PlayerPrefs.SetString(PREF_LAST_SPIN_DATE, today);
        PlayerPrefs.Save();
    }

    #endregion
}
