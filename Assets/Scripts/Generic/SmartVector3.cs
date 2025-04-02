using UnityEngine;

public struct SmartVector3
{
    private Vector3 _vector;
    private float _magnitude;
    private Vector3 _normalized;
    
    public Vector3 value
    {
        get => _vector;
        set
        {
            _vector = value;
            _magnitude = value.magnitude;
            _normalized = value / _magnitude;
        }
    }

    public float magnitude
    {
        get => _magnitude;
        set
        {
            _vector = _vector * (value / _magnitude);
            _magnitude = value;
            // normalized stays the same
        }
    }

    public Vector3 normalized
    {
        get => _normalized;
        set
        {
            _vector = value * _magnitude;
            // magnitude stays the same
            _normalized = value;
        }
    }
    
    public float sqrMagnitude => _magnitude * _magnitude;
    
    public SmartVector3(Vector3 vector) : this()
    {
        this.value = vector;
    }

    public SmartVector3(SmartVector3 other) : this()
    {
        this.value = other.value;
        this.magnitude = other.magnitude;
        this.normalized = other.normalized;
    }
}
