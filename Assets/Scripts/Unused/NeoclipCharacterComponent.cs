using UnityEngine;

public abstract class NeoclipCharacterComponent : MonoBehaviour
{
    public abstract void Init();

    public virtual void Tick() {}
}
