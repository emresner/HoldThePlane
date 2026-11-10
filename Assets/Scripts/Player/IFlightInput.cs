/// <summary>
/// Source of the "hold to rise" signal. Keeps flight logic independent of any specific input device.
/// </summary>
public interface IFlightInput
{
    bool IsHolding { get; }

    /// <summary>True only on the frame the button went down.</summary>
    bool PressedThisFrame { get; }
}
