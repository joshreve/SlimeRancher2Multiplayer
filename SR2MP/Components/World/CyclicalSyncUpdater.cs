using System;
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

    private void SyncMoneyAndUpgrades(System.Collections.Generic.List<Server.Models.ClientInfo> clients)
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

    private void SyncLandPlotsAndRefinery(System.Collections.Generic.List<Server.Models.ClientInfo> clients)
    {
        var plotsPacket = ReSyncManager.CreatePlotsPacket();
        var refineryPacket = ReSyncManager.CreateRefineryPacket();
        foreach (var client in clients)
        {
            Main.Server.SendToClient(plotsPacket, client.EndPoint);
            Main.Server.SendToClient(refineryPacket, client.EndPoint);
        }
    }

    private void SyncSwitchesAndDoors(System.Collections.Generic.List<Server.Models.ClientInfo> clients)
    {
        var switchesPacket = ReSyncManager.CreateSwitchesPacket();
        var accessDoorsPacket = ReSyncManager.CreateAccessDoorsPacket();
        foreach (var client in clients)
        {
            Main.Server.SendToClient(switchesPacket, client.EndPoint);
            Main.Server.SendToClient(accessDoorsPacket, client.EndPoint);
        }
    }

    private void SyncGordosAndPods(System.Collections.Generic.List<Server.Models.ClientInfo> clients)
    {
        var gordosPacket = ReSyncManager.CreateGordoSlimesPacket();
        var treasurePodsPacket = ReSyncManager.CreateTreasurePodsPacket();
        foreach (var client in clients)
        {
            Main.Server.SendToClient(gordosPacket, client.EndPoint);
            Main.Server.SendToClient(treasurePodsPacket, client.EndPoint);
        }
    }

    private void SyncGreyLabyrinth(System.Collections.Generic.List<Server.Models.ClientInfo> clients)
    {
        var puzzleSlotsPacket = ReSyncManager.CreatePuzzleSlotsPacket();
        var plortDepositorsPacket = ReSyncManager.CreatePlortDepositorsPacket();
        var prismaBarriersPacket = ReSyncManager.CreatePrismaBarriersPacket();
        foreach (var client in clients)
        {
            Main.Server.SendToClient(puzzleSlotsPacket, client.EndPoint);
            Main.Server.SendToClient(plortDepositorsPacket, client.EndPoint);
            Main.Server.SendToClient(prismaBarriersPacket, client.EndPoint);
        }
    }

    private void SyncWeatherAndPrices(System.Collections.Generic.List<Server.Models.ClientInfo> clients)
    {
        var pricesPacket = ReSyncManager.CreatePricesPacket();
        foreach (var client in clients)
        {
            Main.Server.SendToClient(pricesPacket, client.EndPoint);
            ReSyncManager.SendWeatherPacket(client.EndPoint);
        }
    }

    private void SyncPediaAndMap(System.Collections.Generic.List<Server.Models.ClientInfo> clients)
    {
        var pediaPacket = ReSyncManager.CreatePediaPacket();
        var mapPacket = ReSyncManager.CreateMapPacket();
        foreach (var client in clients)
        {
            Main.Server.SendToClient(pediaPacket, client.EndPoint);
            Main.Server.SendToClient(mapPacket, client.EndPoint);
        }
    }
}
