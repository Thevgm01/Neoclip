#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class EditorUtils
{
    public static void TryDestroyObjectsImmediate(Object[] objs)
    {
        foreach (Object obj in objs)
        {
            TryDestroyObjectImmediate(obj);
        }
    }
    
    public static void TryDestroyObjectImmediate(Object obj)
    {
        if (obj != null)
        {
            Undo.DestroyObjectImmediate(obj);
        }
    }
}
#endif