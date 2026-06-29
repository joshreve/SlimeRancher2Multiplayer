using System.Net;
using Il2CppMonomiPark.SlimeRancher.Player;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Ammo;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;
using SR2MP.Server.Managers;

namespace SR2MP.Handlers.Ammo;

[PacketHandler((byte)PacketType.AmmoAddToSlot)]
internal sealed class AmmoAddToSlotHandler : BasePacketHandler<AmmoAddToSlotPacket>
{
    protected override bool Handle(AmmoAddToSlotPacket packet, IPEndPoint? _)
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

        var ident = ActorManager.ActorTypes[packet.Identifiable];
        foreach (var ammo in ammos)
        {
            HandlingPacket = true;
            ammo.MaybeAddToSpecificSlot(new AmmoSlot.AmmoMetadata(ident), packet.SlotIndex, packet.Count, false);
            HandlingPacket = false;
        }

        return true;
    }
}