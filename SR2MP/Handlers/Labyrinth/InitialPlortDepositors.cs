using System.Net;
using Il2Cpp;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Labyrinth;

[PacketHandler((byte)PacketType.InitialPlortDepositors, HandlerType.Client)]
internal sealed class InitialPlortDepositorsHandler : BasePacketHandler<InitialPlortDepositorsPacket>
{
    protected override bool Handle(InitialPlortDepositorsPacket packet, IPEndPoint? _)
    {
        var gameModel = GameState;

        foreach (var dep in packet.Depositors)
        {
            if (gameModel.depositors.TryGetValue(dep.ID, out var depModel))
            {
                depModel.AmountDeposited = dep.AmountDeposited;
                if (depModel._gameObject)
                {
                    var comp = depModel._gameObject.GetComponent<PlortDepositor>();
                    if (comp)
                    {
                        comp.OnFilledChangedFromModel();
                    }
                }
            }
            else
            {
                depModel = new PlortDepositorModel
                {
                    _gameObject = null,
                    AmountDeposited = dep.AmountDeposited
                };
                gameModel.depositors.Add(dep.ID, depModel);
            }
        }

        return false;
    }
}
