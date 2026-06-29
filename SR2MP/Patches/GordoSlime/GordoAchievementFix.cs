using HarmonyLib;
using Il2Cpp;
using System;

namespace SR2MP.Patches.GordoSlime;

[HarmonyPatch(typeof(GordoRewardsBase), nameof(GordoRewardsBase.GiveRewards))]
internal static class GordoRewardsBaseGiveRewardsPatch
{
    public static bool Prefix()
    {
        // On client connect, Gordo states are synced, but spawning the actual physical
        // rewards (e.g. slimes or keys) must only happen on the host side to prevent duplicates
        // and avoid NullReferenceExceptions when client loading is in progress.
        if (Main.Client.IsConnected)
        {
            return false; // Bypass reward spawning on the client
        }
        return true;
    }
}

// No [HarmonyPatch] attribute to prevent auto-scan crash.
// Manually patched in Main.cs.
internal static class SteamMetaGamePlatformGrantPatch
{
    public static bool Prefix()
    {
        try
        {
            // Dynamically query Steamworks initialization status using reflection
            var isInitialized = false;
            
            var steamClientType = Type.GetType("Steamworks.SteamClient, Steamworks");
            if (steamClientType != null)
            {
                var prop = steamClientType.GetProperty("IsValid");
                if (prop != null)
                {
                    isInitialized = (bool)prop.GetValue(null);
                }
            }
            
            if (!isInitialized)
            {
                var steamApiType = Type.GetType("Steamworks.SteamAPI, Steamworks");
                if (steamApiType != null)
                {
                    var method = steamApiType.GetMethod("IsSteamRunning");
                    if (method != null)
                    {
                        isInitialized = (bool)method.Invoke(null, null);
                    }
                }
            }
            
            if (!isInitialized)
            {
                return false; // Steamworks is not active, bypass achievement grant to avoid crash
            }
        }
        catch
        {
            return false; // Fail-safe: bypass native call
        }
        
        return true; // Steamworks is active, run native logic
    }
}
