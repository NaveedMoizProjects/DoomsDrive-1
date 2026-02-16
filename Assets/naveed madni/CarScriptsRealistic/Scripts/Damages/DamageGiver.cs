using UnityEngine;

public static class DamageGiver
{
    public static void SendBulletDamage(RaycastHit hit, float damageValue)
    {
        // Using GetComponentInParent to catch scripts on nested models
        DamageablePart part = hit.collider.GetComponentInParent<DamageablePart>();
        if (part != null)
        {
            part.TakeDamage(damageValue);
        }
    }

    public static void SendExplosionDamage(Vector3 blastPoint, float radius, float damage, float force)
    {
        // 1. Find everything in range
        Collider[] hits = Physics.OverlapSphere(blastPoint, radius);

        // Use a HashSet or List to ensure we only push the main Car Rigidbody ONCE per explosion
        // Otherwise, every wheel collider hit will apply the force again (making it glitchy)
        System.Collections.Generic.HashSet<Rigidbody> movedBodies = new System.Collections.Generic.HashSet<Rigidbody>();

        foreach (Collider col in hits)
        {
            // --- DAMAGE LOGIC ---
            DamageablePart part = col.GetComponentInParent<DamageablePart>();
            if (part != null)
            {
                float dist = Vector3.Distance(blastPoint, col.transform.position);
                float proximity = 1f - Mathf.Clamp01(dist / radius);
                part.TakeDamage(damage * proximity);
            }

            // --- PHYSICS FORCE LOGIC ---
            Rigidbody rb = col.GetComponentInParent<Rigidbody>();
            if (rb != null && !movedBodies.Contains(rb))
            {
                // Add to set so we don't double-push the same car
                movedBodies.Add(rb);

                // 2. THE JUMP FIX: Upward Modifier
                // The '3.0f' at the end pushes the "source" of the explosion downward mentally,
                // forcing the physics engine to toss the car UP.
                rb.AddExplosionForce(force, blastPoint, radius, 3.0f, ForceMode.Impulse);

                // 3. KINETIC KICK: A small extra shove if the car is heavy
                Vector3 dir = (rb.transform.position - blastPoint).normalized;
                rb.AddForce(Vector3.up * (force * 0.2f), ForceMode.Impulse);

                // 4. TORQUE: Make it spin/flip slightly
                rb.AddTorque(Random.insideUnitSphere * force, ForceMode.Impulse);
            }

            MeshDeformer deformer = col.GetComponent<MeshDeformer>();
            if (deformer != null)
            {
                // Push the metal in slightly (power = 0.1f to 0.3f)
                deformer.DeformMesh(blastPoint, radius * 0.5f, 0.2f);
            }
        }
    }
}