using UnityEngine;

/// <summary>
/// Something the plane can pick up (a star today). It only knows how to be collected once and how to disappear nicely;
/// what the pickup is worth is decided by whoever listens to <see cref="PlaneCollector.Collected"/>.
/// Needs a trigger collider on the same object.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Collectible : MonoBehaviour
{
    [Tooltip("Score this pickup is worth. A level section is worth about 37 points of distance, so keep it a small bonus.")]
    [SerializeField, Min(0)] private int points = 10;
    [Tooltip("How long the pop (grow and fade) lasts, in seconds.")]
    [SerializeField] private float popDuration = 0.2f;
    [Tooltip("Size multiplier the pickup reaches at the end of the pop.")]
    [SerializeField] private float popScale = 1.6f;

    private SpriteRenderer sprite;
    private Collider2D trigger;
    private Vector3 startScale;
    private Color startColor;
    private float popTimer;

    public int Points => points;

    /// <summary>The pickup's picture; tells what kind of pickup it is (for example which colour of star).</summary>
    public Sprite Icon => sprite.sprite;

    public bool IsCollected { get; private set; }

    private void Awake()
    {
        sprite = GetComponentInChildren<SpriteRenderer>();
        trigger = GetComponent<Collider2D>();
        startScale = transform.localScale;
        startColor = sprite.color;
    }

    /// <summary>Returns false if it was already collected (so it can only count once).</summary>
    public bool Collect()
    {
        if (IsCollected)
        {
            return false;
        }

        IsCollected = true;
        trigger.enabled = false;
        return true;
    }

    private void Update()
    {
        if (!IsCollected)
        {
            return;
        }

        popTimer += Time.deltaTime;
        float t = Mathf.Clamp01(popTimer / popDuration);
        transform.localScale = startScale * Mathf.Lerp(1f, popScale, t);

        Color color = startColor;
        color.a = startColor.a * (1f - t);
        sprite.color = color;

        if (t >= 1f)
        {
            gameObject.SetActive(false);
        }
    }
}
