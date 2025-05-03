using UnityEngine;

/// <summary>
/// A Vector3 that caches its magnitude and normal vectors when created or modified.
/// Additionally, the magnitude and normal can be changed independently, and the main Vector3 is automatically adjusted..
/// </summary>
public struct SmartVector3
{
    private Vector3 _vector;
    private float _magnitude;
    private Vector3 _normalized;
    
    public Vector3 Value
    {
        get => _vector;
        set
        {
            _vector = value;
            _magnitude = value.magnitude;
            _normalized = value / _magnitude;
        }
    }

    public float Magnitude
    {
        get => _magnitude;
        set
        {
            _vector = _vector * (value / _magnitude);
            _magnitude = value;
            // normalized stays the same
        }
    }

    public Vector3 Normalized
    {
        get => _normalized;
        set
        {
            // Currently we're just trusting that 'value' is actually normalized
            _vector = value * _magnitude;
            // magnitude stays the same
            _normalized = value;
        }
    }
    
    public float SqrMagnitude => _magnitude * _magnitude;
    
    public SmartVector3(Vector3 vector) : this()
    {
        this.Value = vector;
    }

    public SmartVector3(SmartVector3 other)
    {
        this._vector = other._vector;
        this._magnitude = other._magnitude;
        this._normalized = other._normalized;
    }
    
    public static implicit operator Vector3(SmartVector3 smartVector) => smartVector.Value;
    public static implicit operator SmartVector3(Vector3 vector) => new SmartVector3(vector);
    
    public static SmartVector3 operator +(SmartVector3 a, Vector3 b) => new SmartVector3(a.Value + b);
    public static SmartVector3 operator +(Vector3 a, SmartVector3 b) => new SmartVector3(a + b.Value);
    public static SmartVector3 operator -(SmartVector3 a, Vector3 b) => new SmartVector3(a.Value - b);
    public static SmartVector3 operator -(Vector3 a, SmartVector3 b) => new SmartVector3(a - b.Value);
    
    public static SmartVector3 operator +(SmartVector3 a, SmartVector3 b) => new SmartVector3(a.Value + b.Value);
    public static SmartVector3 operator -(SmartVector3 a, SmartVector3 b) => new SmartVector3(a.Value - b.Value);
    
    public static SmartVector3 operator *(SmartVector3 a, float f) => new SmartVector3(a.Value * f);
    public static SmartVector3 operator *(float f, SmartVector3 a) => new SmartVector3(a.Value * f);
    public static SmartVector3 operator /(SmartVector3 a, float f) => new SmartVector3(a.Value / f);
}
