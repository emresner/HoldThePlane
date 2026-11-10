using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Checks every level section of every theme against the connection contract and simulates a plane flying through it.
/// Run it from Tools > Level > Validate Sections after changing or adding a section.
/// </summary>
public static class LevelSectionValidator
{
    private const string PlanePrefabPath = "Assets/Prefabs/Plane.prefab";

    // Pass criteria (tuning knobs of the check, not of the game).
    private const float MinSharedGap = 2f;          // free height two neighbours must share at the seam
    private const float EdgeMargin = 1.5f;          // obstacles must stay this far from both ends...
    private const float MinSeconds = 5f;            // ...and a piece should last between these
    private const float MaxSeconds = 9f;            //    many seconds at the reference speed
    private const float MinBuriedDepth = 0.8f;      // ground/ceiling obstacles must sink at least this deep into their strip (the strip's wavy edge dips about half a unit)
    private const float DecorMinGap = 2f;           // decoration must keep this much free space (x) from obstacles on its own side
    private const float DecorFreeBand = 1f;        // decoration must stay out of the middle band: |Y| < this (ground decor below, cloud decor above)
    private const float ReferenceSpeed = 5f;       // world speed the length-in-seconds rule is measured at
    private const float FallbackMaxSpeed = 8f;      // used when no WorldScroller is open; normally its Max Speed is simulated (the hardest case)
    internal const float PlaneAbility = 0.7f;        // only this share of the plane's real vertical speed is trusted
    private const float FlightColumn = 0.2f;        // width of one simulation column (must stay well below the plane width 0.7)
    private const float CellY = 0.05f;
    private const float MaxStraightStretch = 32f;   // longest stretch (world units) where one height inside the middle band stays free before it counts as "nothing to do"

    // Stars. A star may ask for at most "comfort" of the plane's ability (per section difficulty: easy sections may hide stars in
    // trickier spots, hard sections keep them on the natural line). A section that is itself demanding gets a little extra room.
    internal static readonly float[] StarComfortByDifficulty = { 0.30f, 0.22f, 0.15f };
    private const float MinAbility = 0.05f;          // the ability search runs from this up to PlaneAbility...
    internal const float AbilityStep = 0.05f;         // ...in steps of this size
    internal const float StarEdgeMargin = 3f;         // stars stay this far from both ends of the piece
    internal const float StarMinHazardDistance = 1.4f; // star centre to the nearest obstacle box (bounds), so a star never sits right behind a spike where the plane has to dodge and then dive back
    private const float StarCatchShare = 0.6f;     // the simulated plane must hit the middle 60% of the star's pick-up height; the real pick-up area is bigger, which is the safety margin
    internal const float StarMinSpotSeparation = 8f;  // the possible spots of one slot should differ in x by at least this much

    [MenuItem("Tools/Level/Validate Sections")]
    public static void RunFromMenu()
    {
        string report = Run(out int errors, out int warnings);
        if (errors > 0)
        {
            Debug.LogError(report);
        }
        else if (warnings > 0)
        {
            Debug.LogWarning(report);
        }
        else
        {
            Debug.Log(report);
        }
    }

