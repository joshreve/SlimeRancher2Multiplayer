using System.Net;
using Il2CppMonomiPark.SlimeRancher.Player.PlayerItems;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.FX;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;
using Starlight.Storage;

namespace SR2MP.Handlers.FX;

[PacketHandler((byte)PacketType.PlayerFX)]
internal sealed class PlayerFXHandler : BasePacketHandler<PlayerFXPacket>
{
    protected override bool Handle(PlayerFXPacket packet, IPEndPoint? _)
    {
        HandlingPacket = true;

        try
        {
            // SrLogger.LogMessage($"[PlayerFXHandler] Handle: FX={packet.FX}, Player={packet.Player}");

            if (!IsPlayerSoundDictionary[packet.FX])
            {
                var fxPrefab = FXManager.PlayerFXMap[packet.FX];
                if (fxPrefab != null)
                {
                    FXHelpers.SpawnAndPlayFX(fxPrefab, packet.Position, Quaternion.identity);
                }
            }
            else
            {
                var cue = FXManager.PlayerAudioCueMap[packet.FX];

                if (ShouldPlayerSoundBeTransientDictionary[packet.FX])
                {
                    RemoteFXManager.PlayTransientAudio(cue, PlayerObjects[packet.Player].transform.position,
                        PlayerSoundVolumeDictionary[packet.FX]);
                }
                else
                {
                    var playerAudio = PlayerObjects[packet.Player].GetComponent<SECTR_PointSource>();
                    playerAudio.Cue = cue;
                    playerAudio.Loop = DoesPlayerSoundLoopDictionary[packet.FX];
                    playerAudio.instance.Volume = PlayerSoundVolumeDictionary[packet.FX];
                    playerAudio.Play();
                }

                var remotePlayer = PlayerObjects[packet.Player];
                var vacStandard = FindChildRecursive(remotePlayer.transform, "vacStandard");
                if (vacStandard == null)
                {
                    var netPlayer = remotePlayer.GetComponent<SR2MP.Components.Player.NetworkPlayer>();
                    if (netPlayer != null)
                    {
                        netPlayer.LastVacFX = packet.FX;
                        netPlayer.HasPendingVacFXUpdate = true;
                        // SrLogger.LogMessage($"[PlayerFXHandler] Player model not fully loaded yet. Caching pending vac FX: {packet.FX} for player: {packet.Player}");
                    }
                }
                else
                {
                    ApplyVacFX(remotePlayer, packet.FX);
                }
            }
        }
        catch (System.Exception ex)
        {
            SrLogger.LogError("[PlayerFXHandler] Error in Handle: " + ex.ToString());
        }
        finally
        {
            HandlingPacket = false;
        }

        return true;
    }

