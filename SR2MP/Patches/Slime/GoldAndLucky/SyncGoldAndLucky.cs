using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Slime;
using SR2MP.Components.Actor;
using UnityEngine;

namespace SR2MP.Patches.Slime.GoldAndLucky;

[HarmonyPatch(typeof(ProducePlortsOnHit), nameof(ProducePlortsOnHit.ProducePlort))]
internal static class SyncGoldSlimeProducePlort
{
    public static bool Prefix(ProducePlortsOnHit __instance)
    {
        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return true;
        if (HandlingPacket) return true;

        var networkActor = __instance.GetComponent<NetworkActor>();
        if (networkActor != null && !networkActor.LocallyOwned)
        {
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(Il2Cpp.LuckySlimeProduceCoins), nameof(Il2Cpp.LuckySlimeProduceCoins.ProduceCoins))]
internal static class SyncLuckySlimeProduceCoins
{
    public static bool Prefix(Il2Cpp.LuckySlimeProduceCoins __instance)
    {
        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return true;
        if (HandlingPacket) return true;

        var networkActor = __instance.GetComponent<NetworkActor>();
        if (networkActor != null && !networkActor.LocallyOwned)
        {
            return false;
        }

        return true;
    }
}
