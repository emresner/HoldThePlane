using UnityEngine;

/// <summary>
/// A "place for one star" inside a level section. Its children are the possible spots; when the section appears,
/// one of them is picked at random and the others are switched off, so the same section shows its star in a different place each run.
/// </summary>
public class StarSlot : MonoBehaviour
{
    [Tooltip("Testing: 0, 1, ... always uses that child. -1 picks at random (normal play).")]
    [SerializeField] private int forcedSpot = -1;

    [field: SerializeField, DebugReadOnly] public int ChosenSpot { get; private set; }

    private void Awake()
    {
        int count = transform.childCount;
        if (count == 0)
        {
            return;
        }

        ChosenSpot = forcedSpot >= 0 ? Mathf.Min(forcedSpot, count - 1) : Random.Range(0, count);
        for (int i = 0; i < count; i++)
        {
            transform.GetChild(i).gameObject.SetActive(i == ChosenSpot);
        }
    }
}