    internal static void ApplyVacFX(GameObject remotePlayer, PlayerFXType fx)
    {
        UpdateVacAnimator(remotePlayer, fx);
        var vacFX = GetOrCreateVacFX(remotePlayer);
        if (vacFX != null)
        {
            bool active = (fx == PlayerFXType.VacRunningStart || fx == PlayerFXType.VacRunning);

            var modifier = vacFX.GetComponent<RemoteVacFXModifier>();
            if (modifier != null)
            {
                modifier.VacActive = active;
            }

            var anim = vacFX.GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetBool("active", active);
                anim.SetBool("vacMode", active);
            }

            vacFX.SetActive(active);
        }
    }

    private static void UpdateVacAnimator(GameObject remotePlayer, PlayerFXType fx)
    {
        /*
        if (!hasDumpedHierarchy)
        {
            hasDumpedHierarchy = true;
            SrLogger.LogMessage("=== REMOTE PLAYER ROOT HIERARCHY DUMP ===");
            DumpHierarchyToConsole(remotePlayer.transform, "");
            
            if (SceneContext.Instance != null && SceneContext.Instance.Player != null)
            {
                SrLogger.LogMessage("=== LOCAL PLAYER ROOT HIERARCHY DUMP ===");
                DumpHierarchyToConsole(SceneContext.Instance.Player.transform, "");
            }
            SrLogger.LogMessage("=== END ROOT HIERARCHY DUMP ===");
        }
        */

        var vacStandard = FindChildRecursive(remotePlayer.transform, "vacStandard");
        if (vacStandard == null) return;

        var animator = remotePlayer.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            /*
            if (!hasDumpedWeaponAnimator)
            {
                hasDumpedWeaponAnimator = true;
                try
                {
                    SrLogger.LogMessage($"=== PLAYER MAIN ANIMATOR PARAMETERS (count={animator.parameterCount}) ===");
                    for (int i = 0; i < animator.parameterCount; i++)
                    {
                        var param = animator.GetParameter(i);
                        SrLogger.LogMessage($"  Param {i}: name={param.name}, type={param.type}");
                    }
                    SrLogger.LogMessage("=== END PLAYER MAIN ANIMATOR PARAMETERS ===");
                }
                catch (System.Exception ex)
                {
                    SrLogger.LogError("Error dumping main player animator parameters: " + ex.ToString());
                }
            }
            */
            
            try
            {
                if (fx == PlayerFXType.VacRunningStart || fx == PlayerFXType.VacRunning)
                {
                    try { animator.SetInteger("vacMode", 1); } catch {}
                    try { animator.SetBool("vacMode", true); } catch {}
                    try { animator.SetBool("active", true); } catch {}
                }
                else if (fx == PlayerFXType.VacRunningEnd)
                {
                    try { animator.SetInteger("vacMode", 0); } catch {}
                    try { animator.SetBool("vacMode", false); } catch {}
                    try { animator.SetBool("active", false); } catch {}
                }
                else if (fx == PlayerFXType.VacShoot || fx == PlayerFXType.VacShootSound)
                {
                    try { animator.SetInteger("vacMode", 2); } catch {}
                    try { animator.SetBool("vacMode", true); } catch {}
                    try { animator.SetBool("active", true); } catch {}
                }
            }
            catch (System.Exception ex)
            {
                SrLogger.LogDebug($"Failed to set main animator parameters: {ex}");
            }
        }

        var colorAnimator = vacStandard.GetComponent<Il2Cpp.VacColorAnimator>();
        if (colorAnimator != null)
        {
            colorAnimator.enabled = false;

            var spiralRenderer = colorAnimator.SpiralRenderer;
            var mat = colorAnimator._vacSpiralMat;
            if (mat == null && spiralRenderer != null)
            {
                mat = spiralRenderer.material;
            }

            if (fx == PlayerFXType.VacRunningStart || fx == PlayerFXType.VacRunning)
            {
                if (spiralRenderer != null)
                    spiralRenderer.gameObject.SetActive(true);

                if (mat != null)
                {
                    mat.SetColor(Il2Cpp.VacColorAnimator.PROPERTY_SPIRAL_COLOR, new Color(0.2f, 0.6f, 1.0f, 0.4f));
                    mat.SetColor(Il2Cpp.VacColorAnimator.PROPERTY_AMMO_COLOR, new Color(0.2f, 0.6f, 1.0f, 0.4f));
                    mat.SetFloat(Il2Cpp.VacColorAnimator.PROPERTY_AMMO_FULLNESS, 1.0f);
                    mat.SetFloat(Il2Cpp.VacColorAnimator.PROPERTY_PARALLAX_HEIGHT, 1.0f);
                }
            }
            else if (fx == PlayerFXType.VacRunningEnd)
            {
                if (spiralRenderer != null)
                    spiralRenderer.gameObject.SetActive(false);

                if (mat != null)
                {
                    mat.SetFloat(Il2Cpp.VacColorAnimator.PROPERTY_AMMO_FULLNESS, 0.0f);
                    mat.SetFloat(Il2Cpp.VacColorAnimator.PROPERTY_PARALLAX_HEIGHT, 0.0f);
                }
            }
        }

        var interactionFX = remotePlayer.GetComponentInChildren<Il2CppMonomiPark.SlimeRancher.VFX.EnvironmentInteraction.VacuumInteractionFX>();
        if (interactionFX != null)
        {
            interactionFX.vacActive = (fx == PlayerFXType.VacRunningStart || fx == PlayerFXType.VacRunning);
        }
    }

    private static bool hasDumpedHierarchy = false;

    private static void DumpHierarchyToConsole(Transform t, string indent)
    {
        if (t == null) return;
        
        var compsStr = "";
        var comps = t.GetComponents<Component>();
        if (comps != null)
        {
            foreach (var comp in comps)
            {
                if (comp == null) continue;
                var compName = comp.GetIl2CppType().Name;
                
                if (compName == "ParticleSystem")
                {
                    try
                    {
                        var ps = comp.Cast<ParticleSystem>();
                        compName += $"[playing={ps.isPlaying}, particles={ps.particleCount}]";
                    }
                    catch {}
                }
                else if (compName == "MeshRenderer" || compName == "SkinnedMeshRenderer")
                {
                    try
                    {
                        var r = comp.Cast<Renderer>();
                        compName += $"[enabled={r.enabled}, mat={r.sharedMaterial?.name}]";
                    }
                    catch {}
                }
                
                compsStr += (compsStr == "" ? "" : ", ") + compName;
            }
        }
        
        SrLogger.LogMessage($"{indent}Go: {t.name} (active={t.gameObject.activeSelf}/{t.gameObject.activeInHierarchy}) | Comps: [{compsStr}]");
        
        for (int i = 0; i < t.childCount; i++)
        {
            DumpHierarchyToConsole(t.GetChild(i), indent + "  ");
        }
    }

    internal static Transform? FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            var result = FindChildRecursive(parent.GetChild(i), name);
            if (result != null) return result;
        }
        return null;
    }

    private static GameObject? GetOrCreateVacFX(GameObject remotePlayer)
    {
        var vacStandard = FindChildRecursive(remotePlayer.transform, "vacStandard");
        if (vacStandard == null) return null;

        var remoteVacFX = FindChildRecursive(vacStandard, "VacFX");
        if (remoteVacFX != null) return remoteVacFX.gameObject;

        if (SceneContext.Instance == null || SceneContext.Instance.Player == null) return null;
        var pic = SceneContext.Instance.Player.GetComponent<PlayerItemController>();
        if (pic == null || pic._vacuumItem == null || pic._vacuumItem.VacFX == null) return null;

        var localVacFX = pic._vacuumItem.VacFX;

        if (!hasLoggedParentChain)
        {
            hasLoggedParentChain = true;
            try
            {
                var p = localVacFX.transform;
                SrLogger.LogMessage("=== localVacFX Parent Chain ===");
                while (p != null)
                {
                    SrLogger.LogMessage($"  {p.name} | localPos: {p.localPosition}, localRot: {p.localRotation.eulerAngles}, localScale: {p.localScale}");
                    p = p.parent;
                }
                SrLogger.LogMessage("===============================");
            }
            catch (System.Exception ex)
            {
                SrLogger.LogError("Failed to log localVacFX parent chain: " + ex.Message);
            }
        }

        var nozzle = FindChildRecursive(vacStandard, "bone_vac_barrel");
        var targetParent = nozzle ?? vacStandard;

        // SrLogger.LogMessage($"[GetOrCreateVacFX] Target parent for remote VacFX: {targetParent.name} (nozzle found: {nozzle != null})");

        var clonedFX = Object.Instantiate(localVacFX, targetParent);
        clonedFX.name = "VacFX";

        if (nozzle != null)
        {
            clonedFX.transform.localPosition = Quaternion.Euler(90, 0, 0) * localVacFX.transform.localPosition;
            clonedFX.transform.localRotation = Quaternion.Euler(90, 0, 0) * localVacFX.transform.localRotation;
            clonedFX.transform.localScale = localVacFX.transform.localScale;
        }
        else
        {
            clonedFX.transform.localPosition = new Vector3(0.25f, -0.17f, 1.37f) + Quaternion.Euler(90, 0, 0) * localVacFX.transform.localPosition;
            clonedFX.transform.localRotation = Quaternion.Euler(90, 0, 0) * localVacFX.transform.localRotation;
            clonedFX.transform.localScale = localVacFX.transform.localScale;
        }

        // SrLogger.LogMessage($"[GetOrCreateVacFX] Instantiated VacFX on remote player. LocalPos: {clonedFX.transform.localPosition}, LocalRot: {clonedFX.transform.localRotation.eulerAngles}, LocalScale: {clonedFX.transform.localScale}, WorldPos: {clonedFX.transform.position}, WorldRot: {clonedFX.transform.rotation.eulerAngles}");

        SetLayerRecursive(clonedFX, 0);

        var anim = clonedFX.GetComponent<Animator>();
        if (anim != null)
        {
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        var modifier = clonedFX.AddComponent<RemoteVacFXModifier>();
        if (modifier != null)
        {
            modifier.VacActive = false;
        }

        return clonedFX;
    }

    private static void SetLayerRecursive(GameObject obj, int layer)
    {
        if (obj == null) return;
        obj.layer = layer;
        for (int i = 0; i < obj.transform.childCount; i++)
        {
            var child = obj.transform.GetChild(i);
            if (child != null)
            {
                SetLayerRecursive(child.gameObject, layer);
            }
        }
    }

    private static bool hasDumpedWeaponAnimator = false;
    private static bool hasLoggedParentChain = false;
}

[InjectIntoIL]
internal sealed class RemoteVacFXModifier : MonoBehaviour
{
    public bool VacActive;

    private ParticleSystem[] _particleSystems;
    private Renderer[] _renderers;
    private Transform[] _transforms;

    private void Start()
    {
        _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        _renderers = GetComponentsInChildren<Renderer>(true);
        _transforms = GetComponentsInChildren<Transform>(true);
    }

    private void LateUpdate()
    {
        if (_transforms != null)
        {
            for (int i = 0; i < _transforms.Length; i++)
            {
                var t = _transforms[i];
                if (t != null)
                    t.gameObject.layer = 0;
            }
        }

        if (_particleSystems != null)
        {
            for (int i = 0; i < _particleSystems.Length; i++)
            {
                var ps = _particleSystems[i];
                if (ps == null) continue;
                
                ps.gameObject.SetActive(VacActive);
                
                if (VacActive)
                {
                    if (!ps.isPlaying)
                        ps.Play();
                }
                else
                {
                    if (ps.isPlaying)
                        ps.Stop();
                }
            }
        }

        if (_renderers != null)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                var r = _renderers[i];
                if (r == null) continue;
                r.gameObject.SetActive(VacActive);
                r.enabled = VacActive;
            }
        }
    }
}