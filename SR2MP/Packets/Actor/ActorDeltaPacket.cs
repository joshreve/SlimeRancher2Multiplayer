using System.Runtime.InteropServices;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Utils;
using Unity.Mathematics;
using UnityEngine;

namespace SR2MP.Packets.Actor;

internal static class DeltaRegistry
{
    public const byte KeyTransform = 1;
    public const byte KeySlimeEmotions = 2;
    public const byte KeyResource = 3;
    public const byte KeyPlort = 4;

    public static object DeserializeKey(byte key, PacketReader reader)
    {
        return key switch
        {
            KeyTransform => new TransformData
            {
                Position = reader.ReadVector3(),
                Rotation = reader.ReadQuaternion(),
                Velocity = reader.ReadVector3()
            },
            KeySlimeEmotions => new SlimeEmotionsData
            {
                Emotions = reader.ReadFloat4(),
                Sleeping = reader.ReadBool()
            },
            KeyResource => new ResourceData
            {
                Progress = reader.ReadDouble(),
                State = reader.ReadPackedEnum<ResourceCycle.State>()
            },
            KeyPlort => new PlortData
            {
                Invulnerable = reader.ReadBool(),
                InvulnerablePeriod = reader.ReadFloat()
            },
            _ => throw new System.ArgumentException($"Unknown delta key: {key}")
        };
    }

    public static void SerializeValue(byte key, object value, PacketWriter writer)
    {
        switch (key)
        {
            case KeyTransform:
                var t = (TransformData)value;
                writer.WriteVector3(t.Position);
                writer.WriteQuaternion(t.Rotation);
                writer.WriteVector3(t.Velocity);
                break;
            case KeySlimeEmotions:
                var s = (SlimeEmotionsData)value;
                writer.WriteFloat4(s.Emotions);
                writer.WriteBool(s.Sleeping);
                break;
            case KeyResource:
                var r = (ResourceData)value;
                writer.WriteDouble(r.Progress);
                writer.WritePackedEnum(r.State);
                break;
            case KeyPlort:
                var p = (PlortData)value;
                writer.WriteBool(p.Invulnerable);
                writer.WriteFloat(p.InvulnerablePeriod);
                break;
        }
    }
}

public struct DeltaValue
{
    public byte Key;
    public object Value;
}

public struct TransformData
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Velocity;
}

public struct SlimeEmotionsData
{
    public float4 Emotions;
    public bool Sleeping;
}

public struct ResourceData
{
    public double Progress;
    public ResourceCycle.State State;
}

public struct PlortData
{
    public bool Invulnerable;
    public float InvulnerablePeriod;
}

[StructLayout(LayoutKind.Auto)]
internal struct ActorDeltaPacket : IPacket
{
    public ActorId ActorId;
    public System.Collections.Generic.List<DeltaValue> Deltas;

    public readonly PacketType Type => PacketType.ActorDelta;
    public readonly PacketReliability Reliability => PacketReliability.Reliable;
    public readonly NetworkChannel Channel => NetworkChannel.ActorCritical;

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WriteLong(ActorId.Value);
        writer.WriteByte((byte)Deltas.Count);
        foreach (var delta in Deltas)
        {
            writer.WriteByte(delta.Key);
            DeltaRegistry.SerializeValue(delta.Key, delta.Value, writer);
        }
    }

    public void Deserialise(PacketReader reader)
    {
        ActorId = new ActorId(reader.ReadLong());
        byte count = reader.ReadByte();
        Deltas = new System.Collections.Generic.List<DeltaValue>(count);
        for (int i = 0; i < count; i++)
        {
            byte key = reader.ReadByte();
            object val = DeltaRegistry.DeserializeKey(key, reader);
            Deltas.Add(new DeltaValue { Key = key, Value = val });
        }
    }
}
