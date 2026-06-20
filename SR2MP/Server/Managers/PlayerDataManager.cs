using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using MelonLoader;
using MelonLoader.Utils;
using SR2MP.Server.Models;
using SR2MP.Shared.Utils;
using SR2MP.Packets.Ammo;

namespace SR2MP.Server.Managers;

internal sealed class PlayerDataManager
{
    private static PlayerDataManager? instance;
    public static PlayerDataManager Instance => instance ??= new PlayerDataManager();

    private readonly Dictionary<string, PlayerData> playerDataCache = new();
    private string SavePath => Path.Combine(MelonEnvironment.UserDataDirectory, "SR2MP", "player_data.json");

    private PlayerDataManager()
    {
        LoadAllPlayerData();
    }

    public PlayerData GetOrCreatePlayerData(string playerId, string playerName)
    {
        if (playerDataCache.TryGetValue(playerId, out var existingData))
        {
            existingData.PlayerName = playerName;
            existingData.LastConnected = DateTime.UtcNow;
            SavePlayerData(existingData);
            return existingData;
        }

        var newData = new PlayerData(playerId, playerName);
        playerDataCache[playerId] = newData;
        SavePlayerData(newData);
        return newData;
    }

    public void UpdatePlayerInventory(string playerId, AmmoAddPacket packet)
    {
        var data = GetOrCreatePlayerData(playerId, "Player");
        // Find if the identifiable already exists in one of the slots:
        var slotEntry = data.Inventory.FirstOrDefault(x => x.Value.Identifiable == packet.Identifiable);
        if (slotEntry.Value.Count > 0)
        {
            var slot = slotEntry.Value;
            slot.Count += packet.Count;
            data.Inventory[slotEntry.Key] = slot;
        }
        else
        {
            // Find the first empty slot or add a new one:
            var emptySlotKey = data.Inventory.FirstOrDefault(x => x.Value.Count == 0 || x.Value.Identifiable == -1).Key;
            if (data.Inventory.ContainsKey(emptySlotKey))
            {
                var slot = data.Inventory[emptySlotKey];
                slot.Identifiable = packet.Identifiable;
                slot.Count = packet.Count;
                data.Inventory[emptySlotKey] = slot;
            }
            else
            {
                // Find next index
                var nextIndex = data.Inventory.Keys.Count > 0 ? data.Inventory.Keys.Max() + 1 : 0;
                data.Inventory[nextIndex] = new NetworkAmmoSlot
                {
                    Identifiable = packet.Identifiable,
                    Count = packet.Count,
                    SlotDefinition = 0
                };
            }
        }
        SavePlayerData(data);
    }

    public void UpdatePlayerInventory(string playerId, AmmoAddToSlotPacket packet)
    {
        var data = GetOrCreatePlayerData(playerId, "Player");
        data.Inventory[packet.SlotIndex] = new NetworkAmmoSlot
        {
            Identifiable = packet.Identifiable,
            Count = packet.Count,
            SlotDefinition = 0
        };
        SavePlayerData(data);
    }

    public void UpdatePlayerInventory(string playerId, AmmoDecrementPacket packet)
    {
        var data = GetOrCreatePlayerData(playerId, "Player");
        if (data.Inventory.TryGetValue(packet.SlotIndex, out var slot))
        {
            slot.Count = Math.Max(0, slot.Count - packet.Count);
            if (slot.Count == 0)
            {
                slot.Identifiable = -1; // Empty
            }
            data.Inventory[packet.SlotIndex] = slot;
            SavePlayerData(data);
        }
    }

    public void SavePlayerData(PlayerData data)
    {
        playerDataCache[data.PlayerId] = data;
        SaveAllPlayerData();
    }

    public void SaveAllPlayerData()
    {
        try
        {
            var dir = Path.GetDirectoryName(SavePath);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            var json = JsonConvert.SerializeObject(playerDataCache.Values.ToList(), Formatting.Indented);
            File.WriteAllText(SavePath, json);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to save player data: {ex}");
        }
    }

    private void LoadAllPlayerData()
    {
        try
        {
            if (!File.Exists(SavePath))
            {
                return;
            }

            var json = File.ReadAllText(SavePath);
            var playerList = JsonConvert.DeserializeObject<List<PlayerData>>(json);

            if (playerList == null)
                return;

            playerDataCache.Clear();
            foreach (var data in playerList)
            {
                playerDataCache[data.PlayerId] = data;
            }
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to load player data: {ex}");
        }
    }

    public List<PlayerData> GetDisconnectedPlayers()
    {
        var connectedIds = GlobalVariables.PlayerObjects.Keys.ToHashSet();
        connectedIds.Add(GlobalVariables.LocalID);

        return playerDataCache.Values.Where(p => !connectedIds.Contains(p.PlayerId)).ToList();
    }

    public PlayerData? GetPlayerData(string playerId)
    {
        return playerDataCache.GetValueOrDefault(playerId);
    }

    public void ClearInventory(string playerId)
    {
        if (playerDataCache.TryGetValue(playerId, out var data))
        {
            data.Inventory.Clear();
            SavePlayerData(data);
        }
    }
}