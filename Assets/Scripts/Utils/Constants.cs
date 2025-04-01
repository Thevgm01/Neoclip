using UnityEngine;

public static class Constants
{
    public enum Density
    {
        Nothing   =        0,
        Water     =  1000000,
        Air       =     1204,
        Clipspace =    20000,
        Meat      =  1010000,
        Custom    =       -1,
    }
    
    public const float TAU = Mathf.PI * 2.0f;
    public const float PHI = 1.618033988749894f;
}
