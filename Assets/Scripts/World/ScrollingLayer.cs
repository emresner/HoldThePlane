using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Repeats one piece side by side and slides the row to the left as the world scrolls, so it never runs out.
/// Positions are computed from <see cref="WorldScroller.DistanceTravelled"/>, so there is no drift and a restart is a reset.
/// The template must be a child of this object and its origin must be its left edge. Keep this object's scale at 1.
/// </summary>
public class ScrollingLayer : MonoBehaviour
{
    [SerializeField] private WorldScroller worldScroller;
    [Tooltip("1 = moves with the world, smaller values move slower (further away).")]
    [SerializeField, Range(0f, 1f)] private float parallaxFactor = 1f;
    [Tooltip("The piece that gets repeated. Visible in the editor as the first piece.")]
    [SerializeField] private Transform template;
    [Tooltip("Width of one piece in world units (distance between two neighbouring left edges).")]
    [SerializeField] private float pieceWidth = 10f;

    /// <summary>Width of one repeating piece in world units.</summary>
    public float PieceWidth => pieceWidth;

    /// <summary>Where inside the repeating piece (0 = its left edge) the given world X lies right now.</summary>
    public float PiecePositionAt(float worldX)
    {
        return Mathf.Repeat(worldX + worldScroller.DistanceTravelled * parallaxFactor, pieceWidth);
    }

    private readonly List<Transform> pieces = new List<Transform>();
    private Camera cam;
    private Vector3 templateLocalPosition;

    private void Awake()
    {
        cam = Camera.main;
        templateLocalPosition = template.localPosition;
        pieces.Add(template);
    }

    private void LateUpdate()
    {
        float halfWidth = cam.orthographicSize * cam.aspect;
        float camLeft = cam.transform.position.x - halfWidth;
        float camRight = cam.transform.position.x + halfWidth;

        int count = Mathf.CeilToInt((camRight - camLeft) / pieceWidth) + 3;
        EnsurePieceCount(count);

        float scroll = Mathf.Repeat(worldScroller.DistanceTravelled * parallaxFactor, pieceWidth);
        int firstIndex = Mathf.FloorToInt(camLeft / pieceWidth) - 1;

        for (int i = 0; i < pieces.Count; i++)
        {
            float worldX = (firstIndex + i) * pieceWidth - scroll;
            pieces[i].localPosition = new Vector3(worldX - transform.position.x, templateLocalPosition.y, templateLocalPosition.z);
        }
    }

    private void EnsurePieceCount(int count)
    {
        while (pieces.Count < count)
        {
            Transform clone = Instantiate(template, transform);
            clone.name = template.name + " (" + pieces.Count + ")";
            pieces.Add(clone);
        }
    }
}
