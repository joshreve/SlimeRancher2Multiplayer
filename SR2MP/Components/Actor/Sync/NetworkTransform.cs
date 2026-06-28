using UnityEngine;
using SR2MP.Packets.Actor;
using SR2MP.Shared.Utils;

namespace SR2MP.Components.Actor.Sync;

internal sealed class NetworkTransform : NetworkComponent
{
    public override byte Key => DeltaRegistry.KeyTransform;

    public Vector3 PreviousPosition;
    public Vector3 NextPosition;
    private Vector3 lastSentPosition;

    public Quaternion PreviousRotation;
    public Quaternion NextRotation;
    private Quaternion lastSentRotation;

    public Vector3 SavedVelocity;
    private Vector3 lastSentVelocity;

    public float InterpolationStart;
    public float InterpolationEnd;
    private const float MaxExtrapolationTime = 0.5f;

    private const int ForceSendInterval = 10;
    private int skippedUpdates;
    private bool staggerInitialized;

    public NetworkTransform(NetworkActor actor) : base(actor)
    {
    }

    public override bool IsDirty()
    {
        var currentPosition = GameObject.transform.position;
        var currentRotation = GameObject.transform.rotation;
        var currentVelocity = Actor.rigidbody ? Actor.rigidbody.velocity : Vector3.zero;

        return currentPosition != lastSentPosition
            || currentRotation != lastSentRotation
            || currentVelocity != lastSentVelocity;
    }

    public override object GetCurrentData()
    {
        var currentPosition = GameObject.transform.position;
        var currentRotation = GameObject.transform.rotation;
        var currentVelocity = Actor.rigidbody ? Actor.rigidbody.velocity : Vector3.zero;

        if (!staggerInitialized)
        {
            var id = Actor.ActorId;
            if (id.Value != 0)
            {
                skippedUpdates = (int)(id.Value % ForceSendInterval);
                staggerInitialized = true;
            }
        }

        skippedUpdates = 0;
        lastSentPosition = currentPosition;
        lastSentRotation = currentRotation;
        lastSentVelocity = currentVelocity;

        PreviousPosition = currentPosition;
        PreviousRotation = currentRotation;
        NextPosition = currentPosition;
        NextRotation = currentRotation;

        return new TransformData
        {
            Position = currentPosition,
            Rotation = currentRotation,
            Velocity = currentVelocity
        };
    }

    public override void ResetDirty()
    {
    }

    public override void ApplyDelta(object data)
    {
        if (Actor.LocallyOwned || Actor.IsDestroyed)
            return;

        var t = (TransformData)data;
        PreviousPosition = GameObject.transform.position;
        PreviousRotation = GameObject.transform.rotation;
        NextPosition = t.Position;
        NextRotation = t.Rotation;
        SavedVelocity = t.Velocity;
        Actor.savedVelocity = t.Velocity; // Keep NetworkActor.savedVelocity updated for internal references if any
        InterpolationStart = UnityEngine.Time.unscaledTime;
        InterpolationEnd = InterpolationStart + Timers.ActorTimer;
    }

    public override void Update(float deltaTime)
    {
        if (Actor.LocallyOwned || Actor.IsDestroyed || InterpolationEnd <= InterpolationStart)
            return;

        var now = UnityEngine.Time.unscaledTime;

        if (now <= InterpolationEnd)
        {
            var t = Mathf.InverseLerp(InterpolationStart, InterpolationEnd, now);
            var targetPos = Vector3.Lerp(PreviousPosition, NextPosition, t);
            var targetRot = Quaternion.Lerp(PreviousRotation, NextRotation, t);

            if (GameObject.transform.position != targetPos)
                GameObject.transform.position = targetPos;

            if (GameObject.transform.rotation != targetRot)
                GameObject.transform.rotation = targetRot;
        }
        else
        {
            var extrapolationTime = Mathf.Min(now - InterpolationEnd, MaxExtrapolationTime);
            var targetPos = NextPosition + SavedVelocity * extrapolationTime;
            var targetRot = NextRotation;

            if (GameObject.transform.position != targetPos)
                GameObject.transform.position = targetPos;

            if (GameObject.transform.rotation != targetRot)
                GameObject.transform.rotation = targetRot;
        }

        if (Actor.rigidbody && Actor.rigidbody.velocity != SavedVelocity)
            Actor.rigidbody.velocity = SavedVelocity;
    }

    public bool ShouldForceSend()
    {
        if (!staggerInitialized)
        {
            var id = Actor.ActorId;
            if (id.Value != 0)
            {
                skippedUpdates = (int)(id.Value % ForceSendInterval);
                staggerInitialized = true;
            }
        }

        if (++skippedUpdates >= ForceSendInterval)
        {
            skippedUpdates = 0;
            return true;
        }
        return false;
    }
}
