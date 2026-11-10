using System;

/// <summary>
/// Everything the game remembers between sessions, as one plain object. It is turned into JSON by <see cref="SaveSystem"/>.
/// A new field gets a default value here; saves written before the field existed simply load with that default.
/// </summary>
[Serializable]
public class SaveData
{
    /// <summary>Bump this only if an existing field changes meaning (then convert old saves in <see cref="SaveSystem"/>).</summary>
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;

    public int bestScore;

    /// <summary>Id of the chosen plane colour (see <see cref="PlaneSkinCatalog"/>). Empty or unknown = the first one in the catalog.</summary>
    public string planeSkinId = "";

    /// <summary>Master volume from 0 to 1 (see <see cref="AudioManager"/>). A first-time player starts at half volume.</summary>
    public float masterVolume = 0.5f;

    /// <summary>True when the player switched the sound off with the speaker button; the volume setting stays as it was.</summary>
    public bool muted;
}
