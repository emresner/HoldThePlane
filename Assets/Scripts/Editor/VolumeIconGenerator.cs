using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Draws the small white pictures of the volume control (speaker, two sound waves, a slash, a circle) as PNG files in Assets/UI/Icons.
/// They are our own simple shapes, so no outside icon pack is needed; the Image components tint them. Run it again to redraw them.
/// Menu: Tools → UI → Generate Volume Icons.
/// </summary>
public static class VolumeIconGenerator
{
    private const string Folder = "Assets/UI/Icons";
    private const int IconSize = 128;
    private const int CircleSize = 64;
    private const int Samples = 4; // 4x4 samples per pixel: smooth, antialiased edges

    // Shared coordinates (0..1, y up). The waves are circle arcs around the speaker's mouth.
    private static readonly Vector2 WaveCenter = new Vector2(0.55f, 0.5f);
    private static readonly Vector2[] SpeakerShape =
    {
        new Vector2(0.19f, 0.40f), new Vector2(0.33f, 0.40f), new Vector2(0.52f, 0.24f),
        new Vector2(0.52f, 0.76f), new Vector2(0.33f, 0.60f), new Vector2(0.19f, 0.60f)
    };

    [MenuItem("Tools/UI/Generate Volume Icons")]
    public static void Generate()
    {
        Directory.CreateDirectory(Folder);

        Write("Speaker", IconSize, p => InsidePolygon(SpeakerShape, p) || DistanceToPolygon(SpeakerShape, p) <= 0.03f, false);
        Write("SpeakerWaveInner", IconSize, p => DistanceToArc(p, 0.20f, 40f) <= 0.03f, false);
        Write("SpeakerWaveOuter", IconSize, p => DistanceToArc(p, 0.33f, 40f) <= 0.03f, false);
        Write("SpeakerSlash", IconSize, p => DistanceToSegment(p, new Vector2(0.18f, 0.82f), new Vector2(0.84f, 0.18f)) <= 0.045f, false);
        Write("UiCircle", CircleSize, p => Vector2.Distance(p, new Vector2(0.5f, 0.5f)) <= 0.5f, true);

        AssetDatabase.Refresh();
        Debug.Log("Volume icons written to " + Folder);
    }

    private static void Write(string name, int size, Func<Vector2, bool> isInside, bool sliceable)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int hits = 0;
                for (int sy = 0; sy < Samples; sy++)
                {
                    for (int sx = 0; sx < Samples; sx++)
                    {
                        var point = new Vector2((x + (sx + 0.5f) / Samples) / size, (y + (sy + 0.5f) / Samples) / size);
                        if (isInside(point))
                        {
                            hits++;
                        }
                    }
                }

                byte alpha = (byte)Mathf.RoundToInt(255f * hits / (Samples * Samples));
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }

        texture.SetPixels32(pixels);
        string path = $"{Folder}/{name}.png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.spritePixelsPerUnit = 100f;
        if (sliceable)
        {
            importer.spriteBorder = new Vector4(31f, 31f, 31f, 31f);
        }

        importer.SaveAndReimport();
    }

    private static bool InsidePolygon(Vector2[] polygon, Vector2 p)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            bool crosses = (polygon[i].y > p.y) != (polygon[j].y > p.y)
                && p.x < (polygon[j].x - polygon[i].x) * (p.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x;
            if (crosses)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private static float DistanceToPolygon(Vector2[] polygon, Vector2 p)
    {
        float best = float.MaxValue;
        for (int i = 0; i < polygon.Length; i++)
        {
            best = Mathf.Min(best, DistanceToSegment(p, polygon[i], polygon[(i + 1) % polygon.Length]));
        }

        return best;
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * t);
    }

    // Distance to a circle arc that opens to the right: from -halfAngle to +halfAngle around the wave centre.
    private static float DistanceToArc(Vector2 p, float radius, float halfAngleDegrees)
    {
        Vector2 offset = p - WaveCenter;
        float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        if (Mathf.Abs(angle) <= halfAngleDegrees)
        {
            return Mathf.Abs(offset.magnitude - radius);
        }

        float rad = halfAngleDegrees * Mathf.Deg2Rad;
        Vector2 top = WaveCenter + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
        Vector2 bottom = WaveCenter + new Vector2(Mathf.Cos(rad), -Mathf.Sin(rad)) * radius;
        return Mathf.Min(Vector2.Distance(p, top), Vector2.Distance(p, bottom));
    }
}
