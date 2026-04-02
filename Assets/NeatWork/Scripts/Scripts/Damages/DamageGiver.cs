using UnityEngine;
using System.Collections.Generic;

public static class DamageGiver
{
    public static void SendBulletDamage(RaycastHit hit, float damageValue)
    {
        DamageablePart part = hit.collider.GetComponentInParent<DamageablePart>();
        if (part != null)
        {
            part.TakeDamage(damageValue, hit.point, hit.normal);
        }
    }

    // Extended AOE API: optional ownerTag so AOE can filter owner colliders
    public static void SendExplosionDamage(Vector3 blastPoint, float radius, float damage, float force, string ownerTag = "")
    {
        Collider[] hits = Physics.OverlapSphere(blastPoint, radius);
        HashSet<DynamicCarController> carsHit = new HashSet<DynamicCarController>();

        foreach (Collider col in hits)
        {
            // Skip owner's colliders if ownerTag provided
            if (!string.IsNullOrEmpty(ownerTag) && col.CompareTag(ownerTag))
                continue;

            // 1. Find the DamageablePart (Try everything: self, parent, children)
            DamageablePart part = col.GetComponent<DamageablePart>() ??
                                 col.GetComponentInParent<DamageablePart>() ??
                                 col.GetComponentInChildren<DamageablePart>();

            if (part != null)
            {
                float dist = Vector3.Distance(blastPoint, col.transform.position);
                float proximity = 1f - Mathf.Clamp01(dist / radius);

                part.TakeDamage(damage * proximity, blastPoint, (col.transform.position - blastPoint).normalized);

                DynamicCarController controller = col.GetComponentInParent<DynamicCarController>();
                if (controller != null && !carsHit.Contains(controller))
                {
                    carsHit.Add(controller);
                }
            }

            // Apply Physics Push
            Rigidbody rb = col.GetComponentInParent<Rigidbody>();
            if (rb != null) rb.AddExplosionForce(force, blastPoint, radius, 3.0f, ForceMode.Impulse);
        }
    }
}