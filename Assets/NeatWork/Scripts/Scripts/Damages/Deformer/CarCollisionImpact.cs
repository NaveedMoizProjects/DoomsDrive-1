using System.Collections.Generic;
using UnityEngine;

public class CarCollisionImpact : MonoBehaviour
{
    public float damageThreshold = 5f;
    public float damageMultiplier = 2f;
    public float impactRadius = 2.5f;

    [Header("Optimization")]
    public float impactCooldown = 0.15f; // Stops the "machine gun" sound effect
    private float lastImpactTime;

    [Header("Furnishing: Sound & Visuals")]
    public GameObject impactSparkPrefab;
    public AudioSource audioSource;
    public AudioClip crunchSound;

    void OnCollisionEnter(Collision collision)
    {
        if (Time.time < lastImpactTime + impactCooldown) return;

        float impactForce = collision.relativeVelocity.magnitude;

        if (impactForce > damageThreshold)
        {
            lastImpactTime = Time.time;

            ContactPoint contact = collision.contacts[0];
            Vector3 hitPoint = contact.point;
            Vector3 punchDir = -contact.normal; // Direction the dent goes IN

            // --- VISUALS & SOUND ---
            if (impactSparkPrefab)
                Instantiate(impactSparkPrefab, hitPoint, Quaternion.LookRotation(contact.normal));

            if (audioSource && crunchSound)
            {
                audioSource.pitch = Random.Range(0.85f, 1.15f);
                audioSource.PlayOneShot(crunchSound, Mathf.Clamp01(impactForce / 25f));
            }

            // --- DAMAGE DISTRIBUTION ---
            // Find all parts near the impact zone
            Collider[] hitColliders = Physics.OverlapSphere(hitPoint, impactRadius);

            // We use a List to make sure we don't damage the same part twice in one frame
            List<DamageablePart> processedParts = new List<DamageablePart>();

            foreach (var col in hitColliders)
            {
                DamageablePart part = col.GetComponent<DamageablePart>();

                if (part != null && !processedParts.Contains(part))
                {
                    float finalDamage = impactForce * damageMultiplier;

                    // --- THE LINK ---
                    // We pass the hitPoint and normal so the part can dent itself!
                    part.TakeDamage(finalDamage, hitPoint, contact.normal);

                    processedParts.Add(part);
                }
            }
        }
    }
}