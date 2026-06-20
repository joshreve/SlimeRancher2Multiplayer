using System;
using System.Collections.Generic;
using SR2MP.Packets.Ammo;

namespace SR2MP.Server.Models;

internal sealed class PlayerData
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public DateTime LastConnected { get; set; } = DateTime.MinValue;
    public Dictionary<int, NetworkAmmoSlot> Inventory { get; set; } = new();

    public PlayerData() { }

    public PlayerData(string playerId, string playerName)
    {
        PlayerId = playerId;
        PlayerName = playerName;
        LastConnected = DateTime.UtcNow;
    }
}