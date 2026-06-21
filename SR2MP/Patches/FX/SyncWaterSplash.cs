using HarmonyLib;
using SR2MP.Packets.FX;
using SR2MP.Shared.Managers;
using UnityEngine;

namespace SR2MP.Patches.FX;

[HarmonyPatch(typeof(Il2Cpp.SplashOnTrigger), nameof(Il2Cpp.SplashOnTrigger.Awake))]
internal static class SyncWaterSplashAwake
{
    public static GameObject? WaterSplashPrefab { get; private set; }

    public static void Postfix(Il2Cpp.SplashOnTrigger __instance)
    {
        if (__instance.playerSplashFX != null)
        {
            WaterSplashPrefab = __instance.playerSplashFX;
            if (FXManager != null && FXManager.PlayerFXMap != null)
            {
                FXManager.PlayerFXMap[PlayerFXType.WaterSplash] = __instance.playerSplashFX;
            }
        }
    }
}

[HarmonyPatch(typeof(Il2Cpp.SplashOnTrigger), nameof(Il2Cpp.SplashOnTrigger.SpawnAndPlayFX))]
internal static class SyncWaterSplashSpawnAndPlay
{
    public static void Postfix(Il2Cpp.SplashOnTrigger __instance, GameObject prefab, Collider collider)
    {
        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return;
        if (HandlingPacket) return;

        if (collider != null && collider.gameObject == SceneContext.Instance.Player)
        {
            var packet = new PlayerFXPacket
            {
                FX = PlayerFXType.WaterSplash,
                Position = collider.transform.position,
                Player = string.Empty
            };
            Main.SendToAllOrServer(packet);
        }
    }
}
