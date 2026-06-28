using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Actor;
using System;
using UnityEngine;

namespace SR2MP.Components.Actor.Sync;

internal sealed class NetworkResourceCycle : NetworkComponent
{
    public override byte Key => DeltaRegistry.KeyResource;

    private readonly ResourceCycle? cycle;
    private ResourceCycle.State lastSentResourceState;
    private double lastSentResourceProgress;

    private ResourceCycle.State? prevResourceState;
    private bool? cachedCycleReleasing;
    private bool shouldUpdateResourceState;

    private bool? CycleReleasing => cycle?._preparingToRelease;

    public NetworkResourceCycle(NetworkActor actor) : base(actor)
    {
        cycle = GameObject.GetComponent<ResourceCycle>();
    }

    public override bool IsDirty()
    {
        if (cycle?._model == null)
            return false;

        return cycle._model.state != lastSentResourceState ||
               System.Math.Abs(cycle._model.progressTime - lastSentResourceProgress) > 0.0001;
    }

    public override object GetCurrentData()
    {
        if (cycle?._model == null)
        {
            return new ResourceData
            {
                State = ResourceCycle.State.UNRIPE,
                Progress = 0
            };
        }

        var currentState = cycle._model.state;
        var currentProgress = cycle._model.progressTime;

        lastSentResourceState = currentState;
        lastSentResourceProgress = currentProgress;

        return new ResourceData
        {
            State = currentState,
            Progress = currentProgress
        };
    }

    public override void ResetDirty()
    {
    }

    public override void ApplyDelta(object data)
    {
        var r = (ResourceData)data;
        SetResourceState(r.State, r.Progress);
    }

    public override void Update(float deltaTime)
    {
        UpdateResourceState();
        HandleCycleReleasing();
    }

    private void UpdateResourceState()
    {
        if (Actor.LocallyOwned || cycle == null || cycle._model == null || shouldUpdateResourceState)
            return;

        cycle._model.progressTime = double.MaxValue;
        shouldUpdateResourceState = false;
    }

    private void HandleCycleReleasing()
    {
        if (CycleReleasing != cachedCycleReleasing)
        {
            if (CycleReleasing == true)
            {
                var actorId = Actor.ActorId;
                if (actorId.Value != 0)
                {
                    Main.SendToAllOrServer(new ActorTransferPacket { ActorId = actorId, OwnerId = LocalID });
                }
            }
        }

        cachedCycleReleasing = CycleReleasing;
    }

    public void SetResourceState(ResourceCycle.State state, double progress, bool force = false)
    {
        if (cycle == null)
            return;

        shouldUpdateResourceState = true;

        if (cycle._model != null && cycle._model.progressTime != progress)
            cycle._model.progressTime = progress;

        if (!force && prevResourceState == state)
            return;

        prevResourceState = state;

        try
        {
            if (cycle._model != null)
                cycle._model.state = state;

            ApplyResourceStateChanges(state);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"SetResourceState error: {ex}");
        }
    }

    private void ApplyResourceStateChanges(ResourceCycle.State state)
    {
        switch (state)
        {
            case ResourceCycle.State.UNRIPE:
                HandleUnripeState();
                break;
            case ResourceCycle.State.RIPE:
                HandleRipeState();
                break;
            case ResourceCycle.State.EDIBLE:
                HandleEdibleState();
                break;
            case ResourceCycle.State.ROTTEN:
                cycle!.SetRotten(false);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

    private void HandleUnripeState()
    {
        if (GameObject.transform.localScale.x < cycle!._defaultScale.x * 0.33f)
            GameObject.transform.localScale = cycle._defaultScale * 0.33f;

        if (cycle._vacuumable)
            cycle._vacuumable.enabled = false;

        if (Actor.rigidbody && cycle._joint != null)
            Actor.rigidbody.isKinematic = true;
    }

    private void HandleRipeState()
    {
        if (cycle!._vacuumable)
            cycle._vacuumable.enabled = true;

        if (GameObject.transform.localScale.x < cycle._defaultScale.x)
            GameObject.transform.localScale = cycle._defaultScale;

        if (cycle._joint == null)
            return;

        if (Actor.rigidbody)
        {
            Actor.rigidbody.isKinematic = false;
            Actor.rigidbody.WakeUp();
        }

        cycle.DetachFromJoint();
    }

    private void HandleEdibleState()
    {
        if (cycle!._vacuumable)
        {
            cycle._vacuumable.enabled = true;
            cycle._vacuumable.Pending = false;
        }

        if (Actor.rigidbody)
        {
            Actor.rigidbody.isKinematic = false;
            Actor.rigidbody.WakeUp();
        }

        if (cycle._joint != null)
            cycle.DetachFromJoint();

        cycle._preparingToRelease = false;

        if (cycle.ToShake)
            cycle.ToShake.localPosition = cycle._toShakeDefaultPos;
    }
}
