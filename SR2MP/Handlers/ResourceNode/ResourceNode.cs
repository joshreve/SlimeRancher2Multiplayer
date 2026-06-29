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
    protected override bool Handle(ResourceNodePacket packet, IPEndPoint? sender)
    {
        var node = FindNode(packet.NodeId);

        // --- Host receives RequestSpawn from a client ---
        if (Main.Server.IsRunning && packet.RequestSpawn)
        {
            if (node != null)
            {
                if (node._model != null && node._model.nodeState == Il2Cpp.ResourceNode.NodeState.HARVESTED)
                {
                    return true;
                }

                // Host has the scene loaded — spawn the loot locally.
                // The spawned actor flows through OnActorSpawn → ActorSpawnPacket relay.
                HandlingPacket = true;
                try
                {
                    node.SpawnSingleResource();
                }
                catch (System.Exception ex)
                {
                    SrLogger.LogDebug($"Failed to spawn resource from node {packet.NodeId}: {ex.Message}");
                }
                HandlingPacket = false;

                // Relay a non-spawn state update to all clients so they wiggle the node in sync
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
                // Host does NOT have the scene loaded. Relay the RequestSpawn back to
                // ALL clients — the client that has the node loaded will execute the spawn.
                // This is a temporary delegation until ScenePresenceManager (Phase 2) enables
                // targeted delegation to the specific peer with the scene loaded.
                Main.Server.SendToAll(packet);
            }

            return true;
        }

        // --- Client receives relayed RequestSpawn (host didn't have scene loaded) ---
        if (!Main.Server.IsRunning && packet.RequestSpawn)
        {
            if (node != null)
            {
                if (node._model != null && node._model.nodeState == Il2Cpp.ResourceNode.NodeState.HARVESTED)
                {
                    return true;
                }

                // This client has the scene loaded — execute the spawn locally.
                // The spawned actor flows through OnActorSpawn → ActorSpawnPacket → host relay.
                try
                {
                    node.SpawnSingleResource();
                }
                catch (System.Exception ex)
                {
                    SrLogger.LogDebug($"Failed to spawn resource from node {packet.NodeId}: {ex.Message}");
                }

                // Send state update (non-spawn) to host for relay to other clients
                var statePacket = new ResourceNodePacket
                {
                    NodeId = packet.NodeId,
                    State = (byte)Il2Cpp.ResourceNode.NodeState.HARVESTING,
                    RequestSpawn = false
                };
                Main.Client.SendPacket(statePacket);
            }
            // If this client also doesn't have the node, silently ignore — another client will handle it.

            return true;
        }

        // --- State update (non-spawn): wiggle or deplete the node ---
        if (node == null)
        {
            // Host relays state updates even when the node isn't locally loaded,
            // so other clients who do have it can apply the visual state.
            if (Main.Server.IsRunning)
            {
                Main.Server.SendToAll(packet);
            }

            SrLogger.LogDebug($"ResourceNode {packet.NodeId} not found (likely unloaded)");
            return true;
        }

        HandlingPacket = true;

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
