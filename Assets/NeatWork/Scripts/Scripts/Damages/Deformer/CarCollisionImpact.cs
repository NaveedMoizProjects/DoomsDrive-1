using System.Collections.Generic;
using UnityEngine;

public class CarCollisionImpact : MonoBehaviour
{
    public float damageThreshold = 5f;
    public float damageMultiplier = 2f;
    public float impactRadius = 2.5f;

    [Header("Optimization")]
    public float impactCooldown = 0.15f;
    private float lastImpactTime;

    [Header("Data Source (Unified)")]
    public SurfaceEffectData effectLibrary; // Use the same asset as UniversalHazard

    [Header("Sound")]
    public AudioSource audioSource;
    public AudioClip crunchSound;

    [Header("Ignore collisions from these layers (e.g. PlayerBullet / EnemyBullet)")]
    public LayerMask ignoreCollisionLayers = 0;

    void OnCollisionEnter(Collision collision)
    {
        // Ignore collisions from projectile layers to avoid bullets triggering large collision damage
        if (((1 << collision.gameObject.layer) & ignoreCollisionLayers.value) != 0)
            return;

        if (Time.time < lastImpactTime + impactCooldown) return;

        float impactForce = collision.relativeVelocity.magnitude;

        if (impactForce > damageThreshold)
        {
            lastImpactTime = Time.time;
            ContactPoint contact = collision.contacts[0];
            Vector3 hitPoint = contact.point;
            string hitTag = collision.collider.tag;

            // --- ALIGNED VISUALS (Pooling) ---
            if (effectLibrary != null)
            {
                string poolTag = effectLibrary.GetPoolTagForSurface(hitTag);
                GameObject fx = ObjectPooler.Instance.SpawnFromPool(poolTag, hitPoint, Quaternion.LookRotation(contact.normal));

                // Return to pool after 2 seconds (matching your hazard logic)
                if (fx != null) StartCoroutine(DisableAfterDelay(fx, 2f));
            }

            // --- SOUND ---
            if (audioSource && crunchSound)
            {
                audioSource.pitch = Random.Range(0.85f, 1.15f);
                audioSource.PlayOneShot(crunchSound, Mathf.Clamp01(impactForce / 25f));
            }

            // --- DAMAGE DISTRIBUTION ---
            Collider[] hitColliders = Physics.OverlapSphere(hitPoint, impactRadius);
            List<DamageablePart> processedParts = new List<DamageablePart>();

            foreach (var col in hitColliders)
            {
                DamageablePart part = col.GetComponent<DamageablePart>();
                if (part != null && !processedParts.Contains(part))
                {
                    float finalDamage = impactForce * damageMultiplier;
                    part.TakeDamage(finalDamage, hitPoint, contact.normal);
                    processedParts.Add(part);
                }
            }
        }
    }

    private System.Collections.IEnumerator DisableAfterDelay(GameObject obj, float t)
    {
        yield return new WaitForSeconds(t);
        obj.SetActive(false);
    }
}