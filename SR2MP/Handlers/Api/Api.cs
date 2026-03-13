using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Api;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Api;

[PacketHandler((byte)PacketType.ApiCall)]
internal sealed class ApiHandler : BasePacketHandler<ApiPacket>
{
    protected override bool Handle(ApiPacket packet, IPEndPoint? clientEp)
    {
        return true;
    }
}

public static class ApiHandlers
{
    private static readonly Dictionary<byte, IClientPacketHandler> ClientHandlers = new();
    private static readonly Dictionary<byte, IServerPacketHandler> ServerHandlers = new();
}