using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Writes a text with the Tappy Plane letter sprites: every letter is its own Image, tilted a little the other way than its neighbour,
/// tinted in turn, and (while playing) bobbing up and down in a calm wave where neighbours move in opposite directions.
/// The letters are real children saved in the prefab, so edit <see cref="text"/> and run "Rebuild Letters" (right-click the component header).
/// </summary>
public class SpriteLetters : MonoBehaviour
{
    [SerializeField] private string text = "HOLD THE PLANE";
    [Tooltip("Sprites for A to Z, in that order.")]
    [SerializeField] private Sprite[] letters = new Sprite[26];
    [Tooltip("Scales the sprites (1 = the sprite's own pixel size).")]
    [SerializeField, Min(0.1f)] private float letterScale = 1.5f;
    [Tooltip("Extra space between two letters; negative lets them overlap a little.")]
    [SerializeField] private float spacing = -2f;
    [Tooltip("Space for a blank between two words.")]
    [SerializeField, Min(0f)] private float wordGap = 50f;
    [Tooltip("Tilt of every letter in degrees; the sign flips from one letter to the next.")]
    [SerializeField, Range(0f, 15f)] private float tilt = 3f;
    [Tooltip("Letter colours, used in turn. White keeps the sprite's own colour.")]
    [SerializeField] private Color[] colors = { Color.white };

    [System.Serializable]
    public struct KerningPair
    {
        [Tooltip("The two letters in reading order, for example PA.")]
        public string pair;
        [Tooltip("Moves the second letter (and everything after it) by this much; negative = closer. The line is centred again afterwards.")]
        public float adjust;
    }

    [Tooltip("Letter pairs that look too far apart or too close (for example P next to A). Run Rebuild Letters after changing it.")]
    [SerializeField] private KerningPair[] kerning = new KerningPair[0];

    [Header("Bobbing")]
    [Tooltip("How far a letter moves up and down, in canvas units. 0 = still.")]
    [SerializeField, Min(0f)] private float bobHeight = 8f;
    [Tooltip("Seconds for one full up-and-down.")]
    [SerializeField, Min(0.1f)] private float bobSeconds = 1.8f;

    private RectTransform[] letterRects = new RectTransform[0];
    private float[] baseY = new float[0];

    private void Awake()
    {
        if (transform.childCount == 0)
        {
            Rebuild();
        }

        CacheLetters();
    }

    private void Update()
    {
        if (bobHeight <= 0f)
        {
            return;
        }

        float phase = Time.unscaledTime / bobSeconds * Mathf.PI * 2f;
        for (int i = 0; i < letterRects.Length; i++)
        {
            // Even letters go up while odd letters go down (opposite phase), all at the same time.
            float direction = i % 2 == 0 ? 1f : -1f;
            Vector2 position = letterRects[i].anchoredPosition;
            position.y = baseY[i] + Mathf.Sin(phase) * bobHeight * direction;
            letterRects[i].anchoredPosition = position;
        }
    }

    [ContextMenu("Rebuild Letters")]
    public void Rebuild()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject old = transform.GetChild(i).gameObject;
            if (Application.isPlaying)
            {
                Destroy(old);
            }
            else
            {
                DestroyImmediate(old);
            }
        }

        string upper = text.ToUpperInvariant();

        // First pass: how wide is everything, so the line can be centred.
        float total = 0f;
        for (int i = 0; i < upper.Length; i++)
        {
            total += AdvanceFor(upper[i]) + KerningBefore(upper, i);
        }

        float x = -total * 0.5f;
        int placed = 0;
        for (int i = 0; i < upper.Length; i++)
        {
            char c = upper[i];
            x += KerningBefore(upper, i);
            float advance = AdvanceFor(c);
            if (c >= 'A' && c <= 'Z' && letters[c - 'A'] != null)
            {
                Sprite sprite = letters[c - 'A'];
                float width = sprite.rect.width * letterScale;

                var go = new GameObject("Letter_" + c, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = sprite.rect.size * letterScale;
                rect.anchoredPosition = new Vector2(x + width * 0.5f, 0f);
                rect.localEulerAngles = new Vector3(0f, 0f, placed % 2 == 0 ? tilt : -tilt);

                var image = go.GetComponent<Image>();
                image.sprite = sprite;
                image.color = colors.Length == 0 ? Color.white : colors[placed % colors.Length];
                image.raycastTarget = false;
                placed++;
            }

            x += advance;
        }

        ((RectTransform)transform).sizeDelta = new Vector2(total, 64f * letterScale);
        CacheLetters();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    private void CacheLetters()
    {
        letterRects = new RectTransform[transform.childCount];
        baseY = new float[transform.childCount];
        for (int i = 0; i < letterRects.Length; i++)
        {
            letterRects[i] = (RectTransform)transform.GetChild(i);
            baseY[i] = letterRects[i].anchoredPosition.y;
        }
    }

    /// <summary>Extra space before the letter at <paramref name="index"/> when its pair with the letter in front is listed in <see cref="kerning"/>.</summary>
    private float KerningBefore(string upper, int index)
    {
        if (index == 0)
        {
            return 0f;
        }

        foreach (KerningPair entry in kerning)
        {
            if (!string.IsNullOrEmpty(entry.pair) && entry.pair.Length == 2
                && char.ToUpperInvariant(entry.pair[0]) == upper[index - 1]
                && char.ToUpperInvariant(entry.pair[1]) == upper[index])
            {
                return entry.adjust;
            }
        }

        return 0f;
    }

    private float AdvanceFor(char c)
    {
        if (c >= 'A' && c <= 'Z' && letters[c - 'A'] != null)
        {
            return letters[c - 'A'].rect.width * letterScale + spacing;
        }

        return wordGap;
    }
}
