using System;
using UnityEngine;

/// <summary>
/// A small sparkle where a pickup was collected. Every kind of pickup has its own burst (gold, silver and bronze stars sparkle in their own colour);
/// the kind is recognised by the picture of the collected object. It only listens to <see cref="PlaneCollector.Collected"/>;
/// score and the pickup's own pop animation do not know about it.
/// </summary>
public class PickupEffects : MonoBehaviour
{
    [Serializable]
    private class Entry
    {
        [Tooltip("The collectible's sprite (for example starGold).")]
        public Sprite icon;

        public ParticleSystem burst;

        [Min(0)] public int count = 7;
    }

    [SerializeField] private PlaneCollector collector;
    [SerializeField] private Entry[] entries = new Entry[0];

    private void OnEnable()
    {
        collector.Collected += Play;
    }

    private void OnDisable()
    {
        collector.Collected -= Play;
    }

    private void Play(Collectible collectible)
    {
        foreach (Entry entry in entries)
        {
            if (entry.icon == collectible.Icon)
            {
                ParticleBurst.At(entry.burst, collectible.transform.position, entry.count);
                return;
            }
        }
    }
}
