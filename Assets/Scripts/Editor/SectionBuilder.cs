using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds the level sections of a theme from short pattern descriptions instead of hand-placed rocks.
/// A pattern says where a rock stands (x) and how high its tip reaches (world Y); the builder works out the rock's scale and position
/// so the tip lands exactly there and the base is sunk into the strip. It rewrites the existing section prefabs in place (same files, same GUIDs),
/// adds new ones when a theme needs more, re-places the decoration and the stars, and prints how fast the world may get before a section stops being passable.
/// Run it from Tools > Level, then Tools > Level > Validate Sections.
/// </summary>
public static class SectionBuilder
{
    private const string SectionsFolder = "Assets/Prefabs/World/Sections";
    private const string ObstaclesFolder = "Assets/Prefabs/World/Obstacles";
    private const string DecorFolder = "Assets/Prefabs/World/Decor";
    private const string ThemesFolder = "Assets/Data/Themes";

    // Geometry of the rock sprite (height 2.4, centred) and of the strips (origins of GroundAnchored / CeilingAnchored).
    private const float GroundTop = -3.29f;
    private const float CeilingBottom = 3.29f;
    private const float RockHalfHeight = 1.2f;
    private const float RockHalfWidth = 0.54f;
    private const float Buried = 1.0f;             // how far a rock's base sinks into its strip (the validator needs at least 0.8)
    private const float MinScaleY = 0.7f;          // the shortest rock we use (a stub); this decides how low a ground tip / how high a ceiling tip can be
    private const float LowestGroundTip = GroundTop + 2.4f * MinScaleY - Buried;
    private const float HighestCeilingTip = CeilingBottom - 2.4f * MinScaleY + Buried;
    private const float MaxTip = 2.0f;             // no rock reaches further than this towards the middle of the screen
    private const float DecorMinGap = 2.1f;        // a little above the validator's 2.0

    public struct Rock
    {
        public bool ceiling;
        public float x;
        public float tip;
        public float width;
    }

    public sealed class Spec
    {
        /// <summary>File name, "Theme_NN_Title". An existing file with the same "Theme_NN_" start is rewritten in place and renamed (same GUID); otherwise a new file is made.</summary>
        public string name;
        public int difficulty;
        public float length;
        public readonly List<Rock> rocks = new List<Rock>();

        public Spec(string name, int difficulty, float length)
        {
            this.name = name;
            this.difficulty = difficulty;
            this.length = length;
        }

        /// <summary>A rock standing on the ground whose tip reaches <paramref name="tip"/>.</summary>
        public Spec Up(float x, float tip, float width = 1.2f)
        {
            rocks.Add(new Rock { ceiling = false, x = x, tip = tip, width = width });
            return this;
        }

        /// <summary>A rock hanging from the ceiling whose tip reaches <paramref name="tip"/>.</summary>
        public Spec Down(float x, float tip, float width = 1.2f)
        {
            rocks.Add(new Rock { ceiling = true, x = x, tip = tip, width = width });
            return this;
        }

        /// <summary>A ground rock and a ceiling rock at the same x: a gate whose free opening is centred on <paramref name="centre"/>.</summary>
        public Spec Gate(float x, float centre, float gap, float width = 1.2f)
        {
            Up(x, centre - gap * 0.5f, width);
            Down(x, centre + gap * 0.5f, width);
            return this;
        }

        /// <summary>
        /// A row of gates every <paramref name="step"/> units starting at <paramref name="from"/>, one per value in <paramref name="centres"/> (the opening's centre height).
        /// </summary>
        public Spec Gates(float from, float step, float gap, float width, params float[] centres)
        {
            for (int i = 0; i < centres.Length; i++)
            {
                Gate(from + i * step, centres[i], gap, width);
            }

            return this;
        }

        /// <summary>
        /// A canyon: a gate every <paramref name="step"/> units from <paramref name="from"/> to <paramref name="to"/>, the opening's centre following <paramref name="centre"/>(x).
        /// The plane is only forced to follow it when the centre moves further than the opening is wide (otherwise one straight height fits every gate).
        /// </summary>
        public Spec Corridor(float from, float to, float step, System.Func<float, float> centre, float gap, float width = 1.4f)
        {
            for (float x = from; x <= to + 0.001f; x += step)
            {
                Gate(x, centre(x), gap, width);
            }

            return this;
        }
    }

