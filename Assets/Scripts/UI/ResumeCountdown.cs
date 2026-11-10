using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The 3-2-1 after Resume: shows one number at a time with a little pop, then tells the <see cref="RunController"/> to carry on.
/// The game is frozen (timeScale 0) the whole time, so it counts in real time.
/// (Whether the numbers are visible is decided by a <see cref="StateVisibility"/> next to it.)
/// </summary>
public class ResumeCountdown : MonoBehaviour
{
    [SerializeField] private RunController runController;
    [SerializeField] private Image numberImage;
    [Tooltip("Sprites for 0 to 9, in that order.")]
    [SerializeField] private Sprite[] digitSprites = new Sprite[10];

    [Tooltip("Scales the sprites (1 = the sprite's own pixel size).")]
    [SerializeField, Min(0.1f)] private float digitScale = 4f;

    [Header("Timing")]
    [SerializeField, Range(1, 9)] private int startFrom = 3;
    [Tooltip("Seconds every number stays on screen.")]
    [SerializeField, Min(0.1f)] private float secondsPerStep = 0.8f;

    [Header("Pop")]
    [Tooltip("Every number starts this many times bigger and shrinks to normal size.")]
    [SerializeField, Min(1f)] private float popScale = 1.5f;
    [SerializeField, Min(0.01f)] private float popSeconds = 0.25f;

    /// <summary>Raised when a new number appears, with its position in the countdown (0 for the first number, 1 for the second...).</summary>
    public event System.Action<int> Ticked;

    private void OnEnable()
    {
        runController.StateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        runController.StateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameState state)
    {
        StopAllCoroutines();
        if (state == GameState.Resuming)
        {
            StartCoroutine(CountDown());
        }
    }

    private IEnumerator CountDown()
    {
        RectTransform numberRect = numberImage.rectTransform;
        for (int number = startFrom; number >= 1; number--)
        {
            numberImage.sprite = digitSprites[number];
            numberRect.sizeDelta = digitSprites[number].rect.size * digitScale;
            Ticked?.Invoke(startFrom - number);
            for (float time = 0f; time < secondsPerStep; time += Time.unscaledDeltaTime)
            {
                float pop = Mathf.Clamp01(time / popSeconds);
                numberRect.localScale = Vector3.one * Mathf.Lerp(popScale, 1f, pop);
                yield return null;
            }
        }

        runController.FinishResume();
    }
}
