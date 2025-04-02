using UnityEngine;

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

    public SmartVector3(SmartVector3 other) : this()
    {
        this.Value = other.Value;
        this.Magnitude = other.Magnitude;
        this.Normalized = other.Normalized;
    }
    
    public static implicit operator Vector3(SmartVector3 smartVector) => smartVector.Value;
    public static implicit operator SmartVector3(Vector3 vector) => new SmartVector3(vector);
}
