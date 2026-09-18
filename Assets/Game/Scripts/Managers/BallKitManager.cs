using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages available Ball Kits, active equipped kit selection, and PlayerPrefs persistence.
/// </summary>
public class BallKitManager : Singleton<BallKitManager>
{
    private const string PREF_EQUIPPED_KIT_ID = "Equipped_Ball_Kit_ID";

    [Header("Available Ball Kits")]
    [SerializeField] private BallKitSO defaultKit;
    [SerializeField] private List<BallKitSO> availableKits = new List<BallKitSO>();

    [Header("Equipped State")]
    [SerializeField] private BallKitSO currentEquippedKit;

    public static Action<BallKitSO> OnBallKitChanged;

    public BallKitSO CurrentEquippedKit => currentEquippedKit;
    public List<BallKitSO> AvailableKits => availableKits;

    protected override void Awake()
    {
        base.Awake();
        if (DependencyManager.HasInstance)
        {
            DependencyManager.Instance.Register(this);
        }
    }

    private void Start()
    {
        LoadEquippedKit();
    }

    public void RegisterKits(List<BallKitSO> kits, BallKitSO defaultBallKit = null)
    {
        availableKits = kits ?? new List<BallKitSO>();
        if (defaultBallKit != null)
        {
            defaultKit = defaultBallKit;
        }

        LoadEquippedKit();
    }

    private void LoadEquippedKit()
    {
        string savedKitId = PlayerPrefs.GetString(PREF_EQUIPPED_KIT_ID, string.Empty);

        if (!string.IsNullOrEmpty(savedKitId))
        {
            BallKitSO foundKit = availableKits.Find(k => k != null && k.kitId == savedKitId);
            if (foundKit != null)
            {
                currentEquippedKit = foundKit;
                return;
            }
        }

        // Fallback to default kit or first available kit
        if (defaultKit != null)
        {
            currentEquippedKit = defaultKit;
        }
        else if (availableKits != null && availableKits.Count > 0)
        {
            currentEquippedKit = availableKits[0];
        }
    }

    /// <summary>
    /// Equips a specified Ball Kit asset and saves selection to PlayerPrefs.
    /// </summary>
    public void EquipKit(BallKitSO kit)
    {
        if (kit == null || currentEquippedKit == kit) return;

        currentEquippedKit = kit;
        PlayerPrefs.SetString(PREF_EQUIPPED_KIT_ID, kit.kitId);
        PlayerPrefs.Save();

        Debug.Log($"[BallKitManager] Successfully equipped Ball Kit: {kit.kitName} ({kit.kitId})");
        OnBallKitChanged?.Invoke(currentEquippedKit);
    }

    /// <summary>
    /// Equips a Ball Kit by its unique string ID.
    /// </summary>
    public bool EquipKit(string kitId)
    {
        return EquipKitById(kitId);
    }

    /// <summary>
    /// Equips a Ball Kit by its unique string ID.
    /// </summary>
    public bool EquipKitById(string kitId)
    {
        BallKitSO kit = availableKits.Find(k => k != null && k.kitId == kitId);
        if (kit != null)
        {
            EquipKit(kit);
            return true;
        }

        Debug.LogWarning($"[BallKitManager] Could not find kit with ID '{kitId}' to equip.");
        return false;
    }

    /// <summary>
    /// Returns next tier for current equipped kit.
    /// </summary>
    public BallDataSO GetNextTier(BallDataSO currentTier)
    {
        if (currentEquippedKit != null)
        {
            return currentEquippedKit.GetNextTier(currentTier);
        }
        return null;
    }

    /// <summary>
    /// Returns a random droppable tier for current equipped kit.
    /// </summary>
    public BallDataSO GetRandomDroppableTier()
    {
        if (currentEquippedKit != null)
        {
            return currentEquippedKit.GetRandomDroppableTier();
        }
        return null;
    }
}
