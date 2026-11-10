using System;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// The one place that plays sound effects and owns the master volume. It lives in its own prefab (Resources/AudioManager) that is created
/// once when the game starts and survives scene changes, so a button click is not cut off when its button loads a new scene.
/// Playing is a pool of <see cref="AudioSource"/>s (no objects are created while playing); volume and mute go to the mixer's "MasterVolume" parameter
/// and are remembered through <see cref="SaveSystem"/>.
/// </summary>
public class AudioManager : MonoBehaviour
{
    private const string VolumeParameter = "MasterVolume";
    private const float SilenceDecibels = -80f;

    [SerializeField] private AudioMixer mixer;
    [SerializeField] private AudioMixerGroup effectsGroup;
    [SerializeField] private SoundCatalog catalog;
    [Tooltip("How many sounds can play at the same time. A new one takes the oldest voice when all are busy.")]
    [SerializeField, Min(1)] private int voiceCount = 8;

    private AudioSource[] voices;
    private float[] voiceFreeAt;
    private float[] lastPlayed;
    private bool settingsDirty;

    public static AudioManager Instance { get; private set; }

    /// <summary>Raised when the volume or the mute switch changed (the volume control redraws itself).</summary>
    public event Action SettingsChanged;

    /// <summary>Master volume from 0 (silent) to 1 (full).</summary>
    public float Volume => SaveSystem.Data.masterVolume;

    public bool Muted => SaveSystem.Data.muted;

    /// <summary>True when nothing can be heard: muted, or the slider is at zero.</summary>
    public bool IsSilent => Muted || Volume <= 0.001f;

    // Created before the first scene loads, so every scene (including the one opened directly in the Editor) already has it.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Create()
    {
        if (Instance != null)
        {
            return;
        }

        AudioManager prefab = Resources.Load<AudioManager>("AudioManager");
        if (prefab == null)
        {
            Debug.LogError("Assets/Resources/AudioManager.prefab is missing: the game will be silent.");
            return;
        }

        Instantiate(prefab).name = "AudioManager";
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        voices = new AudioSource[voiceCount];
        voiceFreeAt = new float[voiceCount];
        lastPlayed = new float[Enum.GetValues(typeof(SoundId)).Length];
        Array.Fill(lastPlayed, float.NegativeInfinity);

        for (int i = 0; i < voiceCount; i++)
        {
            var voice = new GameObject("Voice " + i).AddComponent<AudioSource>();
            voice.transform.SetParent(transform, false);
            voice.playOnAwake = false;
            voice.outputAudioMixerGroup = effectsGroup;
            voices[i] = voice;
        }
    }

    private void Start()
    {
        // Applied in Start as well: the mixer ignores a value set in the very first frame on some platforms.
        ApplyVolume();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>Plays a sound effect. <paramref name="pitchMultiplier"/> scales every layer's pitch; <paramref name="delay"/> postpones all layers.</summary>
    public void Play(SoundId id, float pitchMultiplier = 1f, float delay = 0f)
    {
        if (!catalog.TryGet(id, out SoundCatalog.Sound sound))
        {
            return;
        }

        float now = Time.realtimeSinceStartup;
        if (now - lastPlayed[(int)id] < sound.minInterval)
        {
            return;
        }

        lastPlayed[(int)id] = now;
        foreach (SoundCatalog.Layer layer in sound.layers)
        {
            if (layer.clip == null)
            {
                continue;
            }

            float pitch = UnityEngine.Random.Range(layer.pitchRange.x, layer.pitchRange.y) * pitchMultiplier;
            StartLayer(layer, pitch, delay + layer.delay, now);
        }
    }

    /// <summary>Sets the master volume (0 to 1). It is applied at once; write it to storage with <see cref="SaveSettings"/>.</summary>
    public void SetVolume(float volume)
    {
        SaveSystem.Data.masterVolume = Mathf.Clamp01(volume);
        settingsDirty = true;
        ApplyVolume();
    }

    public void SetMuted(bool muted)
    {
        SaveSystem.Data.muted = muted;
        settingsDirty = true;
        ApplyVolume();
        SaveSettings();
    }

    /// <summary>Writes the volume and mute switch to storage if they changed (not on every slider tick: storage is slow on the web).</summary>
    public void SaveSettings()
    {
        if (!settingsDirty)
        {
            return;
        }

        settingsDirty = false;
        SaveSystem.Save();
    }

    private void StartLayer(SoundCatalog.Layer layer, float pitch, float delay, float now)
    {
        AudioSource voice = TakeVoice(now, out int index);
        voice.clip = layer.clip;
        voice.volume = layer.volume;
        voice.pitch = pitch;
        voiceFreeAt[index] = now + delay + layer.clip.length / Mathf.Max(0.1f, pitch);

        if (delay > 0f)
        {
            voice.PlayDelayed(delay);
        }
        else
        {
            voice.Play();
        }
    }

    // A free voice if there is one, otherwise the one that is going to finish first.
    private AudioSource TakeVoice(float now, out int index)
    {
        index = 0;
        for (int i = 0; i < voices.Length; i++)
        {
            if (voiceFreeAt[i] <= now)
            {
                index = i;
                break;
            }

            if (voiceFreeAt[i] < voiceFreeAt[index])
            {
                index = i;
            }
        }

        voices[index].Stop();
        return voices[index];
    }

    private void ApplyVolume()
    {
        mixer.SetFloat(VolumeParameter, Muted ? SilenceDecibels : ToDecibels(Volume));
        SettingsChanged?.Invoke();
    }

    // Loudness is felt on a logarithmic scale, so the slider (linear 0..1) is converted to decibels: 0.5 = -6 dB, 0.1 = -20 dB.
    private static float ToDecibels(float volume)
    {
        return volume <= 0.0001f ? SilenceDecibels : Mathf.Max(SilenceDecibels, 20f * Mathf.Log10(volume));
    }
}