    [MenuItem("Tools/Level/Rebuild Blue Sections")]
    public static void RebuildBlueFromMenu()
    {
        Debug.Log(Rebuild("Blue", "Ice", BlueSpecs(), 1200));
    }

    [MenuItem("Tools/Level/Rebuild Orange Sections")]
    public static void RebuildOrangeFromMenu()
    {
        Debug.Log(Rebuild("Orange", "Dirt", OrangeSpecs(), 2200));
    }

    [MenuItem("Tools/Level/Rebuild Green Sections")]
    public static void RebuildGreenFromMenu()
    {
        Debug.Log(Rebuild("Green", "Grass", GreenSpecs(), 3300));
    }

    // ---------------------------------------------------------------------------------------------------------------------------------
    // Orange (dirt): mesas and canyons. Same rules as Blue (2 easy, 3 medium, 2 hard; opening 4.0 / 3.4 / 3.0), but wider rocks and a different rhythm.
    // ---------------------------------------------------------------------------------------------------------------------------------
    private static List<Spec> OrangeSpecs()
    {
        var list = new List<Spec>();

        // Easy: swing rocks starting from the ground, and a gentle wide canyon.
        list.Add(new Spec("Orange_01_MesaSwing", 1, 44f)
            .Up(8f, -0.45f, 1.4f).Down(17f, 0.45f, 1.4f).Up(26f, -0.45f, 1.4f).Down(35f, 0.45f, 1.4f));

        list.Add(new Spec("Orange_02_WideCanyon", 1, 44f)
            .Gates(8f, 7f, 4f, 1.6f, 0f, 0.6f, 0f, -0.6f, 0f));

        // Medium: fat pinches, the mirror of the quick zig-zag, a ramp of steps up and down.
        list.Add(new Spec("Orange_03_Pinches", 2, 44f)
            .Gates(8f, 9f, 3.4f, 1.8f, 0.6f, -0.6f, 0.6f, -0.6f));

        list.Add(new Spec("Orange_04_Crossfire", 2, 44f)
            .Up(8f, -0.5f, 1.5f).Down(16f, 0.5f, 1.5f).Down(22f, 0.3f, 1.4f).Up(30f, -0.5f, 1.5f).Up(36f, -0.3f, 1.4f));

        list.Add(new Spec("Orange_05_Steps", 2, 44f)
            .Gates(7f, 4f, 3.4f, 1.7f, -1.2f, -0.4f, 0.4f, 1.2f, 1.2f, 0.4f, -0.4f, -1.2f));

        // Hard: tighter pinches (opening 3.0, closer together) and a small valley.
        list.Add(new Spec("Orange_06_TightPinches", 3, 44f)
            .Gates(7f, 7.5f, 3f, 1.6f, 1f, -1f, 1f, -1f, 1f));

        list.Add(new Spec("Orange_07_Valley", 3, 44f)
            .Gates(7f, 3.5f, 3f, 1.6f, 0.8f, 0.8f, 0f, 0f, -0.8f, -0.8f, 0f, 0f, 0.8f, 0.8f));

        return list;
    }

    // ---------------------------------------------------------------------------------------------------------------------------------
    // Green (grass): rolling hills and waves. Same rules as Blue and Orange, with sine-wave canyons and terraces.
    // ---------------------------------------------------------------------------------------------------------------------------------
    private static List<Spec> GreenSpecs()
    {
        var list = new List<Spec>();

        // Easy: widely spaced swing rocks (the hills), and a slow wave.
        list.Add(new Spec("Green_01_RollingHills", 1, 44f)
            .Down(9f, 0.45f).Up(19f, -0.45f).Down(29f, 0.45f).Up(39f, -0.45f));

        list.Add(new Spec("Green_02_WideWave", 1, 44f)
            .Corridor(8f, 36f, 4f, x => 0.7f * Mathf.Sin((x - 8f) / 28f * 2f * Mathf.PI), 4f, 1.4f));

        // Medium: a livelier wave, a double-hill zig-zag, terraces.
        list.Add(new Spec("Green_03_WaveGates", 2, 44f)
            .Corridor(7f, 39f, 4.5f, x => 1f * Mathf.Sin((x - 7f) / 32f * 2f * Mathf.PI), 3.4f, 1.5f));

        list.Add(new Spec("Green_04_DoubleHills", 2, 44f)
            .Down(7f, 0.5f).Down(13f, 0.3f, 1.3f).Up(21f, -0.5f).Up(27f, -0.3f, 1.3f).Down(35f, 0.5f));

        list.Add(new Spec("Green_05_Terraces", 2, 44f)
            .Gates(7f, 4f, 3.4f, 1.8f, 1f, 1f, 0f, 0f, -1f, -1f, 0f, 0f, 1f));

        // Hard: a tighter wave (opening 3.0) and narrow zig-zag gates followed by two swing rocks.
        list.Add(new Spec("Green_06_TightWave", 3, 44f)
            .Corridor(6f, 38f, 4f, x => 1f * Mathf.Sin((x - 6f) / 32f * 2f * Mathf.PI), 3f, 1.5f));

        list.Add(new Spec("Green_07_NarrowZig", 3, 44f)
            .Gates(8f, 8f, 3f, 1.4f, 1.2f, -1.2f, 1.2f).Down(33f, 0.3f, 1.3f).Up(40f, -0.3f, 1.3f));

        return list;
    }

