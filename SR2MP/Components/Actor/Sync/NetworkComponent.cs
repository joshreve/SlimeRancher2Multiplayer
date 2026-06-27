using UnityEngine;

namespace SR2MP.Components.Actor.Sync;

internal abstract class NetworkComponent
{
    public readonly NetworkActor Actor;
    public readonly GameObject GameObject;
    public abstract byte Key { get; }

    protected NetworkComponent(NetworkActor actor)
    {
        Actor = actor;
        GameObject = actor.gameObject;
    }

    public abstract bool IsDirty();
    public abstract object GetCurrentData();
    public abstract void ResetDirty();
    public abstract void ApplyDelta(object data);

    public virtual void Update(float deltaTime) {}
}
