using System.Net;
using SR2MP.Packets.Utils;

namespace SR2MP.Shared;

internal readonly struct ServerHandleCache
{
    public readonly PacketReader Reader;
    public readonly IServerPacketHandler Handler;
    public readonly IPEndPoint ClientEp;
    public readonly float ReceiveTime;

    public ServerHandleCache(PacketReader reader, IServerPacketHandler handler, IPEndPoint clientEp, float receiveTime)
    {
        Reader = reader;
        Handler = handler;
        ClientEp = clientEp;
        ReceiveTime = receiveTime;
    }
}

internal readonly struct ClientHandleCache
{
    public readonly PacketReader Reader;
    public readonly IClientPacketHandler Handler;
    public readonly float ReceiveTime;

    public ClientHandleCache(PacketReader reader, IClientPacketHandler handler, float receiveTime)
    {
        Reader = reader;
        Handler = handler;
        ReceiveTime = receiveTime;
    }
}