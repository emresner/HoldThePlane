using UnityEngine;

/// <summary>
/// Answers "where is the top of the ground strip at this world X?". The strip scrolls and repeats, so the answer
/// comes from the strip's own scroll position (<see cref="ScrollingLayer"/>) and its baked edge shape (<see cref="GroundProfile"/>).
/// Put it on the object that carries the ground <see cref="ScrollingLayer"/>.
/// </summary>
[RequireComponent(typeof(ScrollingLayer))]
public class GroundSurface : MonoBehaviour
{
    [SerializeField] private GroundProfile profile;

    private ScrollingLayer layer;

    /// <summary>Width of one repeating strip piece (the strip image can only change at multiples of it).</summary>
    public float PieceWidth => layer.PieceWidth;

    private void Awake()
    {
        layer = GetComponent<ScrollingLayer>();
    }

    /// <summary>
    /// World Y of the lowest point of the strip's top edge between two world X values, right now.
    /// "Lowest" so that a trunk standing on a bump still touches the strip on the dip next to it.
    /// The strip stands at a fixed place (its origin is the bottom of the strip), whatever the screen shape.
    /// </summary>
    public float LowestSurfaceY(float worldXFrom, float worldXTo)
    {
        float stripBottom = transform.position.y;
        if (profile == null || !profile.HasData)
        {
            Debug.LogError("GroundSurface has no baked profile. Run Tools > Level > Bake Ground Profile and assign it.", this);
            return stripBottom + 0.5f;
        }

        float from = layer.PiecePositionAt(worldXFrom);
        float to = from + (worldXTo - worldXFrom);
        return stripBottom + profile.LowestHeight(from, to);
    }
}
