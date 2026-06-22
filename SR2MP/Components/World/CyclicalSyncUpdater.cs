using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Economy;
using Il2CppMonomiPark.SlimeRancher.Weather;
using Il2CppMonomiPark.SlimeRancher.World;
using MelonLoader;
using SR2MP.Packets.Internal;
using SR2MP.Shared.Managers;
using SR2MP.Shared.Utils;
using Starlight.Storage;
using UnityEngine;
using SR2MP.Packets.Loading;
using SR2MP.Packets.TreasurePod;

namespace SR2MP.Components.World;

[InjectIntoIL]
internal sealed class CyclicalSyncUpdater : MonoBehaviour
{
    private float timer;
    private int currentCycleStep;

    public void Update()
    {
        timer += UnityEngine.Time.deltaTime;

        if (timer < Timers.CyclicalSyncTimer)
            return;

        timer = 0f;

        if (!Main.Server.IsRunning)
            return;

        var clients = Main.Server.ClientManager.GetAllClients().ToList();
        if (clients.Count == 0)
            return;

        switch (currentCycleStep)
        {
            case 0:
                SyncMoneyAndUpgrades(clients);
                break;
            case 1:
                SyncLandPlotsAndRefinery(clients);
                break;
            case 2:
                SyncSwitchesAndDoors(clients);
                break;
            case 3:
                SyncGordosAndPods(clients);
                break;
            case 4:
                SyncGreyLabyrinth(clients);
                break;
            case 5:
                SyncWeatherAndPrices(clients);
                break;
            case 6:
                SyncPediaAndMap(clients);
                break;
        }

        currentCycleStep = (currentCycleStep + 1) % 7;
    }

    private static void SyncMoneyAndUpgrades(System.Collections.Generic.List<Server.Models.ClientInfo> clients)
    {
        var money = SceneContext.Instance.PlayerState.GetCurrency(
            GameContext.Instance.LookupDirector._currencyList[0].Cast<ICurrency>());
        var rainbowMoney = SceneContext.Instance.PlayerState.GetCurrency(
            GameContext.Instance.LookupDirector._currencyList[1].Cast<ICurrency>());
        
        var upgradesPacket = ReSyncManager.CreateUpgradesPacket();

        foreach (var client in clients)
        {
            var approvePacket = new ConnectionApprovePacket
            {
                InitialJoin = false,
                PlayerId = client.PlayerId,
                OtherPlayers = Array.ConvertAll(PlayerManager.GetAllPlayers().ToArray(),
                    p => (p.PlayerId, p.Username)),
                Money = money,
                RainbowMoney = rainbowMoney,
                AllowCheats = Main.AllowCheats
            };
            Main.Server.SendToClient(approvePacket, client.EndPoint);
            Main.Server.SendToClient(upgradesPacket, client.EndPoint);
        }
    }

    private static void SyncLandPlotsAndRefinery(System.Collections.Generic.List<Server.Models.ClientInfo> clients)
    {
        var defaultPlotsPacket = ReSyncManager.CreatePlotsPacket();
        var refineryPacket = ReSyncManager.CreateRefineryPacket();
        foreach (var client in clients)
        {
            var remotePlayer = PlayerManager.GetPlayer(client.PlayerId);
            var plotsPacket = remotePlayer != null
                ? CreateFilteredPlotsPacket(remotePlayer.Position, MaxSyncDistance)
                : defaultPlotsPacket;

            Main.Server.SendToClient(plotsPacket, client.EndPoint);
            Main.Server.SendToClient(refineryPacket, client.EndPoint);
        }
    }

    private static void SyncSwitchesAndDoors(System.Collections.Generic.List<Server.Models.ClientInfo> clients)
    {
        var defaultSwitchesPacket = ReSyncManager.CreateSwitchesPacket();
        var defaultAccessDoorsPacket = ReSyncManager.CreateAccessDoorsPacket();
        foreach (var client in clients)
        {
            var remotePlayer = PlayerManager.GetPlayer(client.PlayerId);
            var switchesPacket = remotePlayer != null
                ? CreateFilteredSwitchesPacket(remotePlayer.Position, MaxSyncDistance)
                : defaultSwitchesPacket;
            var accessDoorsPacket = remotePlayer != null
                ? CreateFilteredAccessDoorsPacket(remotePlayer.Position, MaxSyncDistance)
                : defaultAccessDoorsPacket;

            Main.Server.SendToClient(switchesPacket, client.EndPoint);
            Main.Server.SendToClient(accessDoorsPacket, client.EndPoint);
        }
    }

