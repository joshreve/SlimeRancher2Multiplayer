using System.Collections;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Weather;
using Il2CppMonomiPark.SlimeRancher.World;
using Il2CppMonomiPark.SlimeRancher.UI.Map;
using SR2MP.Packets.World;
using SR2MP.Server.Managers;

namespace SR2MP.Client.Managers;

internal static class NetworkWeatherManager
{
    public static WeatherRegistry Registry => SceneContext.Instance.WeatherRegistry;

    public static WeatherDirector Director
    {
        get
        {
            if (!director)
            {
                director = Resources.FindObjectsOfTypeAll<WeatherDirector>().FirstOrDefault()!;
            }

            return director;
        }
    }

    public static LightningStrike Lightning
    {
        get
        {
            if (!lightning)
            {
                lightning = Resources.FindObjectsOfTypeAll<LightningStrike>().First(x => x.BlastPower < 2749f);
            }

            return lightning;
        }
    }

    private static LightningStrike lightning;
    private static WeatherDirector director;

    public static readonly Dictionary<int, WeatherStateDefinition> WeatherStates = new();

    internal static void Initialize()
    {
        var refer = GameContext.Instance.AutoSaveDirector._saveReferenceTranslation;
        foreach (var state in refer._weatherStateTranslation.RawLookupDictionary)
        {
            WeatherStates.Add(refer.GetPersistenceId(state.value), state.value.TryCast<WeatherStateDefinition>()!);
        }
    }

    public static void CheckInitialized()
    {
        if (WeatherStates.Count == 0)
            Initialize();
    }

    public static int GetPersistentID(WeatherStateDefinition state)
        => GameContext.Instance.AutoSaveDirector._saveReferenceTranslation
            .GetPersistenceId(state.Cast<IWeatherState>());

    internal static IEnumerator Apply(WeatherPacket packet, bool immediate)
    {
        while (HandlingPacket)
        {
            yield return null;
        }

        WeatherUpdateHelper.EnsureLookupInitialized();

        yield return new WaitFrames(3);

        if (SceneContext.Instance == null || Registry == null || Director == null)
        {
            yield break;
        }

        HandlingPacket = true;

        var registry = Registry;
        var localDirector = Director;

        var zoneKeys = new List<ZoneDefinition>();
        foreach (var zone in registry._zones)
        {
            if (zone.Key != null)
                zoneKeys.Add(zone.Key);
            yield return null;
        }

        byte zoneId = 0;
        foreach (var zoneKey in zoneKeys)
        {
            if (zoneKey == null)
                continue;

            if (!packet.Zones.TryGetValue(zoneId, out var data))
                continue;

            var zone = registry._zones[zoneKey];
            if (zone == null)
                continue;

            var forecastCopy = new List<WeatherModel.ForecastEntry>();
            if (zone.Forecast != null)
            {
                foreach (var forecast in zone.Forecast)
                {
                    if (forecast != null)
                        forecastCopy.Add(forecast);
                }
            }

            foreach (var forecast in forecastCopy)
            {
                if (forecast == null || forecast.State == null)
                    continue;

                yield return null;
                var patternInstance = registry.GetWeatherPatternInstance(
                    zoneKey,
                    forecast.Pattern
                );

                if (patternInstance == null)
                {
                    if (zone.Parameters != null)
                    {
                        localDirector.StopState(
                            forecast.State.Cast<IWeatherState>(),
                            zone.Parameters
                        );
                    }
                }
                else
                {
                    registry.StopPatternState(
                        zoneKey,
                        patternInstance,
                        forecast.State
                    );
                }

                yield return new WaitFrames(2);
            }

            zone.Forecast.Clear();
            zone.Parameters.WindDirection = data.WindSpeed;

            foreach (var forecast in data.WeatherForecasts)
            {
                var pattern = WeatherUpdateHelper.GetPatternForZoneAndState(zoneKey, forecast.State.name);
                yield return null;

                if (pattern == null)
                {
                    SrLogger.LogWarning($"[NetworkWeather] Skipping forecast entry with null pattern for zone {zoneKey?.name} and state {forecast.State?.name}");
                    continue;
                }

                zone.Forecast.Add(new WeatherModel.ForecastEntry
                {
                    State = forecast.State.Cast<IWeatherState>(),
                    Pattern = pattern,
                    Started = forecast.WeatherStarted,
                    StartTime = forecast.StartTime,
                    EndTime = forecast.EndTime
                });

                yield return new WaitFrames(2);
            }

            yield return null;
            zoneId++;
            yield return new WaitFrames(2);
        }

        if (!registry._zones.TryGetValue(localDirector.Zone, out var activeZone))
            yield break;

        var activeCopy = new List<WeatherModel.ForecastEntry>();
        if (activeZone.Forecast != null)
        {
            foreach (var activeForecast in activeZone.Forecast)
            {
                if (activeForecast != null)
                    activeCopy.Add(activeForecast);
                yield return null;
            }
        }

        yield return null;

        foreach (var forecast in activeCopy)
        {
            if (forecast == null || forecast.State == null)
                continue;

            yield return null;
            var patternInstance = registry.GetWeatherPatternInstance(
                localDirector.Zone,
                forecast.Pattern
            );

            yield return null;
            if (patternInstance == null)
            {
                if (activeZone.Parameters != null)
                {
                    localDirector.RunState(forecast.State.Cast<IWeatherState>(), activeZone.Parameters, immediate);
                }
            }
            else
            {
                registry.RunPatternState(
                    localDirector.Zone,
                    patternInstance,
                    forecast.State,
                    immediate
                );
            }

            yield return new WaitFrames(3);
        }

        if (SR2MP.Patches.Map.OnMapUIAppear.ActiveMapUI != null)
        {
            var zoomedOutUI = SR2MP.Patches.Map.OnMapUIAppear.ActiveMapUI._zoomedOutUI;
            if (zoomedOutUI != null && zoomedOutUI._zoneMarkerUIs != null)
            {
                foreach (var markerUI in zoomedOutUI._zoneMarkerUIs)
                {
                    var zoneMarkerUI = markerUI.TryCast<ZoneMarkerUI>();
                    if (zoneMarkerUI != null)
                    {
                        zoneMarkerUI.SetUpWeather();
                    }
                }
            }
        }

        HandlingPacket = false;
    }
}