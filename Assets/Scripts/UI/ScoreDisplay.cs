using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the score with the Tappy Plane digit sprites. The shown number rolls up towards the real score instead of jumping,
/// pulses a little every <see cref="milestoneStep"/> points while the digits change colour in a wave, and gives a bigger bump when a bonus is collected.
/// It only reads from <see cref="ScoreKeeper"/>; the digit Images are ready-made children (left to right), unused ones are hidden.
/// </summary>
public class ScoreDisplay : MonoBehaviour
{
    private const int MaxScore = 9999999;

    [SerializeField] private ScoreKeeper scoreKeeper;
    [Tooltip("Sprites for 0 to 9, in that order.")]
    [SerializeField] private Sprite[] digitSprites = new Sprite[10];
    [Tooltip("The digit Images, left to right. As many as the biggest number you want to show.")]
    [SerializeField] private Image[] digits;
    [Tooltip("Scales the digit sprites (1 = the sprite's own pixel size).")]
    [SerializeField, Min(0.1f)] private float digitScale = 1.25f;
    [Tooltip("The part that grows and shrinks for the bump and the pulse. Usually the parent of the digits.")]
    [SerializeField] private RectTransform animated;

    [Header("Rolling number")]
    [Tooltip("The shown number never moves slower than this (points per second), so the steady flow of distance looks smooth.")]
    [SerializeField, Min(1f)] private float minRollSpeed = 25f;
    [Tooltip("A jump (for example a bonus) is counted up in about this many seconds.")]
    [SerializeField, Min(0.05f)] private float catchUpSeconds = 0.3f;

    [Tooltip("0 = when switched on, show the current score right away (the HUD). More than 0 = start from 0 and count up to the current score in about this many seconds (the Game Over panel).")]
    [SerializeField, Min(0f)] private float countUpSeconds;

    [Header("Bonus bump")]
    [Tooltip("Extra size at the peak of the bump, as a share (0.25 = 25% bigger).")]
    [SerializeField, Min(0f)] private float bumpSize = 0.25f;
    [SerializeField, Min(0.05f)] private float bumpSeconds = 0.2f;

    [Header("Milestone (every N points)")]
    [SerializeField, Min(1)] private int milestoneStep = 100;
    [Tooltip("Extra size at the peak of the milestone pulse (0.08 = 8% bigger).")]
    [SerializeField, Min(0f)] private float pulseSize = 0.08f;
    [SerializeField, Min(0.05f)] private float pulseSeconds = 0.35f;
    [Tooltip("Colour of each hundred, in order, looping. The first one is used from 0 to 99 (white keeps the sprites' own colours).")]
    [SerializeField] private Color[] levelColors = { Color.white };
    [Tooltip("How long a digit takes to change to the new colour.")]
    [SerializeField, Min(0.05f)] private float colorFadeSeconds = 1f;
    [Tooltip("Delay between one digit and the next (left to right) when the colour changes, so it runs through the number like a wave.")]
    [SerializeField, Min(0f)] private float colorWaveDelay = 0.1f;

    [field: SerializeField, DebugReadOnly] public float DisplayedScore { get; private set; }
    [field: SerializeField, DebugReadOnly] public int Level { get; private set; }

    private int target;
    private float countUpSpeed;
    private int shownNumber = -1;
    private Color previousColor;
    private Color currentColor;
    private float colorTimer;
    private float bumpTimer;
    private float pulseTimer;

    private void Awake()
    {
        DisplayedScore = 0f;
        Level = 0;
        currentColor = ColorForLevel(0);
        previousColor = currentColor;
        colorTimer = float.MaxValue;
        bumpTimer = float.MaxValue;
        pulseTimer = float.MaxValue;
        Show(0);
    }

    private void OnEnable()
    {
        scoreKeeper.ScoreChanged += HandleScoreChanged;
        scoreKeeper.BonusEarned += HandleBonusEarned;

        // Catch up with the score that already exists: this display may be switched on after the run (Game Over panel).
        target = Mathf.Min(scoreKeeper.Score, MaxScore);
        if (countUpSeconds > 0f)
        {
            DisplayedScore = 0f;
            countUpSpeed = target / countUpSeconds;
        }
        else
        {
            DisplayedScore = target;
            countUpSpeed = 0f;
        }
    }

    private void OnDisable()
    {
        scoreKeeper.ScoreChanged -= HandleScoreChanged;
        scoreKeeper.BonusEarned -= HandleBonusEarned;
    }

    private void HandleScoreChanged(int score)
    {
        target = Mathf.Min(score, MaxScore);
    }

    private void HandleBonusEarned(int points)
    {
        bumpTimer = 0f;
    }

    private void Update()
    {
        float gap = target - DisplayedScore;
        if (gap > 0f)
        {
            float speed = countUpSpeed > 0f ? countUpSpeed : Mathf.Max(minRollSpeed, gap / catchUpSeconds);
            DisplayedScore = Mathf.MoveTowards(DisplayedScore, target, speed * Time.deltaTime);
        }

        int number = Mathf.FloorToInt(DisplayedScore);
        if (number != shownNumber)
        {
            Show(number);
        }

        int level = number / milestoneStep;
        if (level > Level)
        {
            Level = level;
            previousColor = currentColor;
            currentColor = ColorForLevel(level);
            colorTimer = 0f;
            pulseTimer = 0f;
        }

        colorTimer += Time.deltaTime;
        bumpTimer += Time.deltaTime;
        pulseTimer += Time.deltaTime;
        ApplyColors();
        ApplyScale();
    }

    private Color ColorForLevel(int level)
    {
        return levelColors.Length == 0 ? Color.white : levelColors[level % levelColors.Length];
    }

    /// <summary>Puts the number into the digit Images (right-aligned in the available Images, extra ones hidden).</summary>
    private void Show(int number)
    {
        shownNumber = number;
        int count = number.ToString().Length;
        int remaining = number;
        for (int i = digits.Length - 1; i >= 0; i--)
        {
            Image image = digits[i];
            int used = digits.Length - 1 - i;
            bool visible = used < count;
            image.gameObject.SetActive(visible);
            if (!visible)
            {
                continue;
            }

            image.sprite = digitSprites[remaining % 10];
            image.rectTransform.sizeDelta = image.sprite.rect.size * digitScale;
            remaining /= 10;
        }
    }

    private void ApplyColors()
    {
        int count = shownNumber.ToString().Length;
        for (int i = 0; i < digits.Length; i++)
        {
            int position = i - (digits.Length - count); // 0 = first visible digit
            float t = Mathf.Clamp01((colorTimer - Mathf.Max(0, position) * colorWaveDelay) / colorFadeSeconds);
            digits[i].color = Color.Lerp(previousColor, currentColor, t * t * (3f - 2f * t));
        }
    }

    private void ApplyScale()
    {
        float scale = 1f;
        scale += Bump(bumpTimer, bumpSeconds) * bumpSize;
        scale += Bump(pulseTimer, pulseSeconds) * pulseSize;
        animated.localScale = new Vector3(scale, scale, 1f);
    }

    /// <summary>0 before and after the effect, a smooth hill up to 1 in between.</summary>
    private static float Bump(float time, float duration)
    {
        return time >= duration ? 0f : Mathf.Sin(time / duration * Mathf.PI);
    }
}
