using UnityEngine;

/// <summary>
/// One hand-made piece of the level. The origin is the left edge; the piece extends <see cref="Length"/> units to the right.
/// The piece promises a free height band at both ends (the connection contract), so any piece whose exit band overlaps
/// the next piece's entry band can follow it and the plane can always fly through the seam.
/// Obstacles standing on the ground or hanging from the ceiling live under <see cref="groundAnchored"/> / <see cref="ceilingAnchored"/>,
/// which mark where the strips are: the strips and everything on them stand at fixed places, so a piece plays the same on every screen shape.
/// </summary>
public class LevelSection : MonoBehaviour
{
    /// <summary>Everything in a piece (rocks, decoration) stays at least this far from both of its ends. The scenery (ground, ceiling, background) changes theme in that free space, so the cut is never seen under a rock. Checked by the validator.</summary>
    public const float SceneryClearance = 2f;

    [Tooltip("Width of the piece in world units. Time on screen = Length / world speed.")]
    [SerializeField] private float length = 35f;
    [Tooltip("1 = calm, 3 = hardest. The generator unlocks higher values the further the player gets.")]
    [SerializeField, Range(1, 3)] private int difficulty = 1;
    [Tooltip("Height band (world Y, min and max) that is guaranteed to be free at the left edge.")]
    [SerializeField] private Vector2 entryGap = new Vector2(-1f, 1f);
    [Tooltip("Height band (world Y, min and max) that is guaranteed to be free at the right edge.")]
    [SerializeField] private Vector2 exitGap = new Vector2(-1f, 1f);
    [Tooltip("Parent of everything that stands on the ground. Its origin is the top of the ground strip.")]
    [SerializeField] private Transform groundAnchored;
    [Tooltip("Parent of everything that hangs from the ceiling. Its origin is the bottom of the ceiling strip.")]
    [SerializeField] private Transform ceilingAnchored;

    public float Length => length;
    public int Difficulty => difficulty;
    public Vector2 EntryGap => entryGap;
    public Vector2 ExitGap => exitGap;
    public Transform GroundAnchored => groundAnchored;
    public Transform CeilingAnchored => ceilingAnchored;

    /// <summary>Height of the free space shared by the exit band of <paramref name="from"/> and the entry band of <paramref name="to"/>. Zero or less means no shared space.</summary>
    public static float GapOverlap(LevelSection from, LevelSection to)
    {
        return Mathf.Min(from.exitGap.y, to.entryGap.y) - Mathf.Max(from.exitGap.x, to.entryGap.x);
    }

    private void OnValidate()
    {
        length = Mathf.Max(1f, length);
    }

    private void OnDrawGizmosSelected()
    {
        Matrix4x4 previous = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        // Outline of the piece (the screen is about 8 units tall).
        Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
        Gizmos.DrawWireCube(new Vector3(length * 0.5f, 0f, 0f), new Vector3(length, 8f, 0f));

        // Free bands at both ends.
        Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.9f);
        DrawBand(0f, entryGap);
        DrawBand(length, exitGap);

#if UNITY_EDITOR
        DrawGroundBand();
#endif
        Gizmos.matrix = previous;
    }

#if UNITY_EDITOR
    private static GroundProfile cachedProfile;

    /// <summary>
    /// Shows where the wavy top edge of the ground strip can be: the bottom of a tree must end up between the two orange lines
    /// (the real height is fixed while playing by <see cref="GroundSnap"/>, so the exact Y of a tree does not matter here).
    /// </summary>
    private void DrawGroundBand()
    {
        if (groundAnchored == null)
        {
            return;
        }

        if (cachedProfile == null)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:GroundProfile");
            if (guids.Length > 0)
            {
                cachedProfile = UnityEditor.AssetDatabase.LoadAssetAtPath<GroundProfile>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
            }
        }

        if (cachedProfile == null || !cachedProfile.HasData)
        {
            return;
        }

        // The ground container's origin is the strip's top bounding line; surface heights are measured from the strip bottom.
        float highest = cachedProfile.MaxHeight - cachedProfile.StripHeight;
        float lowest = cachedProfile.MinHeight - cachedProfile.StripHeight;
        Matrix4x4 keep = Gizmos.matrix;
        Gizmos.matrix = groundAnchored.localToWorldMatrix;
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.9f);
        Gizmos.DrawLine(new Vector3(0f, highest, 0f), new Vector3(length, highest, 0f));
        Gizmos.DrawLine(new Vector3(0f, lowest, 0f), new Vector3(length, lowest, 0f));
        Gizmos.matrix = keep;
    }
#endif

    private static void DrawBand(float x, Vector2 band)
    {
        Gizmos.DrawLine(new Vector3(x, band.x, 0f), new Vector3(x, band.y, 0f));
        Gizmos.DrawLine(new Vector3(x - 0.3f, band.x, 0f), new Vector3(x + 0.3f, band.x, 0f));
        Gizmos.DrawLine(new Vector3(x - 0.3f, band.y, 0f), new Vector3(x + 0.3f, band.y, 0f));
    }
}
