using UnityEngine;

public abstract class ConditionallyUpdatedProperty<T>
{
    private T value;
    private int lastUpdate = 0;
    
    protected abstract int CurrentUpdate();

    protected abstract T PropertyFunction();
    
    public T Get()
    {
        int currentUpdate = CurrentUpdate();
        if (lastUpdate < currentUpdate)
        {
            lastUpdate = currentUpdate;
            value = PropertyFunction();
        }

        return value;
    }
}

public abstract class FrameCountUpdatedProperty<T> : ConditionallyUpdatedProperty<T>
{
    protected override int CurrentUpdate()
    {
        return Time.frameCount;
    }
}

public abstract class FixedFrameCountUpdatedProperty<T> : ConditionallyUpdatedProperty<T>
{
    protected override int CurrentUpdate()
    {
        return Utils.FixedUpdateCount;
    }
}