using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

/// <summary>
/// Sent by peers when they enter or exit a SceneGroup (zone).
/// The host maintains a registry of which peers have which zones loaded,
/// enabling targeted spawn delegation and ownership tie-breaking.
/// </summary>
internal sealed class ScenePresencePacket : IPacket
{
    public string PlayerId = string.Empty;
    public int SceneGroupId;
    public bool Entered;

    public PacketType Type => PacketType.ScenePresence;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.Important;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteStringWithoutSize(PlayerId);
        writer.WritePackedInt(SceneGroupId);
        writer.WritePackedBool(Entered);
    }

    public void Deserialise(PacketReader reader)
    {
        PlayerId = reader.ReadPooledStringOfSize(16)!;
        SceneGroupId = reader.ReadPackedInt();
        Entered = reader.ReadPackedBool();
    }
}
