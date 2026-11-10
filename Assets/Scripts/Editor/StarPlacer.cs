using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Puts one <see cref="StarSlot"/> with two possible star spots into every section of a theme.
/// For each section it tries a grid of positions, keeps those the plane can collect within the section's comfort limit
/// (see <see cref="LevelSectionValidator.StarComfortByDifficulty"/>) and then picks two of them at random: easy sections get the trickier
/// positions, hard sections get positions that lie on the natural flight line. Running it again replaces the old stars.
/// After it, run Tools > Level > Validate Sections, and move a star by hand in Prefab Mode if one feels wrong.
/// </summary>
public static class StarPlacer
{
    private const string StarsChildName = "Stars";
    private const float StepX = 1f;
    private const float StepY = 0.5f;
    private const float MaxAbsY = 2.2f;
    private const float Clearance = 1.5f;          // a bit more than the validator's minimum distance to obstacle boxes
    private const float MinSpotSeparation = 10f;   // the two spots of a slot should be clearly different places

    [MenuItem("Tools/Level/Place Stars In Selected Theme")]
    public static void PlaceFromMenu()
    {
        var theme = Selection.activeObject as ThemeDefinition;
        if (theme == null)
        {
            Debug.LogError("Select a Theme asset (Assets/Data/Themes) in the Project window first.");
            return;
        }

        string kind = theme.name.Contains("Orange") ? "Silver" : theme.name.Contains("Green") ? "Bronze" : "Gold";
        var star = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/World/Collectibles/Star_{kind}.prefab");
        Debug.Log(Place(theme, star, 5000));
    }

    public static string Place(ThemeDefinition theme, GameObject starPrefab, int seed)
    {
        var log = new StringBuilder();
        LevelSectionValidator.PlaneLimits plane = LevelSectionValidator.CreatePlaneLimits();
        float radius = starPrefab.GetComponent<CircleCollider2D>().radius * starPrefab.transform.localScale.x;
        log.AppendLine($"Placing {starPrefab.name} in '{theme.name}' (simulating at world speed {plane.worldSpeed}, star radius {radius:0.00}).");

        for (int index = 0; index < theme.Sections.Count; index++)
        {
            LevelSection prefab = theme.Sections[index];
            var random = new System.Random(seed + 101 * index);
            List<Candidate> candidates = FindCandidates(prefab, plane, radius, log, out float baseNeed, out float limit);
            if (candidates.Count == 0)
            {
                log.AppendLine($"  {prefab.name}: NO valid star position found.");
                continue;
            }

            List<Candidate> pool = PoolFor(candidates, prefab.Difficulty);
            Candidate first = pool[random.Next(pool.Count)];
            Candidate? second = PickSecond(pool, candidates, first, random);

            AddSlot(prefab, starPrefab, first, second);
            string secondText = second.HasValue ? $"({second.Value.position.x:0.0}, {second.Value.position.y:0.00}) needs {second.Value.need * 100:0}%" : "none (no second spot far enough)";
            log.AppendLine($"  {prefab.name} (difficulty {prefab.Difficulty}, section alone {baseNeed * 100:0}%, limit {limit * 100:0}%, {candidates.Count} candidates): " +
                           $"A ({first.position.x:0.0}, {first.position.y:0.00}) needs {first.need * 100:0}%, B {secondText}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return log.ToString();
    }

    private struct Candidate
    {
        public Vector2 position;
        public float need;
    }

    private static List<Candidate> FindCandidates(LevelSection prefab, LevelSectionValidator.PlaneLimits plane, float radius, StringBuilder log, out float baseNeed, out float limit)
    {
        var result = new List<Candidate>();
        baseNeed = 0f;
        limit = 0f;

        Scene scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, scene);
            instance.transform.position = Vector3.zero;

            // Old stars must not count as obstacles.
            Transform oldStars = instance.transform.Find(StarsChildName);
            if (oldStars != null)
            {
                oldStars.gameObject.SetActive(false);
            }

            Physics2D.SyncTransforms();
            var section = instance.GetComponent<LevelSection>();
            var hazards = new List<Collider2D>();
            foreach (Collider2D collider in instance.GetComponentsInChildren<Collider2D>())
            {
                if (collider.GetComponent<Hazard>() != null)
                {
                    hazards.Add(collider);
                }
            }

            LevelSectionValidator.FlightGrid grid = LevelSectionValidator.BuildGrid(scene, section, plane);
            baseNeed = LevelSectionValidator.RequiredAbility(grid, section, plane, null);
            float comfort = LevelSectionValidator.StarComfortByDifficulty[Mathf.Clamp(prefab.Difficulty - 1, 0, 2)];
            limit = Mathf.Min(LevelSectionValidator.PlaneAbility, Mathf.Max(comfort, baseNeed + LevelSectionValidator.AbilityStep));

            for (float x = LevelSectionValidator.StarEdgeMargin + 0.5f; x <= section.Length - LevelSectionValidator.StarEdgeMargin - 0.5f; x += StepX)
            {
                for (float y = -MaxAbsY; y <= MaxAbsY + 0.001f; y += StepY)
                {
                    var position = new Vector2(x, y);
                    if (TooCloseToHazard(hazards, position))
                    {
                        continue;
                    }

                    var spot = new LevelSectionValidator.StarSpot { label = "candidate", position = position, radius = radius };
                    if (!LevelSectionValidator.Simulate(grid, section, plane, limit, spot, out _))
                    {
                        continue;
                    }

                    result.Add(new Candidate { position = position, need = LevelSectionValidator.RequiredAbility(grid, section, plane, spot) });
                }
            }
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }

        result.Sort((a, b) => a.need.CompareTo(b.need));
        return result;
    }

