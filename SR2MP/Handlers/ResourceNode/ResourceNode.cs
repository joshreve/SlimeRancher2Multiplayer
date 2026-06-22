using System.Net;
using Il2Cpp;
using Il2CppMonomiPark.SlimeRancher.World.ResourceNode;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.World;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.ResourceNode;

[PacketHandler((byte)PacketType.ResourceNode)]
internal sealed class ResourceNodeHandler : BasePacketHandler<ResourceNodePacket>
{
    protected override bool Handle(ResourceNodePacket packet, IPEndPoint? _)
    {
        var node = FindNode(packet.NodeId);
        if (node == null)
        {
            // If the server receives a state update but does not have the scene loaded,
            // we should still relay it to other clients who might have the scene loaded.
            if (Main.Server.IsRunning && !packet.RequestSpawn)
            {
                Main.Server.SendToAll(packet);
            }

            SrLogger.LogDebug($"ResourceNode {packet.NodeId} not found (likely unloaded on host)");
            return true;
        }

        HandlingPacket = true;

        if (Main.Server.IsRunning && packet.RequestSpawn)
        {
            // Server spawns the actual item (fallback for legacy clients)
            node.SpawnSingleResource();
            
            // Relay the packet to all clients so they wiggle the node in sync
            var relayPacket = new ResourceNodePacket
            {
                NodeId = packet.NodeId,
                State = (byte)Il2Cpp.ResourceNode.NodeState.HARVESTING,
                RequestSpawn = false
            };
            Main.Server.SendToAll(relayPacket);
        }
        else
        {
            // Client or Host wiggles or depletes the node based on state
            var model = node._model;
            if (model != null)
            {
                var state = (Il2Cpp.ResourceNode.NodeState)packet.State;
                model.nodeState = state;
                node.UpdateForState();
            }

            if (Main.Server.IsRunning)
            {
                // Relay the state update to all clients
                Main.Server.SendToAll(packet);
            }
        }

        HandlingPacket = false;
        return true;
    }

    private static Il2Cpp.ResourceNode? FindNode(string nodeId)
    {
        foreach (var director in ResourceNodeDirector.AllResourceDirectors)
        {
            if (director == null || director.NodeSpawners == null) continue;
            foreach (var spawner in director.NodeSpawners)
            {
                if (spawner != null && spawner._model != null && spawner._model.nodeId == nodeId)
                {
                    return spawner.AttachedNode;
                }
            }
        }
        return null;
    }
}
