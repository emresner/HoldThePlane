/// <summary>
/// Every sound effect the game can play. Scripts ask for a sound by this name; which clips, volume and pitch it means is set in the <see cref="SoundCatalog"/>.
/// New values are added at the END (the catalog asset stores the numbers).
/// </summary>
public enum SoundId
{
    Crash,
    StarPickup,
    ScoreMilestone,
    NewBest,
    ButtonMain,
    ButtonLight,
    SelectorTap,
    CountdownTick,
    CountdownGo
}
