using System;
using System.Net;
using Il2CppMonomiPark.SlimeRancher.Player.CharacterController.Abilities;
using Il2CppMonomiPark.SlimeRancher.Audio;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Player;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;
using SR2MP.Components.Player;
using UnityEngine;

namespace SR2MP.Handlers.Player;

[PacketHandler((byte)PacketType.PlayerPulseWave, HandlerType.Both)]
internal sealed class PlayerPulseWaveHandler : BasePacketHandler<PlayerPulseWavePacket>
{
    protected override bool Handle(PlayerPulseWavePacket packet, IPEndPoint? sender)
    {
        if (Main.Server.IsRunning && sender != null)
        {
            Main.Server.SendToAllExcept(packet, sender);
        }

        // SrLogger.LogMessage($"[PlayerPulseWaveHandler] Received pulse wave at {packet.Position}");

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
            var fx = Object.Instantiate(data.PulseFx, packet.Position, packet.Rotation);
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

            // Exclude players from raw rigidbody explosion force
            if (col.GetComponentInParent<NetworkPlayer>() != null || col.CompareTag("Player"))
                continue;

            var rb = col.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(power, packet.Position, radius, 0.5f, ForceMode.Impulse);
            }
        }

        // Apply force to local player if enabled
        if (Main.PlayerPulsingEnabled && SceneContext.Instance != null && SceneContext.Instance.Player != null)
        {
            var playerPos = SceneContext.Instance.Player.transform.position;
            var dist = Vector3.Distance(packet.Position, playerPos);
            if (dist < radius)
            {
                var controller = SceneContext.Instance.Player.GetComponent<Il2CppMonomiPark.SlimeRancher.Player.CharacterController.SRCharacterController>();
                if (controller != null)
                {
                    var dir = (playerPos - packet.Position).normalized;
                    if (dir.sqrMagnitude < 0.001f) dir = Vector3.up;
                    var forceFactor = 1f - (dist / radius);
                    var combinedForce = packet.PulsingForce * Main.PlayerPulsingForce;
                    var pushVelocity = dir * (power * forceFactor * 1.2f * combinedForce);
                    pushVelocity.y = Mathf.Max(pushVelocity.y, 4f * forceFactor * combinedForce);

                    controller.ForceUnground();
                    controller.BaseVelocity = controller.BaseVelocity + pushVelocity;
                    // SrLogger.LogMessage($"[PlayerPulseWaveHandler] Pushed local player with velocity: {pushVelocity}");
                }
            }
        }

        return true;
    }
}
