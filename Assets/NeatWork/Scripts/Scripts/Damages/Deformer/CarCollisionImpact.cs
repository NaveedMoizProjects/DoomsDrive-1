using UnityEngine;

public class CarCollisionImpact : MonoBehaviour
{
    public float damageThreshold = 5f;
    public float damageMultiplier = 2f;
    public float impactRadius = 2.5f;

    [Header("Furnishing: Sound & Visuals")]
    public GameObject impactSparkPrefab;
    public AudioSource audioSource;
    public AudioClip crunchSound;

    void OnCollisionEnter(Collision collision)
    {
        float impactForce = collision.relativeVelocity.magnitude;

        if (impactForce > damageThreshold)
        {
            // The exact point of contact
            ContactPoint contact = collision.contacts[0];
            Vector3 hitPoint = contact.point;

            // The direction of the hit (Normal)
            // We use -contact.normal to push the metal "in" towards the car's body
            Vector3 punchDir = -contact.normal;

            // --- FURNISHING ---
            if (impactSparkPrefab)
                Instantiate(impactSparkPrefab, hitPoint, Quaternion.LookRotation(contact.normal));

            if (audioSource && crunchSound)
                audioSource.PlayOneShot(crunchSound, Mathf.Clamp01(impactForce / 20f));

            // --- DEFORMATION & DAMAGE ---
            Collider[] hitColliders = Physics.OverlapSphere(hitPoint, impactRadius);
            foreach (var col in hitColliders)
            {
                // Damage Logic
                DamageablePart part = col.GetComponentInParent<DamageablePart>();
                if (part != null)
                {
                    float finalDamage = impactForce * damageMultiplier;
                    part.TakeDamage(finalDamage);
                }

                // Mesh Deformation Logic
                MeshDeformer deformer = col.GetComponent<MeshDeformer>();
                if (deformer != null)
                {
                    // Scale power by impact force so faster hits = bigger dents
                    float power = Mathf.Min(0.3f, impactForce * 0.01f);

                    // FIXED: Added the punchDir (Normal) argument
                    deformer.DeformMesh(hitPoint, impactRadius, power, punchDir);
                }
            }
        }
    }
}