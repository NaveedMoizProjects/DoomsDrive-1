using UnityEngine;

public class CarEffectsManager : MonoBehaviour
{
    [Header("Particle Systems")]
    public ParticleSystem smokeParticles;
    public ParticleSystem fireParticles;

    [Header("Thresholds (0.0 to 1.0)")]
    [Tooltip("Start smoking when health is below this %")]
    [Range(0f, 1f)] public float smokeThreshold = 0.7f;
    [Tooltip("Start fire when health is below this %")]
    [Range(0f, 1f)] public float fireThreshold = 0.3f;

    private DamageablePart corePart;

    void Start()
    {
        // Initial setup
        FindCorePart();

        // Ensure particles are off at the start of the race
        if (smokeParticles) smokeParticles.Stop();
        if (fireParticles) fireParticles.Stop();
    }

    void FindCorePart()
    {
        // Look through all children to find the engine/core
        DamageablePart[] allParts = GetComponentsInChildren<DamageablePart>();
        foreach (var part in allParts)
        {
            if (part.type == DamageablePart.PartType.Core)
            {
                corePart = part;
                break;
            }
        }

        if (corePart == null)
            Debug.LogWarning($"CarEffectsManager on {gameObject.name}: No 'Core' part found!");
    }

    // This is called by DamageablePart.cs whenever it takes a hit
    public void RefreshEffects()
    {
        if (corePart == null) return;

        // Calculate the current percentage based on the Core's health
        // We use 0.01f as a buffer to prevent math errors at exactly 0
        float healthPercent = Mathf.Clamp01(corePart.health / corePart.maxHealth);

        HandleFumes(healthPercent);
    }

    private void HandleFumes(float percent)
    {
        // --- SMOKE LOGIC ---
        // If health is below threshold, keep smoking (even if health is 0)
        if (percent <= smokeThreshold)
        {
            if (smokeParticles != null)
            {
                if (!smokeParticles.isPlaying) smokeParticles.Play();

                // Dynamic Intensity: Thicker smoke as health drops
                var emission = smokeParticles.emission;
                // Remaps the range [smokeThreshold to 0] into [0 to 1]
                float intensity = Mathf.InverseLerp(smokeThreshold, 0f, percent);
                emission.rateOverTime = Mathf.Lerp(10f, 60f, intensity);
            }
        }
        else
        {
            // If repaired or healthy, stop the smoke
            if (smokeParticles != null && smokeParticles.isPlaying)
                smokeParticles.Stop();
        }

        // --- FIRE LOGIC ---
        if (percent <= fireThreshold)
        {
            if (fireParticles != null && !fireParticles.isPlaying)
                fireParticles.Play();
        }
        else
        {
            if (fireParticles != null && fireParticles.isPlaying)
                fireParticles.Stop();
        }
    }
}