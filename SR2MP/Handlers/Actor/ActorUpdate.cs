using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Actor;

[PacketHandler((byte)PacketType.ActorUpdate)]
internal sealed class ActorUpdateHandler : BasePacketHandler<ActorUpdatePacket>
{
    protected override bool Handle(ActorUpdatePacket packet, IPEndPoint? _)
    {
        if (!ActorManager.Actors.TryGetValue(packet.ActorId.Value, out var model))
            return Main.Server.IsRunning;

        var actorModel = model.Cast<IdentifiableModel>();
        if (actorModel.TryGetNetworkComponent(out var actor))
        {
            if (!actor.LocallyOwned && actor.transformComponent != null)
            {
                actor.transformComponent.ApplyDelta(new TransformData
                {
                    Position = packet.Position,
                    Rotation = packet.Rotation,
                    Velocity = packet.Velocity
                });
            }
        }
        return true;
    }
}