    // ---------------------------------------------------------------------------------------------------------------------------------
    // Blue (ice): swing rocks (alternating ceiling and ground rocks), gates with a moving opening, and stairs.
    // The validator checks each one can be flown at the game's top world speed.
    // ---------------------------------------------------------------------------------------------------------------------------------
    private static List<Spec> BlueSpecs()
    {
        var list = new List<Spec>();

        // Seven sections: 2 easy, 3 medium, 2 hard. The difficulty is mostly the width of the opening (gap) and how often the plane must change height:
        //   easy   gap 4.0 and wide spacing      medium gap 3.4 / closer rocks      hard gap 3.0 (never narrower: the plane wobbles, so 2.6 was too tight in testing).
        // A "gate" is a ground rock and a ceiling rock at the same x; the opening's centre moves from gate to gate.
        // The plane is 0.4 tall, but a held/released plane swings about a metre, so openings are generous.

        // Easy
        list.Add(new Spec("Blue_01_ZigZag", 1, 44f)
            .Down(8f, 0.45f).Up(17f, -0.45f).Down(26f, 0.45f).Up(35f, -0.45f));

        list.Add(new Spec("Blue_02_WideGates", 1, 44f)
            .Gate(9f, 0.6f, 4f, 1.4f).Gate(18f, -0.6f, 4f, 1.4f).Gate(27f, 0.6f, 4f, 1.4f).Gate(36f, -0.6f, 4f, 1.4f));

        // Medium
        list.Add(new Spec("Blue_03_MidGates", 2, 44f)
            .Gate(8f, 0.9f, 3.4f, 1.4f).Gate(16f, -0.9f, 3.4f, 1.4f).Gate(24f, 0.9f, 3.4f, 1.4f).Gate(32f, -0.9f, 3.4f, 1.4f).Gate(40f, 0.9f, 3.4f, 1.4f));

        // Five rocks, closer together; twice two on the same side in a row.
        list.Add(new Spec("Blue_04_QuickZigZag", 2, 44f)
            .Down(8f, 0.5f).Up(16f, -0.5f).Up(22f, -0.3f, 1.3f).Down(30f, 0.5f).Down(36f, 0.3f, 1.3f));

        // A simple stairs trap: three steps up (two gates each), then three steps down.
        list.Add(new Spec("Blue_05_SoftStairs", 2, 44f)
            .Corridor(7f, 22f, 3f, x => -1f + Mathf.Floor((x - 7f) / 6f + 0.001f), 3.4f, 1.8f)
            .Corridor(28f, 40f, 3f, x => 1f - Mathf.Floor((x - 28f) / 6f + 0.001f), 3.4f, 1.8f));

        // Hard
        list.Add(new Spec("Blue_06_NarrowGates", 3, 44f)
            .Gate(8f, 1.2f, 3f, 1.4f).Gate(16f, -1.2f, 3f, 1.4f).Gate(24f, 1.2f, 3f, 1.4f).Gate(32f, -1.2f, 3f, 1.4f).Gate(40f, 1.2f, 3f, 1.4f));

        // Stairs down, simpler than the first version: three steps of two gates, 1.0 per step, opening 3.0.
        list.Add(new Spec("Blue_07_IceStairsDown", 3, 40f)
            .Corridor(7f, 24.5f, 3.5f, x => 1f - Mathf.Floor((x - 7f) / 7f + 0.001f), 3f, 1.7f));

        return list;
    }

