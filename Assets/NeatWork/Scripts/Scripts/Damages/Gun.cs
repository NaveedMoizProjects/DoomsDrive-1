using UnityEngine;

public class Gun : MonoBehaviour
{
    [Header("Assignments")]
    public string bulletPoolTag = "Bullet";             // generic fallback
    public string playerBulletPoolTag = "PlayerBullet"; // pooled prefab tag for player bullets
    public string enemyBulletPoolTag = "EnemyBullet";   // pooled prefab tag for enemy bullets

    public string ownerTag = "Player";
    public Transform spawnPoint;
    public ParticleSystem muzzleFlash;
    public AudioSource gunAudioSource;

    [Header("Layer Names (create these in Unity Tags & Layers)")]
    public string playerBulletLayerName = "PlayerBullet";
    public string enemyBulletLayerName = "EnemyBullet";

    [Header("Settings")]
    public float launchForce = 200f;
    public float fireRate = 0.5f;
    private float nextTimeToShoot = 0f;

    private Rigidbody carRb; // Cached reference to the car's physics

    [Header("Overheat / Cooldown (Inspector controllable)")]
    [Tooltip("Enable automatic overheat cooldown when firing continuously.")]
    public bool enableOverheatCooldown = true;
    [Tooltip("Seconds of continuous firing required to trigger cooldown.")]
    public float overheatThresholdSeconds = 3f;
    [Tooltip("Cooldown duration (seconds) during which firing is blocked.")]
    public float cooldownDuration = 5f;
    [Tooltip("Show a brief UI message when cooldown starts/ends (MessageUIManager if available).")]
    public bool showCooldownMessage = true;

    // runtime
    private float firingHoldTimer = 0f;
    private bool inCooldown = false;
    private float cooldownTimer = 0f;

    void Start()
    {
        carRb = GetComponentInParent<Rigidbody>();
    }

    void Update()
    {
        // Update cooldown timer if active
        if (inCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                inCooldown = false;
                if (showCooldownMessage && MessageUIManager.Instance != null)
                    MessageUIManager.Instance.ProcessMessage("Gun ready", WorldTriggerMessage.MessageType.Info, 1.5f);
            }
        }

        bool firingInput = (Input.GetKey(KeyCode.F) || Input.GetMouseButton(0));

        // If cooling, block continuous-fire logic
        if (firingInput && !inCooldown)
        {
            // Track continuous hold time
            firingHoldTimer += Time.deltaTime;

            // Fire according to fireRate
            if (Time.time >= nextTimeToShoot)
            {
                nextTimeToShoot = Time.time + fireRate;
                Shoot();
            }

            // Check overheat
            if (enableOverheatCooldown && firingHoldTimer >= overheatThresholdSeconds)
            {
                StartCooldown();
            }
        }
        else
        {
            // Not firing or in cooldown -> reset hold timer
            firingHoldTimer = 0f;
        }
    }

    private void StartCooldown()
    {
        inCooldown = true;
        cooldownTimer = Mathf.Max(0f, cooldownDuration);

        if (showCooldownMessage && MessageUIManager.Instance != null)
            MessageUIManager.Instance.ProcessMessage($"Weapon overheated\n{cooldownTimer:0}s", WorldTriggerMessage.MessageType.Warning, 2f);
    }

    void Shoot()
    {
        if (inCooldown) return; // safeguard

        if (spawnPoint == null)
        {
            Debug.LogWarning("Gun: spawnPoint is null.");
            return;
        }

        if (muzzleFlash != null) muzzleFlash.Play();
        if (gunAudioSource != null) gunAudioSource.Play();

        Quaternion bulletRotation = spawnPoint.rotation * Quaternion.Euler(90, 0, 0);

        string chosenPoolTag;
        string chosenLayerName;
        if (ownerTag == "Player")
        {
            chosenPoolTag = playerBulletPoolTag;
            chosenLayerName = playerBulletLayerName;
        }
        else
        {
            chosenPoolTag = !string.IsNullOrEmpty(enemyBulletPoolTag) ? enemyBulletPoolTag : bulletPoolTag;
            chosenLayerName = enemyBulletLayerName;
        }

        // Spawn inactive so we can configure ownership/layer before physics wakes up.
        GameObject projectile = ObjectPooler.Instance.SpawnFromPool(chosenPoolTag, spawnPoint.position, bulletRotation, false);
        if (projectile == null && chosenPoolTag != bulletPoolTag)
        {
            Debug.LogWarning($"Gun: pool '{chosenPoolTag}' not found. Falling back to '{bulletPoolTag}'.");
            projectile = ObjectPooler.Instance.SpawnFromPool(bulletPoolTag, spawnPoint.position, bulletRotation, false);
        }

        if (projectile == null)
        {
            Debug.LogWarning($"Gun: SpawnFromPool returned null for '{chosenPoolTag}' and fallback.");
            return;
        }

        // set descriptive tag
        projectile.tag = (ownerTag == "Player") ? "PlayerBullet" : "EnemyBullet";

        // set layer recursively (so physics collision matrix applies)
        int layer = LayerMask.NameToLayer(chosenLayerName);
        if (layer == -1)
        {
            Debug.LogWarning($"Gun: Layer '{chosenLayerName}' not found. Set it in __Edit > Project Settings > Tags & Layers__.");
        }
        else
        {
            SetLayerRecursively(projectile, layer);
        }

        // setup hazard/ownership for pooled bullets
        UniversalHazard hazard = projectile.GetComponent<UniversalHazard>();
        if (hazard != null)
        {
            hazard.SetupIgnoreTag(ownerTag);
            hazard.preserveOwnerOnEnable = true;
            hazard.ignoreOwnerInAOE = true;
        }

        // ignore collisions with shooter colliders (extra safety)
        Collider[] projectileCols = projectile.GetComponentsInChildren<Collider>(true);
        Collider[] shooterCols = GetComponentsInParent<Collider>();
        foreach (var pCol in projectileCols)
            foreach (var sCol in shooterCols)
                if (pCol != null && sCol != null)
                    Physics.IgnoreCollision(pCol, sCol, true);

        // activate AFTER configuration
        projectile.SetActive(true);

        // set velocity AFTER activation (OnEnable may reset rb)
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.angularVelocity = Vector3.zero;
            Vector3 carVelocity = (carRb != null) ? carRb.velocity : Vector3.zero;
            rb.velocity = carVelocity + (spawnPoint.forward * launchForce);
        }
        else
        {
            Debug.LogWarning("Gun: spawned projectile missing Rigidbody.");
        }
    }

    private void SetLayerRecursively(GameObject root, int layer)
    {
        if (root == null) return;
        root.layer = layer;
        foreach (Transform t in root.transform)
            SetLayerRecursively(t.gameObject, layer);
    }
}