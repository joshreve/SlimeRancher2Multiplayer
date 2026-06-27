using System.Collections.Generic;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Components.Actor;
using UnityEngine;

namespace SR2MP.Shared.Managers;

internal sealed class ClientSpawnRegistry
{
    public struct PendingSpawn
    {
        public long TempId;
        public int ActorType;
        public Vector3 Position;
        public GameObject GameObject;
        public float RegisteredTime;
    }

    private readonly Dictionary<long, PendingSpawn> _pending = new();

    public void Register(long tempId, int actorType, Vector3 pos, GameObject obj)
    {
        _pending[tempId] = new PendingSpawn
        {
            TempId = tempId,
            ActorType = actorType,
            Position = pos,
            GameObject = obj,
            RegisteredTime = Time.realtimeSinceStartup
        };
        SrLogger.LogMessage($"[ClientSpawnRegistry] Registered tempId {tempId} for actorType {actorType}");
    }

    public void ConfirmAndRemap(long tempId, long canonicalId)
    {
        if (!_pending.TryGetValue(tempId, out var pending))
        {
            SrLogger.LogWarning($"[ClientSpawnRegistry] Tried to confirm unregistered tempId {tempId}");
            return;
        }

        _pending.Remove(tempId);

        if (!pending.GameObject)
        {
            SrLogger.LogWarning($"[ClientSpawnRegistry] GameObject for tempId {tempId} was already destroyed");
            return;
        }

        var identifiableActor = pending.GameObject.GetComponent<IdentifiableActor>();
        if (!identifiableActor)
        {
            SrLogger.LogWarning($"[ClientSpawnRegistry] GameObject for tempId {tempId} has no IdentifiableActor");
            return;
        }

        var model = identifiableActor._model;
        if (model == null)
        {
            SrLogger.LogWarning($"[ClientSpawnRegistry] Model for tempId {tempId} is null");
            return;
        }

        var oldId = model.actorId.Value;

        // Remove old mapping from ActorManager.Actors and GameState
        GlobalVariables.ActorManager.Actors.Remove(oldId);
        if (GlobalVariables.GameState != null)
        {
            GlobalVariables.GameState.identifiables.Remove(new ActorId(oldId));
        }

        // Set the new canonical ID on the model
        model.actorId = new ActorId(canonicalId);

        // Re-add to ActorManager.Actors and GameState
        GlobalVariables.ActorManager.Actors[canonicalId] = model;
        if (GlobalVariables.GameState != null)
        {
            GlobalVariables.GameState.identifiables[model.actorId] = model;
        }

        // Also update the NetworkActor component if it exists
        var netActor = pending.GameObject.GetComponent<NetworkActor>();
        if (netActor)
        {
            netActor.IsValid = true;
        }

        SrLogger.LogMessage($"[ClientSpawnRegistry] Remapped tempId {tempId} -> canonicalId {canonicalId}");
    }

    public void CleanupExpired(float timeoutSeconds = 10f)
    {
        var now = Time.realtimeSinceStartup;
        var toRemove = new List<long>();

        foreach (var pair in _pending)
        {
            if (now - pair.Value.RegisteredTime > timeoutSeconds)
            {
                toRemove.Add(pair.Key);
                if (pair.Value.GameObject)
                {
                    SrLogger.LogWarning($"[ClientSpawnRegistry] TempId {pair.Key} expired without confirmation, destroying GameObject");
                    Object.Destroy(pair.Value.GameObject);
                }
            }
        }

        foreach (var tempId in toRemove)
        {
            _pending.Remove(tempId);
        }
    }

    public void Clear()
    {
        _pending.Clear();
        SrLogger.LogMessage("[ClientSpawnRegistry] Cleared all pending spawns");
    }
}
