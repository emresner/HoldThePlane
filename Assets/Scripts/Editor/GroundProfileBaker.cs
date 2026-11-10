using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Measures the wavy top edge of the ground strip image once and stores it in a <see cref="GroundProfile"/> asset,
/// so the game can look up the ground height at any spot without touching the image at run time.
/// Run it again if the strip image ever changes.
/// </summary>
public static class GroundProfileBaker
{
    private const string StripImagePath = "Assets/Graphics/kenney-tappy-plane/PNG/groundIce.png";
    private const string ProfileAssetPath = "Assets/Data/GroundProfile_Ice.asset";
    private const float PixelsPerUnit = 100f;
    private const float OpaqueAlpha = 0.5f;

    [MenuItem("Tools/Level/Bake Ground Profile")]
    public static void BakeFromMenu()
    {
        Debug.Log(Bake());
    }

    public static string Bake()
    {
        // Read the PNG file directly, so the Kenney import settings (which are not readable) stay untouched.
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.LoadImage(File.ReadAllBytes(StripImagePath));
        Color32[] pixels = texture.GetPixels32();
        int width = texture.width;
        int height = texture.height;
        Object.DestroyImmediate(texture);

        var heights = new float[width];
        int alphaLimit = Mathf.RoundToInt(OpaqueAlpha * 255f);
        for (int x = 0; x < width; x++)
        {
            int top = 0;
            for (int y = height - 1; y >= 0; y--)
            {
                if (pixels[y * width + x].a > alphaLimit)
                {
                    top = y + 1;
                    break;
                }
            }

            heights[x] = top / PixelsPerUnit;
        }

        var profile = AssetDatabase.LoadAssetAtPath<GroundProfile>(ProfileAssetPath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<GroundProfile>();
            AssetDatabase.CreateAsset(profile, ProfileAssetPath);
        }

        profile.SetData(width / PixelsPerUnit, height / PixelsPerUnit, heights);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        return $"Ground profile baked: {width} columns, strip {width / PixelsPerUnit:0.00} x {height / PixelsPerUnit:0.00}, surface between {profile.MinHeight:0.00} and {profile.MaxHeight:0.00} above the strip bottom.";
    }
}
