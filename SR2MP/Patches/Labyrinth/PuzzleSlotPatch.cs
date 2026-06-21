using HarmonyLib;
using Il2Cpp;
using SR2MP.Packets.World;

namespace SR2MP.Patches.Labyrinth;

[HarmonyPatch]
internal static class PuzzleSlotPatch
{
    [HarmonyPostfix, HarmonyPatch(typeof(PuzzleSlot), nameof(PuzzleSlot.OnFilledChanged))]
    public static void OnFilledChanged(PuzzleSlot __instance)
    {
        if (HandlingPacket) return;

        string id = "";
        foreach (var pair in GameState.slots)
        {
            if (pair.value == __instance._model)
            {
                id = pair.key;
                break;
            }
        }

        if (!string.IsNullOrEmpty(id))
        {
            Main.SendToAllOrServer(new PuzzleSlotPacket
            {
                ID = id,
                Filled = __instance._model.filled
            });
        }
    }
}
