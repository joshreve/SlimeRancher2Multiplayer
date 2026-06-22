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

        // Allow execution on both client and host. This enables immediate client-side
        // spawning, which supports harvesting in scenes unloaded by the host. The spawned 
        // actor is automatically synchronized to other players via the OnActorSpawn patch.
        return true;
    }
}
