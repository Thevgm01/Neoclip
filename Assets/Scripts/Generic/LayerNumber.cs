using System;
using UnityEngine;

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