using UnityEngine;

public class CarEffectsManager : MonoBehaviour
{
    [Header("Particle Systems")]
    public ParticleSystem smokeParticles;
    public ParticleSystem fireParticles;

    [Header("Core Health Thresholds (0.0 to 1.0)")]
    [Range(0f, 1f)] public float smokeThreshold = 0.7f; // Starts smoking at 70% Core health
    [Range(0f, 1f)] public float fireThreshold = 0.3f;  // Starts fire at 30% Core health

    private DamageablePart corePart;

    void Start()
    {
        // Find all parts and identify the one labeled "Core"
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
            Debug.LogError("CarEffectsManager: No part with type 'Core' found on this car!");

        if (smokeParticles) smokeParticles.Stop();
        if (fireParticles) fireParticles.Stop();
    }

    // Called by DamageablePart.cs when damage is taken
    public void RefreshEffects()
    {
        if (corePart == null) return;

        // Since health is float (0-100), we calculate the percentage
        // We assume 100 is max health. If not, you can use a 'maxHealth' variable from corePart
        float healthPercent = corePart.health / 100f;

        HandleFumes(healthPercent);
    }

    private void HandleFumes(float percent)
    {
        // Logic: If core health is lower than threshold, play particles

        // SMOKE
        if (percent <= smokeThreshold && percent > 0)
        {
            if (!smokeParticles.isPlaying) smokeParticles.Play();

            // Optional: Make smoke thicker as health drops
            var emission = smokeParticles.emission;
            emission.rateOverTime = Mathf.Lerp(50, 10, percent);
        }
        else if (percent > smokeThreshold || percent <= 0)
        {
            if (smokeParticles.isPlaying) smokeParticles.Stop();
        }

        // FIRE
        if (percent <= fireThreshold && percent > 0)
        {
            if (!fireParticles.isPlaying) fireParticles.Play();
        }
        else
        {
            if (fireParticles.isPlaying) fireParticles.Stop();
        }
    }
}