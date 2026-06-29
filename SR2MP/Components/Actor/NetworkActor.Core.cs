using System;
using System.Collections;
using System.Collections.Generic;
using Il2CppInterop.Runtime.Attributes;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Player.CharacterController;
using Il2CppMonomiPark.SlimeRancher.Regions;
using Il2CppMonomiPark.SlimeRancher.Slime;
using JetBrains.Annotations;
using SR2MP.Packets.Actor;
using SR2MP.Components.Actor.Sync;
using SR2MP.Shared.Managers;
using SR2MP.Shared.Utils;
using Starlight.Storage;
using Delegate = Il2CppSystem.Delegate;
using Type = Il2CppSystem.Type;
using UnityEngine;

namespace SR2MP.Components.Actor;

[InjectIntoIL]
internal sealed class NetworkActor : MonoBehaviour
{
    public RegionMember? RegionMember;
    private Identifiable identifiable;
    public ResourceCycle? cycle;
    public Rigidbody rigidbody;
    public SlimeEmotions emotions;
    private PlortModel? plortModel;

    public float SyncTimer = Timers.ActorTimer;
    public bool ShouldUpdateResourceState;
    public bool IsValid = true;
    public bool IsDestroyed;
    public byte AttemptedGetIdentifiable;
    public bool CachedLocallyOwned;

    public Vector3 savedVelocity;

    internal bool isSlime;
    private bool isResource;
    private bool isPlort;

    // Component Syncing fields
    private NetworkTransform? transformComponent;
    private NetworkSlimeEmotions? emotionsComponent;
    private NetworkResourceCycle? resourceComponent;
    private NetworkPlort? plortComponent;

    public readonly List<NetworkComponent> syncComponents = new();

    public ActorId ActorId
    {
        get
        {
            if (IsDestroyed)
            {
                IsValid = false;
                return new ActorId(0);
            }

            if (identifiable != null)
                return GetActorIdSafe();

            if (AttemptedGetIdentifiable >= 10)
            {
                SrLogger.LogWarning("Failed to get Identifiable after 10 attempts");
                IsValid = false;
                return new ActorId(0);
            }

            try
            {
                identifiable = GetComponent<Identifiable>();
                AttemptedGetIdentifiable++;
            }
            catch (Exception ex)
            {
                SrLogger.LogWarning($"Failed to get Identifiable component: {ex.Message}");
                AttemptedGetIdentifiable++;
                IsValid = false;
                return new ActorId(0);
            }

            return identifiable ? GetActorIdSafe() : new ActorId(0);
        }
    }

