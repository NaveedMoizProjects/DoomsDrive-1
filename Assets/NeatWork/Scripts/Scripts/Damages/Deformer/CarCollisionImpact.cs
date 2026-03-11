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
        // 1. Check if enough time has passed since the last big hit
        if (Time.time < lastImpactTime + impactCooldown) return;

        float impactForce = collision.relativeVelocity.magnitude;

        if (impactForce > damageThreshold)
        {
            lastImpactTime = Time.time; // Reset cooldown

            ContactPoint contact = collision.contacts[0];
            Vector3 hitPoint = contact.point;
            Vector3 punchDir = -contact.normal;

            // --- FURNISHING ---
            if (impactSparkPrefab)
                Instantiate(impactSparkPrefab, hitPoint, Quaternion.LookRotation(contact.normal));

            if (audioSource && crunchSound)
            {
                // 2. Randomize pitch slightly so it sounds more "natural"
                audioSource.pitch = Random.Range(0.85f, 1.15f);
                audioSource.PlayOneShot(crunchSound, Mathf.Clamp01(impactForce / 25f));
            }

            // --- DEFORMATION & DAMAGE ---
            Collider[] hitColliders = Physics.OverlapSphere(hitPoint, impactRadius);
            foreach (var col in hitColliders)
            {
                DamageablePart part = col.GetComponentInParent<DamageablePart>();
                if (part != null)
                {
                    float finalDamage = impactForce * damageMultiplier;
                    part.TakeDamage(finalDamage);
                }

                MeshDeformer deformer = col.GetComponent<MeshDeformer>();
                if (deformer != null)
                {
                    float power = Mathf.Min(0.3f, impactForce * 0.01f);
                    deformer.DeformMesh(hitPoint, impactRadius, power, punchDir);
                }
            }
        }
    }
}