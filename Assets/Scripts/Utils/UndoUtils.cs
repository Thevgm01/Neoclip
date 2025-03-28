#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class UndoUtils
{
    public static void TryDestroyObjectImmediate(Object obj)
    {
        if (obj != null)
        {
            Undo.DestroyObjectImmediate(obj);
        }
    }
    
    public static void TryDestroyObjectsImmediate(Object[] objs)
    {
        foreach (Object obj in objs)
        {
            TryDestroyObjectImmediate(obj);
        }
    }
    
    public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
    {
        if (gameObject.TryGetComponent(out T component))
        {
            return component;
        }
        return Undo.AddComponent<T>(gameObject);
    }
}
#endif