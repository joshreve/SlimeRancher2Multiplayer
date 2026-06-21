using System;
using HarmonyLib;
using Il2Cpp;

namespace SR2MP.Patches.LandPlots;

[HarmonyPatch(typeof(LandPlot), nameof(LandPlot.OnDestroy))]
internal static class SafeLandPlotDestroy
{
    [HarmonyFinalizer]
    public static Exception? Finalizer(Exception? __exception)
    {
        if (__exception != null)
        {
            SrLogger.LogWarning($"Suppressed exception in LandPlot.OnDestroy: {__exception.Message}");
            return null; // Suppress exception
        }
        return null;
    }
}
