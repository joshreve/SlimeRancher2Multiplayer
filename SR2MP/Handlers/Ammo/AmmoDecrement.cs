using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Ammo;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;
using SR2MP.Server.Managers;

namespace SR2MP.Handlers.Ammo;

[PacketHandler((byte)PacketType.AmmoDecrement)]
internal sealed class AmmoDecrementHandler : BasePacketHandler<AmmoDecrementPacket>
{
    protected override bool Handle(AmmoDecrementPacket packet, IPEndPoint? _)
    {
        if (packet.ID != null && packet.ID.StartsWith("PLAYER_"))
        {
            if (Main.Server.IsRunning)
            {
                PlayerDataManager.Instance.UpdatePlayerInventory(packet.ID, packet);
            }
            return true;
        }

        var ammos = NetworkAmmoManager.GetLinkedAmmoManagers(packet.ID);
        if (ammos.Count == 0) return false;

        foreach (var ammo in ammos)
        {
            if (packet.SlotIndex >= 0 && packet.SlotIndex < ammo.Slots.Count)
            {
                HandlingPacket = true;
                ammo.Decrement(packet.SlotIndex, packet.Count);
                HandlingPacket = false;
            }
        }

        return true;
    }
}