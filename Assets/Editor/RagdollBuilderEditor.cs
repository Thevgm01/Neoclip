#if UNITY_EDITOR

using UnityEditor;
using UnityEngine.UIElements;

[CustomEditor(typeof(RagdollBuilder))]
public class RagdollBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        RagdollBuilder builder = (RagdollBuilder)target;

        if (DrawDefaultInspector())
        {
            
        }
    }
}
#endif