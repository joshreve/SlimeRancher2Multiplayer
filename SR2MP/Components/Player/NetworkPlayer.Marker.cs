using Il2CppMonomiPark.SlimeRancher.SceneManagement;
using Il2CppMonomiPark.SlimeRancher.UI;
using Il2CppTMPro;

namespace SR2MP.Components.Player;
// part of the class that is using IMapMarkerSource interface stuff
// (Also other map marker stuff)
internal partial class NetworkPlayer
{
    private void SetupMarker()
    {
        // Local players don't need a separate marker
        if (IsLocal)
        {
            return;
        }

        var markerComponent = gameObject.AddComponent<RadarTrackedPointOfInterest>();
        markerComponent.enabled = false;
        markerComponent._worldRadarPrefab = null;
        markerComponent._compassRadarPrefab = Instantiate(PlayerCompassPrefab);
        markerComponent._isOptional = false;
        markerComponent._overflowMode = RadarCompassOverflowMode.CLAMP;
        markerComponent._ranchBehaviour = RadarEntryRanchHandling.SHOW_IN_RANCH_AS_WELL;
        markerComponent.enabled = true;
        
        SrLogger.LogMessage($"Remote player marker added: {model!.PlayerId}", SrLogTarget.Both);
    }

    private void UpdateMarker()
    {
        var marker = PlayerMarkerTransforms[ID];
        if (!marker.mainMarker || !marker.markerArrow)
        {
            
        }
    }
    //private bool IsMapMarkerActive
    //private Vector3 MapPosition
    //private SceneGroup SceneGroup 
}