using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(LayerNumber))]
public class LayerNumberDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty layerNumber = property.FindPropertyRelative("value");
        layerNumber.intValue = EditorGUI.LayerField(position, label, layerNumber.intValue);
    }
}
