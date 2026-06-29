using SR2MP.Packets.Ammo;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Player;

internal sealed class PlayerInventorySyncPacket : IPacket
{
    public NetworkAmmo Ammo = new();
    public bool HasPosition;
    public float PosX;
    public float PosY;
    public float PosZ;

    public PacketType Type => PacketType.PlayerInventorySync;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.Important;

    public void Serialise(PacketWriter writer)
    {
        Ammo.Serialise(writer);
        writer.WritePackedBool(HasPosition);
        if (HasPosition)
        {
            writer.WriteFloat(PosX);
            writer.WriteFloat(PosY);
            writer.WriteFloat(PosZ);
        }
    }

    public void Deserialise(PacketReader reader)
    {
        Ammo = new NetworkAmmo();
        Ammo.Deserialise(reader);
        HasPosition = reader.ReadPackedBool();
        if (HasPosition)
        {
            PosX = reader.ReadFloat();
            PosY = reader.ReadFloat();
            PosZ = reader.ReadFloat();
        }
    }
}
