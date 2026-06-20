using SR2MP.Packets.Ammo;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Player;

internal sealed class PlayerInventorySyncPacket : IPacket
{
    public NetworkAmmo Ammo = new();

    public PacketType Type => PacketType.PlayerInventorySync;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.Player;

    public void Serialise(PacketWriter writer)
    {
        Ammo.Serialise(writer);
    }

    public void Deserialise(PacketReader reader)
    {
        Ammo = new NetworkAmmo();
        Ammo.Deserialise(reader);
    }
}
