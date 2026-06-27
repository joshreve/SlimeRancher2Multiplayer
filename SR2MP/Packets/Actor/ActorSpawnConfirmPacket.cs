using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Actor;

internal struct ActorSpawnConfirmPacket : IPacket
{
    public long ClientTempId;
    public long HostCanonicalId;

    public readonly PacketType Type => PacketType.ActorSpawnConfirm;
    public readonly PacketReliability Reliability => PacketReliability.Reliable;
    public readonly NetworkChannel Channel => NetworkChannel.ActorCritical;

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WritePackedLong(ClientTempId);
        writer.WritePackedLong(HostCanonicalId);
    }

    public void Deserialise(PacketReader reader)
    {
        ClientTempId = reader.ReadPackedLong();
        HostCanonicalId = reader.ReadPackedLong();
    }
}
