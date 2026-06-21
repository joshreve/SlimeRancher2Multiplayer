using System.Net;
using Il2CppMonomiPark.SlimeRancher.Player.PlayerItems;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.FX;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.FX;

[PacketHandler((byte)PacketType.PlayerFX)]
internal sealed class PlayerFXHandler : BasePacketHandler<PlayerFXPacket>
{
    protected override bool Handle(PlayerFXPacket packet, IPEndPoint? _)
    {
        HandlingPacket = true;

        try
        {
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
                UpdateVacAnimator(remotePlayer, packet.FX);
                var vacFX = GetOrCreateVacFX(remotePlayer);
                if (vacFX != null)
                {
                    if (packet.FX == PlayerFXType.VacRunningStart || packet.FX == PlayerFXType.VacRunning)
                    {
                        vacFX.SetActive(true);
                    }
                    else if (packet.FX == PlayerFXType.VacRunningEnd)
                    {
                        vacFX.SetActive(false);
                    }
                }
            }
        }
        catch { /* Errors here are typically non-serious related to scene loading */ }
        finally
        {
            HandlingPacket = false;
        }

        return true;
    }

    private static void UpdateVacAnimator(GameObject remotePlayer, PlayerFXType fx)
    {
        var vacStandard = FindChildRecursive(remotePlayer.transform, "vacStandard");
        if (vacStandard == null) return;

        var animator = vacStandard.GetComponent<Animator>();
        if (animator != null)
        {
            if (fx == PlayerFXType.VacRunningStart || fx == PlayerFXType.VacRunning)
            {
                animator.SetInteger("vacMode", 1);
                animator.SetBool("active", true);
            }
            else if (fx == PlayerFXType.VacRunningEnd)
            {
                animator.SetInteger("vacMode", 0);
                animator.SetBool("active", false);
            }
            else if (fx == PlayerFXType.VacShoot || fx == PlayerFXType.VacShootSound)
            {
                animator.SetInteger("vacMode", 2);
                animator.SetBool("active", true);
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
                if (!System.IO.File.Exists("E:\\Users\\jashreve\\git\\SlimeRancher2Multiplayer\\vac_hierarchy_dump.txt"))
                {
                    try
                    {
                        var sb = new System.Text.StringBuilder();
                        sb.AppendLine("=== REMOTE PLAYER VAC HIERARCHY ===");
                        DumpHierarchy(vacStandard, sb, "");
                        
                        if (SceneContext.Instance != null && SceneContext.Instance.Player != null)
                        {
                            var localVac = FindChildRecursive(SceneContext.Instance.Player.transform, "vacStandard");
                            if (localVac != null)
                            {
                                sb.AppendLine("\n=== LOCAL PLAYER VAC HIERARCHY ===");
                                DumpHierarchy(localVac, sb, "");
                            }
                        }
                        
                        System.IO.File.WriteAllText("E:\\Users\\jashreve\\git\\SlimeRancher2Multiplayer\\vac_hierarchy_dump.txt", sb.ToString());
                    }
                    catch (System.Exception ex)
                    {
                        System.IO.File.WriteAllText("E:\\Users\\jashreve\\git\\SlimeRancher2Multiplayer\\vac_hierarchy_dump.txt", ex.ToString());
                    }
                }

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

    private static void DumpHierarchy(Transform t, System.Text.StringBuilder sb, string indent)
    {
        if (t == null) return;
        sb.AppendLine($"{indent}GameObject: {t.name} (activeSelf={t.gameObject.activeSelf}, activeInHierarchy={t.gameObject.activeInHierarchy})");
        var comps = t.GetComponents<Component>();
        if (comps != null)
        {
            foreach (var comp in comps)
            {
                if (comp == null) continue;
                sb.AppendLine($"{indent}  Component: {comp.GetIl2CppType().FullName}");
                if (comp.GetIl2CppType().FullName == "UnityEngine.ParticleSystem")
                {
                    try
                    {
                        var ps = comp.Cast<ParticleSystem>();
                        sb.AppendLine($"{indent}    ParticleSystem: isPlaying={ps.isPlaying}, isEmitting={ps.isEmitting}, particleCount={ps.particleCount}");
                    }
                    catch {}
                }
                else if (comp.GetIl2CppType().FullName == "UnityEngine.MeshRenderer" || comp.GetIl2CppType().FullName == "UnityEngine.SkinnedMeshRenderer")
                {
                    try
                    {
                        var r = comp.Cast<Renderer>();
                        sb.AppendLine($"{indent}    Renderer: enabled={r.enabled}, sharedMaterial={r.sharedMaterial?.name}");
                    }
                    catch {}
                }
            }
        }
        for (int i = 0; i < t.childCount; i++)
        {
            DumpHierarchy(t.GetChild(i), sb, indent + "  ");
        }
    }

    private static Transform? FindChildRecursive(Transform parent, string name)
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

        var remoteVacFX = FindChildRecursive(vacStandard, "RemoteVacFX");
        if (remoteVacFX != null) return remoteVacFX.gameObject;

        if (SceneContext.Instance == null || SceneContext.Instance.Player == null) return null;
        var pic = SceneContext.Instance.Player.GetComponent<PlayerItemController>();
        if (pic == null || pic._vacuumItem == null || pic._vacuumItem.VacFX == null) return null;

        var localVacFX = pic._vacuumItem.VacFX;
        var localParentName = localVacFX.transform.parent.name;

        var targetParent = FindChildRecursive(remotePlayer.transform, localParentName) ?? vacStandard;

        var clonedFX = Object.Instantiate(localVacFX, targetParent);
        clonedFX.name = "RemoteVacFX";
        clonedFX.transform.localPosition = localVacFX.transform.localPosition;
        clonedFX.transform.localRotation = localVacFX.transform.localRotation;
        clonedFX.transform.localScale = localVacFX.transform.localScale;

        return clonedFX;
    }
}