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
using UnityEngine;

namespace SR2MP.Server.Managers;

internal sealed class PlayerDataManager
{
    private static PlayerDataManager? instance;
    public static PlayerDataManager Instance => instance ??= new PlayerDataManager();

    private readonly Dictionary<string, PlayerData> playerDataCache = new();
    private static string SavePath => Path.Combine(MelonEnvironment.UserDataDirectory, "SR2MP", "player_data.json");
    private readonly System.Threading.Timer saveTimer;

    private PlayerDataManager()
    {
        LoadAllPlayerData();
        saveTimer = new System.Threading.Timer(PeriodicSave, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
    }

    private void PeriodicSave(object? state)
    {
        if (Main.Server != null && Main.Server.IsRunning)
        {
            SaveAllPlayerData();
        }
    }

    public PlayerData GetOrCreatePlayerData(string playerId, string playerName)
    {
        lock (playerDataCache)
        {
            if (playerDataCache.TryGetValue(playerId, out var existingData))
            {
                existingData.PlayerName = playerName;
                existingData.LastConnected = DateTime.UtcNow;
                return existingData;
            }

            var newData = new PlayerData(playerId, playerName);
            playerDataCache[playerId] = newData;
            return newData;
        }
    }

    public void UpdatePlayerPosition(string playerId, Vector3 position, string sceneGroup)
    {
        lock (playerDataCache)
        {
            var data = GetOrCreatePlayerData(playerId, "Player");
            data.PosX = position.x;
            data.PosY = position.y;
            data.PosZ = position.z;
            data.SceneGroup = sceneGroup;
        }
    }

    public void UpdatePlayerInventory(string playerId, AmmoAddPacket packet)
    {
        lock (playerDataCache)
        {
            var data = GetOrCreatePlayerData(playerId, "Player");
            var slotEntry = data.Inventory.FirstOrDefault(x => x.Value.Identifiable == packet.Identifiable);
            if (slotEntry.Value.Count > 0)
            {
                var slot = slotEntry.Value;
                slot.Count += packet.Count;
                data.Inventory[slotEntry.Key] = slot;
            }
            else
            {
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
    }

    public void UpdatePlayerInventory(string playerId, AmmoAddToSlotPacket packet)
    {
        lock (playerDataCache)
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
    }

    public void UpdatePlayerInventory(string playerId, AmmoDecrementPacket packet)
    {
        lock (playerDataCache)
        {
            var data = GetOrCreatePlayerData(playerId, "Player");
            if (data.Inventory.TryGetValue(packet.SlotIndex, out var slot))
            {
                slot.Count = Math.Max(0, slot.Count - packet.Count);
                if (slot.Count == 0)
                {
                    slot.Identifiable = -1;
                }
                data.Inventory[packet.SlotIndex] = slot;
                SavePlayerData(data);
            }
        }
    }

    public void SavePlayerData(PlayerData data)
    {
        lock (playerDataCache)
        {
            playerDataCache[data.PlayerId] = data;
            SaveAllPlayerData();
        }
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
            string json;
            lock (playerDataCache)
            {
                json = JsonConvert.SerializeObject(playerDataCache.Values.ToList(), Formatting.Indented);
            }
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

            lock (playerDataCache)
            {
                playerDataCache.Clear();
                foreach (var data in playerList)
                {
                    playerDataCache[data.PlayerId] = data;
                }
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

        lock (playerDataCache)
        {
            return playerDataCache.Values.Where(p => !connectedIds.Contains(p.PlayerId)).ToList();
        }
    }

    public PlayerData? GetPlayerData(string playerId)
    {
        lock (playerDataCache)
        {
            return playerDataCache.GetValueOrDefault(playerId);
        }
    }

    public void ClearInventory(string playerId)
    {
        lock (playerDataCache)
        {
            if (playerDataCache.TryGetValue(playerId, out var data))
            {
                data.Inventory.Clear();
                SavePlayerData(data);
            }
        }
    }
}