    private static bool TooCloseToHazard(List<Collider2D> hazards, Vector2 position)
    {
        foreach (Collider2D hazard in hazards)
        {
            if (Mathf.Sqrt(hazard.bounds.SqrDistance(position)) < Clearance)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Candidates are sorted from least to most demanding. Hard sections take the easiest third, easy ones the demanding half.</summary>
    private static List<Candidate> PoolFor(List<Candidate> sorted, int difficulty)
    {
        float from = difficulty >= 3 ? 0f : difficulty == 2 ? 0.3f : 0.5f;
        float to = difficulty >= 3 ? 0.3f : difficulty == 2 ? 0.7f : 1f;
        int start = Mathf.Min(sorted.Count - 1, Mathf.FloorToInt(sorted.Count * from));
        int end = Mathf.Clamp(Mathf.CeilToInt(sorted.Count * to), start + 1, sorted.Count);
        return sorted.GetRange(start, end - start);
    }

    private static Candidate? PickSecond(List<Candidate> pool, List<Candidate> all, Candidate first, System.Random random)
    {
        foreach (List<Candidate> source in new[] { pool, all })
        {
            var far = source.FindAll(c => Mathf.Abs(c.position.x - first.position.x) >= MinSpotSeparation);
            if (far.Count > 0)
            {
                return far[random.Next(far.Count)];
            }
        }

        return null;
    }

    private static void AddSlot(LevelSection prefab, GameObject starPrefab, Candidate first, Candidate? second)
    {
        string path = AssetDatabase.GetAssetPath(prefab);
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Transform old = root.transform.Find(StarsChildName);
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            var stars = new GameObject(StarsChildName).transform;
            stars.SetParent(root.transform, false);
            var slot = new GameObject("StarSlot_1");
            slot.transform.SetParent(stars, false);
            slot.AddComponent<StarSlot>();

            var spots = new List<Candidate> { first };
            if (second.HasValue)
            {
                spots.Add(second.Value);
            }

            spots.Sort((a, b) => a.position.x.CompareTo(b.position.x));
            for (int i = 0; i < spots.Count; i++)
            {
                var star = (GameObject)PrefabUtility.InstantiatePrefab(starPrefab, slot.transform);
                star.name = "Spot_" + (char)('A' + i);
                star.transform.localPosition = new Vector3(spots[i].position.x, spots[i].position.y, 0f);
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
