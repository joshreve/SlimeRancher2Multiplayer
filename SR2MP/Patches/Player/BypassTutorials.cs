#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception
#pragma warning disable S108    // Either remove or fill this block of code
#pragma warning disable S2486   // Handle the exception or explain in a comment why it can be ignored

using HarmonyLib;
using Il2Cpp;
using Il2CppMonomiPark.SlimeRancher.Tutorial;
using UnityEngine;

namespace SR2MP.Patches.Player;

internal static class TutorialBypassHelper
{
    public static void CleanUpTutorials()
    {
        if (!Main.Client.IsConnected || Main.Server.IsRunning)
            return;

        try
        {
            if (SceneContext.Instance == null) return;
            var director = SceneContext.Instance.TutorialDirector;
            if (director != null)
            {
                SrLogger.LogMessage("[TutorialBypass] Cleaning up existing tutorials and queue...");
                
                // Clear the queued tutorials
                if (director._queue != null)
                {
                    director._queue.Clear();
                }

                // Close active popup
                if (director._currPopup != null)
                {
                    SrLogger.LogMessage("[TutorialBypass] Closing active tutorial popup window...");
                    try
                    {
                        director._currPopup.Close();
                    }
                    catch (System.Exception ex)
                    {
                        SrLogger.LogDebug($"[TutorialBypass] Failed to close _currPopup: {ex.Message}");
                    }
                }

                director._currentPopupTutorial = null;
            }
        }
        catch (System.Exception ex)
        {
            SrLogger.LogError($"[TutorialBypass] Error cleaning up TutorialDirector: {ex}");
        }

        try
        {
            // Close any orphan TutorialPopupUI components in the scene
            var popups = UnityEngine.Object.FindObjectsOfType<Il2CppMonomiPark.SlimeRancher.UI.Popup.TutorialPopupUI>();
            if (popups != null && popups.Length > 0)
            {
                SrLogger.LogMessage($"[TutorialBypass] Found {popups.Length} active TutorialPopupUI in scene, closing them.");
                foreach (var popup in popups)
                {
                    if (popup != null)
                    {
                        try { popup.Close(); } catch (System.Exception) { /* Ignored: Popup close might fail if already closing */ }
                        try { UnityEngine.Object.Destroy(popup.gameObject); } catch (System.Exception) { /* Ignored: GameObject might already be destroyed */ }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            SrLogger.LogError($"[TutorialBypass] Error finding/closing orphan popups: {ex}");
        }

        try
        {
            // Close any orphan GenericTutorialPopupUI components in the scene
            var genericPopups = UnityEngine.Object.FindObjectsOfType<Il2CppMonomiPark.SlimeRancher.UI.Popup.GenericTutorialPopupUI>();
            if (genericPopups != null && genericPopups.Length > 0)
            {
                SrLogger.LogMessage($"[TutorialBypass] Found {genericPopups.Length} active GenericTutorialPopupUI in scene, closing them.");
                foreach (var gp in genericPopups)
                {
                    if (gp != null)
                    {
                        try { gp.Hide(); } catch (System.Exception) { /* Ignored: Hide might fail if already hidden */ }
                        try { UnityEngine.Object.Destroy(gp.gameObject); } catch (System.Exception) { /* Ignored: GameObject might already be destroyed */ }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            SrLogger.LogError($"[TutorialBypass] Error destroying generic popups: {ex}");
        }

        try
        {
            // Clean up and disable/remove all TutorialRadar components (star markers)
            var radars = UnityEngine.Object.FindObjectsOfType<TutorialRadar>();
            if (radars != null && radars.Length > 0)
            {
                SrLogger.LogMessage($"[TutorialBypass] Found {radars.Length} TutorialRadars, disabling and removing them...");
                foreach (var radar in radars)
                {
                    if (radar != null)
                    {
                        try
                        {
                            radar.SetTrackedOnRadar(false);
                        }
                        catch (System.Exception)
                        {
                            /* Ignored: SetTrackedOnRadar might fail if radar is uninitialized */
                        }
                        try
                        {
                            UnityEngine.Object.Destroy(radar);
                        }
                        catch (System.Exception)
                        {
                            /* Ignored: Radar component might already be destroyed */
                        }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            SrLogger.LogError($"[TutorialBypass] Error removing TutorialRadars: {ex}");
        }
    }
}

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

[HarmonyPatch(typeof(TutorialDirector), nameof(TutorialDirector.Start))]
internal static class TutorialDirectorStartPatch
{
    public static void Postfix(TutorialDirector __instance)
    {
        TutorialBypassHelper.CleanUpTutorials();
    }
}

[HarmonyPatch(typeof(TutorialDirector), nameof(TutorialDirector.OnEnable))]
internal static class TutorialDirectorOnEnablePatch
{
    public static void Postfix(TutorialDirector __instance)
    {
        TutorialBypassHelper.CleanUpTutorials();
    }
}

[HarmonyPatch(typeof(TutorialDirector), nameof(TutorialDirector.InitForLevel))]
internal static class TutorialDirectorInitForLevelPatch
{
    public static void Postfix(TutorialDirector __instance)
    {
        TutorialBypassHelper.CleanUpTutorials();
    }
}