    private static void SyncGordosAndPods(System.Collections.Generic.List<Server.Models.ClientInfo> clients)
    {
        var defaultGordosPacket = ReSyncManager.CreateGordoSlimesPacket();
        var defaultTreasurePodsPacket = ReSyncManager.CreateTreasurePodsPacket();
        foreach (var client in clients)
        {
            var remotePlayer = PlayerManager.GetPlayer(client.PlayerId);
            var gordosPacket = remotePlayer != null
                ? CreateFilteredGordoSlimesPacket(remotePlayer.Position, MaxSyncDistance)
                : defaultGordosPacket;
            var treasurePodsPacket = remotePlayer != null
                ? CreateFilteredTreasurePodsPacket(remotePlayer.Position, MaxSyncDistance)
                : defaultTreasurePodsPacket;

            Main.Server.SendToClient(gordosPacket, client.EndPoint);
            Main.Server.SendToClient(treasurePodsPacket, client.EndPoint);
        }
    }

    private static void SyncGreyLabyrinth(System.Collections.Generic.List<Server.Models.ClientInfo> clients)
    {
        var defaultPuzzleSlotsPacket = ReSyncManager.CreatePuzzleSlotsPacket();
        var defaultPlortDepositorsPacket = ReSyncManager.CreatePlortDepositorsPacket();
        var defaultPrismaBarriersPacket = ReSyncManager.CreatePrismaBarriersPacket();
        foreach (var client in clients)
        {
            var remotePlayer = PlayerManager.GetPlayer(client.PlayerId);
            var puzzleSlotsPacket = remotePlayer != null
                ? CreateFilteredPuzzleSlotsPacket(remotePlayer.Position, MaxSyncDistance)
                : defaultPuzzleSlotsPacket;
            var plortDepositorsPacket = remotePlayer != null
                ? CreateFilteredPlortDepositorsPacket(remotePlayer.Position, MaxSyncDistance)
                : defaultPlortDepositorsPacket;
            var prismaBarriersPacket = remotePlayer != null
                ? CreateFilteredPrismaBarriersPacket(remotePlayer.Position, MaxSyncDistance)
                : defaultPrismaBarriersPacket;

            Main.Server.SendToClient(puzzleSlotsPacket, client.EndPoint);
            Main.Server.SendToClient(plortDepositorsPacket, client.EndPoint);
            Main.Server.SendToClient(prismaBarriersPacket, client.EndPoint);
        }
    }

    private static void SyncWeatherAndPrices(System.Collections.Generic.List<Server.Models.ClientInfo> clients)
    {
        var pricesPacket = ReSyncManager.CreatePricesPacket();
        foreach (var client in clients)
        {
            Main.Server.SendToClient(pricesPacket, client.EndPoint);
            ReSyncManager.SendWeatherPacket(client.EndPoint);
        }
    }

    private static void SyncPediaAndMap(System.Collections.Generic.List<Server.Models.ClientInfo> clients)
    {
        var pediaPacket = ReSyncManager.CreatePediaPacket();
        var mapPacket = ReSyncManager.CreateMapPacket();
        foreach (var client in clients)
        {
            Main.Server.SendToClient(pediaPacket, client.EndPoint);
            Main.Server.SendToClient(mapPacket, client.EndPoint);
        }
    }

    private static InitialLandPlotsPacket CreateFilteredPlotsPacket(Vector3 clientPos, float maxDistance)
    {
        var packet = ReSyncManager.CreatePlotsPacket();
        packet.LandPlots = packet.LandPlots.Where(p =>
        {
            if (GameState.landPlots.TryGetValue(p.ID, out var plot) && plot.gameObj != null)
            {
                return Vector3.SqrMagnitude(plot.gameObj.transform.position - clientPos) <= maxDistance * maxDistance;
            }
            return true;
        }).ToList();
        return packet;
    }

