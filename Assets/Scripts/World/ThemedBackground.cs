using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Shows the background map of the current theme and, while the theme changes, fades the new map in over the old one.
/// Every theme has its own background layer (a <see cref="ScrollingLayer"/>); only the two that are needed right now are switched on.
/// The fade is driven by <see cref="ThemeCycle.GetBackgroundBlend"/>, so it runs while the change of the ground passes over the screen.
/// The camera's clear colour fades along, so tall screens that show more than the map never see a hard edge.
/// Without a <see cref="ThemeCycle"/> (nothing drives it) the layers stay as they were set up: the menu shows the first theme only.
/// </summary>
[DefaultExecutionOrder(10)] // after ScrollingLayer has placed this frame's pieces
public class ThemedBackground : MonoBehaviour
{
    [System.Serializable]
    private class Entry
    {
        public ThemeDefinition theme;
        public ScrollingLayer layer;
    }

    [SerializeField] private ThemeCycle themeCycle;
    [SerializeField] private Entry[] backgrounds;

    private Camera cam;

    private void Awake()
    {
        cam = Camera.main;
    }

    private void LateUpdate()
    {
        if (themeCycle == null)
        {
            return;
        }

        themeCycle.GetBackgroundBlend(out ThemeDefinition from, out ThemeDefinition to, out float t);
        float fade = t * t * (3f - 2f * t); // smoothstep: starts and ends gently

        foreach (Entry entry in backgrounds)
        {
            bool isFrom = entry.theme == from;
            bool isTo = entry.theme == to;
            bool needed = isFrom || isTo;
            if (entry.layer.gameObject.activeSelf != needed)
            {
                entry.layer.gameObject.SetActive(needed);
            }

            if (needed)
            {
                // The new map lies on top of the old one and becomes visible; when nothing changes only one map is on (fully visible).
                bool fadingIn = isTo && from != to;
                Apply(entry.layer.transform, fadingIn ? fade : 1f, fadingIn ? 1 : 0);
            }
        }

        if (cam != null)
        {
            cam.backgroundColor = Color.Lerp(from.SkyColor, to.SkyColor, fade);
        }
    }

    /// <summary>Sets the opacity and drawing order of every piece of the layer (the layer copies its first piece to fill the screen).</summary>
    private static void Apply(Transform layer, float alpha, int order)
    {
        for (int i = 0; i < layer.childCount; i++)
        {
            var map = layer.GetChild(i).GetComponentInChildren<Tilemap>();
            map.color = new Color(1f, 1f, 1f, alpha);
            map.GetComponent<TilemapRenderer>().sortingOrder = order;
        }
    }
}
