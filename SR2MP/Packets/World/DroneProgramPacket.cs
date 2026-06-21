using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

internal sealed class DroneProgramPacket : IPacket
{
    public long ActorId;
    public int TargetIdent;
    public int Target;
    public int Sink;
    public int Source;

    public PacketType Type => PacketType.DroneProgram;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer)
    {
        writer.WritePackedLong(ActorId);
        writer.WritePackedInt(TargetIdent);
        writer.WritePackedInt(Target);
        writer.WritePackedInt(Sink);
        writer.WritePackedInt(Source);
    }

    public void Deserialise(PacketReader reader)
    {
        ActorId = reader.ReadPackedLong();
        TargetIdent = reader.ReadPackedInt();
        Target = reader.ReadPackedInt();
        Sink = reader.ReadPackedInt();
        Source = reader.ReadPackedInt();
    }
}
