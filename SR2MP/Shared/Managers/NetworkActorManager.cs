using System.Collections;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Components.Actor;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Loading;

namespace SR2MP.Shared.Managers;

internal sealed partial class NetworkActorManager
{
    public readonly Dictionary<long, IdentifiableModel> Actors = new();
    public readonly Dictionary<int, IdentifiableType> ActorTypes = new();
    public readonly Dictionary<IdentifiableType, int> TypeToId = new();

    public static int GetPersistentID(IdentifiableType type)
    {
        if (type == null) return -1;
        if (GlobalVariables.ActorManager != null && GlobalVariables.ActorManager.TypeToId.TryGetValue(type, out var id))
            return id;
        return -1;
    }

    internal void Initialize(GameContext context)
    {
        ActorTypes.Clear();
        TypeToId.Clear();
        Actors.Clear();

        int nextId = 1;
        foreach (var type in context.AutoSaveDirector._saveReferenceTranslation._identifiableTypeLookup)
        {
            if (type.value == null) continue;
            ActorTypes[nextId] = type.value;
            TypeToId[type.value] = nextId;
            nextId++;
        }

        ActorTypes[-1] = null!;

        StartCoroutine(ZoneLoadingLoop());
    }

    private int _previousSceneGroupId = -1;

    private IEnumerator ZoneLoadingLoop()
    {
        while (true)
        {
            yield return new WaitForSceneGroupLoad(false);
            yield return new WaitForSceneGroupLoad();

            if (!Main.Server.IsRunning && !Main.Client.IsConnected)
                continue;

            if (!SystemContext.Instance.SceneLoader.IsCurrentSceneGroupGameplay())
                continue;

            var gameModel = SceneContext.Instance?.GameModel;
            if (!gameModel)
                continue;

            var scene = SystemContext.Instance.SceneLoader.CurrentSceneGroup;
            var currentSceneGroupId = NetworkSceneManager.GetPersistentID(scene);

            // Broadcast scene presence changes for spawn delegation and ownership.
            BroadcastScenePresence(currentSceneGroupId);

            foreach (var actor in gameModel!.identifiables)
            {
                if (actor.value.ident.IsPlayer)
                    continue;

                if (actor.value.TryCast<ActorModel>() == null)
                    continue;

                var obj = actor.value.GetGameObject();
                if (!obj)
                    continue;

                Object.Destroy(obj);
                Actors.Remove(actor.value.actorId.Value);
            }

            foreach (var actor2 in gameModel.identifiables)
            {
                if (actor2.value.ident.IsPlayer)
                    continue;

                var model = actor2.value.TryCast<ActorModel>();

                if (model == null)
                    continue;

                if (!model.ident.prefab)
                    continue;

                if (actor2.value.sceneGroup != scene)
                    continue;

                HandlingPacket = true;
                var obj = InstantiationHelpers.InstantiateActorFromModel(model);
                HandlingPacket = false;

                if (!obj)
                    continue;

                var networkComponent = obj.AddComponent<NetworkActor>();

                networkComponent.previousPosition = model.lastPosition;
                networkComponent.nextPosition     = model.lastPosition;
                networkComponent.previousRotation = model.lastRotation;
                networkComponent.nextRotation     = model.lastRotation;

                ActorManager.Actors.Add(model.actorId.Value, model);
            }

            yield return TakeOwnershipOfNearby();
        }
    }

    private static bool ActorIDAlreadyInUse(ActorId id)
        => SceneContext.Instance?.GameModel?.TryGetIdentifiableModel(id, out _) ?? false;

    public static long GetHighestActorIdInRange(long min, long max)
    {
        var result = min;
        foreach (var actor in GameState.identifiables)
        {
            var id = actor.value.actorId.Value;
            if (id < min || id >= max)
                continue;
            if (id > result)
                result = id;
        }

        return result;
    }

    internal IEnumerator TakeOwnershipOfNearby()
    {
        const int max = 12;

        if (SceneContext.Instance == null || SceneContext.Instance.player == null)
            yield break;

        var player = SceneContext.Instance.player;
        var bounds = new Bounds(player.transform.position, new Vector3(600, 1250, 600));
        
        var i = 0;
        foreach (var actor in Actors)
        {
            if (actor.Value == null)
                continue;
            
            if (!bounds.Contains(actor.Value.lastPosition))
                continue;

            if (!actor.Value.TryGetNetworkComponent(out var netActor))
                continue;

            if (!IsLocalPlayerNearest(actor.Value, player.transform.position))
                continue;

            netActor.LocallyOwned = true;

            var actorId = netActor.ActorId;
            if (actorId.Value == 0)
                continue;

            var packet = new ActorTransferPacket { ActorId = actorId, OwnerId = LocalID };
            Main.SendToAllOrServer(packet);
            i++;

            if (i <= max)
                continue;
            
            yield return null;
            i = 0;
        }
    }

    public static long AllocateCanonicalActorId()
    {
        if (GameState == null || GameState._actorIdProvider == null)
            return 0;

        var provider = GameState._actorIdProvider;
        var id = provider._nextActorId;
        provider._nextActorId++;
        return id;
    }

    private static bool IsLocalPlayerNearest(IdentifiableModel actorModel, Vector3 localPlayerPos)
    {
        if (actorModel.sceneGroup == null)
            return true;

        var sceneGroupId = NetworkSceneManager.GetPersistentID(actorModel.sceneGroup);
        var actorPos = actorModel.lastPosition;
        var localDistSq = (localPlayerPos - actorPos).sqrMagnitude;

        var playersInScene = GlobalVariables.ScenePresenceManager.GetPlayersInSceneGroup(sceneGroupId);
        foreach (var playerId in playersInScene)
        {
            if (playerId == LocalID)
                continue;

            var remotePlayer = GlobalVariables.PlayerManager.GetPlayer(playerId);
            if (remotePlayer == null)
                continue;

            var remoteDistSq = (remotePlayer.Position - actorPos).sqrMagnitude;
            if (remoteDistSq < localDistSq)
                return false;
        }

        return true;
    }

    private void BroadcastScenePresence(int currentSceneGroupId)
    {
        if (_previousSceneGroupId != -1 && _previousSceneGroupId != currentSceneGroupId)
        {
            var exitPacket = new ScenePresencePacket
            {
                PlayerId = LocalID,
                SceneGroupId = _previousSceneGroupId,
                Entered = false
            };
            GlobalVariables.ScenePresenceManager.OnPlayerExitedScene(LocalID, _previousSceneGroupId);
            Main.SendToAllOrServer(exitPacket);
        }

        var enterPacket = new ScenePresencePacket
        {
            PlayerId = LocalID,
            SceneGroupId = currentSceneGroupId,
            Entered = true
        };
        GlobalVariables.ScenePresenceManager.OnPlayerEnteredScene(LocalID, currentSceneGroupId);
        Main.SendToAllOrServer(enterPacket);

        _previousSceneGroupId = currentSceneGroupId;
    }
}