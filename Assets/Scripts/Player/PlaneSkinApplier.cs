using UnityEngine;

/// <summary>
/// Puts the saved plane colour on the plane in the game: it gives the plane's Animator the controller of the chosen skin.
/// Done in Awake so the very first frame already shows the right colour.
/// </summary>
public class PlaneSkinApplier : MonoBehaviour
{
    [SerializeField] private PlaneSkinCatalog catalog;
    [SerializeField] private Animator animator;

    private void Awake()
    {
        if (catalog == null || catalog.Count == 0)
        {
            return;
        }

        int index = catalog.IndexOf(SaveSystem.Data.planeSkinId);
        animator.runtimeAnimatorController = catalog[index].flight;
    }
}
