using System.Net;
using Il2CppMonomiPark.SlimeRancher.Player.PlayerItems;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.FX;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.FX;

[PacketHandler((byte)PacketType.PlayerFX)]
internal sealed class PlayerFXHandler : BasePacketHandler<PlayerFXPacket>
{
    protected override bool Handle(PlayerFXPacket packet, IPEndPoint? _)
    {
        HandlingPacket = true;

        try
        {
            if (!IsPlayerSoundDictionary[packet.FX])
            {
                var fxPrefab = FXManager.PlayerFXMap[packet.FX];
                if (fxPrefab != null)
                {
                    FXHelpers.SpawnAndPlayFX(fxPrefab, packet.Position, Quaternion.identity);
                }
            }
            else
            {
                var cue = FXManager.PlayerAudioCueMap[packet.FX];

                if (ShouldPlayerSoundBeTransientDictionary[packet.FX])
                {
                    RemoteFXManager.PlayTransientAudio(cue, PlayerObjects[packet.Player].transform.position,
                        PlayerSoundVolumeDictionary[packet.FX]);
                }
                else
                {
                    var playerAudio = PlayerObjects[packet.Player].GetComponent<SECTR_PointSource>();
                    playerAudio.Cue = cue;
                    playerAudio.Loop = DoesPlayerSoundLoopDictionary[packet.FX];
                    playerAudio.instance.Volume = PlayerSoundVolumeDictionary[packet.FX];
                    playerAudio.Play();
                }

                var vacItem = PlayerObjects[packet.Player].GetComponentInChildren<VacuumItem>();
                if (vacItem != null && vacItem.VacFX != null)
                {
                    if (packet.FX == PlayerFXType.VacRunningStart || packet.FX == PlayerFXType.VacRunning)
                    {
                        vacItem.VacFX.SetActive(true);
                    }
                    else if (packet.FX == PlayerFXType.VacRunningEnd)
                    {
                        vacItem.VacFX.SetActive(false);
                    }
                }
            }
        }
        catch { /* Errors here are typically non-serious related to scene loading */ }
        finally
        {
            HandlingPacket = false;
        }

        return true;
    }
}