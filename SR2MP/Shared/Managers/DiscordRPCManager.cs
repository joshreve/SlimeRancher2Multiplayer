using System.Collections.ObjectModel;
using DiscordRPC;
using Il2CppMonomiPark.SlimeRancher.World;
using SR2E.Managers;

namespace SR2MP.Shared.Managers;

internal static class DiscordRPCManager
{
    private enum Zone : byte
    {
        Unknown,
        Conservatory,
        RainbowFields,
        StarlightStand,
        EmberValley,
        PowderfallBluffs,
        LabyrinthWaterworks,
        LabyrinthLavaDepths,
        LabyrinthDreamland,
        LabyrinthHub,
        LabyrinthTerrarium,
        LabyrinthCore,
        MainMenu,

        // Introduce at like, 0.4 or 1.0.?
        FinalBoss,
        Ending
    }

    // This can be public, do not freak out :)
    private const string DiscordAppID = "1422276739026911262";
    private static DiscordRpcClient? rpcClient;

    private static readonly ReadOnlyDictionary<Zone, string> ZoneToStatusKey =
        new(new Dictionary<Zone, string>
        {
            {Zone.Unknown, "status.unknown"},
            {Zone.Conservatory, "status.ranch"},
            {Zone.RainbowFields, "status.fields"},
            {Zone.StarlightStand, "status.strand"},
            {Zone.EmberValley, "status.valley"},
            {Zone.PowderfallBluffs, "status.bluffs"},
            {Zone.LabyrinthTerrarium, "status.terrarium"},
            {Zone.LabyrinthLavaDepths, "status.lavadepths"},
            {Zone.LabyrinthWaterworks, "status.waterworks"},
            {Zone.LabyrinthDreamland, "status.dreamland"},
            {Zone.LabyrinthHub, "status.labyrinth"},
            {Zone.LabyrinthCore, "status.core"},
            {Zone.MainMenu, "status.menu"},
            {Zone.FinalBoss, "status.boss"}, // this uses markdown
            {Zone.Ending, "status.ending"}
        });

    private static readonly ReadOnlyDictionary<string, Zone> DefinitionToZone =
        new(new Dictionary<string, Zone>
        {
            {"Conservatory", Zone.Conservatory},
            {"Labyrinth hub", Zone.LabyrinthHub},
            {"RainbowFields", Zone.RainbowFields},
            {"Luminous Strand", Zone.StarlightStand},
            {"Rumbling Gorge", Zone.EmberValley},
            {"Zoo_Debug", Zone.MainMenu},
            {"Powderfall Bluffs", Zone.PowderfallBluffs},
            {"Labyrinth dreamland", Zone.LabyrinthDreamland},
            {"Labyrinth valley entrance", Zone.LabyrinthLavaDepths},
            {"Labyrinth strand entrance", Zone.LabyrinthWaterworks},
            {"Labyrinth terrarium", Zone.LabyrinthTerrarium},
            {"Labyrinth core", Zone.LabyrinthCore},
            {"Conservatory Archway", Zone.Conservatory},
            {"Conservatory Den", Zone.Conservatory},
            {"Conservatory Digsite", Zone.Conservatory},
            {"Conservatory Gully", Zone.Conservatory},
            {"Conservatory Pools", Zone.Conservatory}
        });

    private static readonly ReadOnlyDictionary<Zone, string> ZoneToIcon =
        new(new Dictionary<Zone, string>
        {
            {Zone.Unknown, "unknown"},
            {Zone.Conservatory, "conservatory"},
            {Zone.RainbowFields, "rainbowfields"},
            {Zone.StarlightStand, "starlightstand"},
            {Zone.EmberValley, "embervalley"},
            {Zone.PowderfallBluffs, "powderfallbluffs"},
            {Zone.LabyrinthHub, "impossiblesky"},
            {Zone.LabyrinthTerrarium, "terrarium"},
            {Zone.LabyrinthLavaDepths, "lavadepths"},
            {Zone.LabyrinthWaterworks, "waterworks"},
            {Zone.LabyrinthDreamland, "dreamland"},
            {Zone.LabyrinthCore, "core"},
            {Zone.MainMenu, "mainmenu"},
            {Zone.FinalBoss, "battle"},
            {Zone.Ending, "ending"}
        });

    private const string DetailsStringOnlineKey = "details.online";
    private const string DetailsStringOnlineSoloKey = "details.solo";
    private const string DetailsStringOfflineKey = "details.offline";

    private static string DetailsStringOnline
        => SR2ELanguageManger.translation(DetailsStringOnlineKey);
    private static string DetailsStringOnlineSolo
        => SR2ELanguageManger.translation(DetailsStringOnlineSoloKey);
    private static string DetailsStringOffline
        => SR2ELanguageManger.translation(DetailsStringOfflineKey);
    private static string GetStatus(Zone zone)
        => SR2ELanguageManger.translation(ZoneToStatusKey[zone]);
    
    
    public static void Initialize()
    {
        rpcClient = new DiscordRpcClient(DiscordAppID);

        rpcClient.Initialize();

        UpdatePresence();
    }

    public static void Shutdown()
    {
        rpcClient?.Dispose();
    }

    public static ZoneDefinition? currentZone;

    public static bool IsInEndingCutscene => SystemContext.Instance.SceneLoader._currentSceneGroup?.name == "OutroSequence";
    public static bool IsFightingFinalBoss => BossFightController.Instance?._bossFightIsActive == true;

    internal static void UpdatePresence()
    {
        var online = Main.Server.IsRunning || Main.Client.IsConnected;
        var solo = PlayerManager.PlayerCount < 2;

        var details = online
            ? solo
                ? DetailsStringOnlineSolo
                : string.Format(DetailsStringOnline, PlayerManager.PlayerCount)
            : DetailsStringOffline;
        var currentLocation = currentZone ? (DefinitionToZone.TryGetValue(currentZone!.name, out var zone) ? zone : Zone.Unknown) : Zone.MainMenu;

        if (IsFightingFinalBoss)
            currentLocation = Zone.FinalBoss;
        if (IsInEndingCutscene)
            currentLocation = Zone.Ending;

        var status = GetStatus(currentLocation);
        var icon = ZoneToIcon[currentLocation];

        rpcClient?.SetPresence(new RichPresence
        {
            Details = details,
            State = status,
            Assets = new Assets
            {
                LargeImageKey = icon,
                LargeImageText = string.Empty
            },
            Buttons = new[]
            {
                new Button
                {
                    Label = "SR2 Multiplayer Discord",
                    Url = "https://discord.gg/a7wfBw5feU"
                }
            }
        });
    }
}