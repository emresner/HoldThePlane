using System;
using UnityEngine;

/// <summary>
/// The list of sound effects: for every <see cref="SoundId"/> one or more layers (clips that start together), each with its own volume, pitch range and delay.
/// A crash is two layers (a heavy thump and a metal clang); a button click is one. Tuning a sound means editing this asset, not code.
/// </summary>
[CreateAssetMenu(menuName = "Audio/Sound Catalog", fileName = "SoundCatalog")]
public class SoundCatalog : ScriptableObject
{
    [Serializable]
    public class Layer
    {
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("Every play picks a random pitch between these two (1 = the clip as it is). A small range keeps repeated sounds from feeling copy-pasted.")]
        public Vector2 pitchRange = Vector2.one;
        [Tooltip("Seconds to wait before this layer starts.")]
        [Min(0f)] public float delay;
    }

    [Serializable]
    public class Sound
    {
        public SoundId id;
        public Layer[] layers = new Layer[0];
        [Tooltip("The same sound is skipped if it was played less than this many seconds ago (stops a burst of pickups from piling up).")]
        [Min(0f)] public float minInterval;
    }

    [SerializeField] private Sound[] sounds = new Sound[0];

    public bool TryGet(SoundId id, out Sound sound)
    {
        foreach (Sound candidate in sounds)
        {
            if (candidate.id == id)
            {
                sound = candidate;
                return true;
            }
        }

        sound = null;
        return false;
    }
}
