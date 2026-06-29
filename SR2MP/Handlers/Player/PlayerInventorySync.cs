using System.Net;
using Il2CppMonomiPark.SlimeRancher.Player;
using Il2CppMonomiPark.SlimeRancher.Player.CharacterController;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Player;
using SR2MP.Packets.Utils;
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
        
        NetworkAmmoManager.ApplyInventory(localAmmo, packet.Ammo.AmmoSlots);

        if (packet.HasPosition)
        {
            var targetPos = new Vector3(packet.PosX, packet.PosY, packet.PosZ);
            var characterController = SceneContext.Instance?.Player?.GetComponent<SRCharacterController>();
            if (characterController != null)
            {
                characterController.Position = targetPos;
                SrLogger.LogMessage($"Teleported local player to saved position: {targetPos}");
            }
        }
        
        HandlingPacket = false;
        return true;
    }
}
