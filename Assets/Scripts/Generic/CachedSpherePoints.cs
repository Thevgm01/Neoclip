using System.Collections.Generic;
using UnityEngine;

public static class CachedSpherePoints
{
    public abstract class ArrayDictionary<T>
    {
        private Dictionary<T, Vector3[]> arraysByKey;

        protected abstract Vector3[] Generate(T key);
        
        public Vector3[] Get(T key)
        {
            arraysByKey ??= new Dictionary<T, Vector3[]>();

            if (!arraysByKey.TryGetValue(key, out Vector3[] vectors))
            {
                vectors = Generate(key);
                arraysByKey.Add(key, vectors);
            }

            return vectors;
        }
    }
    
    public class GoldenSpiralShell : ArrayDictionary<int>
    {
        // https://stackoverflow.com/a/44164075
        protected override Vector3[] Generate(int count)
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
    }
    
    public class SolidSphere : ArrayDictionary<(float, float)>
    {
        protected override Vector3[] Generate((float, float) radiusAndSpacing)
        {
            float radius = radiusAndSpacing.Item1;
            float spacing = radiusAndSpacing.Item2;
            List<Vector3> gridPoints = new();
        
            for (float x = spacing * 0.5f; x < radius; x += spacing)
                for (float y = spacing * 0.5f; y < radius; y += spacing)
                    for (float z = spacing * 0.5f; z < radius; z += spacing)
                        if (new Vector3(x, y, z).sqrMagnitude < radius * radius)
                        {
                            gridPoints.Add(new Vector3( x,  y,  z));
                            gridPoints.Add(new Vector3(-x,  y,  z));
                            gridPoints.Add(new Vector3( x, -y,  z));
                            gridPoints.Add(new Vector3(-x, -y,  z));
                            gridPoints.Add(new Vector3( x,  y, -z));
                            gridPoints.Add(new Vector3(-x,  y, -z));
                            gridPoints.Add(new Vector3( x, -y, -z));
                            gridPoints.Add(new Vector3(-x, -y, -z));
                        }
            
            return gridPoints.ToArray();
        }
    }
    
    public static GoldenSpiralShell goldenSpiralShellInstance = new();
    public static SolidSphere solidSphereInstance = new();
}
