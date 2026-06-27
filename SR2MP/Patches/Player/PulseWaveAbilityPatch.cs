using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Player.CharacterController.Abilities;
using SR2MP.Packets.Player;
using UnityEngine;

namespace SR2MP.Patches.Player;

[HarmonyPatch(typeof(PulseWaveAbilityBehavior), nameof(PulseWaveAbilityBehavior.Start))]
internal static class PulseWaveAbilityPatch
{
    public static void Postfix(PulseWaveAbilityBehavior __instance)
    {
        if (HandlingPacket) return;

        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return;

        var position = __instance.VacOrigin != null ? __instance.VacOrigin.position : SceneContext.Instance.Player.transform.position;
        var packet = new PlayerPulseWavePacket { Position = position };

        Main.SendToAllOrServer(packet);
    }
}
