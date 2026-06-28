using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Collections.Generic;
using Il2CppMonomiPark.SlimeRancher;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Internal;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Utils;
using UnityEngine;

namespace SR2MP.Handlers.Loading;

[PacketHandler((byte)Packets.Utils.PacketType.SaveFile, HandlerType.Client)]
internal sealed class SaveFileHandler : BasePacketHandler<SaveFilePacket>
{
    protected override bool Handle(SaveFilePacket packet, IPEndPoint? sender)
    {
        if (Main.Server.IsRunning) return false;

        SrLogger.LogMessage($"[SaveFileHandler] Received save file from server ({packet.SaveBytes.Length} bytes).");

        var dir = SaveSlotCloner.GetSaveDirectory();
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
        {
            SrLogger.LogError("Save directory not found during save file transfer.");
            return false;
        }

        var existingSaves = SaveSlotCloner.ListSaves();
        var slotsUsed = new HashSet<int>();
        foreach (var s in existingSaves)
        {
            var fileName = Path.GetFileName(s);
            var match = System.Text.RegularExpressions.Regex.Match(fileName, @"^\d{14}_(\d+)_\d+\.sav$");
            if (match.Success && int.TryParse(match.Groups[1].Value, out var slot))
            {
                slotsUsed.Add(slot);
            }
        }

        int targetSlot = -1;
        for (int s = 1; s <= 5; s++)
        {
            if (!slotsUsed.Contains(s))
            {
                targetSlot = s;
                break;
            }
        }

        if (targetSlot == -1)
        {
            targetSlot = 5;
            var slot5Saves = existingSaves.Where(s => {
                var match = System.Text.RegularExpressions.Regex.Match(Path.GetFileName(s), @"^\d{14}_5_\d+\.sav$");
                return match.Success;
            }).ToList();

            if (slot5Saves.Count > 0)
            {
                var backupDir = Path.Combine(dir, "Backups", "Slot5_TempBackup");
                Directory.CreateDirectory(backupDir);
                foreach (var file in slot5Saves)
                {
                    var destFile = Path.Combine(backupDir, Path.GetFileName(file));
                    File.Copy(file, destFile, true);
                    File.Delete(file);
                }
                SrLogger.LogMessage("Slot 5 saves backed up to Slot5_TempBackup.");
            }
        }

        var targetSlot0Indexed = targetSlot - 1;
        if (!SaveSlotCloner.ProcessAndWriteSaveBytes(packet.SaveBytes, packet.OriginalSaveName, targetSlot0Indexed, out var targetSaveName))
        {
            SrLogger.LogError("Failed to write incoming save file.");
            return false;
        }

        var targetSlotName = $"Slot {targetSlot}";
        SrLogger.LogMessage($"[SaveFileHandler] Wrote save file to {targetSlotName} as {targetSaveName}.sav. Triggering game load...");

        Actor.ActorsLoadHandler.ResetSyncState();

        if (ConnectionApproveHandler.PendingApprove != null)
        {
            var approve = ConnectionApproveHandler.PendingApprove;
            MelonLoader.MelonCoroutines.Start(WaitForConnectionLoad(approve));
        }
        else
        {
            SrLogger.LogWarning("Received save file but no pending connection approval was stored.");
        }

        var identifier = new GameSaveIdentifier(targetSlotName, targetSaveName);
        GameContext.Instance.AutoSaveDirector.BeginLoad(identifier, null);

        return true;
    }

    private static System.Collections.IEnumerator WaitForConnectionLoad(ConnectionApprovePacket approve)
    {
        while (SystemContext.Instance == null || SystemContext.Instance.SceneLoader == null || !SystemContext.Instance.SceneLoader.IsCurrentSceneGroupGameplay())
        {
            yield return null;
        }

        while (SceneContext.Instance == null || SceneContext.Instance.PlayerState == null || SceneContext.Instance.PlayerState._model == null)
        {
            yield return null;
        }

        SrLogger.LogMessage("[SaveFileHandler] Game loaded successfully. Completing connection...");
        ConnectionApproveHandler.CompleteConnection(approve);
    }
}
