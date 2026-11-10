using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the endless level out of hand-made <see cref="LevelSection"/> prefabs.
/// A cursor marks where the next piece starts; whenever it comes close to the right screen edge, a piece that fits
/// the previous one is picked from the active theme's pool and placed there.
/// The cursor is kept in distance units like everything else (see <see cref="WorldScroller.DistanceTravelled"/>).
/// Which difficulty is picked how often depends on the run time (see <see cref="DifficultyPhase"/>): calm at first, harder later, but every difficulty can appear from the start.
/// </summary>
public class SectionGenerator : MonoBehaviour
{
    [SerializeField] private WorldScroller worldScroller;
    [Tooltip("Ground strip height lookup; lets trees stand on the strip. Optional: without it trees stay where the prefab put them.")]
    [SerializeField] private GroundSurface groundSurface;
    [Tooltip("The plane; its X decides when a new piece will reach the player.")]
    [SerializeField] private Transform plane;
    [Tooltip("Decides which theme's pool the next piece comes from.")]
    [SerializeField] private ThemeCycle themeCycle;
    [Tooltip("Obstacle-free piece placed first, so the run starts calmly.")]
    [SerializeField] private LevelSection introSection;
    [Tooltip("Obstacle-free piece placed every time the theme changes. The ground and ceiling change in its middle, where no rock can show the cut. Optional: without it the change is a hard cut at the start of the new theme's first piece.")]
    [SerializeField] private LevelSection transitionSection;
    [Tooltip("How far outside the right screen edge the next piece is created (world units).")]
    [SerializeField] private float spawnMargin = 2f;
    [Tooltip("Smallest free height (world units) that two neighbouring pieces must share at their seam.")]
    [SerializeField] private float minGapClearance = 2f;
    private const float SceneryMargin = 0.25f; // how close to a rock the ground may change at the very most

    /// <summary>The mix of difficulties from <see cref="startSeconds"/> of run time on. Only the ratio of the three weights matters (60, 30, 10 means 60% / 30% / 10%).</summary>
    [System.Serializable]
    public struct DifficultyPhase
    {
        [Tooltip("Run time in seconds when this mix starts. Measured when the piece reaches the plane, not when it is created.")]
        public float startSeconds;
        [Tooltip("Weight of difficulty 1 pieces.")]
        public float easy;
        [Tooltip("Weight of difficulty 2 pieces.")]
        public float medium;
        [Tooltip("Weight of difficulty 3 pieces.")]
        public float hard;

        public DifficultyPhase(float startSeconds, float easy, float medium, float hard)
        {
            this.startSeconds = startSeconds;
            this.easy = easy;
            this.medium = medium;
            this.hard = hard;
        }

        public float WeightOf(int difficulty)
        {
            return difficulty <= 1 ? easy : difficulty == 2 ? medium : hard;
        }
    }

    [Header("Difficulty")]
    [Tooltip("The difficulty mix over time, in order of start time (the first should start at 0). Inside one difficulty every piece has the same chance, and a piece never follows itself.")]
    [SerializeField] private DifficultyPhase[] phases =
    {
        new DifficultyPhase(0f, 60f, 30f, 10f),
        new DifficultyPhase(20f, 30f, 60f, 10f),
        new DifficultyPhase(45f, 22f, 44f, 33f),
        new DifficultyPhase(120f, 10f, 30f, 60f),
    };
    [Tooltip("Writes a line to the Console each time the difficulty mix or the theme changes.")]
    [SerializeField] private bool logTierChanges = true;

    [Header("Live values (read only)")]
    [Tooltip("Which entry of 'Phases' (0 = the first) the newest piece was picked from.")]
    [SerializeField, DebugReadOnly] private int currentPhase;
    [Tooltip("The piece the plane is flying through right now.")]
    [SerializeField, DebugReadOnly] private string currentSection;
    [SerializeField, DebugReadOnly] private int currentDifficulty;
    [Tooltip("The newest piece, created just outside the right edge of the screen.")]
    [SerializeField, DebugReadOnly] private string lastPlacedSection;
    [SerializeField, DebugReadOnly] private int piecesPlaced;
    [Tooltip("Theme whose pool the newest piece came from.")]
    [SerializeField, DebugReadOnly] private string currentTheme;

