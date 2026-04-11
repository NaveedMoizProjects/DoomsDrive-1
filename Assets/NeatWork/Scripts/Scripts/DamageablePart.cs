using UnityEngine;
public class DamageablePart : MonoBehaviour
{
    public string partName;
    public float health = 100f;
    public float maxHealth = 100f;
    public enum PartType { Wheel, Door, Body, Core, player, enemy }
    public PartType type;

    [Header("Protection Settings")]
    public float damageThreshold = 5f; // Hits weaker than this are ignored
    public bool canBeDestroyed = true;

    [Header("Enemy Explosion (when PartType == enemy)")]
    [Tooltip("Enable explosion when this enemy part dies.")]
    public bool enemyExplodes = true;
    public float enemyExplodeRadius = 5f;
    public float enemyExplodeDamage = 50f;
    public float enemyExplodeForce = 800f;
    public GameObject enemyExplosionPrefab;
    public float enemyExplosionLifetime = 2f;

    private GameObject rootCar;
    private bool isBroken = false;

    void Start()
    {
        rootCar = transform.root != null ? transform.root.gameObject : null;
        if (maxHealth <= 0f) maxHealth = health;
        SyncWithManager();
    }

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        // 1. Check if damage is enabled for the player
        if (rootCar != null && rootCar.CompareTag("Player") && DamageManager.Instance != null && !DamageManager.Instance.allowPlayerDamage)
            return;

        // 2. NEW: Ignore "Micro-Damage" (Prevents braking/bumping from breaking wheels)
        if (amount < damageThreshold) return;

        if (health <= 0 || isBroken) return;

        health -= amount;
        SyncWithManager();

        if (type == PartType.Wheel)
        {
            DynamicCarController controller = GetComponentInParent<DynamicCarController>();
            // Only apply damage if it's actually a significant hit
            if (controller != null) controller.ApplyDamageToWheel(partName, amount);
        }

        if (health <= 0 && canBeDestroyed) BreakPart();
    }

    // --- Add this to catch Physics Collisions automatically ---
    private void OnCollisionEnter(Collision collision)
    {
        // Ignore if we hit our own car body
        if (collision.gameObject.transform.root == transform.root) return;

        // Calculate damage based on impact velocity (Mass * Speed)
        float impactForce = collision.relativeVelocity.magnitude;

        // Only take damage if we hit something hard
        if (impactForce > damageThreshold)
        {
            TakeDamage(impactForce * 2f, collision.contacts[0].point, collision.contacts[0].normal);
        }
    }

    void BreakPart()
    {
        if (isBroken) return;
        isBroken = true;

        // If this is an enemy part, do an explosion and destroy the enemy
        if (type == PartType.enemy)
        {
            // Explosion AOE damage + physics impulse (uses DamageGiver helper)
            if (enemyExplodes)
            {
                DamageGiver.SendExplosionDamage(transform.position, enemyExplodeRadius, enemyExplodeDamage, enemyExplodeForce, "");
            }

            // Optional visual effect
            if (enemyExplosionPrefab != null)
            {
                var fx = Instantiate(enemyExplosionPrefab, transform.position, Quaternion.identity);
                if (enemyExplosionLifetime > 0f) Destroy(fx, enemyExplosionLifetime);
            }

            // Destroy the enemy root gameobject (use rootCar when available)
            GameObject toDestroy = (rootCar != null) ? rootCar : transform.root != null ? transform.root.gameObject : gameObject;
            Destroy(toDestroy);
            return;
        }

        if (type == PartType.Core)
        {
            if (RespawnManager.Instance != null)
                RespawnManager.Instance.TriggerDeath("ENGINE TOTALED");
            return;
        }

        if (type == PartType.Wheel)
        {
            DynamicCarController controller = GetComponentInParent<DynamicCarController>();
            if (controller != null)
            {
                controller.DisconnectWheel(partName);

                // SHOW PROMPT instead of immediate destroy
                if (RespawnManager.Instance != null)
                {
                    RespawnManager.Instance.PromptRespawn("CRITICAL WHEEL LOSS");
                }
            }
            this.enabled = false;
            return;
        }

        transform.SetParent(null);
        if (!GetComponent<Rigidbody>()) gameObject.AddComponent<Rigidbody>();
        this.enabled = false;
    }

    void SyncWithManager()
    {
        if (DamageManager.Instance != null)
        {
            rootCar = transform.root != null ? transform.root.gameObject : rootCar;
            DamageManager.Instance.UpdateHealth(gameObject.GetInstanceID(), Mathf.Max(0, health), type, rootCar);

            if (type == PartType.Core && rootCar != null)
            {
                CarEffectsManager effects = rootCar.GetComponent<CarEffectsManager>();
                if (effects != null) effects.RefreshEffects();
            }
        }
    }
}