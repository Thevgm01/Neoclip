using System.Collections.Generic;
using UnityEngine;

public static class UniformSpherePoints
{
    private static Dictionary<int, Vector3[]> vectorArraysByCount;

    // https://stackoverflow.com/a/44164075
    private static Vector3[] GeneratePoints(int count)
    {
        Vector3[] points = new Vector3[count];
        float negativeTwoOverCount = -2.0f / count;

        for (int i = 0; i < count; i++)
        {
            float phi = Mathf.Acos(1.0f + (i + 0.5f) * negativeTwoOverCount);
            float theta = Constants.TAU * Constants.PHI * i;
            
            points[i] = new Vector3(
                Mathf.Cos(theta) * Mathf.Sin(phi), 
                Mathf.Sin(theta) * Mathf.Sin(phi), 
                Mathf.Cos(phi));
        }

        return points;
    }
    
    public static Vector3[] GetCachedVectors(int count)
    {
        vectorArraysByCount ??= new Dictionary<int, Vector3[]>();

        if (!vectorArraysByCount.TryGetValue(count, out Vector3[] vectors))
        {
            vectors = GeneratePoints(count);
            vectorArraysByCount.Add(count, vectors);
        }
        
        return vectors;
    }
}
