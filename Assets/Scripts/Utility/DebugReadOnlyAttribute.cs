using UnityEngine;

/// <summary>
/// Shows a field in the Inspector greyed out, so runtime values (current speed, current tier...) can be watched
/// without being able to edit them by accident. Works on fields and, with <c>[field: DebugReadOnly]</c>, on auto-properties.
/// </summary>
public class DebugReadOnlyAttribute : PropertyAttribute
{
}