    private readonly List<LevelSection> candidates = new List<LevelSection>();
    private readonly List<LevelSection> placed = new List<LevelSection>();
    private Camera cam;
    private LevelSection previous;
    private ThemeDefinition activeTheme;
    private float nextStart;

    private void Awake()
    {
        cam = Camera.main;
        previous = introSection;
        currentPhase = 0;
        piecesPlaced = 0;
        activeTheme = themeCycle.ThemeAt(0f);
        currentTheme = activeTheme.DisplayName;
    }

    private void Start()
    {
        // The first piece begins at the left screen edge (distance travelled is 0 at the start).
        nextStart = cam.transform.position.x - cam.orthographicSize * cam.aspect;
        themeCycle.BeginLevel(nextStart);
        Place(introSection);
    }

    private void Update()
    {
        float camRight = cam.transform.position.x + cam.orthographicSize * cam.aspect;
        while (nextStart - worldScroller.DistanceTravelled <= camRight + spawnMargin)
        {
            Place(PickNext());
        }

        UpdateCurrentSection();
    }

    /// <summary>Finds the piece that covers the plane's column and shows it in the Inspector.</summary>
    private void UpdateCurrentSection()
    {
        placed.RemoveAll(section => section == null);
        float planeX = plane.position.x;
        foreach (LevelSection section in placed)
        {
            float left = section.transform.position.x;
            if (planeX >= left && planeX < left + section.Length)
            {
                currentSection = section.name;
                currentDifficulty = section.Difficulty;
                return;
            }
        }
    }

    /// <summary>
    /// Which difficulty mix applies to a piece that reaches the plane at <paramref name="arrivalTime"/> (run time in seconds).
    /// Pieces are created about a screen ahead of the plane, so the start time is checked against their arrival, not their creation.
    /// </summary>
    private void UpdatePhase(float arrivalTime)
    {
        int phase = 0;
        for (int i = 0; i < phases.Length; i++)
        {
            if (arrivalTime >= phases[i].startSeconds)
            {
                phase = i;
            }
        }

        if (phase != currentPhase)
        {
            currentPhase = phase;
            if (logTierChanges)
            {
                DifficultyPhase mix = phases[phase];
                Debug.Log($"Difficulty mix {phase + 1} starts with the piece created at {worldScroller.ElapsedTime:0.0} s (it reaches the plane at about {arrivalTime:0.0} s, world speed {worldScroller.Speed:0.0}): weights {mix.easy:0.#} / {mix.medium:0.#} / {mix.hard:0.#}.", this);
            }
        }
    }

    private LevelSection PickNext()
    {
        // How long until the left edge of the new piece reaches the plane's column, at the current speed (a close estimate; the speed keeps rising).
        float secondsFromNow = Mathf.Max(0f, nextStart - worldScroller.DistanceTravelled - plane.position.x) / worldScroller.Speed;
        float arrivalTime = worldScroller.ElapsedTime + secondsFromNow;
        UpdatePhase(arrivalTime);
        if (UpdateActiveTheme())
        {
            // The new theme begins with a calm piece; its own pool follows.
            return transitionSection != null ? transitionSection : PickFromPool();
        }

        return PickFromPool();
    }

    private LevelSection PickFromPool()
    {
        CollectCandidates(avoidRepeat: true);

        // With a one-piece pool (or no fitting piece) repeating is better than stalling.
        if (candidates.Count == 0)
        {
            CollectCandidates(avoidRepeat: false);
        }

        if (candidates.Count == 0)
        {
            Debug.LogError($"No section in theme '{activeTheme.DisplayName}' fits after '{previous.name}'. Run Tools > Level > Validate Sections.", this);
            return activeTheme.Sections[Random.Range(0, activeTheme.Sections.Count)];
        }

        return PickWeighted();
    }

    /// <summary>
    /// Looks up the theme for the piece that is about to be placed. Asking at the start of every piece means a piece is never
    /// split between two themes: the old theme simply finishes its last piece and the new one begins with the next.
    /// When the theme changes, the place where the scenery must change is written down in the <see cref="ThemeCycle"/>:
    /// the middle of the transition piece (or the start of the new piece when there is no transition piece).
    /// </summary>
    /// <returns>True when the theme just changed.</returns>
    private bool UpdateActiveTheme()
    {
        ThemeDefinition theme = themeCycle.ThemeAtCoordinate(nextStart);
        if (theme == activeTheme)
        {
            return false;
        }

        activeTheme = theme;
        float sceneryChange = SceneryChangeCoordinate();
        themeCycle.RegisterThemeChange(theme, sceneryChange);
        if (logTierChanges)
        {
            Debug.Log($"Theme '{theme.DisplayName}' starts with the piece created at {worldScroller.ElapsedTime:0.0} s ({themeCycle.LevelDistanceAtCoordinate(nextStart):0} units into the level).", this);
        }

        return true;
    }

