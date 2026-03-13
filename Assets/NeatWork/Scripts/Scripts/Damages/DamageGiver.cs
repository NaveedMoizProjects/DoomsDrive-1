using UnityEngine;
using System.Collections.Generic;

public static class DamageGiver
{
    public static void SendBulletDamage(RaycastHit hit, float damageValue)
    {
        DamageablePart part = hit.collider.GetComponentInParent<DamageablePart>();
        if (part != null)
        {
            // NEW: Pass the hit info so the part handles its own UI and MeshDeformer
            part.TakeDamage(damageValue, hit.point, hit.normal);
        }
    }

    public static void SendExplosionDamage(Vector3 blastPoint, float radius, float damage, float force)
    {
        Collider[] hits = Physics.OverlapSphere(blastPoint, radius);
        HashSet<DynamicCarController> carsHit = new HashSet<DynamicCarController>();

        foreach (Collider col in hits)
        {
            // 1. Find the DamageablePart (Try everything: self, parent, children)
            DamageablePart part = col.GetComponent<DamageablePart>() ??
                                 col.GetComponentInParent<DamageablePart>() ??
                                 col.GetComponentInChildren<DamageablePart>();

            if (part != null)
            {
                float dist = Vector3.Distance(blastPoint, col.transform.position);
                float proximity = 1f - Mathf.Clamp01(dist / radius);

                // Pass damage to the part
                part.TakeDamage(damage * proximity, blastPoint, (col.transform.position - blastPoint).normalized);

                // 2. FORCE the Controller to check health immediately
                DynamicCarController controller = col.GetComponentInParent<DynamicCarController>();
                if (controller != null && !carsHit.Contains(controller))
                {
                    carsHit.Add(controller);
                    // We don't even need to wait for FixedUpdate, 
                    // the TakeDamage above will trigger the internal health drop.
                }
            }

            // Apply Physics Push
            Rigidbody rb = col.GetComponentInParent<Rigidbody>();
            if (rb != null) rb.AddExplosionForce(force, blastPoint, radius, 3.0f, ForceMode.Impulse);
        }
    }
}