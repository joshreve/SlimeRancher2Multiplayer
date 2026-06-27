using System;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

internal struct SaveFilePacket : IPacket
{
    public byte[] SaveBytes;
    public string OriginalSaveName;

    public readonly PacketType Type => PacketType.SaveFile;
    public readonly PacketReliability Reliability => PacketReliability.Reliable;
    public readonly NetworkChannel Channel => NetworkChannel.ActorCritical;

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WriteString(OriginalSaveName);
        writer.WriteArray(SaveBytes, (w, b) => w.WriteByte(b));
    }

    public void Deserialise(PacketReader reader)
    {
        OriginalSaveName = reader.ReadString() ?? string.Empty;
        SaveBytes = reader.ReadArray(r => r.ReadByte()) ?? Array.Empty<byte>();
    }
}
