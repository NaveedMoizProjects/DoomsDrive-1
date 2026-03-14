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

    // Called by the Gun right after spawning
    public void SetupIgnoreTag(string tagToIgnore)
    {
        currentIgnoreTag = tagToIgnore;
    }

    void OnEnable()
    {
        hasExploded = false;
        currentIgnoreTag = ""; // Reset tag so it doesn't carry over in the pool

        if (col != null)
        {
            col.enabled = true;
            col.isTrigger = false;
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (col != null)
        {
            col.enabled = true;
            col.isTrigger = false; // Reset to physical collision for the "Landing" hit
        }

        if (rend != null) rend.enabled = true;
    }

    // --- LANDMINE PROXIMITY DETECTION ---
    private void OnTriggerEnter(Collider other)
    {
        if (type == HazardType.LandMine && !hasExploded)
        {
            // Ignore the shooter even in trigger mode
            if (!string.IsNullOrEmpty(currentIgnoreTag) && other.CompareTag(currentIgnoreTag)) return;

            if (other.CompareTag("Player") || other.CompareTag("AI"))
            {
                TriggerExplosion(transform.position, other.tag, Vector3.up);
            }
        }
    }

    // --- IMPACT LOGIC ---
    void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;

        // 1. Dynamic Ignore Check (Prevents shooting yourself)
        if (!string.IsNullOrEmpty(currentIgnoreTag) && collision.collider.CompareTag(currentIgnoreTag))
        {
            return;
        }

        // 2. Landmine "Stick to Terrain" Logic
        if (type == HazardType.LandMine && collision.collider.CompareTag("Terrain"))
        {
            rb.useGravity = false;
            rb.isKinematic = true;
            col.isTrigger = true; // Switch to proximity trigger mode
            return;
        }

        // 3. Standard Impact Logic
        Vector3 point = collision.contacts[0].point;
        Vector3 normal = collision.contacts[0].normal;
        string hitTag = collision.collider.tag;

        if (type == HazardType.Bullet)
        {
            hasExploded = true;
            SpawnEffect(point, normal, hitTag);

            DamageablePart part = collision.collider.GetComponent<DamageablePart>();
            if (part != null) part.TakeDamage(directDamage, point, normal);

            gameObject.SetActive(false); // Return to pool
        }
        else if (type == HazardType.Explosive)
        {
            TriggerExplosion(point, hitTag, normal);
        }
    }

    void TriggerExplosion(Vector3 point, string tag, Vector3 normal)
    {
        hasExploded = true;
        SpawnEffect(point, normal, tag);

        // Create invisible logic object for the explosion shockwave
        GameObject explosionLogic = new GameObject("ExplosionLogic");
        explosionLogic.transform.position = point;
        explosionLogic.AddComponent<ExplosionShockwave>().Setup(explosionRadius, directDamage, explosionForce);

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

        // Fetch pool tag from Surface Settings
        string poolTag = effectLibrary.GetPoolTagForSurface(tag);

        // Request from Object Pooler
        GameObject fx = ObjectPooler.Instance.SpawnFromPool(poolTag, point, Quaternion.LookRotation(normal));

        if (fx != null) StartCoroutine(ReturnToPool(fx, effectLifeTime));
    }

    IEnumerator ReturnToPool(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        obj.SetActive(false);
    }
}