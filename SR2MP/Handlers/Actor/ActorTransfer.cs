using System.Net;
using Il2CppMonomiPark.SlimeRancher.Player.PlayerItems;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Actor;

[PacketHandler((byte)PacketType.ActorTransfer)]
internal sealed class ActorTransferHandler : BasePacketHandler<ActorTransferPacket>
{
    protected override bool Handle(ActorTransferPacket packet, IPEndPoint? _)
    {
        if (!ActorManager.Actors.TryGetValue(packet.ActorId.Value, out var actor))
            return Main.Server.IsRunning;

        if (!actor.TryGetNetworkComponent(out var component))
            return Main.Server.IsRunning;

        var isNewOwner = packet.OwnerId == LocalID;

        // Only force-release a held object when ownership is being taken AWAY from us.
        if (!isNewOwner)
        {
            var vac = SceneContext.Instance.Player.GetComponent<PlayerItemController>()._vacuumItem;
            var gameObject = actor.GetGameObject();

            if (vac._held == gameObject)
            {
                vac.LockJoint.connectedBody = null;
                vac._held = null;
                vac.SetHeldRad(0f);
                vac._vacMode = VacuumItem.VacMode.NONE;
                gameObject.GetComponent<Vacuumable>().Release();
            }
        }

        // Use OwnerId to determine ownership instead of unconditionally releasing.
        // This fixes ownership races when multiple players are in the same zone.
        component.LocallyOwned = isNewOwner;

        return true;
    }
}