using System;
using UnityEngine;

/// <summary>
/// The list of plane colours the player can choose from. Every colour has two animators:
/// one for the plane in the game and one for the preview in the menu. The first colour uses the original ("parent")
/// Animator Controllers; every other colour uses an Animator Override Controller that swaps only the animation clip (the sprites)
/// and keeps the rest of the parent controller as it is.
/// </summary>
[CreateAssetMenu(menuName = "Plane/Skin Catalog", fileName = "PlaneSkinCatalog")]
public class PlaneSkinCatalog : ScriptableObject
{
    [Serializable]
    public class Skin
    {
        [Tooltip("Stable name that is written to the save file. Do not change it once players have saves.")]
        public string id;

        [Tooltip("Animator of the plane in the game scene.")]
        public RuntimeAnimatorController flight;

        [Tooltip("Animator of the plane shown in the menu.")]
        public RuntimeAnimatorController preview;
    }

    [SerializeField] private Skin[] skins = new Skin[0];

    public int Count => skins.Length;

    public Skin this[int index] => skins[index];

    /// <summary>The position of the skin with this id; 0 (the first skin) when the id is empty or unknown.</summary>
    public int IndexOf(string id)
    {
        for (int i = 0; i < skins.Length; i++)
        {
            if (skins[i].id == id)
            {
                return i;
            }
        }

        return 0;
    }
}
