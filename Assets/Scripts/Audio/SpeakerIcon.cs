using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The animated speaker picture: two sound waves that fade in one after the other, or a slash that pops in when the sound is off.
/// It only draws; <see cref="VolumeControl"/> tells it the state. It uses real time, because the pause menu runs with timeScale 0.
/// </summary>
public class SpeakerIcon : MonoBehaviour
{
    [Tooltip("The whole icon; it makes a little pop whenever the state changes.")]
    [SerializeField] private RectTransform root;
    [Tooltip("Inner wave first, then the outer one.")]
    [SerializeField] private Image[] waves = new Image[2];
    [SerializeField] private Image slash;

    [Header("Animation")]
    [Tooltip("Seconds for the whole on/off change.")]
    [SerializeField, Min(0.05f)] private float changeSeconds = 0.25f;
    [Tooltip("A wave starts at this size and grows to full size while it appears.")]
    [SerializeField, Range(0.1f, 1f)] private float waveStartScale = 0.6f;
    [SerializeField, Min(0f)] private float popSize = 0.15f;
    [SerializeField, Min(0.05f)] private float popSeconds = 0.2f;

    private float progress = 1f; // 1 = sound on (waves), 0 = sound off (slash)
    private float target = 1f;
    private float popTime = float.MaxValue;

    /// <summary>Shows the on or off state. Without <paramref name="animate"/> it jumps there at once.</summary>
    public void SetOn(bool on, bool animate)
    {
        float newTarget = on ? 1f : 0f;
        if (animate && !Mathf.Approximately(newTarget, target))
        {
            popTime = 0f;
        }

        target = newTarget;
        if (!animate)
        {
            progress = target;
            popTime = float.MaxValue;
        }

        Draw();
    }

    private void Update()
    {
        if (!Mathf.Approximately(progress, target))
        {
            progress = Mathf.MoveTowards(progress, target, Time.unscaledDeltaTime / changeSeconds);
        }

        if (popTime < popSeconds)
        {
            popTime += Time.unscaledDeltaTime;
        }

        Draw();
    }

    private void Draw()
    {
        // The waves appear one after the other: the second starts when the first is half way.
        for (int i = 0; i < waves.Length; i++)
        {
            float start = i * 0.4f;
            float presence = Mathf.Clamp01((progress - start) / (1f - start * 1.5f));
            presence = Mathf.SmoothStep(0f, 1f, presence);
            SetImage(waves[i], presence, Mathf.Lerp(waveStartScale, 1f, presence));
        }

        // The slash is the opposite: it pops in with a little overshoot.
        float slashPresence = 1f - progress;
        SetImage(slash, Mathf.Clamp01(slashPresence * 2f), Mathf.LerpUnclamped(0.2f, 1f, EaseOutBack(slashPresence)));

        float pop = popTime < popSeconds ? Mathf.Sin(popTime / popSeconds * Mathf.PI) * popSize : 0f;
        root.localScale = Vector3.one * (1f + pop);
    }

    private static void SetImage(Image image, float alpha, float scale)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
        image.rectTransform.localScale = Vector3.one * scale;
        image.enabled = alpha > 0.001f;
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        t = Mathf.Clamp01(t) - 1f;
        return 1f + c3 * t * t * t + c1 * t * t;
    }
}
