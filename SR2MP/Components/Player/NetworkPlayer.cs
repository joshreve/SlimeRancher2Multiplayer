using Il2CppMonomiPark.SlimeRancher.Map;
using Il2CppMonomiPark.SlimeRancher.Player.CharacterController;
using Il2CppMonomiPark.SlimeRancher.Player.PlayerItems;
using Il2CppMonomiPark.SlimeRancher.UI;
using Il2CppTMPro;
using JetBrains.Annotations;
using MelonLoader;
using Starlight.Utils;
using SR2MP.Client.Models;
using SR2MP.Components.FX;
using SR2MP.Components.Utils;
using SR2MP.Shared.Managers;
using Starlight.Storage;
using static Starlight.ContextShortcuts;
using static SR2MP.Shared.Utils.Timers;

namespace SR2MP.Components.Player;

[InjectIntoIL]
//[InjectIntoIL(typeof(IMapMarkerSource))]
internal partial class NetworkPlayer : MonoBehaviour
{
    private static readonly int HorizontalMovement = Animator.StringToHash("HorizontalMovement");
    private static readonly int ForwardMovement = Animator.StringToHash("ForwardMovement");
    private static readonly int Yaw = Animator.StringToHash("Yaw");
    private static readonly int AirborneState = Animator.StringToHash("AirborneState");
    private static readonly int Moving = Animator.StringToHash("Moving");
    private static readonly int HorizontalSpeed = Animator.StringToHash("HorizontalSpeed");
    private static readonly int ForwardSpeed = Animator.StringToHash("ForwardSpeed");
    private static readonly int Sprinting = Animator.StringToHash("Sprinting");

    // private MeshRenderer[] renderers;
    private Collider collider;

    public Packets.FX.PlayerFXPacket.PlayerFXType LastVacFX = Packets.FX.PlayerFXPacket.PlayerFXType.VacRunningEnd;
    public bool HasPendingVacFXUpdate = false;

    public int previousScene;
    
    public Vector3 previousPosition;
    public Vector3 nextPosition;

    public Vector2 previousRotation;
    public Vector2 nextRotation;

    private float interpolationStart;
    private float interpolationEnd;

    public TextMeshPro UsernamePanel;

    private float transformTimer = PlayerTimer;
    private float fpsTimeAccumulator;
    private int fpsFrameCount;

    private Animator animator;
    private bool hasAnimationController;

    private RemotePlayer? model;

    public Transform camera;

    public string ID { get; internal set; }

    public bool IsLocal { get; internal set; }

    private static TMP_FontAsset GetFont(string fontName) => Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(x => x.name == fontName)!;

    internal TMP_FontAsset usernameFont;
    
    public void SetUsername(string username)
    {
        username = username.Trim();

        UsernamePanel = transform.GetChild(1).GetComponent<TextMeshPro>();
        UsernamePanel.text = username;
        UsernamePanel.alignment = TextAlignmentOptions.Center;
        UsernamePanel.fontSize = 3;
        UsernamePanel.font = GetFont("Runsell Type - HemispheresCaps2 (Latin)");
        usernameFont = UsernamePanel.font;

        if (!UsernamePanel.GetComponent<TransformLookAtCamera>())
        {
            UsernamePanel.gameObject.AddComponent<TransformLookAtCamera>().TargetTransform =
                UsernamePanel.transform;
        }

        try
        {
            var fontMaterial = UsernamePanel.fontSharedMaterial;
            if (fontMaterial != null)
            {
                var customMaterial = new Material(fontMaterial);
                customMaterial.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
                customMaterial.renderQueue = 4000;
                UsernamePanel.material = customMaterial;
            }
        }
        catch (System.Exception ex)
        {
            SrLogger.LogWarning($"Failed to set ZTest Always on UsernamePanel: {ex.Message}");
        }
        
        if (!radarComponent) return;

        var nameLabel = radarComponent!._compassRadarPrefab?
            .transform.GetChild(0)
            .GetComponent<TextMeshProUGUI>();

        if (nameLabel != null)
            nameLabel.SetText(username);
    }

