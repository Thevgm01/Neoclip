using System;
using UnityEngine;

// Normally the only way to select a layer in the inspector is with a LayerMask. However, if you only want one layer
// (and you want the integer that it corresponds to) it's a pain to convert between them. So this class simply wraps
// an int value with EditorGUI.LayerField to get a nice display.
[Serializable]
public struct LayerNumber
{
    public int value;
}

#if UNITY_EDITOR
[UnityEditor.CustomPropertyDrawer(typeof(LayerNumber))]
public class LayerNumberDrawer : UnityEditor.PropertyDrawer
{
    public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
    {
        UnityEditor.SerializedProperty layerNumber = property.FindPropertyRelative("value");
        layerNumber.intValue = UnityEditor.EditorGUI.LayerField(position, label, layerNumber.intValue);
    }
}
#endif