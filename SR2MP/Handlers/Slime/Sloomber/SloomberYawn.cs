using System.Net;
using Il2CppMonomiPark.SlimeRancher.Slime.Slumber;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Slime.Sloomber;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Slime.Sloomber;

[PacketHandler((byte)PacketType.SloomberYawn)]
internal sealed class SloomberYawnHandler : BasePacketHandler<SloomberYawnPacket>
{
    protected override bool Handle(SloomberYawnPacket packet, IPEndPoint? _)
    {
        if (!ActorManager.Actors.TryGetValue(packet.ActorId.Value, out var model))
            return Main.Server.IsRunning;

        if (!model.TryGetNetworkComponent(out var networkComponent))
            return Main.Server.IsRunning;

        var yawn = networkComponent.GetComponent<InitiateYawn>();
        if (!yawn)
            return Main.Server.IsRunning;

        HandlingPacket = true;

        if (packet.Active)
        {
            foreach (var reactable in yawn._yawnComponentReactables)
                reactable.BeginYawn();
        }
        else
        {
            yawn.DoYawn();

            foreach (var reactable in yawn._yawnComponentReactables)
                reactable.EndYawn();
        }

        HandlingPacket = false;
        return true;
    }
}