    [UsedImplicitly]
    public void Awake()
    {
        PlayerManager.OnPlayerGadgetUpdated += OnGadgetUpdate;
        if (transform.GetComponents<NetworkPlayer>().Length > 1)
        {
            Destroy(this);
            return;
        }

        animator = GetComponentInChildren<Animator>();

        if (animator == null)
        {
            SrLogger.LogWarning("NetworkPlayer has no Animator component!");
        }
        AwakeGadgetMode();
    }

    public void Start()
    {
        if (IsLocal)
        {
            camera = GetComponent<SRCharacterController>()._cameraController.transform;
            GetComponent<PlayerItemController>()._vacuumItem.AddComponent<NetworkPlayerSound>();
        }
        else
        {
            PlayerMarkerTransforms[ID] = new();
            if (SR2MP.Patches.Map.OnMapUIAppear.ActiveMapUI != null)
            {
                CreateMapMarker(SR2MP.Patches.Map.OnMapUIAppear.ActiveMapUI);
            }
        }

        UsernamePanel = transform.GetChild(1).GetComponent<TextMeshPro>();

        SetupRenderersAndCollision();
    }

    public void OnDestroy()
    {
        if (IsLocal) return;

        if (PlayerMarkerTransforms.TryGetValue(ID, out var marker))
        {
            if (marker.mainMarker != null)
            {
                Destroy(marker.mainMarker.gameObject);
            }
            PlayerMarkerTransforms.Remove(ID);
        }
    }


    private void SetupRenderersAndCollision()
    {
        // if (IsLocal)
        // {
        //     var modelRenderers = GetComponentsInChildren<MeshRenderer>();
        //     var cameraRenderers = camera.GetComponentsInChildren<MeshRenderer>();
        //     var allRenderers = new MeshRenderer[modelRenderers.Length + cameraRenderers.Length];

        //     modelRenderers.CopyTo(allRenderers, 0);
        //     cameraRenderers.CopyTo(allRenderers, modelRenderers.Length);

        //     renderers = allRenderers;
        // }
        // else
        // {
        //     renderers = GetComponentsInChildren<MeshRenderer>();
        // }

        collider = GetComponentInChildren<Collider>();
    }

    public void Update()
    {
        if (IsLocal)
        {
            fpsFrameCount++;
            fpsTimeAccumulator += UnityEngine.Time.unscaledDeltaTime;
            if (fpsTimeAccumulator >= 0.5f)
            {
                GlobalVariables.LocalFPS = fpsFrameCount / fpsTimeAccumulator;
                fpsFrameCount = 0;
                fpsTimeAccumulator = 0f;
            }
        }

        if (model == null)
        {
            model = PlayerManager.GetPlayer(ID) ?? PlayerManager.AddPlayer(ID);

            if (!UsernamePanel)
                return;

            UsernamePanel.gameObject.AddComponent<TransformLookAtCamera>().TargetTransform =
                UsernamePanel.transform;

            SetupMarker();
            SetUsername(model.Username);

            return;
        }

        transformTimer -= UnityEngine.Time.unscaledDeltaTime;

        if (!IsLocal)
        {
            var timer = Mathf.InverseLerp(interpolationStart, interpolationEnd, UnityEngine.Time.unscaledTime);

            var networkPosition = Vector3.LerpUnclamped(previousPosition, nextPosition, timer);
            var networkLookY = Mathf.LerpAngle(previousRotation.y, nextRotation.y, timer);
            var networkYaw = Mathf.LerpAngle(previousRotation.x, nextRotation.x, timer);

            if (Vector3.SqrMagnitude(transform.position - networkPosition) > 9f)
            {
                transform.position = networkPosition;
                transform.eulerAngles = new Vector3(0, networkYaw, 0);
                ReceivedLookY = networkLookY;
            }
            else
            {
                var blendSpeed = UnityEngine.Time.unscaledDeltaTime * 15f;

                transform.position = Vector3.Lerp(transform.position, networkPosition, blendSpeed);

                ReceivedLookY = Mathf.LerpAngle(ReceivedLookY, networkLookY, blendSpeed);

                var targetRot = Quaternion.Euler(0, networkYaw, 0);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, blendSpeed);
            }
        }

