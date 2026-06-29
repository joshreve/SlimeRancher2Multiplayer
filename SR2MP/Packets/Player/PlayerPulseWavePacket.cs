using SR2MP.Packets.Utils;
using UnityEngine;

namespace SR2MP.Packets.Player;

internal struct PlayerPulseWavePacket : IPacket
{
    public Vector3 Position;
    public Quaternion Rotation;
    public float PulsingForce;

    public readonly PacketType Type => PacketType.PlayerPulseWave;
    public readonly PacketReliability Reliability => PacketReliability.Reliable;
    public readonly NetworkChannel Channel => NetworkChannel.ActorCritical;

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WriteVector3(Position);
        writer.WriteQuaternion(Rotation);
        writer.WriteFloat(PulsingForce);
    }

    public void Deserialise(PacketReader reader)
    {
        Position = reader.ReadVector3();
        Rotation = reader.ReadQuaternion();
        PulsingForce = reader.ReadFloat();
    }
}
