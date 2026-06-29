using HarmonyLib;
using UnityEngine;
using SR2MP.Components.Actor;

namespace SR2MP.Patches.Triggers;

[HarmonyPatch]
internal static class OnTriggerDeposit
{
    private static bool CheckOwnership(Collider collider)
    {
        if (collider == null)
            return true;

        var networkActor = collider.GetComponentInParent<NetworkActor>();
        if (networkActor != null && !networkActor.LocallyOwned)
        {
            // Skip the trigger since we do not own this object.
            // This prevents duplicate triggering/deposits from multiple players.
            return false;
        }

        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SiloCatcher), nameof(SiloCatcher.OnTriggerEnter))]
    public static bool SiloCatcherPrefix(Collider collider) => CheckOwnership(collider);

    [HarmonyPrefix]
    [HarmonyPatch(typeof(FeederRegion), nameof(FeederRegion.OnTriggerEnter))]
    public static bool FeederRegionPrefix(Collider collider) => CheckOwnership(collider);

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ScorePlort), nameof(ScorePlort.OnTriggerEnter))]
    public static bool ScorePlortPrefix(Collider collider) => CheckOwnership(collider);

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlortDepositor), nameof(PlortDepositor.OnTriggerEnter))]
    public static bool PlortDepositorPrefix(Collider collider) => CheckOwnership(collider);

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PuzzleSlot), nameof(PuzzleSlot.OnTriggerEnter))]
    public static bool PuzzleSlotPrefix(Collider collider) => CheckOwnership(collider);

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GordoEatTrigger), nameof(GordoEatTrigger.OnTriggerEnter))]
    public static bool GordoEatTriggerPrefix(Collider collider) => CheckOwnership(collider);

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Il2CppMonomiPark.SlimeRancher.Prestige.ResourceDepositor), nameof(Il2CppMonomiPark.SlimeRancher.Prestige.ResourceDepositor.OnTriggerEnter))]
    public static bool ResourceDepositorPrefix(Collider collider) => CheckOwnership(collider);

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Il2CppMonomiPark.SlimeRancher.Drone.DroneFuelDepositor), nameof(Il2CppMonomiPark.SlimeRancher.Drone.DroneFuelDepositor.OnTriggerEnter))]
    public static bool DroneFuelDepositorPrefix(Collider collider) => CheckOwnership(collider);
}
