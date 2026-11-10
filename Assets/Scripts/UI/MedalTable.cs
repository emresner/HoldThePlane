using System;
using UnityEngine;

/// <summary>Picks the medal for a score: bronze below the silver threshold, then silver, then gold.</summary>
[Serializable]
public class MedalTable
{
    [SerializeField] private Sprite bronze;
    [SerializeField] private Sprite silver;
    [SerializeField] private Sprite gold;
    [Tooltip("Scores from this value up get silver.")]
    [SerializeField, Min(1)] private int silverFrom = 600;
    [Tooltip("Scores from this value up get gold.")]
    [SerializeField, Min(1)] private int goldFrom = 1200;

    public Sprite SpriteFor(int score)
    {
        if (score >= goldFrom)
        {
            return gold;
        }

        return score >= silverFrom ? silver : bronze;
    }
}
