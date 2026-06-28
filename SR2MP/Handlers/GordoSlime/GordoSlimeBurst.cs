using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.GordoSlime;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.GordoSlime;

[PacketHandler((byte)PacketType.GordoBurst)]
internal sealed class GordoSlimeBurstHandler : BasePacketHandler<GordoSlimeBurstPacket>
{
    protected override bool Handle(GordoSlimeBurstPacket packet, IPEndPoint? _)
    {
        if (GameState.gordos.TryGetValue(packet.ID, out var gordoSlime))
        {
            gordoSlime.GordoEatenCount = gordoSlime.targetCount + 1;

            HandlingPacket = true;

            if (gordoSlime.gameObj)
            {
                try
                {
                    var gordoEat = gordoSlime.gameObj.GetComponent<GordoEat>();
                    if (gordoEat != null)
                    {
                        var rewards = gordoEat._rewards;
                        if (rewards != null && rewards._activeRewards == null)
                        {
                            try
                            {
                                rewards.SetupActiveRewards();
                            }
                            catch (System.Exception ex)
                            {
                                SrLogger.LogDebug($"Could not setup active rewards for Gordo: {ex.Message}");
                            }
                        }
                        gordoEat.ImmediateReachedTarget();
                    }
                }
                catch (System.Exception ex)
                {
                    SrLogger.LogWarning($"Failed to burst Gordo immediately: {ex.Message}");
                }
            }

            HandlingPacket = false;
        }
        else
        {
            gordoSlime = new GordoModel
            {
                fashions = new CppCollections.List<IdentifiableType>(0),
                gordoEatCount = 999999,
                gordoSeen = false,
                gameObj = null,
                targetCount = 50
            };

            GameState.gordos.Add(packet.ID, gordoSlime);
        }

        return true;
    }
}