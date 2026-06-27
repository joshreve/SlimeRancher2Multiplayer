using System;
using System.Collections.Generic;
using System.IO;
using SR2MP.Shared.Utils;
using UnityEngine;

namespace SR2MP.Components.UI;

internal sealed partial class MultiplayerUI
{
    private bool viewingSaveManager;
    private int currentSavePage = 0;
    private const int SavesPerPage = 4;
    private string selectedSavePath = string.Empty;
    private string saveManagerStatus = string.Empty;
    private List<string> detectedSaves = new();

    private void SaveManagerScreen()
    {
        DrawText("<b>Save Slot Cloner & Backup</b>");
        DrawText("Duplicates save files to other slots safely.");

        if (GUI.Button(CalculateButtonLayout(6), "Refresh Save List"))
        {
            RefreshSaveList();
        }

        if (GUI.Button(CalculateButtonLayout(6), "Create Manual Backup"))
        {
            saveManagerStatus = SaveSlotCloner.CreateBackup();
        }

        DrawText("Detected Saves (Newest First):");

        var savesCount = detectedSaves.Count;
        var totalPages = Math.Max(1, (int)Math.Ceiling((double)savesCount / SavesPerPage));
        if (currentSavePage >= totalPages) currentSavePage = totalPages - 1;
        if (currentSavePage < 0) currentSavePage = 0;

        int startIndex = currentSavePage * SavesPerPage;
        int endIndex = Math.Min(startIndex + SavesPerPage, savesCount);

        for (int i = startIndex; i < endIndex; i++)
        {
            var savePath = detectedSaves[i];
            var fileName = Path.GetFileName(savePath);
            var isSelected = savePath == selectedSavePath;
            var lastWrite = File.GetLastWriteTime(savePath).ToString("g");

            var slotLabel = "Unknown Slot";
            var match = System.Text.RegularExpressions.Regex.Match(fileName, @"^\d{14}_(\d+)_\d+\.sav$");
            if (match.Success)
            {
                slotLabel = $"Slot {match.Groups[1].Value}";
            }

            var btnLabel = $"{(isSelected ? "<b>" : "")}[{slotLabel}] {lastWrite} ({fileName}){(isSelected ? "</b>" : "")}";
            if (GUI.Button(CalculateButtonLayout(6), btnLabel))
            {
                selectedSavePath = savePath;
                saveManagerStatus = $"Selected {fileName}";
            }
        }

        if (totalPages > 1)
        {
            DrawText($"Page {currentSavePage + 1} of {totalPages}", 2);
            
            if (GUI.Button(CalculateButtonLayout(6, 2, 0), "Previous Page"))
            {
                if (currentSavePage > 0) currentSavePage--;
            }
            if (GUI.Button(CalculateButtonLayout(6, 2, 1), "Next Page"))
            {
                if (currentSavePage < totalPages - 1) currentSavePage++;
            }
        }

        if (!string.IsNullOrEmpty(selectedSavePath))
        {
            var fileName = Path.GetFileName(selectedSavePath);
            DrawText($"Selected: <b>{fileName}</b>");

            DrawText("Clone to Target Slot:", 6);
            
            if (GUI.Button(CalculateButtonLayout(6, 3, 0), "Slot 1")) CloneTo(0);
            if (GUI.Button(CalculateButtonLayout(6, 3, 1), "Slot 2")) CloneTo(1);
            if (GUI.Button(CalculateButtonLayout(6, 3, 2), "Slot 3")) CloneTo(2);

            if (GUI.Button(CalculateButtonLayout(6, 2, 0), "Slot 4")) CloneTo(3);
            if (GUI.Button(CalculateButtonLayout(6, 2, 1), "Slot 5")) CloneTo(4);
        }
        else
        {
            DrawText("Select a save file above to clone.");
        }

        if (!string.IsNullOrEmpty(saveManagerStatus))
        {
            DrawText($"Status: <color=yellow>{saveManagerStatus}</color>");
        }

        if (GUI.Button(CalculateButtonLayout(6), "Back to Main Menu"))
        {
            viewingSaveManager = false;
        }
    }

    private void RefreshSaveList()
    {
        detectedSaves = SaveSlotCloner.ListSaves();
        if (detectedSaves.Count == 0)
        {
            saveManagerStatus = "No save files (.sav) found.";
        }
    }

    private void CloneTo(int slotIndex0Indexed)
    {
        if (string.IsNullOrEmpty(selectedSavePath))
            return;

        if (SaveSlotCloner.CloneSaveToSlot(selectedSavePath, slotIndex0Indexed, out var status))
        {
            saveManagerStatus = status;
            RefreshSaveList();
        }
        else
        {
            saveManagerStatus = $"Clone failed: {status}";
        }
    }
}
