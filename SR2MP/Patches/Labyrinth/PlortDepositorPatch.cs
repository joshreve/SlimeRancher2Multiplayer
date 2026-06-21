using HarmonyLib;
using Il2Cpp;
using SR2MP.Packets.World;

namespace SR2MP.Patches.Labyrinth;

[HarmonyPatch]
internal static class PlortDepositorPatch
{
    [HarmonyPostfix, HarmonyPatch(typeof(PlortDepositor), nameof(PlortDepositor.OnFilledChanged))]
    public static void OnFilledChanged(PlortDepositor __instance, bool isInstant)
    {
        if (HandlingPacket) return;

        string id = "";
        foreach (var pair in GameState.depositors)
        {
            if (pair.value == __instance._model)
            {
                id = pair.key;
                break;
            }
        }

        if (!string.IsNullOrEmpty(id))
        {
            Main.SendToAllOrServer(new PlortDepositorPacket
            {
                ID = id,
                AmountDeposited = __instance._model.AmountDeposited,
                IsInstant = isInstant
            });
        }
    }
}
