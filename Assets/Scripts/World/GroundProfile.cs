using UnityEngine;

/// <summary>
/// The wavy top edge of the ground strip, measured once from its image (see Tools > Level > Bake Ground Profile).
/// Holds one height per image column, measured from the bottom of the strip, so code can ask "how high is the ground at this spot?".
/// </summary>
public class GroundProfile : ScriptableObject
{
    [Tooltip("Width of one repeating strip piece in world units.")]
    [SerializeField] private float pieceWidth = 8.08f;
    [Tooltip("Total height of the strip image in world units.")]
    [SerializeField] private float stripHeight = 0.71f;
    [Tooltip("Surface height above the bottom of the strip, one value per image column.")]
    [SerializeField] private float[] heights = new float[0];

    public float PieceWidth => pieceWidth;
    public float StripHeight => stripHeight;
    public bool HasData => heights != null && heights.Length > 1;

    public float MinHeight { get; private set; }
    public float MaxHeight { get; private set; }

    /// <summary>Used by the baking tool.</summary>
    public void SetData(float width, float height, float[] columnHeights)
    {
        pieceWidth = width;
        stripHeight = height;
        heights = columnHeights;
        CacheRange();
    }

    /// <summary>Surface height at a position inside the piece (0 = left edge, wraps around).</summary>
    public float HeightAt(float position)
    {
        float column = Mathf.Repeat(position, pieceWidth) / pieceWidth * heights.Length;
        int index = Mathf.Min(heights.Length - 1, Mathf.FloorToInt(column));
        return heights[index];
    }

    /// <summary>Lowest surface height between two positions inside the piece (wraps around the piece border).</summary>
    public float LowestHeight(float from, float to)
    {
        float lowest = float.MaxValue;
        float step = pieceWidth / heights.Length;
        for (float position = from; position <= to; position += step)
        {
            lowest = Mathf.Min(lowest, HeightAt(position));
        }

        return Mathf.Min(lowest, HeightAt(to));
    }

    private void OnEnable()
    {
        CacheRange();
    }

    private void CacheRange()
    {
        MinHeight = float.MaxValue;
        MaxHeight = float.MinValue;
        if (!HasData)
        {
            MinHeight = 0f;
            MaxHeight = 0f;
            return;
        }

        foreach (float height in heights)
        {
            MinHeight = Mathf.Min(MinHeight, height);
            MaxHeight = Mathf.Max(MaxHeight, height);
        }
    }
}
