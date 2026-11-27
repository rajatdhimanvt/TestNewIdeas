using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel managing Daily Login Streak Rewards and physics-animated Lucky Spin Wheel.
/// Features rotational deceleration easing curves, sound triggers, and coin payouts.
/// </summary>
public class DailyRewardPanel : UIBasePanel
{
    private struct SpinSegment
    {
        public string label;
        public int coinReward;
        public float startAngle;
        public float endAngle;

        public SpinSegment(string lbl, int coins, float startA, float endA)
        {
            label = lbl;
            coinReward = coins;
            startAngle = startA;
            endAngle = endA;
        }
    }

    [Header("Spin Wheel Visuals")]
    [SerializeField] private Transform wheelTransform;
    [SerializeField] private Button spinButton;
    [SerializeField] private TMP_Text spinButtonText;
    [SerializeField] private TMP_Text statusText;

    [Header("Daily Streak Header")]
    [SerializeField] private TMP_Text streakDayText;
    [SerializeField] private Button claimStreakButton;
    [SerializeField] private TMP_Text claimStreakText;

    [Header("Navigation")]
    [SerializeField] private Button closeButton;

    private bool isSpinning = false;

    private readonly SpinSegment[] segments = new SpinSegment[]
    {
        new SpinSegment("50 COINS", 50, 0f, 60f),
        new SpinSegment("100 COINS", 100, 60f, 120f),
        new SpinSegment("200 COINS", 200, 120f, 180f),
        new SpinSegment("500 COINS", 500, 180f, 240f),
        new SpinSegment("1,000 JACKPOT!", 1000, 240f, 300f),
        new SpinSegment("150 COINS", 150, 300f, 360f)
    };

    private readonly int[] dailyStreakCoins = new int[] { 50, 100, 150, 200, 300, 500, 1000 };

    public override void Init(UIManager manager)
    {
        base.Init(manager);

        if (spinButton != null)
        {
            spinButton.onClick.AddListener(OnSpinClicked);
        }

        if (claimStreakButton != null)
        {
            claimStreakButton.onClick.AddListener(OnClaimStreakClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseClicked);
        }
    }

    public override void OnShow()
    {
        base.OnShow();
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (!PlayerDataManager.HasInstance) return;

        // 1. Daily Login Streak State
        int currentStreak = PlayerDataManager.Instance.GetDailyStreak();
        int nextStreakDay = (currentStreak % 7) + 1;
        int dayCoins = dailyStreakCoins[nextStreakDay - 1];

        if (streakDayText != null)
        {
            streakDayText.text = $"DAY {nextStreakDay} REWARD: +{dayCoins} COINS";
        }

        bool canClaimStreak = PlayerDataManager.Instance.CanClaimDailyReward();
        if (claimStreakButton != null)
        {
            claimStreakButton.interactable = canClaimStreak;
        }

        if (claimStreakText != null)
        {
            claimStreakText.text = canClaimStreak ? "CLAIM REWARD" : "CLAIMED TODAY";
        }

        // 2. Spin Wheel State
        bool canFreeSpin = PlayerDataManager.Instance.CanFreeSpin();
        if (spinButtonText != null)
        {
            spinButtonText.text = canFreeSpin ? "FREE SPIN!" : "SPIN (100 COINS)";
        }

        if (statusText != null)
        {
            statusText.text = canFreeSpin 
                ? "Your Daily Free Spin is Ready!" 
                : "Spin again for 100 Coins!";
        }
    }

    private void OnClaimStreakClicked()
    {
        if (!PlayerDataManager.HasInstance) return;

        if (PlayerDataManager.Instance.CanClaimDailyReward())
        {
            int currentStreak = PlayerDataManager.Instance.GetDailyStreak();
            int nextStreakDay = (currentStreak % 7) + 1;
            int coins = dailyStreakCoins[nextStreakDay - 1];

            PlayerDataManager.Instance.ClaimDailyReward(nextStreakDay, coins);
            RefreshDisplay();

            if (uiManager != null)
            {
                uiManager.ShowPopup("Streak Claimed!", $"Claimed Day {nextStreakDay} login reward: {coins} Coins!");
            }
        }
    }

    private void OnSpinClicked()
    {
        if (isSpinning || !PlayerDataManager.HasInstance) return;

        bool canFreeSpin = PlayerDataManager.Instance.CanFreeSpin();

        if (!canFreeSpin)
        {
            // Check if player has 100 coins for extra spin
            if (!PlayerDataManager.Instance.DeductCoins(100))
            {
                if (uiManager != null)
                {
                    uiManager.ShowPopup("Insufficient Coins", "You need 100 Coins to spin again!");
                }
                return;
            }
        }
        else
        {
            PlayerDataManager.Instance.RecordFreeSpin();
        }

        StartCoroutine(SpinWheelRoutine());
    }

    private IEnumerator SpinWheelRoutine()
    {
        isSpinning = true;
        if (spinButton != null) spinButton.interactable = false;

        // Pick random winning segment
        int selectedIndex = Random.Range(0, segments.Length);
        SpinSegment winningSegment = segments[selectedIndex];

        // Target angle logic (5 full spins + segment angle)
        float segmentCenterAngle = (winningSegment.startAngle + winningSegment.endAngle) / 2f;
        float totalRotation = (360f * 5f) + (360f - segmentCenterAngle);

        float duration = 4.0f;
        float elapsed = 0f;
        float startAngle = wheelTransform != null ? wheelTransform.eulerAngles.z : 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Deceleration Cubic Ease-Out
            float easeOut = 1.0f - Mathf.Pow(1.0f - t, 3);
            float currentAngle = Mathf.Lerp(startAngle, startAngle + totalRotation, easeOut);

            if (wheelTransform != null)
            {
                wheelTransform.rotation = Quaternion.Euler(0, 0, currentAngle);
            }

            yield return null;
        }

        if (wheelTransform != null)
        {
            wheelTransform.rotation = Quaternion.Euler(0, 0, startAngle + totalRotation);
        }

        // Award Coin Prize
        PlayerDataManager.Instance.AddCoins(winningSegment.coinReward);
        GameEvents.OnPlaySFX?.Invoke("ChestOpen");

        yield return new WaitForSeconds(0.3f);

        isSpinning = false;
        if (spinButton != null) spinButton.interactable = true;

        RefreshDisplay();

        if (uiManager != null)
        {
            uiManager.ShowPopup("JACKPOT WINNER!", $"You won {winningSegment.label} from the Lucky Wheel!");
        }
    }

    private void OnCloseClicked()
    {
        if (isSpinning) return;

        if (uiManager != null)
        {
            uiManager.HidePanel<DailyRewardPanel>();
        }
    }
}
