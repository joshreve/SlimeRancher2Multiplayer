using JetBrains.Annotations;
using MelonLoader;
using Starlight.Storage;

namespace SR2MP.Components.FX;

// Modified version of PlayerFootstepFX (from a restored decomp of 'PlayerFootstepFX' qwq)
[InjectIntoIL]
internal sealed class NetworkPlayerFootstep : MonoBehaviour
{
    public Transform SpawnAtTransform;

    public GameObject FootstepFX;
    public GameObject FootstepFXInstance;
    private ParticleSystem footstepParticles;

    private bool playerGrounded;
    private bool playerInWater;

    private const float GroundCheckDistance = 0.15f;
    private const int GroundedLayer = -1728543467;

    [UsedImplicitly]
    public void Awake()
    {
        if (this == null) return;
        if (transform.childCount > 2)
            SpawnAtTransform = transform.GetChild(2);
    }

    private void TryInitializeParticles()
    {
        if (this == null) return;
        if (footstepParticles != null) return;

        if (SpawnAtTransform == null && transform.childCount > 2)
            SpawnAtTransform = transform.GetChild(2);

        if (SpawnAtTransform == null) return;

        if (FXManager == null) return;
        var fx = FXManager.FootstepFX;
        if (fx == null) return;

        FootstepFX = fx;
        FootstepFXInstance = Instantiate(FootstepFX, SpawnAtTransform.position, SpawnAtTransform.rotation);
        if (FootstepFXInstance == null) return;
        FootstepFXInstance.transform.SetParent(SpawnAtTransform.transform);
        footstepParticles = FootstepFXInstance.GetComponentInChildren<ParticleSystem>();
    }

    public void UpdateFXState()
    {
        if (this == null) return;
        TryInitializeParticles();
        if (footstepParticles == null) return;

        if (playerGrounded && !playerInWater)
            footstepParticles.Play(true);
        else
            footstepParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    [UsedImplicitly]
    public void OnTriggerEnter(Collider collider)
    {
        if (this == null) return;
        if (collider == null) return;
        if (!collider.CompareTag("Water") && collider.gameObject.layer != LayerMask.NameToLayer("Water"))
            return;

        playerInWater = true;

        TryInitializeParticles();
        if (footstepParticles != null)
            footstepParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    [UsedImplicitly]
    public void OnTriggerExit(Collider collider)
    {
        if (this == null) return;
        if (collider == null) return;
        if (!collider.CompareTag("Water") && collider.gameObject.layer != LayerMask.NameToLayer("Water"))
            return;

        playerInWater = false;

        TryInitializeParticles();
        if (playerGrounded && footstepParticles != null)
            footstepParticles.Play(true);
    }

    private bool CheckGrounded(int layer)
    {
        if (this == null) return false;
        return Physics.Raycast(transform.position, Vector3.down, GroundCheckDistance, layer);
    }

    public void Update()
    {   // Don't change it, this is the LayerMask qwq
        // "Magic number that breaks everything if you change it"
        if (this == null) return;
        var isGrounded = CheckGrounded(GroundedLayer);

        if (isGrounded == playerGrounded)
            return;

        playerGrounded = isGrounded;
        UpdateFXState();
    }
}