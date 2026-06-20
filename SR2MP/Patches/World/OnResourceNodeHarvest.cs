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

        if (Main.Client.IsConnected)
        {
            var model = __instance._model;
            if (model != null)
            {
                var packet = new ResourceNodePacket
                {
                    NodeId = model.nodeId,
                    State = (byte)Il2Cpp.ResourceNode.NodeState.NONE,
                    RequestSpawn = true
                };
                Main.Client.SendPacket(packet);
            }
            return false; // Skip local spawn on client to prevent duplication
        }

        return true; // Allow execution on server/host
    }
}
