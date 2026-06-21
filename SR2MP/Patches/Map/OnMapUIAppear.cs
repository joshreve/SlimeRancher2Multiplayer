using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.UI.Map;
using Il2CppTMPro;
using SR2MP.Components.Player;
using SR2MP.Shared.Managers;
using UnityEngine.UI;

namespace SR2MP.Patches.Map;

[HarmonyPatch(typeof(MapUI), nameof(MapUI.Start))]
internal class OnMapUIAppear
{
    public static MapUI? ActiveMapUI;

    public static void Postfix(MapUI __instance)
    {
        ActiveMapUI = __instance;
        foreach (var player in PlayerManager.GetAllPlayers())
        {
            if (player.PlayerId == LocalID)
                continue;
            
            if (PlayerObjects.TryGetValue(player.PlayerId, out var playerObj))
            {
                var networkPlayer = playerObj.GetComponent<NetworkPlayer>();
                if (networkPlayer != null)
                {
                    networkPlayer.CreateMapMarker(__instance);
                }
            }
        }
    }
}

[HarmonyPatch(typeof(MapUI), nameof(MapUI.OnDestroy))]
internal class OnMapUIDestroy
{
    public static void Prefix(MapUI __instance)
    {
        if (OnMapUIAppear.ActiveMapUI == __instance)
        {
            OnMapUIAppear.ActiveMapUI = null;
        }

        foreach (var pair in PlayerMarkerTransforms)
        {
            var marker = pair.Value;
            if (marker.mainMarker)
            {
                Object.Destroy(marker.mainMarker.gameObject);
            }
            marker.mainMarker = null;
            marker.markerArrow = null;
        }
    }
}