    /// <summary>
    /// Where the ground and ceiling must change for a theme change that starts at <see cref="nextStart"/>.
    /// The strips can only change at multiples of one strip piece, so take the multiple that falls into the free space around the transition piece
    /// (every piece keeps <see cref="LevelSection.SceneryClearance"/> free at its ends) and is closest to the transition piece's middle.
    /// </summary>
    private float SceneryChangeCoordinate()
    {
        float length = transitionSection != null ? transitionSection.Length : 0f;
        float middle = nextStart + length * 0.5f;
        if (groundSurface == null)
        {
            return middle;
        }

        float width = groundSurface.PieceWidth;
        float low = nextStart - LevelSection.SceneryClearance + SceneryMargin;
        float high = nextStart + length + LevelSection.SceneryClearance - SceneryMargin;
        float nearest = Mathf.Round(middle / width) * width;
        if (nearest >= low && nearest <= high)
        {
            return nearest;
        }

        float other = nearest < low ? nearest + width : nearest - width;
        if (other >= low && other <= high)
        {
            return other;
        }

        Debug.LogWarning($"No strip piece edge falls into the free space around the transition (window {high - low:0.0} wide, a strip piece is {width:0.00}). Make the transition piece longer. The ground change may be seen under a rock.", this);
        return middle;
    }

    private void CollectCandidates(bool avoidRepeat)
    {
        candidates.Clear();
        foreach (LevelSection section in activeTheme.Sections)
        {
            bool allowed = LevelSection.GapOverlap(previous, section) >= minGapClearance
                && !(avoidRepeat && section == previous);
            if (allowed)
            {
                candidates.Add(section);
            }
        }
    }

    /// <summary>
    /// Picks by difficulty first, then evenly inside that difficulty: each difficulty gets the share given by the current entry of <see cref="phases"/>,
    /// no matter how many pieces it contains. Adding more pieces to a difficulty therefore adds variety without changing the mix.
    /// A difficulty with no fitting piece right now simply drops out, and the others share its weight.
    /// </summary>
    private LevelSection PickWeighted()
    {
        float totalWeight = 0f;
        for (int tier = 1; tier <= 3; tier++)
        {
            if (CountOfDifficulty(tier) > 0)
            {
                totalWeight += WeightOfTier(tier);
            }
        }

        float roll = Random.Range(0f, totalWeight);
        foreach (LevelSection section in candidates)
        {
            roll -= WeightOfTier(section.Difficulty) / CountOfDifficulty(section.Difficulty);
            if (roll < 0f)
            {
                return section;
            }
        }

        return candidates[candidates.Count - 1];
    }

    private int CountOfDifficulty(int difficulty)
    {
        int count = 0;
        foreach (LevelSection section in candidates)
        {
            if (section.Difficulty == difficulty)
            {
                count++;
            }
        }

        return count;
    }

    private float WeightOfTier(int tier)
    {
        return phases[Mathf.Clamp(currentPhase, 0, phases.Length - 1)].WeightOf(tier);
    }

    private void Place(LevelSection prefab)
    {
        float distance = worldScroller.DistanceTravelled;
        float x = nextStart - distance;

        LevelSection section = Instantiate(prefab, new Vector3(x, 0f, 0f), Quaternion.identity);
        section.name = prefab.name;
        section.GetComponent<ScrollWithWorld>().Initialize(worldScroller, x, distance, prefab.Length);

        if (groundSurface != null)
        {
            foreach (GroundSnap snap in section.GetComponentsInChildren<GroundSnap>())
            {
                snap.Apply(groundSurface);
            }
        }

        placed.Add(section);
        previous = prefab;
        nextStart += prefab.Length;
        lastPlacedSection = prefab.name;
        currentTheme = activeTheme.DisplayName;
        piecesPlaced++;
    }
}
