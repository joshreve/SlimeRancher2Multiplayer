using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace SR2MP.Shared.Utils;

internal static class SaveSlotCloner
{
    private static readonly Regex SaveFileRegex = new(@"^(\d+)_(\d+)(?:_(\d+))?\.sav$", RegexOptions.Compiled);

    public static string GetSaveDirectory()
    {
        var localLow = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", "MonomiPark", "SlimeRancher2");
        if (!Directory.Exists(localLow))
            return string.Empty;

        // Search for directories containing .sav files recursively
        try
        {
            var savFiles = Directory.GetFiles(localLow, "*.sav", SearchOption.AllDirectories);
            if (savFiles.Length > 0)
            {
                return Path.GetDirectoryName(savFiles[0]) ?? string.Empty;
            }
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"Error scanning for save directories: {ex.Message}");
        }

        // Check fallback Steam directory
        try
        {
            var steamDir = Path.Combine(localLow, "Steam");
            if (Directory.Exists(steamDir))
            {
                var subDirs = Directory.GetDirectories(steamDir);
                if (subDirs.Length > 0)
                    return subDirs[0];
            }
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"Error scanning Steam fallback directory: {ex.Message}");
        }

        return localLow;
    }

    public static List<string> ListSaves()
    {
        var list = new List<string>();
        var dir = GetSaveDirectory();
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            return list;

        try
        {
            var files = Directory.GetFiles(dir, "*.sav");
            foreach (var f in files)
            {
                var name = Path.GetFileName(f);
                if (SaveFileRegex.IsMatch(name))
                {
                    list.Add(f);
                }
            }
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Error listing save files: {ex.Message}");
        }

        // Sort by last write time descending so newest saves appear first
        list.Sort((a, b) => File.GetLastWriteTimeUtc(b).CompareTo(File.GetLastWriteTimeUtc(a)));
        return list;
    }

    public static string CreateBackup()
    {
        var dir = GetSaveDirectory();
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            return "Save directory not found.";

        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupDir = Path.Combine(dir, "Backups", $"Backup_{timestamp}");
            Directory.CreateDirectory(backupDir);

            var files = Directory.GetFiles(dir, "*.sav");
            foreach (var file in files)
            {
                var destFile = Path.Combine(backupDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            return $"All saves successfully backed up to {backupDir}";
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to create save backups: {ex}");
            return $"Backup failed: {ex.Message}";
        }
    }

    public static bool CloneSaveToSlot(string sourcePath, int targetSlot0Indexed, out string statusMessage)
    {
        statusMessage = string.Empty;
        var dir = GetSaveDirectory();
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
        {
            statusMessage = "Save directory not found.";
            return false;
        }

        if (!File.Exists(sourcePath))
        {
            statusMessage = "Source save file does not exist.";
            return false;
        }

        try
        {
            // First, run automated backup
            var backupResult = CreateBackup();
            SrLogger.LogMessage($"Automatic Backup before cloning: {backupResult}");

            // Read source file bytes
            var fileBytes = File.ReadAllBytes(sourcePath);

            var match = SaveFileRegex.Match(Path.GetFileName(sourcePath));
            if (!match.Success)
            {
                statusMessage = "Invalid source save filename format.";
                return false;
            }

            // Find internal metadata offset dynamically by scanning bytes
            var matchIndex = FindMetadataOffset(fileBytes);
            if (matchIndex == -1)
            {
                statusMessage = "Could not locate save metadata inside the file.";
                return false;
            }

            // Generate new timestamp for the cloned save to keep it distinct
            var newTimestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var newSlotChar = (char)('1' + targetSlot0Indexed); // '1' to '5'
            var newSlotByte = (byte)targetSlot0Indexed;         // 0x00 to 0x04

            var newInternalSaveName = $"{newTimestamp}_{newSlotChar}";
            var newInternalSaveNameBytes = Encoding.ASCII.GetBytes(newInternalSaveName);

            // 1. Replace internal save name string (16 bytes starting at matchIndex)
            for (int i = 0; i < 16; i++)
            {
                if (matchIndex + i < fileBytes.Length && i < newInternalSaveNameBytes.Length)
                {
                    fileBytes[matchIndex + i] = newInternalSaveNameBytes[i];
                }
            }

            // 2. Replace ASCII slot text (at matchIndex + 17)
            if (matchIndex + 17 < fileBytes.Length)
            {
                fileBytes[matchIndex + 17] = (byte)newSlotChar;
            }

            // 3. Replace slot index byte (at matchIndex + 18)
            if (matchIndex + 18 < fileBytes.Length)
            {
                fileBytes[matchIndex + 18] = newSlotByte;
            }

            // Write to new file preserving original sub-number if any
            var targetSlot1Indexed = targetSlot0Indexed + 1;
            var subNumber = (match.Groups.Count > 3 && match.Groups[3].Success) ? match.Groups[3].Value : "1";
            var destFileName = $"{newTimestamp}_{targetSlot1Indexed}_{subNumber}.sav";
            var destPath = Path.Combine(dir, destFileName);

            File.WriteAllBytes(destPath, fileBytes);
            statusMessage = $"Cloned successfully to Slot {targetSlot1Indexed} as {destFileName}";
            return true;
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Error cloning save slot: {ex}");
            statusMessage = $"Error: {ex.Message}";
            return false;
        }
    }

    public static byte[] ReadActiveSaveBytes(out string saveName)
    {
        saveName = string.Empty;
        var summary = GameContext.Instance?.AutoSaveDirector?.TryGetCurrentGameSummary();
        if (summary == null)
        {
            SrLogger.LogWarning("No active save summary found.");
            return Array.Empty<byte>();
        }

        saveName = summary.SaveName;
        var dir = GetSaveDirectory();
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            return Array.Empty<byte>();

        var path = Path.Combine(dir, $"{saveName}.sav");
        if (File.Exists(path))
        {
            try
            {
                return File.ReadAllBytes(path);
            }
            catch (Exception ex)
            {
                SrLogger.LogError($"Error reading active save file: {ex}");
            }
        }

        return Array.Empty<byte>();
    }

    public static bool ProcessAndWriteSaveBytes(byte[] fileBytes, string originalSaveName, int targetSlot0Indexed, out string targetFileNameWithoutExtension)
    {
        targetFileNameWithoutExtension = string.Empty;
        var dir = GetSaveDirectory();
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            return false;

        try
        {
            var match = SaveFileRegex.Match(originalSaveName);
            if (!match.Success)
            {
                match = Regex.Match(originalSaveName, @"^(\d{14})_(\d+)_(\d+)");
            }

            if (!match.Success)
            {
                SrLogger.LogError($"Invalid original save name format: {originalSaveName}");
                return false;
            }

            var timestampPart = match.Groups[1].Value;

            var matchIndex = FindMetadataOffset(fileBytes);
            if (matchIndex == -1)
            {
                SrLogger.LogWarning("Could not find save metadata offset; writing bytes unmodified.");
            }
            else
            {
                var newTimestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                var newSlotChar = (char)('1' + targetSlot0Indexed);
                var newSlotByte = (byte)targetSlot0Indexed;

                var newInternalSaveName = $"{newTimestamp}_{newSlotChar}";
                var newInternalSaveNameBytes = Encoding.ASCII.GetBytes(newInternalSaveName);

                for (int i = 0; i < 16; i++)
                {
                    if (matchIndex + i < fileBytes.Length && i < newInternalSaveNameBytes.Length)
                        fileBytes[matchIndex + i] = newInternalSaveNameBytes[i];
                }

                if (matchIndex + 17 < fileBytes.Length)
                    fileBytes[matchIndex + 17] = (byte)newSlotChar;

                if (matchIndex + 18 < fileBytes.Length)
                    fileBytes[matchIndex + 18] = newSlotByte;

                timestampPart = newTimestamp;
            }

            var targetSlot1Indexed = targetSlot0Indexed + 1;
            var subNumber = (match.Groups.Count > 3 && match.Groups[3].Success) ? match.Groups[3].Value : "1";
            targetFileNameWithoutExtension = $"{timestampPart}_{targetSlot1Indexed}_{subNumber}";
            var destPath = Path.Combine(dir, $"{targetFileNameWithoutExtension}.sav");

            File.WriteAllBytes(destPath, fileBytes);
            return true;
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to process and write save bytes: {ex}");
            return false;
        }
    }

    private static int FindMetadataOffset(byte[] fileBytes)
    {
        if (fileBytes == null || fileBytes.Length < 16)
            return -1;

        // Search for a sequence of 14 digits, followed by '_', followed by a digit
        // Digits are ASCII 0x30 to 0x39. '_' is 0x5F.
        for (int i = 0; i <= fileBytes.Length - 16; i++)
        {
            bool isMatch = true;
            for (int j = 0; j < 14; j++)
            {
                byte b = fileBytes[i + j];
                if (b < 0x30 || b > 0x39)
                {
                    isMatch = false;
                    break;
                }
            }
            if (isMatch)
            {
                if (fileBytes[i + 14] == 0x5F && fileBytes[i + 15] >= 0x30 && fileBytes[i + 15] <= 0x39)
                {
                    return i;
                }
            }
        }
        return -1;
    }

    private static int FindPatternIndex(byte[] src, byte[] pattern)
    {
        if (src == null || pattern == null || src.Length < pattern.Length)
            return -1;

        for (int i = 0; i <= src.Length - pattern.Length; i++)
        {
            var match = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (src[i + j] != pattern[j])
                {
                    match = false;
                    break;
                }
            }
            if (match)
                return i;
        }
        return -1;
    }
}