    // ---------------------------------------------------------------------------------------------------------------------------------

    public static string Rebuild(string themeName, string rockKind, List<Spec> specs, int seed)
    {
        var log = new StringBuilder();
        string themePath = $"{ThemesFolder}/Theme_{themeName}.asset";
        var theme = AssetDatabase.LoadAssetAtPath<ThemeDefinition>(themePath);
        if (theme == null)
        {
            return "Theme asset not found: " + themePath;
        }

        var rockUp = AssetDatabase.LoadAssetAtPath<GameObject>($"{ObstaclesFolder}/Rock_Up_{rockKind}.prefab");
        var rockDown = AssetDatabase.LoadAssetAtPath<GameObject>($"{ObstaclesFolder}/Rock_Down_{rockKind}.prefab");
        var star = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/World/Collectibles/Star_{(themeName == "Orange" ? "Silver" : themeName == "Green" ? "Bronze" : "Gold")}.prefab");
        if (rockUp == null || rockDown == null || star == null)
        {
            return $"Rock or star prefab for '{rockKind}' not found.";
        }

        List<DecorTemplate> decorPool = CollectDecor(theme);
        log.AppendLine($"Decor pool: {decorPool.Count} props found in the old '{themeName}' sections.");

        var built = new List<LevelSection>();
        for (int i = 0; i < specs.Count; i++)
        {
            Spec spec = specs[i];
            string path = PrepareFile(themeName, spec, log);
            if (path == null)
            {
                continue;
            }

            BuildSection(path, spec, rockUp, rockDown, decorPool, seed + 37 * i, log);
            built.Add(AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<LevelSection>());
        }

        // Section files of this theme that no spec asks for any more belong to an older design: remove them (they are no longer in the pool).
        var wanted = new HashSet<string>();
        foreach (Spec spec in specs)
        {
            wanted.Add(spec.name);
        }

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { $"{SectionsFolder}/{themeName}" }))
        {
            string existing = AssetDatabase.GUIDToAssetPath(guid);
            if (!wanted.Contains(System.IO.Path.GetFileNameWithoutExtension(existing)))
            {
                log.AppendLine($"Removed old section {existing}");
                AssetDatabase.DeleteAsset(existing);
            }
        }

        // The theme's pool now holds exactly the rebuilt sections.
        var themeObject = new SerializedObject(theme);
        SerializedProperty sections = themeObject.FindProperty("sections");
        sections.arraySize = built.Count;
        for (int i = 0; i < built.Count; i++)
        {
            sections.GetArrayElementAtIndex(i).objectReferenceValue = built[i];
        }

        themeObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(theme);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        log.AppendLine(StarPlacer.Place(theme, star, 5000));
        log.AppendLine(SpeedReport(built));
        return log.ToString();
    }

    private static string PrepareFile(string themeName, Spec spec, StringBuilder log)
    {
        string folder = $"{SectionsFolder}/{themeName}";
        string target = $"{folder}/{spec.name}.prefab";

        if (System.IO.File.Exists(target))
        {
            return target;
        }

        // An older file with the same "Theme_NN_" start is the same slot: rename it instead of making a new asset (keeps its GUID).
        string prefix = spec.name.Substring(0, spec.name.IndexOf('_', spec.name.IndexOf('_') + 1) + 1);
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
        {
            string existing = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileName(existing).StartsWith(prefix))
            {
                string error = AssetDatabase.RenameAsset(existing, spec.name);
                if (!string.IsNullOrEmpty(error))
                {
                    log.AppendLine($"ERROR renaming {existing}: {error}");
                    return null;
                }

                return target;
            }
        }

        // A new section starts as a copy of the empty one (same components, anchors at the right heights).
        if (!AssetDatabase.CopyAsset($"{SectionsFolder}/Start_Empty.prefab", target))
        {
            log.AppendLine($"ERROR: could not create {target}.");
            return null;
        }

        return target;
    }

    private static void BuildSection(string path, Spec spec, GameObject rockUp, GameObject rockDown, List<DecorTemplate> decorPool, int seed, StringBuilder log)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            root.name = spec.name;
            var section = root.GetComponent<LevelSection>();
            var serialized = new SerializedObject(section);
            serialized.FindProperty("length").floatValue = spec.length;
            serialized.FindProperty("difficulty").intValue = spec.difficulty;
            serialized.FindProperty("entryGap").vector2Value = new Vector2(-1f, 1f);
            serialized.FindProperty("exitGap").vector2Value = new Vector2(-1f, 1f);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Empty both anchors and remove the old stars (StarPlacer adds new ones).
            foreach (Transform anchor in new[] { section.GroundAnchored, section.CeilingAnchored })
            {
                for (int i = anchor.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(anchor.GetChild(i).gameObject);
                }
            }

            Transform oldStars = root.transform.Find("Stars");
            if (oldStars != null)
            {
                Object.DestroyImmediate(oldStars.gameObject);
            }

            var groundRocks = new List<Rock>();
            var ceilingRocks = new List<Rock>();
            spec.rocks.Sort((a, b) => a.x.CompareTo(b.x));
            int upCount = 0;
            int downCount = 0;
            foreach (Rock rock in spec.rocks)
            {
                float edgeLeft = rock.x - RockHalfWidth * rock.width;
                float edgeRight = rock.x + RockHalfWidth * rock.width;
                if (edgeLeft < LevelSection.SceneryClearance + 0.2f || edgeRight > spec.length - LevelSection.SceneryClearance - 0.2f)
                {
                    log.AppendLine($"  WARNING {spec.name}: rock at x {rock.x} is too close to the piece edge.");
                }

                if (rock.ceiling)
                {
                    PlaceRock(section.CeilingAnchored, rockDown, rock, "Down_" + (++downCount), log, spec.name);
                    ceilingRocks.Add(rock);
                }
                else
                {
                    PlaceRock(section.GroundAnchored, rockUp, rock, "Up_" + (++upCount), log, spec.name);
                    groundRocks.Add(rock);
                }
            }

            PlaceDecor(section, groundRocks, ceilingRocks, decorPool, new System.Random(seed), spec.length, log, spec.name);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            log.AppendLine($"  built {spec.name}: {groundRocks.Count} ground + {ceilingRocks.Count} ceiling rocks, length {spec.length}, difficulty {spec.difficulty}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void PlaceRock(Transform anchor, GameObject prefab, Rock rock, string name, StringBuilder log, string sectionName)
    {
        float tip = Mathf.Clamp(rock.tip, rock.ceiling ? -MaxTip : LowestGroundTip, rock.ceiling ? HighestCeilingTip : MaxTip);
        if (!Mathf.Approximately(tip, rock.tip))
        {
            log.AppendLine($"  WARNING {sectionName}: {name} tip {rock.tip:0.00} is outside what a rock can reach, using {tip:0.00}.");
        }

        float scaleY;
        float y;
        if (rock.ceiling)
        {
            scaleY = (CeilingBottom + Buried - tip) / 2.4f;
            y = Buried - RockHalfHeight * scaleY;
        }
        else
        {
            scaleY = (tip - GroundTop + Buried) / 2.4f;
            y = RockHalfHeight * scaleY - Buried;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, anchor);
        instance.name = name;
        instance.transform.localPosition = new Vector3(rock.x, y, 0f);
        instance.transform.localScale = new Vector3(rock.width, scaleY, 1f);
    }

    // ---------------------------------------------------------------- decoration

    private struct DecorTemplate
    {
        public GameObject source;
        public Vector3 scale;
        public Color color;
        public bool ground;   // trees and bushes stand on the ground, clouds hang on the ceiling side
        public string name;
    }

    private static List<DecorTemplate> CollectDecor(ThemeDefinition theme)
    {
        var pool = new List<DecorTemplate>();
        foreach (LevelSection section in theme.Sections)
        {
            if (section == null)
            {
                continue;
            }

            foreach (SpriteRenderer sprite in section.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sprite.transform.parent == null || sprite.transform.parent.name != "Decor")
                {
                    continue;
                }

                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(sprite.gameObject);
                if (source == null)
                {
                    continue;
                }

                pool.Add(new DecorTemplate
                {
                    source = source,
                    scale = sprite.transform.localScale,
                    color = sprite.color,
                    ground = sprite.GetComponent<GroundSnap>() != null,
                    name = source.name,
                });
            }
        }

        return pool;
    }

    private static void PlaceDecor(LevelSection section, List<Rock> groundRocks, List<Rock> ceilingRocks, List<DecorTemplate> pool, System.Random random, float length, StringBuilder log, string sectionName)
    {
        var ground = pool.FindAll(t => t.ground);
        var clouds = pool.FindAll(t => !t.ground);
        if (ground.Count == 0 && clouds.Count == 0)
        {
            return;
        }

        PlaceProps(section.GroundAnchored, groundRocks, ground, random.Next(1, 3), 0.75f, 0.75f, length, random);
        PlaceProps(section.CeilingAnchored, ceilingRocks, clouds, random.Next(1, 3), -1.4f, -0.9f, length, random);
    }

    /// <summary>Puts up to <paramref name="count"/> props from the pool on one side, each far enough from the rocks of that side and from the piece ends.</summary>
    private static void PlaceProps(Transform anchor, List<Rock> sideRocks, List<DecorTemplate> templates, int count, float yFrom, float yTo, float length, System.Random random)
    {
        if (templates.Count == 0)
        {
            return;
        }

        var decor = new GameObject("Decor").transform;
        decor.SetParent(anchor, false);
        var placedX = new List<(float x, float half)>();

        for (int n = 0; n < count; n++)
        {
            DecorTemplate template = templates[random.Next(templates.Count)];
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(template.source, decor);
            instance.transform.localScale = template.scale;
            var sprite = instance.GetComponent<SpriteRenderer>();
            sprite.color = template.color;
            float half = sprite.sprite.bounds.extents.x * Mathf.Abs(template.scale.x);

            // Try random spots; keep the first that fits.
            bool placed = false;
            for (int attempt = 0; attempt < 60 && !placed; attempt++)
            {
                float x = Mathf.Lerp(LevelSection.SceneryClearance + half + 0.2f, length - LevelSection.SceneryClearance - half - 0.2f, (float)random.NextDouble());
                if (Fits(x, half, sideRocks, placedX))
                {
                    float y = Mathf.Lerp(yFrom, yTo, (float)random.NextDouble());
                    instance.transform.localPosition = new Vector3(x, y, 0f);
                    placedX.Add((x, half));
                    placed = true;
                }
            }

            if (!placed)
            {
                Object.DestroyImmediate(instance);
            }
        }

        if (decor.childCount == 0)
        {
            Object.DestroyImmediate(decor.gameObject);
        }
    }

    private static bool Fits(float x, float half, List<Rock> sideRocks, List<(float x, float half)> placed)
    {
        foreach (Rock rock in sideRocks)
        {
            float gap = Mathf.Abs(rock.x - x) - RockHalfWidth * rock.width - half;
            if (gap < DecorMinGap)
            {
                return false;
            }
        }

        foreach ((float x, float half) other in placed)
        {
            if (Mathf.Abs(other.x - x) - other.half - half < 1.5f)
            {
                return false;
            }
        }

        return true;
    }

    // ---------------------------------------------------------------- reporting

    /// <summary>For each section: the highest world speed (in steps of 1) at which the validator's plane can still fly it.</summary>
    private static string SpeedReport(List<LevelSection> sections)
    {
        var report = new StringBuilder();
        report.AppendLine("Highest world speed each section stays passable at (trusting 70% of the plane; the game's top speed is the validator's test speed):");
        foreach (LevelSection prefab in sections)
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, scene);
                instance.transform.position = Vector3.zero;
                Physics2D.SyncTransforms();
                var section = instance.GetComponent<LevelSection>();
                foreach (StarSlot slot in section.GetComponentsInChildren<StarSlot>(true))
                {
                    foreach (Collider2D c in slot.GetComponentsInChildren<Collider2D>(true))
                    {
                        c.enabled = false;
                    }
                }

                LevelSectionValidator.PlaneLimits plane = LevelSectionValidator.CreatePlaneLimits();
                LevelSectionValidator.FlightGrid grid = LevelSectionValidator.BuildGrid(scene, section, plane);
                float best = 0f;
                for (float speed = 4f; speed <= 24f; speed += 1f)
                {
                    plane.worldSpeed = speed;
                    if (LevelSectionValidator.Simulate(grid, section, plane, LevelSectionValidator.PlaneAbility, null, out _))
                    {
                        best = speed;
                    }
                    else
                    {
                        break;
                    }
                }

                report.AppendLine($"  {prefab.name}: passable up to world speed {best:0} (game top speed {LevelSectionValidator.CreatePlaneLimits().worldSpeed:0})");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        return report.ToString();
    }
}
