using System;
using System.Net;
using Il2CppMonomiPark.SlimeRancher.Player.CharacterController.Abilities;
using Il2CppMonomiPark.SlimeRancher.Audio;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Player;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;
using UnityEngine;

namespace SR2MP.Handlers.Player;

[PacketHandler((byte)PacketType.PlayerPulseWave, HandlerType.Client)]
internal sealed class PlayerPulseWaveHandler : BasePacketHandler<PlayerPulseWavePacket>
{
    protected override bool Handle(PlayerPulseWavePacket packet, IPEndPoint? sender)
    {
        if (Main.Server.IsRunning) return false;

        SrLogger.LogMessage($"[PlayerPulseWaveHandler] Received pulse wave at {packet.Position}");

        var datas = Resources.FindObjectsOfTypeAll<PulseWaveAbilityData>();
        if (datas.Count == 0)
        {
            SrLogger.LogWarning("No PulseWaveAbilityData asset found in memory.");
            return false;
        }

        var data = datas[0];
        if (data == null)
            return false;

        if (data.PulseFx != null)
        {
            var fx = Object.Instantiate(data.PulseFx, packet.Position, Quaternion.identity);
            fx.SetActive(true);
            Object.Destroy(fx, 3f);
        }

        if (data.PulseWaveStartCue != null)
        {
            RemoteFXManager.PlayTransientAudio(data.PulseWaveStartCue, packet.Position, 0.8f);
        }

        var radius = data.PulseRadius;
        var power = data.PulsePower;
        var layers = data.CollisionLayers;

        var colliders = Physics.OverlapSphere(packet.Position, radius, layers);
        foreach (var col in colliders)
        {
            if (col == null || col.gameObject == null)
                continue;

            var rb = col.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(power, packet.Position, radius, 0.5f, ForceMode.Impulse);
            }
        }

        return true;
    }
}
