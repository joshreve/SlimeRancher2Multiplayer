using System.Collections.Generic;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Actor;

internal struct ActorChecksumPacket : IPacket
{
    public struct Entry
    {
        public int SceneGroupId;
        public int ActorCount;
    }

    public List<Entry> Entries;

    public readonly PacketType Type => PacketType.ActorChecksum;
    public readonly PacketReliability Reliability => PacketReliability.Reliable;
    public readonly NetworkChannel Channel => NetworkChannel.Important;

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WritePackedInt(Entries.Count);
        foreach (var entry in Entries)
        {
            writer.WritePackedInt(entry.SceneGroupId);
            writer.WritePackedInt(entry.ActorCount);
        }
    }

    public void Deserialise(PacketReader reader)
    {
        var count = reader.ReadPackedInt();
        Entries = new List<Entry>(count);
        for (var i = 0; i < count; i++)
        {
            Entries.Add(new Entry
            {
                SceneGroupId = reader.ReadPackedInt(),
                ActorCount = reader.ReadPackedInt()
            });
        }
    }
}
