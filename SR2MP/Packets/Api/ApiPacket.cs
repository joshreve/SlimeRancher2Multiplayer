using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Api;

internal readonly struct ApiPacket : IPacket
{
    public PacketType Type => PacketType.ApiCall;
    public PacketReliability Reliability { get; }

    public ApiPacket(PacketReliability reliability) => Reliability = reliability;

    public void Deserialise(PacketReader reader) { }

    public void Serialise(PacketWriter writer) { }
}