    private ActorId GetActorIdSafe()
    {
        try
        {
            return identifiable.GetActorId();
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"Failed to get ActorId: {ex.Message}");
            IsValid = false;
            return new ActorId(0);
        }
    }

    public ActorModel? ActorModel
    {
        get
        {
            var id = ActorId;
            if (id.Value != 0 && ActorManager.Actors.TryGetValue(id.Value, out var model))
            {
                return model.Cast<ActorModel>();
            }
            return null;
        }
    }

    public bool LocallyOwned { get; set; }

    // Backwards Compatibility Delegated Properties (for Spawning manager & other places)
    public Vector3 previousPosition
    {
        get => transformComponent?.PreviousPosition ?? Vector3.zero;
        set { if (transformComponent != null) transformComponent.PreviousPosition = value; }
    }
    public Vector3 nextPosition
    {
        get => transformComponent?.NextPosition ?? Vector3.zero;
        set { if (transformComponent != null) transformComponent.NextPosition = value; }
    }
    public Quaternion previousRotation
    {
        get => transformComponent?.PreviousRotation ?? Quaternion.identity;
        set { if (transformComponent != null) transformComponent.PreviousRotation = value; }
    }
    public Quaternion nextRotation
    {
        get => transformComponent?.NextRotation ?? Quaternion.identity;
        set { if (transformComponent != null) transformComponent.NextRotation = value; }
    }

    public void SetResourceState(ResourceCycle.State state, double progress, bool force = false)
    {
        resourceComponent?.SetResourceState(state, progress, force);
    }

    public void Start()
    {
        try
        {
            if (GetComponent<Gadget>() || GetComponent<SRCharacterController>())
            {
                Destroy(this);
                return;
            }

            emotions     = GetComponent<SlimeEmotions>();
            rigidbody    = GetComponent<Rigidbody>();
            identifiable = GetComponent<Identifiable>();
            cycle        = GetComponent<ResourceCycle>();
            RegionMember = GetComponent<RegionMember>();

            CachedLocallyOwned = LocallyOwned;

            GetActorType();
            
            SetRigidbodyState(LocallyOwned);

            // Initialize plain C# components dynamically based on type
            transformComponent = new NetworkTransform(this);
            syncComponents.Add(transformComponent);

            if (isSlime)
            {
                emotionsComponent = new NetworkSlimeEmotions(this);
                syncComponents.Add(emotionsComponent);
            }
            else if (isResource)
            {
                resourceComponent = new NetworkResourceCycle(this);
                syncComponents.Add(resourceComponent);
            }
            else if (isPlort)
            {
                plortComponent = new NetworkPlort(this);
                syncComponents.Add(plortComponent);
            }

            if (RegionMember != null)
                SetupHibernationEvent();
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"NetworkActor.Start error: {ex}");
            IsValid = false;
        }
    }

    private void GetActorType()
    {
        if (ActorId.Value == 0 || !GameState.identifiables.TryGetValue(ActorId, out var identModel))
            return;

        isSlime    = identModel.TryCast<SlimeModel>()   != null;
        isResource = identModel.TryCast<ProduceModel>() != null;
        isPlort    = identModel.TryCast<PlortModel>()   != null;
    }

    private void SetupHibernationEvent()
    {
        try
        {
            var delegateType = Type.GetType("MonomiPark.SlimeRancher.Regions.RegionMember")
                ?.GetEvent("BeforeHibernationChanged")
                ?.EventHandlerType;

            if (delegateType == null)
                return;

            var hibernationDelegate = Delegate.CreateDelegate(
                delegateType,
                Cast<Il2CppSystem.Object>(),
                nameof(HibernationChanged),
                true);

            RegionMember?.add_BeforeHibernationChanged(hibernationDelegate.Cast<RegionMember.OnHibernationChange>());
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"Failed to add hibernation event: {ex.Message}");
        }
    }

    [HideFromIl2Cpp]
    private IEnumerator WaitOneFrameOnHibernationChange(bool hibernating)
    {
        yield return null;

        if (!IsValid || IsDestroyed)
            yield break;

        try
        {
            var actorId = ActorId;

            if (actorId.Value == 0)
                yield break;

            LocallyOwned = !hibernating;

            if (hibernating)
                Main.SendToAllOrServer(new ActorUnloadPacket { ActorId = actorId });
            else
                Main.SendToAllOrServer(new ActorTransferPacket { ActorId = actorId, OwnerId = LocalID });
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"WaitOneFrameOnHibernationChange error: {ex}");
            IsValid = false;
        }
    }

    public void HibernationChanged(bool value)
    {
        if (!IsValid || IsDestroyed)
            return;

        try
        {
            ContextShortcuts.StartCoroutine(WaitOneFrameOnHibernationChange(value));
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"HibernationChanged error: {ex}");
        }
    }

    public void Update()
    {
        if (IsDestroyed)
            return;

        if (!IsValid)
        {
            IsDestroyed = true;
            Destroy(this);
            return;
        }

        try
        {
            // Plain C# component updates
            float dt = UnityEngine.Time.deltaTime;
            for (int i = 0; i < syncComponents.Count; i++)
            {
                syncComponents[i].Update(dt);
            }

            HandleOwnershipChange();

            // Check and sync critical state updates (Emotions, Resources, Plorts) every frame
            if (LocallyOwned && IsCloseToAnyPlayer(MaxSyncDistance))
                SendStateUpdate();

            SyncTimer -= UnityEngine.Time.unscaledDeltaTime;

            if (SyncTimer >= 0)
                return;

            SyncTimer = Timers.ActorTimer;

            // Check and sync transform updates on the unscaled interval
            if (LocallyOwned)
            {
                if (IsCloseToAnyPlayer(MaxSyncDistance))
                    SendWorldUpdate();
            }
            else
            {
                CheckAndStealOwnership();
            }
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"NetworkActor.Update error: {ex}");
            IsValid = false;
        }
    }

    private void HandleOwnershipChange()
    {
        if (CachedLocallyOwned != LocallyOwned)
        {
            SetRigidbodyState(LocallyOwned);

            if (LocallyOwned && rigidbody)
                rigidbody.velocity = savedVelocity;
        }

        CachedLocallyOwned = LocallyOwned;
    }

    private void SetRigidbodyState(bool locallyOwned)
    {
        if (rigidbody == null || IsDestroyed)
            return;

        try
        {
            rigidbody.constraints = locallyOwned
                ? RigidbodyConstraints.None
                : RigidbodyConstraints.FreezeAll;

            if (locallyOwned)
            {
                rigidbody.WakeUp();
            }
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"SetRigidbodyState error: {ex.Message}");
        }
    }

    [HideFromIl2Cpp]
    private void CheckAndStealOwnership()
    {
        if (LocallyOwned || (!Main.Client.IsConnected && !Main.Server.IsRunning))
            return;

        var actorModel = ActorModel;
        if (actorModel == null || actorModel.sceneGroup == null)
            return;

        var localPlayer = SceneContext.Instance?.player;
        if (!localPlayer)
            return;

        var localPlayerPos = localPlayer.transform.position;
        var actorPos = transform.position;
        var localDistSq = (localPlayerPos - actorPos).sqrMagnitude;

        const float maxOwnershipStealDistance = 25f;
        if (localDistSq > maxOwnershipStealDistance * maxOwnershipStealDistance)
            return;

        var sceneGroupId = NetworkSceneManager.GetPersistentID(actorModel.sceneGroup);
        var playersInScene = GlobalVariables.ScenePresenceManager.GetPlayersInSceneGroup(sceneGroupId);

        const float ownershipHysteresis = 5f;
        var localDist = Mathf.Sqrt(localDistSq);

        foreach (var playerId in playersInScene)
        {
            if (playerId == LocalID)
                continue;

            var remotePlayer = GlobalVariables.PlayerManager.GetPlayer(playerId);
            if (remotePlayer == null)
                continue;

            var remotePlayerPos = remotePlayer.Position;
            var remoteDistSq = (remotePlayerPos - actorPos).sqrMagnitude;
            var remoteDist = Mathf.Sqrt(remoteDistSq);

            if (localDist >= remoteDist - ownershipHysteresis)
            {
                return;
            }
        }

        LocallyOwned = true;
        var actorId = ActorId;
        if (actorId.Value != 0)
        {
            var packet = new ActorTransferPacket { ActorId = actorId, OwnerId = LocalID };
            Main.SendToAllOrServer(packet);
        }
    }

    [UsedImplicitly]
    public void OnDestroy()
    {
        IsDestroyed = true;
        IsValid     = false;

        if (LocallyOwned)
        {
            if (SystemContext.Instance != null && SystemContext.Instance.SceneLoader != null && SystemContext.Instance.SceneLoader.IsSceneLoadInProgress)
                return;

            try
            {
                var actorId = ActorId;
                if (actorId.Value != 0)
                {
                    ActorManager.Actors.Remove(actorId.Value);

                    if (Main.Server.IsRunning || Main.Client.IsConnected)
                    {
                        var packet = new ActorDestroyPacket { ActorId = actorId };
                        Main.SendToAllOrServer(packet);
                    }
                }
            }
            catch
            {
                // ignored
            }
        }
    }

    private bool IsCloseToAnyPlayer(float maxDistance)
    {
        if (SceneContext.Instance == null || SceneContext.Instance.player == null)
            return false;

        var localPlayerPos = SceneContext.Instance.player.transform.position;
        if (Vector3.SqrMagnitude(localPlayerPos - transform.position) <= maxDistance * maxDistance)
            return true;

        foreach (var remotePlayer in PlayerManager.GetAllPlayers())
        {
            if (Vector3.SqrMagnitude(remotePlayer.Position - transform.position) <= maxDistance * maxDistance)
                return true;
        }

        return false;
    }

    [HideFromIl2Cpp]
    private void SendStateUpdate()
    {
        var actorId = ActorId;
        if (actorId.Value == 0)
            return;

        var deltas = new List<DeltaValue>();

        // Check Emotions, ResourceCycle, Plort components (everything except Transform)
        for (int i = 0; i < syncComponents.Count; i++)
        {
            var comp = syncComponents[i];
            if (comp.Key != DeltaRegistry.KeyTransform && comp.IsDirty())
            {
                deltas.Add(new DeltaValue { Key = comp.Key, Value = comp.GetCurrentData() });
                comp.ResetDirty();
            }
        }

        if (deltas.Count > 0)
        {
            Main.SendToAllOrServer(new ActorDeltaPacket
            {
                ActorId = actorId,
                Deltas = deltas
            });
        }
    }

    [HideFromIl2Cpp]
    private void SendWorldUpdate()
    {
        var actorId = ActorId;
        if (actorId.Value == 0)
            return;

        var deltas = new List<DeltaValue>();

        if (transformComponent != null)
        {
            if (transformComponent.IsDirty() || transformComponent.ShouldForceSend())
            {
                deltas.Add(new DeltaValue { Key = transformComponent.Key, Value = transformComponent.GetCurrentData() });
                transformComponent.ResetDirty();
            }
        }

        if (deltas.Count > 0)
        {
            Main.SendToAllOrServer(new ActorDeltaPacket
            {
                ActorId = actorId,
                Deltas = deltas
            });
        }
    }

    [HideFromIl2Cpp]
    public void ApplyDelta(ActorDeltaPacket packet)
    {
        if (LocallyOwned || IsDestroyed)
            return;

        foreach (var delta in packet.Deltas)
        {
            var comp = FindComponent(delta.Key);
            if (comp != null)
            {
                comp.ApplyDelta(delta.Value);
            }
        }
    }

    [HideFromIl2Cpp]
    private NetworkComponent? FindComponent(byte key)
    {
        for (int i = 0; i < syncComponents.Count; i++)
        {
            if (syncComponents[i].Key == key)
                return syncComponents[i];
        }
        return null;
    }
}