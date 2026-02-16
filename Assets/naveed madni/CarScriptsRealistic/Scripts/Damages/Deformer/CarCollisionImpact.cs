using UnityEngine;

public class CarCollisionImpact : MonoBehaviour
{
    public float damageThreshold = 5f; // Minimum speed/force to cause damage
    public float damageMultiplier = 2f; // How much damage to deal based on impact
    public float impactRadius = 2.5f; // How many parts get "dented" nearby

    void OnCollisionEnter(Collision collision)
    {
        // 1. Calculate how hard the hit was
        float impactForce = collision.relativeVelocity.magnitude;

        if (impactForce > damageThreshold)
        {
            // 2. Find the point of impact
            Vector3 hitPoint = collision.contacts[0].point;

            // 3. Find all DamageableParts near the hit
            // This works for Walls, Other Cars, or anything with a collider
            Collider[] hitColliders = Physics.OverlapSphere(hitPoint, impactRadius);

            foreach (var col in hitColliders)
            {
                DamageablePart part = col.GetComponentInParent<DamageablePart>();
                if (part != null)
                {
                    // Apply damage based on how hard we hit
                    float finalDamage = impactForce * damageMultiplier;
                    part.TakeDamage(finalDamage);
                }

                // 4. Trigger Deformation (Visual dents)
                MeshDeformer deformer = col.GetComponent<MeshDeformer>();
                if (deformer != null)
                {
                    // Dent the metal exactly where it hit the wall
                    deformer.DeformMesh(hitPoint, impactRadius, 0.15f);
                }
            }
        }
    }
}