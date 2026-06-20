using HarmonyLib;
using SR2MP.Packets.World;
using SR2MP.Packets.Utils;

namespace SR2MP.Patches.World;

[HarmonyPatch(typeof(Il2Cpp.ResourceNode), "SetStateHarvesting")]
internal static class OnResourceNodeHarvesting
{
    public static void Postfix(Il2Cpp.ResourceNode __instance)
    {
        if (HandlingPacket)
            return;

        var model = __instance._model;
        if (model != null)
        {
            var packet = new ResourceNodePacket
            {
                NodeId = model.nodeId,
                State = (byte)Il2Cpp.ResourceNode.NodeState.HARVESTING,
                RequestSpawn = false
            };
            Main.SendToAllOrServer(packet);
        }
    }
}

[HarmonyPatch(typeof(Il2Cpp.ResourceNode), "SetStateEmpty")]
internal static class OnResourceNodeEmpty
{
    public static void Postfix(Il2Cpp.ResourceNode __instance)
    {
        if (HandlingPacket)
            return;

        var model = __instance._model;
        if (model != null)
        {
            var packet = new ResourceNodePacket
            {
                NodeId = model.nodeId,
                State = (byte)Il2Cpp.ResourceNode.NodeState.HARVESTED,
                RequestSpawn = false
            };
            Main.SendToAllOrServer(packet);
        }
    }
}
