using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The order of the themes and how long each one lasts, and the record of where each theme really started.
/// The level generator asks "which theme is next?" (by distance along the level) and writes down where it switched;
/// the scenery (ground, ceiling, later the background) asks "which theme is drawn at this spot?" and reads that record,
/// so what the player sees changes exactly where the generator changed.
/// The order never changes (blue, orange, green, blue, ...), but each run may begin at a different theme.
/// </summary>
public class ThemeCycle : MonoBehaviour
{
    [SerializeField] private WorldScroller worldScroller;
    [Tooltip("Themes in the order they appear. After the last one the cycle starts over.")]
    [SerializeField] private ThemeDefinition[] themes;
    [Tooltip("Level length (world units) after which the next theme starts. A piece is never cut in half: the theme only changes at the start of a piece, so a theme lasts a little longer than this, up to one piece.")]
    [SerializeField, Min(1f)] private float distancePerTheme = 100f;

    [Header("Start")]
    [Tooltip("On: every run begins at a random theme (the order after it stays the same). Off: every run begins at 'Start Theme'.")]
    [SerializeField] private bool randomStartTheme = true;
    [Tooltip("Index in the theme list to begin with when 'Random Start Theme' is off (0 = first theme). Handy for testing one theme.")]
    [SerializeField, Min(0)] private int startTheme;
    [Tooltip("Testing aid: the theme never changes, the run stays at its start theme. Turn off 'Random Start Theme' and pick 'Start Theme' to test one theme only. Keep this off in the real game.")]
    [SerializeField] private bool lockTheme;

    [Header("Background")]
    [Tooltip("The background of the old theme fades into the new one while the change of the ground passes over the screen. This is how long that takes, measured in world units of scrolling (a screen is about 18 wide).")]
    [SerializeField, Min(1f)] private float backgroundFadeLength = 18f;

    [Header("Live values (read only)")]
    [SerializeField, DebugReadOnly] private string startThemeName;

    private struct Change
    {
        public float Coordinate;
        public ThemeDefinition Theme;
    }

    private readonly List<Change> changes = new List<Change>();
    private int startIndex = -1;
    private float levelOrigin;

    public int ThemeCount => themes.Length;

    /// <summary>Index in the theme list of the theme this run begins with.</summary>
    public int StartIndex
    {
        get
        {
            // Chosen on first use, so it does not matter which script asks first.
            if (startIndex < 0)
            {
                startIndex = randomStartTheme ? Random.Range(0, themes.Length) : Mathf.Min(startTheme, themes.Length - 1);
                startThemeName = themes[startIndex].DisplayName;
            }

            return startIndex;
        }
    }

    /// <summary>The level generator tells where the level begins (its first piece), so every distance is measured from the same point.</summary>
    public void BeginLevel(float originCoordinate)
    {
        levelOrigin = originCoordinate;
        changes.Clear();
        changes.Add(new Change { Coordinate = float.NegativeInfinity, Theme = ThemeAt(0f) });
    }

    /// <summary>The generator switched to <paramref name="theme"/>: from <paramref name="coordinate"/> on, the scenery should look like it.</summary>
    public void RegisterThemeChange(ThemeDefinition theme, float coordinate)
    {
        changes.Add(new Change { Coordinate = coordinate, Theme = theme });
    }

    /// <summary>Index in the theme list for a point <paramref name="levelDistance"/> units into the level.</summary>
    public int ThemeIndexAt(float levelDistance)
    {
        int step = lockTheme ? 0 : Mathf.FloorToInt(Mathf.Max(0f, levelDistance) / distancePerTheme);
        return (StartIndex + step) % themes.Length;
    }

    public ThemeDefinition ThemeAt(float levelDistance)
    {
        return themes[ThemeIndexAt(levelDistance)];
    }

    /// <summary>Which theme should come next at this place in the level (a place given in the generator's own coordinate, where a piece starts).</summary>
    public ThemeDefinition ThemeAtCoordinate(float coordinate)
    {
        return ThemeAt(LevelDistanceAtCoordinate(coordinate));
    }

    /// <summary>Distance into the level of a place given in the generator's own coordinate.</summary>
    public float LevelDistanceAtCoordinate(float coordinate)
    {
        return coordinate - levelOrigin;
    }

    public ThemeDefinition ThemeByIndex(int index)
    {
        return themes[index];
    }

    /// <summary>
    /// How the background should look right now: the old theme fully visible, the new one fading in on top of it.
    /// The fade starts when a change of the ground comes into view and ends when it has left the screen on the other side.
    /// When nothing is changing, <paramref name="from"/> and <paramref name="to"/> are the same theme and <paramref name="t"/> is 1.
    /// </summary>
    public void GetBackgroundBlend(out ThemeDefinition from, out ThemeDefinition to, out float t)
    {
        if (changes.Count == 0)
        {
            from = to = ThemeAt(0f);
            t = 1f;
            return;
        }

        // The middle of the screen is at "distance travelled" in the generator's coordinate; a change at coordinate c is on the screen centre when they are equal.
        float centre = worldScroller.DistanceTravelled;
        for (int i = changes.Count - 1; i > 0; i--)
        {
            float start = changes[i].Coordinate - backgroundFadeLength * 0.5f;
            if (centre >= start)
            {
                from = changes[i - 1].Theme;
                to = changes[i].Theme;
                t = Mathf.Clamp01((centre - start) / backgroundFadeLength);
                return;
            }
        }

        from = to = changes[0].Theme;
        t = 1f;
    }

    /// <summary>The theme the scenery should show at this world X right now, according to where the generator really switched.</summary>
    public ThemeDefinition ThemeAtWorldX(float worldX)
    {
        if (changes.Count == 0)
        {
            return ThemeAt(0f);
        }

        float coordinate = worldX + worldScroller.DistanceTravelled;
        for (int i = changes.Count - 1; i > 0; i--)
        {
            if (coordinate >= changes[i].Coordinate)
            {
                return changes[i].Theme;
            }
        }

        return changes[0].Theme;
    }
}
