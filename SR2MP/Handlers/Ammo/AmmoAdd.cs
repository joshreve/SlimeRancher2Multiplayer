using System.Net;
using Il2CppMonomiPark.SlimeRancher.Player;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Ammo;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;
using SR2MP.Server.Managers;

namespace SR2MP.Handlers.Ammo;

[PacketHandler((byte)PacketType.AmmoAdd)]
internal sealed class AmmoAddHandler : BasePacketHandler<AmmoAddPacket>
{
    protected override bool Handle(AmmoAddPacket packet, IPEndPoint? _)
    {
        if (packet.ID != null && packet.ID.StartsWith("PLAYER_"))
        {
            if (Main.Server.IsRunning)
            {
                PlayerDataManager.Instance.UpdatePlayerInventory(packet.ID, packet);
            }
            return true;
        }

        var ammo = NetworkAmmoManager.GetAmmo(packet.ID);

        if (ammo == null) return false;
        var ident = ActorManager.ActorTypes[packet.Identifiable];
        
        var slotIdx = ammo.GetNextSlot(ident);
        if (slotIdx >= 0 && slotIdx < ammo.Slots.Count)
        {
            HandlingPacket = true;
            ammo.MaybeAddToSpecificSlot(new AmmoSlot.AmmoMetadata(ident), slotIdx, packet.Count, false);
            HandlingPacket = false;
        }

        return true;
    }
}