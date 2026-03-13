namespace SR2MP.Packets.Utils;

public interface ICustomPacket : IReliabilityNetObject
{
    byte PacketHeader { get; }
}