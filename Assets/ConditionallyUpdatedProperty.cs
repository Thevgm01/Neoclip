using System;
using UnityEngine;

public abstract class ConditionallyUpdatedProperty<T>
{
    private T value;
    private float lastUpdate = -1.0f;
    private readonly Func<T> propertyFunction;
    
    protected abstract float CurrentUpdate();

    public T GetValue()
    {
        float currentUpdate = CurrentUpdate();
        if (lastUpdate < currentUpdate)
        {
            lastUpdate = currentUpdate;
            value = propertyFunction();
        }

        return value;
    }

    protected ConditionallyUpdatedProperty(Func<T> propertyFunction)
    {
        this.propertyFunction = propertyFunction;
    }
}

public class TimeUpdatedProperty<T> : ConditionallyUpdatedProperty<T>
{
    protected override float CurrentUpdate()
    {
        return Time.time;
    }

    public TimeUpdatedProperty(Func<T> propertyFunction) : base(propertyFunction) {}
}

public class FixedTimeUpdatedProperty<T> : ConditionallyUpdatedProperty<T>
{
    protected override float CurrentUpdate()
    {
        return Time.fixedTime;
    }
    
    public FixedTimeUpdatedProperty(Func<T> propertyFunction) : base(propertyFunction) {}
}