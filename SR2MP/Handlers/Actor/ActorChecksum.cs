using System;
using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Internal;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.Actor;

[PacketHandler((byte)PacketType.ActorChecksum, HandlerType.Client)]
internal sealed class ActorChecksumHandler : BasePacketHandler<ActorChecksumPacket>
{
    private static DateTime _lastResyncRequest = DateTime.MinValue;

    protected override bool Handle(ActorChecksumPacket packet, IPEndPoint? sender)
    {
        var hasMismatch = false;

        foreach (var entry in packet.Entries)
        {
            var localCount = GetActorCountInSceneGroup(entry.SceneGroupId);
            if (localCount != entry.ActorCount)
            {
                SrLogger.LogWarning($"[ActorChecksum] Drift detected in scene group {entry.SceneGroupId}! Host count: {entry.ActorCount}, Client count: {localCount}");
                hasMismatch = true;
            }
        }

        if (hasMismatch && (DateTime.UtcNow - _lastResyncRequest) > TimeSpan.FromSeconds(30))
        {
            SrLogger.LogMessage("[ActorChecksum] Requesting resync due to actor drift.");
            _lastResyncRequest = DateTime.UtcNow;
            Main.Client.SendPacket(new ResyncRequestPacket());
        }

        return true;
    }

    private static int GetActorCountInSceneGroup(int sceneGroupId)
    {
        var count = 0;
        foreach (var pair in GlobalVariables.ActorManager.Actors)
        {
            var model = pair.Value;
            if (model == null || model.sceneGroup == null)
                continue;

            try
            {
                if (NetworkSceneManager.GetPersistentID(model.sceneGroup) == sceneGroupId)
                {
                    count++;
                }
            }
            catch
            {
                // Ignore any native retrieval failures
            }
        }
        return count;
    }
}