    /// <param name="worldSpeedOverride">Simulate at this world speed instead of the scene's Max Speed (0 = use the scene). Handy to find how fast the world may get.</param>
    public static string Run(out int errors, out int warnings, float worldSpeedOverride = 0f)
    {
        var report = new StringBuilder();
        int errorCount = 0;
        int warningCount = 0;
        void Error(string message) { errorCount++; report.AppendLine("ERROR   " + message); }
        void Warn(string message) { warningCount++; report.AppendLine("WARNING " + message); }

        PlaneLimits plane = CreatePlaneLimits(worldSpeedOverride);
        report.AppendLine($"Plane: rise {plane.rise}, fall {plane.fall}, Y {plane.minY}..{plane.maxY}, capsule {plane.capsule}; simulation at world speed {plane.worldSpeed} (the fastest the world gets), trusting {PlaneAbility * 100}% of plane speed.");

        foreach (string themeGuid in AssetDatabase.FindAssets("t:ThemeDefinition"))
        {
            var theme = AssetDatabase.LoadAssetAtPath<ThemeDefinition>(AssetDatabase.GUIDToAssetPath(themeGuid));
            report.AppendLine($"Theme '{theme.DisplayName}' ({theme.Sections.Count} sections)");

            foreach (LevelSection section in theme.Sections)
            {
                if (section == null)
                {
                    Error($"{theme.name}: empty slot in the section list.");
                    continue;
                }

                CheckSection(section, plane, Error, Warn, report);
            }

            for (int i = 0; i < theme.Sections.Count; i++)
            {
                for (int j = 0; j < theme.Sections.Count; j++)
                {
                    if (i == j || theme.Sections[i] == null || theme.Sections[j] == null)
                    {
                        continue;
                    }

                    float shared = LevelSection.GapOverlap(theme.Sections[i], theme.Sections[j]);
                    if (shared < MinSharedGap)
                    {
                        Error($"{theme.name}: {theme.Sections[i].name} -> {theme.Sections[j].name} shares only {shared:0.0} free height at the seam (need {MinSharedGap}).");
                    }
                }
            }
        }

        report.AppendLine($"Done: {errorCount} errors, {warningCount} warnings.");
        errors = errorCount;
        warnings = warningCount;
        return report.ToString();
    }

    /// <summary>Everything drawn in a piece must keep <see cref="LevelSection.SceneryClearance"/> free at both ends, so a theme change of the scenery never lands under a rock.</summary>
    private static void CheckSceneryClearance(GameObject instance, LevelSection prefab, System.Action<string> error)
    {
        bool any = false;
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>())
        {
            any = true;
            minX = Mathf.Min(minX, renderer.bounds.min.x);
            maxX = Mathf.Max(maxX, renderer.bounds.max.x);
        }

        if (!any)
        {
            return;
        }

