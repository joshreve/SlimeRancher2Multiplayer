using HarmonyLib;
using Il2Cpp;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Components.Actor;
using SR2MP.Shared.Managers;
using UnityEngine;

namespace SR2MP.Patches.Actor;

[HarmonyPatch(typeof(InstantiationHelpers), nameof(InstantiationHelpers.InstantiateActorFromModel))]
internal static class InstantiateActorFromModelPatch
{
    public static void Postfix(ActorModel model, ref GameObject __result)
    {
        if (HandlingPacket) return;

        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return;

        if (__result == null || model == null) return;

        var actorId = model.actorId;
        if (actorId.Value == 0) return;

        var networkComponent = __result.GetComponent<NetworkActor>();
        if (networkComponent == null)
        {
            networkComponent = __result.AddComponent<NetworkActor>();
            networkComponent.LocallyOwned = Main.Server.IsRunning || (actorId.Value >= 3000000000L);
            
            networkComponent.previousPosition = __result.transform.position;
            networkComponent.nextPosition = __result.transform.position;
            networkComponent.previousRotation = __result.transform.rotation;
            networkComponent.nextRotation = __result.transform.rotation;
        }

        var identModel = model.TryCast<IdentifiableModel>();
        if (identModel != null)
        {
            GlobalVariables.ActorManager.Actors[actorId.Value] = identModel;
        }
    }
}
