using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Utils;

namespace SR2MP.Handlers.Actor;

[PacketHandler((byte)PacketType.ActorDestroy)]
internal sealed class ActorDestroyHandler : BasePacketHandler<ActorDestroyPacket>
{
    protected override bool Handle(ActorDestroyPacket packet, IPEndPoint? _)
    {
        if (ActorManager.Actors.TryGetValue(packet.ActorId.Value, out var actor))
        {
            if (actor.TryCast<GadgetModel>(out var gm))
            {
                GameState.DestroyGadgetModel(gm);
            }
            else
            {
                GameState.identifiables.Remove(packet.ActorId);
                if (actor.ident != null && GameState.identifiablesByIdent.ContainsKey(actor.ident))
                {
                    GameState.identifiablesByIdent[actor.ident].Remove(actor);
                }
                GameState.DestroyIdentifiableModel(actor);
            }
            ActorManager.Actors.Remove(packet.ActorId.Value);

            HandlingPacket = true;
            try
            {
                var obj = actor.GetGameObject();
                if (obj)
                    Destroyer.DestroyAny(obj, "SR2MP.ActorDestroyHandler");
            }
            catch { }
            HandlingPacket = false;
        }
        else
        {
            if (GameState.TryGetIdentifiableModel(packet.ActorId, out var actorModel))
            {
                GameState.identifiables.Remove(packet.ActorId);
                if (actorModel.ident != null && GameState.identifiablesByIdent.ContainsKey(actorModel.ident))
                {
                    GameState.identifiablesByIdent[actorModel.ident].Remove(actorModel);
                }
                GameState.DestroyIdentifiableModel(actorModel);
                ActorManager.Actors.Remove(actorModel.actorId.Value);

                HandlingPacket = true;
                try
                {
                    var obj = actorModel.GetGameObject();
                    if (obj)
                        Destroyer.DestroyAny(obj, "SR2MP.ActorDestroyHandler");
                }
                catch { }
                HandlingPacket = false;
            }
            else
            {
                GadgetModel? gm = null;
                try
                {
                    var model = GameState.GetIdentifiableModel(packet.ActorId);
                    if (model != null)
                        gm = model.TryCast<GadgetModel>();
                }
                catch { }

                if (gm != null)
                {
                    GameState.DestroyGadgetModel(gm);
                    ActorManager.Actors.Remove(packet.ActorId.Value);

                    var obj = gm.GetGameObject();
                    if (obj)
                    {
                        HandlingPacket = true;
                        try
                        {
                            Destroyer.DestroyAny(obj, "SR2MP.ActorDestroyHandler");
                        }
                        catch { }
                        HandlingPacket = false;
                    }
                }
            }
        }

        return true;
    }
}