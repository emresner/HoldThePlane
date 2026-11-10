using UnityEditor;
using UnityEngine;

/// <summary>Draws fields marked with <see cref="DebugReadOnlyAttribute"/> greyed out.</summary>
[CustomPropertyDrawer(typeof(DebugReadOnlyAttribute))]
public class DebugReadOnlyDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUI.PropertyField(position, property, label, true);
        }
    }
}
