using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gives every piece of a scrolling ground or ceiling strip the look of the theme that is there.
/// Each piece (a child of the <see cref="ScrollingLayer"/>) has a "Sprite" child with the strip image and a "Fill" child with the block under it.
/// A piece takes the theme of its middle, so the look changes piece by piece (a hard cut, no blending).
/// Without a <see cref="ThemeCycle"/> (the menu scene) nothing changes.
/// </summary>
[RequireComponent(typeof(ScrollingLayer))]
[DefaultExecutionOrder(10)] // after ScrollingLayer has placed (and, if needed, created) this frame's pieces
public class ThemedStrip : MonoBehaviour
{
    [SerializeField] private ThemeCycle themeCycle;

    private struct Piece
    {
        public Transform Root;
        public SpriteRenderer Strip;
        public SpriteRenderer Fill;
        public ThemeDefinition Theme;
    }

    private readonly List<Piece> pieces = new List<Piece>();
    private ScrollingLayer layer;

    private void Awake()
    {
        layer = GetComponent<ScrollingLayer>();
    }

    private void LateUpdate()
    {
        if (themeCycle == null)
        {
            return;
        }

        CollectNewPieces();
        float halfWidth = layer.PieceWidth * 0.5f;
        for (int i = 0; i < pieces.Count; i++)
        {
            Piece piece = pieces[i];
            ThemeDefinition theme = themeCycle.ThemeAtWorldX(piece.Root.position.x + halfWidth);
            if (theme == piece.Theme)
            {
                continue;
            }

            piece.Strip.sprite = theme.GroundSprite;
            piece.Fill.color = theme.FillColor;
            piece.Theme = theme;
            pieces[i] = piece;
        }
    }

    /// <summary>The layer creates its pieces by copying the first one, so new children show up over time.</summary>
    private void CollectNewPieces()
    {
        for (int i = pieces.Count; i < transform.childCount; i++)
        {
            Transform root = transform.GetChild(i);
            pieces.Add(new Piece
            {
                Root = root,
                Strip = root.Find("Sprite").GetComponent<SpriteRenderer>(),
                Fill = root.Find("Fill").GetComponent<SpriteRenderer>(),
            });
        }
    }
}
