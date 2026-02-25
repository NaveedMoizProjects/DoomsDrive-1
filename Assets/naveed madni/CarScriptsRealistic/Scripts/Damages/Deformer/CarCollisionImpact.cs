using UnityEngine;

public class CarCollisionImpact : MonoBehaviour
{
    public float damageThreshold = 5f;
    public float damageMultiplier = 2f;
    public float impactRadius = 2.5f;

    [Header("Furnishing: Sound & Visuals")]
    public GameObject impactSparkPrefab; // Spark VFX
    public AudioSource audioSource;
    public AudioClip crunchSound; // Metal hitting metal

    void OnCollisionEnter(Collision collision)
    {
        float impactForce = collision.relativeVelocity.magnitude;

        if (impactForce > damageThreshold)
        {
            Vector3 hitPoint = collision.contacts[0].point;

            // --- FURNISHING START ---
            // Play Spark at the hit point
            if (impactSparkPrefab)
                Instantiate(impactSparkPrefab, hitPoint, Quaternion.LookRotation(collision.contacts[0].normal));

            // Play Sound
            if (audioSource && crunchSound)
                audioSource.PlayOneShot(crunchSound, impactForce / 20f); // Louder if hit harder
            // --- FURNISHING END ---

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
                    deformer.DeformMesh(hitPoint, impactRadius, 0.15f);
                }
            }
        }
    }
}