using UnityEngine;

public static class DamageGiver
{
    public static void SendBulletDamage(RaycastHit hit, float damageValue)
    {
        DamageablePart part = hit.collider.GetComponentInParent<DamageablePart>();
        if (part != null)
        {
            part.TakeDamage(damageValue);
        }

        // Bullets should dent too! 
        // We use -hit.normal to push the metal "into" the object
        MeshDeformer deformer = hit.collider.GetComponent<MeshDeformer>();
        if (deformer != null)
        {
            deformer.DeformMesh(hit.point, 0.2f, 0.05f, -hit.normal);
        }
    }

    public static void SendExplosionDamage(Vector3 blastPoint, float radius, float damage, float force)
    {
        Collider[] hits = Physics.OverlapSphere(blastPoint, radius);
        System.Collections.Generic.HashSet<Rigidbody> movedBodies = new System.Collections.Generic.HashSet<Rigidbody>();

        foreach (Collider col in hits)
        {
            float dist = Vector3.Distance(blastPoint, col.transform.position);
            float proximity = 1f - Mathf.Clamp01(dist / radius);

            // --- DAMAGE LOGIC ---
            DamageablePart part = col.GetComponentInParent<DamageablePart>();
            if (part != null)
            {
                part.TakeDamage(damage * proximity);
            }

            // --- PHYSICS FORCE LOGIC ---
            Rigidbody rb = col.GetComponentInParent<Rigidbody>();
            if (rb != null && !movedBodies.Contains(rb))
            {
                movedBodies.Add(rb);
                rb.AddExplosionForce(force, blastPoint, radius, 3.0f, ForceMode.Impulse);
                rb.AddForce(Vector3.up * (force * 0.2f), ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * force, ForceMode.Impulse);
            }

            // --- MESH DEFORMATION LOGIC (The Accuracy Fix) ---
            MeshDeformer deformer = col.GetComponent<MeshDeformer>();
            if (deformer != null)
            {
                // FIX: Direction is from blast to the object, NOT world zero
                Vector3 punchDir = (col.transform.position - blastPoint).normalized;

                // We scale the power by proximity so things further away dent less
                float scaledPower = 0.25f * proximity;

                // Use a smaller radius for the mesh dent than the physics blast 
                // to keep it looking sharp and not 'melted'
                deformer.DeformMesh(blastPoint, radius * 0.6f, scaledPower, punchDir);
            }
        }
    }
}