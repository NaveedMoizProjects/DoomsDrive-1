using UnityEngine;

public class DamageablePart : MonoBehaviour
{
    public string partName;
    public float health = 100f;
    public float maxHealth = 100f;
    public enum PartType { Wheel, Door, Body, Core, player, enemy }
    public PartType type;

    [Header("Protection Settings")]
    public float damageThreshold = 5f;
    public bool canBeDestroyed = true;

    [Header("Enemy Explosion (when PartType == enemy)")]
    [Tooltip("Enable explosion when this enemy part dies.")]
    public bool enemyExplodes = true;
    public float enemyExplodeRadius = 5f;
    public float enemyExplodeDamage = 50f;
    public float enemyExplodeForce = 800f;
    public GameObject enemyExplosionPrefab;
    public float enemyExplosionLifetime = 2f;

    [Header("Player Blood Splash VFX")]
    [Tooltip("Assign your blood splash GameObject here (e.g. a screen overlay or particle system).")]
    public GameObject bloodSplashVFX;
    public float bloodSplashDuration = 2f;

    private GameObject rootCar;
    private bool isBroken = false;
    private float bloodSplashTimer = 0f;
    private bool bloodSplashActive = false;

    void Start()
    {
        rootCar = transform.root != null ? transform.root.gameObject : null;
        if (maxHealth <= 0f) maxHealth = health;

        if (bloodSplashVFX != null)
            bloodSplashVFX.SetActive(false);

        SyncWithManager();
    }

    void Update()
    {
        if (bloodSplashActive)
        {
            bloodSplashTimer -= Time.deltaTime;
            if (bloodSplashTimer <= 0f)
            {
                bloodSplashActive = false;
                if (bloodSplashVFX != null)
                    bloodSplashVFX.SetActive(false);
            }
        }
    }

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (rootCar != null && rootCar.CompareTag("Player") && DamageManager.Instance != null && !DamageManager.Instance.allowPlayerDamage)
            return;

        if (amount < damageThreshold) return;
        if (health <= 0 || isBroken) return;

        health -= amount;
        SyncWithManager();

        if (type == PartType.player && bloodSplashVFX != null)
        {
            bloodSplashVFX.SetActive(true);
            bloodSplashTimer = bloodSplashDuration;
            bloodSplashActive = true;
        }

        if (type == PartType.Wheel)
        {
            DynamicCarController controller = GetComponentInParent<DynamicCarController>();
            if (controller != null) controller.ApplyDamageToWheel(partName, amount);
        }

        if (health <= 0 && canBeDestroyed) BreakPart();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.transform.root == transform.root) return;

        float impactForce = collision.relativeVelocity.magnitude;

        if (impactForce > damageThreshold)
        {
            TakeDamage(impactForce * 2f, collision.contacts[0].point, collision.contacts[0].normal);
        }
    }

    void BreakPart()
    {
        if (isBroken) return;
        isBroken = true;

        // Notify LevelFailManager for configured part types (it will decide)
        //LevelFailedManager.Instance?.NotifyPartDestroyed(type);

        if (type == PartType.enemy)
        {
            if (enemyExplodes)
            {
                DamageGiver.SendExplosionDamage(transform.position, enemyExplodeRadius, enemyExplodeDamage, enemyExplodeForce, "");
            }

            if (enemyExplosionPrefab != null)
            {
                var fx = Instantiate(enemyExplosionPrefab, transform.position, Quaternion.identity);
                if (enemyExplosionLifetime > 0f) Destroy(fx, enemyExplosionLifetime);
            }

            GameObject toDestroy = (rootCar != null) ? rootCar : transform.root != null ? transform.root.gameObject : gameObject;
            Destroy(toDestroy);
            return;
        }

        // Player part destroyed -> use RespawnManager logic (respawn or level-fail)
        if (type == PartType.player)
        {
            if (RespawnManager.Instance != null)
            {
                RespawnManager.Instance.OnPlayerZero();
            }
            else
            {
                // fallback: destroy root if no respawn manager
                GameObject toDestroy = (rootCar != null) ? rootCar : transform.root != null ? transform.root.gameObject : gameObject;
                Destroy(toDestroy);
            }
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