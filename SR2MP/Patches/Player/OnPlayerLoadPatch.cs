using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Player.CharacterController;
using Starlight.Utils;
using SR2MP.Components.Player;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Player;

[HarmonyPatch(typeof(SRCharacterController), nameof(SRCharacterController.Awake))]
internal static class OnPlayerLoadPatch
{
    private static void RegisterLocalPlayerAmmo(string playerId)
    {
        var localAmmo = SceneContext.Instance?.PlayerState?.Ammo;
        if (localAmmo != null)
        {
            NetworkAmmoManager.RegisterAmmoPointer(localAmmo, playerId);
            SrLogger.LogMessage($"Registered local player ammo pointer for {playerId}");
        }
        else
        {
            SrLogger.LogWarning($"Local player ammo was null during registration for {playerId}!");
        }
    }

    public static void Postfix(SRCharacterController __instance)
    {
        if (Main.Server.IsRunning)
        {
            var networkPlayer = __instance.AddComponent<NetworkPlayer>();
            networkPlayer.ID = Main.Server.PlayerId;
            networkPlayer.IsLocal = true;
            RegisterLocalPlayerAmmo(networkPlayer.ID);
            PlayerManager.AddPlayer(networkPlayer.ID).Username = Main.Username;
        }
        else if (Main.Client.IsConnected)
        {
            var networkPlayer = __instance.AddComponent<NetworkPlayer>();
            networkPlayer.ID = Main.Client.PlayerId;
            networkPlayer.IsLocal = true;
            RegisterLocalPlayerAmmo(networkPlayer.ID);
            PlayerManager.AddPlayer(networkPlayer.ID).Username = Main.Username;
        }
        else
        {
            Main.Client.OnConnected += id =>
            {
                if (!__instance)
                    return;

                var networkPlayer = __instance.AddComponent<NetworkPlayer>();
                networkPlayer.ID = id;
                networkPlayer.IsLocal = true;
                RegisterLocalPlayerAmmo(networkPlayer.ID);

                PlayerManager.AddPlayer(id).Username = Main.Username;
            };

            Main.Server.OnServerStarted += () =>
            {
                if (!__instance)
                    return;

                PlayerManager.AddPlayer(Main.Server.PlayerId).Username = Main.Username;

                var networkPlayer = __instance.AddComponent<NetworkPlayer>();
                networkPlayer.ID = Main.Server.PlayerId;
                networkPlayer.IsLocal = true;
                RegisterLocalPlayerAmmo(networkPlayer.ID);
            };
        }
    }
}