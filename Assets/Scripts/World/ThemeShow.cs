using UnityEngine;

/// <summary>
/// Lets the scenery change theme on its own, for scenes without a level (the menu): the world scrolls slowly and every few seconds
/// the next theme begins at the right edge of the screen, with the same ground change and background fade the game uses.
/// It only tells the <see cref="ThemeCycle"/> where the next change is; the ground and the background do the rest.
/// </summary>
public class ThemeShow : MonoBehaviour
{
    [SerializeField] private ThemeCycle themeCycle;
    [SerializeField] private WorldScroller worldScroller;
    [Tooltip("Gives the width of one ground strip piece; the ground can only change at the edge of a piece.")]
    [SerializeField] private GroundSurface groundSurface;
    [Tooltip("Seconds until the next theme begins to appear.")]
    [SerializeField, Min(1f)] private float secondsPerTheme = 5f;

    private Camera cam;
    private float timer;
    private int index;

    private void Start()
    {
        cam = Camera.main;
        themeCycle.BeginLevel(0f);
        index = themeCycle.StartIndex;
        worldScroller.Begin();
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer < secondsPerTheme)
        {
            return;
        }

        timer -= secondsPerTheme;
        index = (index + 1) % themeCycle.ThemeCount;

        // Just outside the right edge of the screen, on the edge of a ground strip piece.
        float halfWidth = cam.orthographicSize * cam.aspect;
        float pieceWidth = groundSurface.PieceWidth;
        float coordinate = Mathf.Ceil((worldScroller.DistanceTravelled + halfWidth + 1f) / pieceWidth) * pieceWidth;
        themeCycle.RegisterThemeChange(themeCycle.ThemeByIndex(index), coordinate);
    }
}