    private static InitialSwitchesPacket CreateFilteredSwitchesPacket(Vector3 clientPos, float maxDistance)
    {
        var packet = ReSyncManager.CreateSwitchesPacket();
        packet.Switches = packet.Switches.Where(s =>
        {
            if (GameState.switches.TryGetValue(s.ID, out var switchModel) && switchModel.gameObj != null)
            {
                return Vector3.SqrMagnitude(switchModel.gameObj.transform.position - clientPos) <= maxDistance * maxDistance;
            }
            return true;
        }).ToList();
        return packet;
    }

    private static InitialAccessDoorsPacket CreateFilteredAccessDoorsPacket(Vector3 clientPos, float maxDistance)
    {
        var packet = ReSyncManager.CreateAccessDoorsPacket();
        packet.Doors = packet.Doors.Where(d =>
        {
            if (GameState.doors.TryGetValue(d.ID, out var doorModel) && doorModel.gameObj != null)
            {
                return Vector3.SqrMagnitude(doorModel.gameObj.transform.position - clientPos) <= maxDistance * maxDistance;
            }
            return true;
        }).ToList();
        return packet;
    }

    private static InitialGordosPacket CreateFilteredGordoSlimesPacket(Vector3 clientPos, float maxDistance)
    {
        var packet = ReSyncManager.CreateGordoSlimesPacket();
        packet.GordoSlimes = packet.GordoSlimes.Where(g =>
        {
            if (GameState.gordos.TryGetValue(g.Id, out var gordoModel) && gordoModel.gameObj != null)
            {
                return Vector3.SqrMagnitude(gordoModel.gameObj.transform.position - clientPos) <= maxDistance * maxDistance;
            }
            return true;
        }).ToList();
        return packet;
    }

    private static InitialTreasurePodsPacket CreateFilteredTreasurePodsPacket(Vector3 clientPos, float maxDistance)
    {
        var packet = ReSyncManager.CreateTreasurePodsPacket();
        var filteredPods = new Dictionary<int, Il2Cpp.TreasurePod.State>();
        foreach (var kvp in packet.TreasurePods)
        {
            var key = "pod" + kvp.Key;
            if (GameState.pods.TryGetValue(key, out var podModel) && podModel.gameObj != null)
            {
                if (Vector3.SqrMagnitude(podModel.gameObj.transform.position - clientPos) > maxDistance * maxDistance)
                    continue;
            }
            filteredPods.Add(kvp.Key, kvp.Value);
        }
        packet.TreasurePods = filteredPods;
        return packet;
    }

    private static InitialPuzzleSlotsPacket CreateFilteredPuzzleSlotsPacket(Vector3 clientPos, float maxDistance)
    {
        var packet = ReSyncManager.CreatePuzzleSlotsPacket();
        packet.Slots = packet.Slots.Where(s =>
        {
            if (GameState.slots.TryGetValue(s.ID, out var slotModel) && slotModel.gameObj != null)
            {
                return Vector3.SqrMagnitude(slotModel.gameObj.transform.position - clientPos) <= maxDistance * maxDistance;
            }
            return true;
        }).ToList();
        return packet;
    }

    private static InitialPlortDepositorsPacket CreateFilteredPlortDepositorsPacket(Vector3 clientPos, float maxDistance)
    {
        var packet = ReSyncManager.CreatePlortDepositorsPacket();
        packet.Depositors = packet.Depositors.Where(d =>
        {
            if (GameState.depositors.TryGetValue(d.ID, out var depositorModel) && depositorModel._gameObject != null)
            {
                return Vector3.SqrMagnitude(depositorModel._gameObject.transform.position - clientPos) <= maxDistance * maxDistance;
            }
            return true;
        }).ToList();
        return packet;
    }

    private static InitialPrismaBarriersPacket CreateFilteredPrismaBarriersPacket(Vector3 clientPos, float maxDistance)
    {
        var packet = ReSyncManager.CreatePrismaBarriersPacket();
        var barriers = GameState.AllPrismaBarriers();
        packet.Barriers = packet.Barriers.Where(b =>
        {
            if (barriers.TryGetValue(b.ID, out var barrierModel) && barrierModel._gameObj != null)
            {
                return Vector3.SqrMagnitude(barrierModel._gameObj.transform.position - clientPos) <= maxDistance * maxDistance;
            }
            return true;
        }).ToList();
        return packet;
    }
}
