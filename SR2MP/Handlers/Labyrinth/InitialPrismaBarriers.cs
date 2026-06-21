using System.Net;
using Il2CppMonomiPark.SlimeRancher.Labyrinth;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Labyrinth;

[PacketHandler((byte)PacketType.InitialPrismaBarriers, HandlerType.Client)]
internal sealed class InitialPrismaBarriersHandler : BasePacketHandler<InitialPrismaBarriersPacket>
{
    protected override bool Handle(InitialPrismaBarriersPacket packet, IPEndPoint? _)
    {
        var gameModel = GameState;

        foreach (var bar in packet.Barriers)
        {
            if (gameModel.AllPrismaBarriers().TryGetValue(bar.ID, out var barModel))
            {
                barModel.ActivationTime = bar.ActivationTime;
                if (barModel._gameObj)
                {
                    var comp = barModel._gameObj.GetComponent<PrismaBarrier>();
                    if (comp)
                    {
                        comp.SetActivationTime(bar.ActivationTime);
                    }
                }
            }
            else
            {
                barModel = new PrismaBarrierModel(bar.ID)
                {
                    _gameObj = null,
                    ActivationTime = bar.ActivationTime
                };
                gameModel.AllPrismaBarriers().Add(bar.ID, barModel);
            }
        }

        return false;
    }
}