        float left = minX;
        float right = prefab.Length - maxX;
        if (left < LevelSection.SceneryClearance || right < LevelSection.SceneryClearance)
        {
            error($"{prefab.name}: content comes closer than {LevelSection.SceneryClearance} to the piece edge (free space left {left:0.00}, right {right:0.00}). The scenery cut at a theme change could show under it.");
        }
    }

    private static void CheckSection(LevelSection prefab, PlaneLimits plane, System.Action<string> error, System.Action<string> warn, StringBuilder report)
    {
        string name = prefab.name;
        float seconds = prefab.Length / ReferenceSpeed;
        if (seconds < MinSeconds || seconds > MaxSeconds)
        {
            warn($"{name}: lasts {seconds:0.0} s at speed {ReferenceSpeed} (wanted {MinSeconds}-{MaxSeconds}).");
        }

        if (prefab.GroundAnchored == null || prefab.CeilingAnchored == null)
        {
            error($"{name}: Ground Anchored / Ceiling Anchored is not set.");
        }

        Scene scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, scene);
            instance.transform.position = Vector3.zero;
            Physics2D.SyncTransforms();
            CheckSceneryClearance(instance, prefab, error);

            var section = instance.GetComponent<LevelSection>();
            Collider2D[] colliders = instance.GetComponentsInChildren<Collider2D>();
            int hazards = 0;

            foreach (Collider2D collider in colliders)
            {
                if (collider.GetComponent<Hazard>() == null)
                {
                    continue;
                }

                hazards++;
                Bounds b = collider.bounds;

                if (b.min.x < 0f || b.max.x > section.Length)
                {
                    error($"{name}: {collider.transform.name} sticks out of the piece (x {b.min.x:0.0}..{b.max.x:0.0}, length {section.Length}).");
                }

                if (b.min.x < EdgeMargin && Overlaps(b, section.EntryGap) || b.max.x > section.Length - EdgeMargin && Overlaps(b, section.ExitGap))
                {
                    error($"{name}: {collider.transform.name} blocks the entry or exit gap near the edge.");
                }

                CheckBuried(name, collider, section, error);
            }

            CheckDecor(name, section, error);
            List<StarSpot> starSpots = ReadStarSpots(name, section, error, warn);

            FlightGrid grid = BuildGrid(scene, section, plane);
            CheckForcedMovement(name, section, grid, plane, error, warn);
            bool simulated = Simulate(grid, section, plane, PlaneAbility, null, out string failure);
            report.AppendLine($"  {name}: {hazards} obstacles, {seconds:0.0} s, flight {(simulated ? "passable" : "NOT passable")}");
            if (!simulated)
            {
                error($"{name}: {failure}");
            }
            else if (starSpots.Count > 0)
            {
                CheckStarReach(name, section, starSpots, grid, plane, error, report);
            }
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    /// <summary>
    /// A piece must make the plane move. If some constant height inside the entry/exit band (the middle of the screen) is free all the way through,
    /// the player can just hold still and the obstacles mean nothing. That is a warning to review. A long stretch where holding still works is a warning too.
    /// </summary>
    private static void CheckForcedMovement(string name, LevelSection section, FlightGrid grid, PlaneLimits plane, System.Action<string> error, System.Action<string> warn)
    {
        float low = Mathf.Max(section.EntryGap.x, section.ExitGap.x);
        float high = Mathf.Min(section.EntryGap.y, section.ExitGap.y);
        int columns = grid.freeBands.Length;
        int longestRun = 0;
        float freeLineY = float.NaN;

        // The straight-line error only counts heights inside the middle band; the long-stretch warning counts every height the plane can fly at
        // (a plane that climbs to the top and sits there is not playing the section either).
        for (float y = plane.minY; y <= plane.maxY + 0.0001f; y += CellY)
        {
            bool inBand = y >= low - 0.0001f && y <= high + 0.0001f;
            int run = 0;
            for (int c = 0; c < columns; c++)
            {
                if (IsFree(grid.freeBands[c], y))
                {
                    run++;
                    if (run > longestRun)
                    {
                        longestRun = run;
                    }

                    if (run == columns && inBand)
                    {
                        freeLineY = y;
                    }
                }
                else
                {
                    run = 0;
                }
            }
        }

        if (!float.IsNaN(freeLineY))
        {
            // A warning, not an error: wide gates cannot force a height change (the free heights of neighbouring gates overlap by design),
            // so this is a list to review rather than a failure.
            warn($"{name}: the plane can fly straight at height {freeLineY:0.00} through the whole piece; fine for wide-gate pieces, otherwise the obstacles force nothing.");
            return;
        }

        float stretch = longestRun * grid.columnWidth;
        if (stretch > MaxStraightStretch)
        {
            warn($"{name}: the plane can hold one height for {stretch:0} units in a row (more than {MaxStraightStretch:0}); something should force it to move sooner.");
        }
    }

    private static bool IsFree(List<Vector2> bands, float y)
    {
        foreach (Vector2 band in bands)
        {
            if (y >= band.x && y <= band.y)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Decoration lives under a child called "Decor". It must never hurt the plane and must stay out of the middle height band.</summary>
    private static void CheckDecor(string name, LevelSection section, System.Action<string> error)
    {
        foreach (Transform container in new[] { section.GroundAnchored, section.CeilingAnchored })
        {
            Transform decor = container != null ? container.Find("Decor") : null;
            if (decor == null)
            {
                continue;
            }

            foreach (Collider2D collider in decor.GetComponentsInChildren<Collider2D>())
            {
                error($"{name}: decor '{collider.transform.name}' has a collider; decoration must not be solid or a trigger.");
            }

            foreach (Hazard hazard in decor.GetComponentsInChildren<Hazard>())
            {
                error($"{name}: decor '{hazard.transform.name}' has a Hazard component.");
            }

            foreach (SpriteRenderer sprite in decor.GetComponentsInChildren<SpriteRenderer>())
            {
                Bounds b = sprite.bounds;
                bool fromGround = container == section.GroundAnchored;

                if (fromGround && sprite.GetComponent<GroundSnap>() == null)
                {
                    error($"{name}: decor '{sprite.transform.name}' stands on the ground but has no GroundSnap, so it may float over the strip's dips.");
                }

                // Keep props away from obstacles on their own side, so they never look like part of one.
                foreach (Collider2D hazard in container.GetComponentsInChildren<Collider2D>())
                {
                    if (hazard.GetComponent<Hazard>() == null)
                    {
                        continue;
                    }

                    float gap = Mathf.Max(b.min.x - hazard.bounds.max.x, hazard.bounds.min.x - b.max.x);
                    if (gap < DecorMinGap)
                    {
                        error($"{name}: decor '{sprite.transform.name}' is only {Mathf.Max(0f, gap):0.0} from obstacle '{hazard.transform.name}' (needs {DecorMinGap}).");
                    }
                }

                if (fromGround && b.max.y > -DecorFreeBand)
                {
                    error($"{name}: decor '{sprite.transform.name}' reaches into the middle band (top {b.max.y:0.00}, must stay below {-DecorFreeBand}).");
                }

                if (!fromGround && b.min.y < DecorFreeBand)
                {
                    error($"{name}: decor '{sprite.transform.name}' reaches into the middle band (bottom {b.min.y:0.00}, must stay above {DecorFreeBand}).");
                }
            }
        }
    }

    internal struct StarSpot
    {
        public string label;
        public Vector2 position;
        public float radius;
    }

    /// <summary>
    /// Reads every possible star position (children of a <see cref="StarSlot"/>), checks their setup and the simple placement rules,
    /// then switches their colliders off so the flight simulation does not mistake a star for an obstacle.
    /// </summary>
    private static List<StarSpot> ReadStarSpots(string name, LevelSection section, System.Action<string> error, System.Action<string> warn)
    {
        var spots = new List<StarSpot>();
        var hazards = new List<Collider2D>();
        foreach (Collider2D collider in section.GetComponentsInChildren<Collider2D>())
        {
            if (collider.GetComponent<Hazard>() != null)
            {
                hazards.Add(collider);
            }
        }

        foreach (StarSlot slot in section.GetComponentsInChildren<StarSlot>(true))
        {
            var slotSpots = new List<StarSpot>();
            foreach (Transform child in slot.transform)
            {
                string label = $"{slot.name}/{child.name}";
                var circle = child.GetComponent<CircleCollider2D>();
                if (circle == null || !circle.isTrigger)
                {
                    error($"{name}: star '{label}' needs a CircleCollider2D set to Is Trigger.");
                    continue;
                }

                if (child.GetComponent<Collectible>() == null)
                {
                    error($"{name}: star '{label}' has no Collectible component.");
                }

                if (child.GetComponent<Hazard>() != null)
                {
                    error($"{name}: star '{label}' has a Hazard component.");
                }

                var spot = new StarSpot
                {
                    label = label,
                    position = child.TransformPoint(circle.offset),
                    radius = circle.radius * Mathf.Max(Mathf.Abs(child.lossyScale.x), Mathf.Abs(child.lossyScale.y)),
                };

                if (spot.position.x < StarEdgeMargin || spot.position.x > section.Length - StarEdgeMargin)
                {
                    error($"{name}: star '{label}' is too close to the end of the piece (x {spot.position.x:0.0}, needs {StarEdgeMargin}..{section.Length - StarEdgeMargin:0.0}).");
                }

                foreach (Collider2D hazard in hazards)
                {
                    float distance = Mathf.Sqrt(hazard.bounds.SqrDistance(spot.position));
                    if (distance < StarMinHazardDistance)
                    {
                        error($"{name}: star '{label}' is only {distance:0.00} from obstacle '{hazard.transform.name}' (needs {StarMinHazardDistance}).");
                    }
                }

                foreach (StarSpot other in slotSpots)
                {
                    if (Mathf.Abs(other.position.x - spot.position.x) < StarMinSpotSeparation)
                    {
                        warn($"{name}: '{label}' and '{other.label}' are less than {StarMinSpotSeparation} apart in x, so the random choice would hardly be noticed.");
                    }
                }

                slotSpots.Add(spot);
            }

            spots.AddRange(slotSpots);
        }

        foreach (StarSlot slot in section.GetComponentsInChildren<StarSlot>(true))
        {
            foreach (Collider2D collider in slot.GetComponentsInChildren<Collider2D>(true))
            {
                collider.enabled = false;
            }
        }

        return spots;
    }

    /// <summary>
    /// A star must be reachable without asking more of the plane than its section's difficulty allows (see <see cref="StarComfortByDifficulty"/>).
    /// "Asking" = the smallest share of the plane's vertical speed with which entry -> star -> exit is still flyable, at the fastest world speed.
    /// </summary>
    private static void CheckStarReach(string name, LevelSection section, List<StarSpot> spots, FlightGrid grid, PlaneLimits plane, System.Action<string> error, StringBuilder report)
    {
        float baseNeed = RequiredAbility(grid, section, plane, null);
        float comfort = StarComfortByDifficulty[Mathf.Clamp(section.Difficulty - 1, 0, StarComfortByDifficulty.Length - 1)];
        float limit = Mathf.Max(comfort, baseNeed + AbilityStep);

        foreach (StarSpot spot in spots)
        {
            float need = RequiredAbility(grid, section, plane, spot);
            bool ok = need <= limit + 0.0001f;
            report.AppendLine($"    star {spot.label}: needs {need * 100:0}% of plane speed (section alone {baseNeed * 100:0}%, limit {limit * 100:0}%) {(ok ? "ok" : "TOO HARD")}");
            if (!ok)
            {
                error($"{name}: star '{spot.label}' needs {need * 100:0}% of the plane's vertical speed to collect (limit {limit * 100:0}% for difficulty {section.Difficulty}).");
            }
        }
    }

    /// <summary>Smallest tested ability (a share of the plane's real vertical speed) with which the section, optionally through a star, can be flown. Infinity if even the full trusted ability fails.</summary>
    internal static float RequiredAbility(FlightGrid grid, LevelSection section, PlaneLimits plane, StarSpot? star)
    {
        int levels = Mathf.RoundToInt((PlaneAbility - MinAbility) / AbilityStep) + 1;
        float AbilityAt(int index) { return Mathf.Min(PlaneAbility, MinAbility + index * AbilityStep); }

        if (!Simulate(grid, section, plane, AbilityAt(levels - 1), star, out _))
        {
            return float.PositiveInfinity;
        }

        int low = 0;
        int high = levels - 1;
        while (low < high)
        {
            int middle = (low + high) / 2;
            if (Simulate(grid, section, plane, AbilityAt(middle), star, out _))
            {
                high = middle;
            }
            else
            {
                low = middle + 1;
            }
        }

        return AbilityAt(low);
    }

    private static void CheckBuried(string name, Collider2D collider, LevelSection section, System.Action<string> error)
    {
        Bounds b = collider.bounds;
        if (collider.transform.IsChildOf(section.GroundAnchored) && b.min.y > section.GroundAnchored.position.y - MinBuriedDepth)
        {
            error($"{name}: {collider.transform.name} does not sink into the ground strip (bottom {b.min.y:0.00}, strip top {section.GroundAnchored.position.y:0.00}).");
        }

        if (collider.transform.IsChildOf(section.CeilingAnchored) && b.max.y < section.CeilingAnchored.position.y + MinBuriedDepth)
        {
            error($"{name}: {collider.transform.name} does not sink into the ceiling strip (top {b.max.y:0.00}, strip bottom {section.CeilingAnchored.position.y:0.00}).");
        }
    }

    private static bool Overlaps(Bounds b, Vector2 band)
    {
        return b.max.y > band.x && b.min.y < band.y;
    }

    /// <summary>
    /// For every column of a section (0.2 wide, narrower than the plane so no obstacle can slip through), the height ranges where the plane's
    /// capsule touches nothing. Measured once, then many flights can be simulated cheaply.
    /// </summary>
    internal sealed class FlightGrid
    {
        public float columnWidth;
        public List<Vector2>[] freeBands;
    }

    internal static FlightGrid BuildGrid(Scene scene, LevelSection section, PlaneLimits plane)
    {
        PhysicsScene2D physics = scene.GetPhysicsScene2D();
        int columns = Mathf.CeilToInt(section.Length / FlightColumn) + 1;
        int rows = Mathf.RoundToInt((plane.maxY - plane.minY) / CellY) + 1;
        // Yeni ContactFilter2D hiçbir şeyi filtrelemez (layer/derinlik/normal kapalı); sadece trigger'ları dahil ediyoruz.
        var filter = new ContactFilter2D { useTriggers = true };
        var hits = new Collider2D[4];

        var grid = new FlightGrid { columnWidth = FlightColumn, freeBands = new List<Vector2>[columns] };
        for (int c = 0; c < columns; c++)
        {
            var bands = new List<Vector2>();
            bool inBand = false;
            float start = 0f;
            float last = 0f;
            for (int r = 0; r < rows; r++)
            {
                float y = plane.minY + r * CellY;
                bool free = physics.OverlapCapsule(new Vector2(c * FlightColumn, y), plane.capsule, CapsuleDirection2D.Horizontal, 0f, filter, hits) == 0;
                if (free)
                {
                    if (!inBand)
                    {
                        start = y;
                        inBand = true;
                    }

                    last = y;
                }
                else if (inBand)
                {
                    bands.Add(new Vector2(start - CellY * 0.5f, last + CellY * 0.5f));
                    inBand = false;
                }
            }

            if (inBand)
            {
                bands.Add(new Vector2(start - CellY * 0.5f, last + CellY * 0.5f));
            }

            grid.freeBands[c] = bands;
        }

        return grid;
    }

    /// <summary>
    /// Moves a plane through the section column by column. It can only rise or fall so far while one column scrolls past
    /// (its real vertical speed times <paramref name="ability"/>), so the heights it can be at form ranges that widen and get cut by obstacles.
    /// Every start height in the entry band must lead to the exit band.
    /// </summary>
    /// <param name="ability">Share of the plane's real vertical speed the simulated plane may use (1 = all of it).</param>
    /// <param name="star">If set, the plane must also pass through this star on the way.</param>
    internal static bool Simulate(FlightGrid grid, LevelSection section, PlaneLimits plane, float ability, StarSpot? star, out string failure)
    {
        float dt = grid.columnWidth / plane.worldSpeed;
        float up = plane.rise * ability * dt;
        float down = plane.fall * ability * dt;
        int columns = grid.freeBands.Length;
        int starColumn = star.HasValue ? Mathf.Clamp(Mathf.RoundToInt(star.Value.position.x / grid.columnWidth), 1, columns - 1) : -1;

        // The plane is allowed to start anywhere in the entry band.
        foreach (float startY in new[] { section.EntryGap.x, (section.EntryGap.x + section.EntryGap.y) * 0.5f, section.EntryGap.y })
        {
            var reach = Intersect(new List<Vector2> { new Vector2(startY, startY) }, grid.freeBands[0]);
            if (reach.Count == 0)
            {
                failure = $"start height {startY:0.0} is blocked.";
                return false;
            }

            for (int c = 1; c < columns; c++)
            {
                reach = Intersect(Widen(reach, down, up, plane.minY, plane.maxY), grid.freeBands[c]);
                if (c == starColumn)
                {
                    // Only the heights close enough to the star collect it.
                    float y = star.Value.position.y;
                    float catchHeight = star.Value.radius * StarCatchShare;
                    reach = Intersect(reach, new List<Vector2> { new Vector2(y - catchHeight, y + catchHeight) });
                    if (reach.Count == 0)
                    {
                        failure = $"starting at height {startY:0.0} the plane cannot reach the star.";
                        return false;
                    }
                }

                if (reach.Count == 0)
                {
                    failure = $"starting at height {startY:0.0} the plane runs out of room at x = {c * grid.columnWidth:0.0}.";
                    return false;
                }
            }

            bool reachesExit = false;
            foreach (Vector2 range in reach)
            {
                if (range.x <= section.ExitGap.y && range.y >= section.ExitGap.x)
                {
                    reachesExit = true;
                    break;
                }
            }

            if (!reachesExit)
            {
                failure = $"starting at height {startY:0.0} the plane cannot reach the exit band.";
                return false;
            }
        }

        failure = null;
        return true;
    }

    /// <summary>Stretches every range downward/upward and merges ranges that now touch. Input and output are sorted.</summary>
    private static List<Vector2> Widen(List<Vector2> ranges, float down, float up, float minY, float maxY)
    {
        var result = new List<Vector2>(ranges.Count);
        foreach (Vector2 range in ranges)
        {
            var wide = new Vector2(Mathf.Max(minY, range.x - down), Mathf.Min(maxY, range.y + up));
            if (result.Count > 0 && wide.x <= result[result.Count - 1].y)
            {
                result[result.Count - 1] = new Vector2(result[result.Count - 1].x, Mathf.Max(result[result.Count - 1].y, wide.y));
            }
            else
            {
                result.Add(wide);
            }
        }

        return result;
    }

    /// <summary>The parts that two sorted lists of ranges have in common.</summary>
    private static List<Vector2> Intersect(List<Vector2> a, List<Vector2> b)
    {
        var result = new List<Vector2>();
        int i = 0;
        int j = 0;
        while (i < a.Count && j < b.Count)
        {
            float low = Mathf.Max(a[i].x, b[j].x);
            float high = Mathf.Min(a[i].y, b[j].y);
            if (low <= high)
            {
                result.Add(new Vector2(low, high));
            }

            if (a[i].y < b[j].y)
            {
                i++;
            }
            else
            {
                j++;
            }
        }

        return result;
    }

    internal struct PlaneLimits
    {
        public float rise;
        public float fall;
        public float minY;
        public float maxY;
        public Vector2 capsule;
        public float worldSpeed;
    }

    /// <summary>The plane's limits plus the world speed to simulate at (the override, else the open scene's Max Speed).</summary>
    internal static PlaneLimits CreatePlaneLimits(float worldSpeedOverride = 0f)
    {
        PlaneLimits plane = ReadPlaneLimits();
        var scroller = Object.FindFirstObjectByType<WorldScroller>();
        plane.worldSpeed = worldSpeedOverride > 0f ? worldSpeedOverride : scroller != null ? scroller.MaxSpeed : FallbackMaxSpeed;
        return plane;
    }

    private static PlaneLimits ReadPlaneLimits()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlanePrefabPath);
        var movement = new SerializedObject(prefab.GetComponent<PlaneMovement>());
        return new PlaneLimits
        {
            rise = movement.FindProperty("maxRiseSpeed").floatValue,
            fall = movement.FindProperty("maxFallSpeed").floatValue,
            minY = movement.FindProperty("minY").floatValue,
            maxY = movement.FindProperty("maxY").floatValue,
            capsule = prefab.GetComponent<CapsuleCollider2D>().size,
        };
    }
}
