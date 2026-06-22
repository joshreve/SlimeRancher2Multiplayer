using HarmonyLib;
using SR2MP.Packets.World;
using SR2MP.Packets.Utils;

namespace SR2MP.Patches.World;

[HarmonyPatch(typeof(Il2Cpp.ResourceNode), nameof(Il2Cpp.ResourceNode.SpawnSingleResource))]
internal static class OnResourceNodeHarvest
{
    public static bool Prefix(Il2Cpp.ResourceNode __instance)
    {
        if (HandlingPacket)
            return true;

        // If not in a multiplayer session, allow normal vanilla behavior.
        if (!Main.Server.IsRunning && !Main.Client.IsConnected)
            return true;

        // Host always spawns locally — it is the spawn authority when it has the scene loaded.
        if (Main.Server.IsRunning)
            return true;

        // Client: delegate spawn to the host via RequestSpawn.
        // The host (or scene-owner, in a future phase) will execute the spawn
        // and relay the result to all clients via ActorSpawnPacket.
        // This prevents duplicate loot drops when both client and host spawn independently.
        var model = __instance._model;
        if (model != null)
        {
            var packet = new ResourceNodePacket
            {
                NodeId = model.nodeId,
                State = (byte)Il2Cpp.ResourceNode.NodeState.HARVESTING,
                RequestSpawn = true
            };
            Main.Client.SendPacket(packet);
        }

        return false;
    }
}
