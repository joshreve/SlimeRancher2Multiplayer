using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Components.Actor;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.Actor;

[PacketHandler((byte)PacketType.ActorSpawn)]
internal sealed class ActorSpawnHandler : BasePacketHandler<ActorSpawnPacket>
{
    protected override bool Handle(ActorSpawnPacket packet, IPEndPoint? sender)
    {
        var isServer = Main.Server.IsRunning;
        var isTempId = packet.ActorId.Value >= 3000000000L;

        if (isServer && isTempId && sender != null)
        {
            var canonicalId = NetworkActorManager.AllocateCanonicalActorId();
            var oldTempId = packet.ActorId.Value;

            SrLogger.LogMessage($"[ActorSpawnHandler] Server remapping tempId {oldTempId} -> canonicalId {canonicalId} for client {sender}");

            // Update local packet copy
            packet.ActorId = new ActorId(canonicalId);

            // Send confirmation back to the originating client
            var confirmPacket = new ActorSpawnConfirmPacket
            {
                ClientTempId = oldTempId,
                HostCanonicalId = canonicalId
            };
            Main.Server.SendToClient(confirmPacket, sender);

            // Relay the updated spawn packet to all other clients
            Main.Server.SendToAllExcept(packet, sender);

            // Spawn the actor locally on the host
            SpawnActorLocally(packet);

            return false; // Prevent automatic relay
        }

        if (ActorManager.Actors.ContainsKey(packet.ActorId.Value))
        {
            // SrLogger.LogDebug($"Actor {packet.ActorId.Value} already exists");
            return false;
        }

        SpawnActorLocally(packet);
        return true;
    }

    private static void SpawnActorLocally(ActorSpawnPacket packet)
    {
        ActorManager.TrySpawnNetworkActor(packet.ActorId, packet.Position, packet.Rotation, packet.ActorType, packet.SceneGroup, out var actor);

        if (actor == null)
            return;

        var spawnedObj = actor.GetGameObject();
        if (spawnedObj)
        {
            var netActor = spawnedObj.GetComponent<NetworkActor>();
            if (netActor)
            {
                netActor.OwnerId = packet.OwnerId;
            }
        }

        if (packet.MaterialIndex != (byte)SprinkleMaterialType.none)
        {
            var gameObj = actor.GetGameObject();
            if (gameObj)
                StartCoroutine(NetworkActorManager.ApplySprinkleMaterial(gameObj, (SprinkleMaterialType)packet.MaterialIndex));
        }

        var slime = actor.TryCast<SlimeModel>();
        if (slime == null)
            return;

        slime.firstAppearanceSaveSet = packet.FirstAppearance;
        slime.secondAppearanceSaveSet = packet.SecondAppearance;
        slime.Emotions = packet.Emotions;
        slime.isSleeping = packet.Sleeping;

        if (packet.Radiancy != (byte)ActorAppearanceType.Default)
            NetworkActorManager.ApplyRadiancy(slime, (ActorAppearanceType)packet.Radiancy);
    }
}