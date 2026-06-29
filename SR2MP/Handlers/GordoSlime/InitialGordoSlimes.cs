using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.GordoSlime;

[PacketHandler((byte)PacketType.InitialGordos, HandlerType.Client)]
internal sealed class InitialGordoSlimeLoadHandler : BasePacketHandler<InitialGordosPacket>
{
    protected override bool Handle(InitialGordosPacket packet, IPEndPoint? _)
    {
        // Since the client loads the host's save game, Gordo states are already
        // perfectly synchronized from the save file. Additional sync at connect
        // is redundant and can overwrite or misalign native Gordo components.
        return true;
    }
}