using System.Collections.Generic;
using UnityEngine;

public static class SpherePointGenerator
{
    // https://stackoverflow.com/a/44164075
    public static Vector3[] GenerateGoldenSpiralShell(int count)
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

    public static Vector3[] GenerateGridSphere(float radius, float spacing)
    {
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
