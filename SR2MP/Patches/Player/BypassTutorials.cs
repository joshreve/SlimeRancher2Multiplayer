using HarmonyLib;
using Il2Cpp;
using Il2CppMonomiPark.SlimeRancher.Tutorial;

namespace SR2MP.Patches.Player;

[HarmonyPatch(typeof(TutorialDirector), nameof(TutorialDirector.IsEnabled))]
internal static class TutorialDirectorIsEnabledPatch
{
    public static bool Prefix(ref bool __result)
    {
        if (Main.Client.IsConnected && !Main.Server.IsRunning)
        {
            __result = false;
            return false; // Skip the original method
        }
        return true;
    }
}

[HarmonyPatch(typeof(TutorialDirector), nameof(TutorialDirector.QueueTutorial))]
internal static class TutorialDirectorQueueTutorialPatch
{
    public static bool Prefix(TutorialDefinition tutorial)
    {
        if (Main.Client.IsConnected && !Main.Server.IsRunning)
        {
            return false; // Skip original method, do not queue the tutorial popup
        }
        return true;
    }
}

[HarmonyPatch(typeof(TutorialDefinition), nameof(TutorialDefinition.IsComplete))]
internal static class TutorialDefinitionIsCompletePatch
{
    public static bool Prefix(ref bool __result)
    {
        if (Main.Client.IsConnected && !Main.Server.IsRunning)
        {
            __result = true;
            return false; // Skip original method, report tutorial as always completed
        }
        return true;
    }
}

