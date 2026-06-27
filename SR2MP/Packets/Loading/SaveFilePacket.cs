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
        writer.WritePackedInt(SaveBytes.Length);
        writer.WriteSpan(SaveBytes);
    }

    public void Deserialise(PacketReader reader)
    {
        OriginalSaveName = reader.ReadString() ?? string.Empty;
        var length = reader.ReadPackedInt();
        SaveBytes = new byte[length];
        reader.ReadToSpan(SaveBytes);
    }
}
