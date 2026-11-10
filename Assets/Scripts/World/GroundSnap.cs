using UnityEngine;

/// <summary>
/// Lets a prop stand on the ground strip whatever the strip's wavy edge looks like at the spot where the prop appears.
/// <see cref="SectionGenerator"/> calls <see cref="Apply"/> right after it creates the section; place the prop anywhere along X
/// and leave its Y roughly on the strip, the exact height is fixed at that moment.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class GroundSnap : MonoBehaviour
{
    [Tooltip("Half the width of the part that touches the ground (the trunk), in sprite units at scale 1.")]
    [SerializeField] private float trunkHalfWidth = 0.3f;
    [Tooltip("How far the bottom of the prop sinks below the lowest ground point under it (world units).")]
    [SerializeField] private float sink = 0.06f;

    public void Apply(GroundSurface surface)
    {
        var sprite = GetComponent<SpriteRenderer>();
        float half = trunkHalfWidth * Mathf.Abs(transform.lossyScale.x);
        float x = transform.position.x;
        float baseY = surface.LowestSurfaceY(x - half, x + half) - sink;

        // The sprite's origin is its centre, so lift the centre by half the sprite's height above the base.
        Vector3 position = transform.position;
        position.y = baseY + sprite.bounds.extents.y;
        transform.position = position;
    }
}
