using System;
using UnityEngine;

public class IgnoreBackfacesTemporary : IDisposable
{
    public IgnoreBackfacesTemporary()
    {
        Physics.queriesHitBackfaces = false;
    }
    
    public void Dispose()
    {
        Physics.queriesHitBackfaces = true;
    }
}
