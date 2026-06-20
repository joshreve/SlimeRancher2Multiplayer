using System.Net;
using Il2CppMonomiPark.SlimeRancher.Player;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Player;
using SR2MP.Shared.Managers;
using SR2MP.Shared.Utils;

namespace SR2MP.Handlers.Player;

[PacketHandler((byte)PacketType.PlayerInventorySync, HandlerType.Client)]
internal sealed class PlayerInventorySyncHandler : BasePacketHandler<PlayerInventorySyncPacket>
{
    protected override bool Handle(PlayerInventorySyncPacket packet, IPEndPoint? _)
    {
        var localAmmo = SceneContext.Instance?.PlayerState?.Ammo;
        if (localAmmo == null)
        {
            SrLogger.LogWarning("Cannot sync player inventory: local ammo is null!");
            return false;
        }

        SrLogger.LogMessage("Applying synced player inventory from server...");

        HandlingPacket = true;
        
        // Loop over local slots and update them from packet slots:
        for (int i = 0; i < localAmmo.Slots.Count; i++)
        {
            var slot = localAmmo.Slots[i];
            if (packet.Ammo.AmmoSlots.TryGetValue(i, out var netSlot))
            {
                slot._count = netSlot.Count;
                if (netSlot.Count > 0 && netSlot.Identifiable != -1)
                {
                    slot._id = GlobalVariables.ActorManager.ActorTypes.TryGetValue(netSlot.Identifiable, out var type) ? type : null!;
                }
                else
                {
                    slot._id = null;
                }
            }
            else
            {
                slot._count = 0;
                slot._id = null;
            }
        }
        
        HandlingPacket = false;
        return true;
    }
}
