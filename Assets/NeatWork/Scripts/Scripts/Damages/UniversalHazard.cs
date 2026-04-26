using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class UniversalHazard : MonoBehaviour
{
    public enum HazardType { Bullet, Explosive, LandMine }
    public HazardType type;

    [Header("Data Source")]
    public SurfaceEffectData effectLibrary;

    [Header("Damage Settings")]
    public float directDamage = 50f;
    public float explosionRadius = 5f;
    public float explosionForce = 1000f;
    public float effectLifeTime = 2.0f;

    [Header("Ownership / Filtering")]
    [Tooltip("If true the last owner tag set via SetupIgnoreTag will be preserved through OnEnable (useful for pooling).")]
    public bool preserveOwnerOnEnable = true;
    [Tooltip("If true the owner's tag will be ignored by the AOE explosion")]
    public bool ignoreOwnerInAOE = true;

    private bool hasExploded = false;
    private string currentIgnoreTag = "";
    private Rigidbody rb;
    private Collider col;
    private Renderer rend;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        rend = GetComponent<Renderer>();
    }

    public void SetupIgnoreTag(string tagToIgnore)
    {
        currentIgnoreTag = tagToIgnore;
    }

    void OnEnable()
    {
        hasExploded = false;
        // don't wipe owner when pooling; respect preserveOwnerOnEnable
        if (!preserveOwnerOnEnable)
            currentIgnoreTag = "";

        if (rb != null)
        {
            rb.isKinematic = false;

            // FIX: For bullets at 200 speed, gravity makes them "heavy"
            // We only want gravity for Landmines or slow Explosives
            rb.useGravity = (type != HazardType.Bullet);

            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (col != null)
        {
            col.enabled = true;
            col.isTrigger = false;
        }

        if (rend != null) rend.enabled = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (type == HazardType.LandMine && !hasExploded)
        {
            // Check ownership to prevent self-detonation
            if (!string.IsNullOrEmpty(currentIgnoreTag) && other.CompareTag(currentIgnoreTag)) return;

            if (other.CompareTag("Player") /*|| other.CompareTag("AI")*/)
            {
                TriggerExplosion(transform.position, other.tag, Vector3.up);
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;

        // 1. IMPROVED OWNERSHIP CHECK
        // If we have a tag, check it. 
        if (!string.IsNullOrEmpty(currentIgnoreTag))
        {
            if (collision.collider.CompareTag(currentIgnoreTag)) return;
        }
        else
        {
            // SAFETY FALLBACK: If the Gun hasn't set the tag yet (Race Condition), 
            // and we hit something tagged "Player" or "AI" immediately, ignore it for 1 frame.
            if (collision.collider.CompareTag("Player") /*|| collision.collider.CompareTag("AI")*/) return;
        }

        // 2. Landmine Logic... (keep your existing code)
        if (type == HazardType.LandMine && collision.collider.CompareTag("Terrain"))
        {
            rb.useGravity = false;
            rb.isKinematic = true;
            col.isTrigger = true;
            return;
        }

        // 3. Impact Logic
        ContactPoint contact = collision.contacts[0];
        HandleBulletImpact(contact.point, contact.normal, collision.collider.tag, collision.collider);
    }

    void HandleBulletImpact(Vector3 point, Vector3 normal, string hitTag, Collider hitCollider)
    {
        // Owner safety: do not apply direct damage to the owner tag
        if (!string.IsNullOrEmpty(currentIgnoreTag) && hitCollider.CompareTag(currentIgnoreTag))
        {
            // just deactivate bullet quietly
            gameObject.SetActive(false);
            return;
        }

        hasExploded = true;
        SpawnEffect(point, normal, hitTag);

        DamageablePart part = hitCollider.GetComponent<DamageablePart>();
        if (part != null) part.TakeDamage(directDamage, point, normal);

        gameObject.SetActive(false);
    }

    void TriggerExplosion(Vector3 point, string tag, Vector3 normal)
    {
        hasExploded = true;
        SpawnEffect(point, normal, tag);

        // Explosion logic (damage and push)
        GameObject explosionLogic = new GameObject("ExplosionLogic");
        explosionLogic.transform.position = point;
        var shock = explosionLogic.AddComponent<ExplosionShockwave>();
        // Pass owner tag so AOE can optionally ignore it
        string ownerForAOE = ignoreOwnerInAOE ? currentIgnoreTag : "";
        shock.Setup(explosionRadius, directDamage, explosionForce, ownerForAOE);

        if (type == HazardType.LandMine)
        {
            if (col != null) col.enabled = false;
            if (rend != null) rend.enabled = false;
            StartCoroutine(ReturnToPool(gameObject, 0.1f));
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    void SpawnEffect(Vector3 point, Vector3 normal, string tag)
    {
        if (effectLibrary == null) return;
        string poolTag = effectLibrary.GetPoolTagForSurface(tag);
        GameObject fx = ObjectPooler.Instance.SpawnFromPool(poolTag, point, Quaternion.LookRotation(normal));
        if (fx != null) StartCoroutine(ReturnToPool(fx, effectLifeTime));
    }

    IEnumerator ReturnToPool(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        obj.SetActive(false);
    }
}