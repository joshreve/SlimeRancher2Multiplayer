using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.Actor;

[PacketHandler((byte)PacketType.ActorSpawnConfirm, HandlerType.Client)]
internal sealed class ActorSpawnConfirmHandler : BasePacketHandler<ActorSpawnConfirmPacket>
{
    protected override bool Handle(ActorSpawnConfirmPacket packet, IPEndPoint? sender)
    {
        GlobalVariables.ClientSpawnRegistry.ConfirmAndRemap(packet.ClientTempId, packet.HostCanonicalId);
        return true;
    }
}
