using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Actor;

[PacketHandler((byte)PacketType.ActorDelta)]
internal sealed class ActorDeltaHandler : BasePacketHandler<ActorDeltaPacket>
{
    protected override bool Handle(ActorDeltaPacket packet, IPEndPoint? _)
    {
        if (!ActorManager.Actors.TryGetValue(packet.ActorId.Value, out var model))
            return Main.Server.IsRunning;

        var actor = model.Cast<ActorModel>();

        if (!actor.TryGetNetworkComponent(out var networkComponent))
            return Main.Server.IsRunning;

        networkComponent.ApplyDelta(packet);
        return true;
    }
}