        if (HasPendingVacFXUpdate)
        {
            var vacStandard = Handlers.FX.PlayerFXHandler.FindChildRecursive(transform, "vacStandard");
            if (vacStandard != null)
            {
                HasPendingVacFXUpdate = false;
                Handlers.FX.PlayerFXHandler.ApplyVacFX(gameObject, LastVacFX);
            }
        }

        ReloadMeshTransform();

        UpdateGadgetMode();
        UpdateForceField();

        UpdateMarker();
        
        if (transformTimer >= 0f)
            return;

        transformTimer = PlayerTimer;

        if (IsLocal)
        {
            var currentSceneGroup = SystemContext.Instance?.SceneLoader?._currentSceneGroup;
            var sceneId = currentSceneGroup != null ? NetworkSceneManager.GetPersistentID(currentSceneGroup) : 1;

            RemotePlayerManager.SendPlayerUpdate(
                position: transform.position,
                rotation: transform.eulerAngles.y,
                horizontalMovement: animator.GetFloat(HorizontalMovement),
                forwardMovement: animator.GetFloat(ForwardMovement),
                yaw: animator.GetFloat(Yaw),
                airborneState: animator.GetInteger(AirborneState),
                moving: animator.GetBool(Moving),
                horizontalSpeed: animator.GetFloat(HorizontalSpeed),
                forwardSpeed: animator.GetFloat(ForwardSpeed),
                sprinting: animator.GetBool(Sprinting),
                lookY: camera.eulerAngles.x,
                sceneGroup: sceneId,
                fps: GlobalVariables.LocalFPS
            );

            if (Main.Server.IsRunning)
            {
                var sceneName = currentSceneGroup != null ? currentSceneGroup.name : "SystemCore";
                SR2MP.Server.Managers.PlayerDataManager.Instance.UpdatePlayerPosition(ID, transform.position, sceneName);
            }
        }
        else
        {
            if (!hasAnimationController)
            {
                var localAnimator = sceneContext.player?.GetComponent<Animator>();
                var playerAnimatorController = localAnimator?.runtimeAnimatorController;

                if (playerAnimatorController != null)
                {
                    hasAnimationController = true;
                    animator.runtimeAnimatorController =
                        Instantiate(playerAnimatorController);
                    animator.avatar = localAnimator.avatar;
                    SetupAnimations();
                }
            }

            previousPosition = nextPosition;
            nextPosition = model.Position;

            previousRotation = new Vector2(transform.eulerAngles.y, model.LastLookY);
            nextRotation = new Vector2(model.Rotation, model.LookY);

            interpolationStart = UnityEngine.Time.unscaledTime;
            interpolationEnd = UnityEngine.Time.unscaledTime + PlayerTimer;

            animator.SetFloat(HorizontalMovement, model.HorizontalMovement);
            animator.SetFloat(ForwardMovement, model.ForwardMovement);
            animator.SetFloat(Yaw, model.Yaw);
            animator.SetInteger(AirborneState, model.AirborneState);
            animator.SetBool(Moving, model.Moving);
            animator.SetFloat(HorizontalSpeed, model.HorizontalSpeed);
            animator.SetFloat(ForwardSpeed, model.ForwardSpeed);
            animator.SetBool(Sprinting, model.Sprinting);
        }
    }

    private void ReloadMeshTransform()
    {
        // foreach (var renderer in renderers)
        // {
        //     // This is for the getter to refresh the render position stuff qwq
        //     var bounds = renderer.bounds;
        //     var localBounds = renderer.localBounds;
        // }

        if (IsLocal)
            return;

        collider.enabled = false;
        collider.enabled = true;
    }

    [UsedImplicitly]
    public void LateUpdate() => AnimateArmY();
}