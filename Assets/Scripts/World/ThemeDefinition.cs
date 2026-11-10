using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data for one visual theme: the pool of level sections that may be generated while the theme is active,
/// and the look of the ground and ceiling strips (the strip image and the colour of the block under it).
/// Background maps and decoration join it in the next stages.
/// </summary>
[CreateAssetMenu(fileName = "Theme_New", menuName = "Game/Theme Definition")]
public class ThemeDefinition : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private List<LevelSection> sections = new List<LevelSection>();

    [Header("Ground and ceiling")]
    [Tooltip("Image of the ground strip. The ceiling uses the same image upside down. All strips must have the same top edge shape.")]
    [SerializeField] private Sprite groundSprite;
    [Tooltip("Colour of the block that fills the screen below the ground strip (and above the ceiling) on tall screens. Use the bottom colour of the strip image.")]
    [SerializeField] private Color fillColor = Color.white;

    [Header("Sky")]
    [Tooltip("Colour behind the background map (shown where the map does not reach, for example on very tall screens). Use the colour of the theme's sky tile.")]
    [SerializeField] private Color skyColor = Color.white;

    public string DisplayName => displayName;
    public IReadOnlyList<LevelSection> Sections => sections;
    public Sprite GroundSprite => groundSprite;
    public Color FillColor => fillColor;
    public Color SkyColor => skyColor;
}
