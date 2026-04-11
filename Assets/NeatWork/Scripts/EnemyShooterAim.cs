using UnityEngine;

public class EnemyShooterAim : MonoBehaviour
{
    [Header("Target")]
    public Transform playerTarget;

    [Header("Aim Settings")]
    public float aimSpeed = 5f;
    public bool aimOnlyYAxis = true;

    [Header("Shooting")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 5f;
    public float launchForce = 50f;

    [Header("Ownership")]
    [Tooltip("Tag that the spawned bullet will ignore (owner).")]
    public string ownerTag = "Enemy";

    [Header("Layer Ownership (optional)")]
    [Tooltip("Name of the layer that owner (enemy) GameObjects use.")]
    public string ownerLayerName = "Enemy";
    [Tooltip("Name of the layer to assign to spawned enemy bullets.")]
    public string bulletLayerName = "EnemyBullet";

    [Header("Effects")]
    public ParticleSystem muzzleFlash;
    public AudioSource gunAudioSource;

    private float nextFireTime;

    void OnEnable()
    {
        if (RespawnManager.Instance != null)
            RespawnManager.Instance.OnPlayerSpawned += OnPlayerSpawned;
    }

    void OnDisable()
    {
        if (RespawnManager.Instance != null)
            RespawnManager.Instance.OnPlayerSpawned -= OnPlayerSpawned;
    }

    void Start()
    {
        LocatePlayer();
    }

    void Update()
    {
        if (playerTarget == null)
        {
            LocatePlayer();
            if (playerTarget == null) return;
        }

        AimAtPlayer();
        Shoot();
    }

    void OnPlayerSpawned(GameObject newPlayer)
    {
        if (newPlayer != null)
        {
            playerTarget = newPlayer.transform;
            Debug.Log("EnemyShooterAim: Player assigned from RespawnManager");
        }
    }

    void LocatePlayer()
    {
        // 1️⃣ Try RespawnManager
        if (RespawnManager.Instance != null && RespawnManager.Instance.currentCar != null)
        {
            playerTarget = RespawnManager.Instance.currentCar.transform;

            // Ensure event subscription
            RespawnManager.Instance.OnPlayerSpawned -= OnPlayerSpawned;
            RespawnManager.Instance.OnPlayerSpawned += OnPlayerSpawned;

            Debug.Log("EnemyShooterAim: Found player via RespawnManager");
            return;
        }

        // 2️⃣ Fallback: Find by tag
        GameObject found = GameObject.FindWithTag("Player");
        if (found != null)
        {
            playerTarget = found.transform;
            Debug.Log("EnemyShooterAim: Found player via tag");
        }
    }

    void AimAtPlayer()
    {
        Vector3 direction = playerTarget.position - transform.position;

        if (aimOnlyYAxis)
            direction.y = 0;

        Quaternion lookRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            lookRotation,
            Time.deltaTime * aimSpeed
        );
    }

    void Shoot()
    {
        if (Time.time < nextFireTime) return;

        nextFireTime = Time.time + 1f / fireRate;

        if (muzzleFlash != null) muzzleFlash.Play();
        if (gunAudioSource != null) gunAudioSource.Play();

        Quaternion bulletRotation = firePoint.rotation * Quaternion.Euler(90, 0, 0);

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, bulletRotation);

        if (bullet != null)
        {
            // set descriptive tag
            bullet.tag = "EnemyBullet";

            // assign layer to bullet and make physics ignore collisions with owner layer (if layers exist)
            int bLayer = LayerMask.NameToLayer(bulletLayerName);
            int oLayer = LayerMask.NameToLayer(ownerLayerName);
            if (bLayer != -1)
            {
                SetLayerRecursively(bullet, bLayer);

                if (oLayer != -1)
                {
                    // Ensure bullets don't collide with owners' layer
                    Physics.IgnoreLayerCollision(bLayer, oLayer, true);
                }
            }
        }

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(firePoint.forward * launchForce, ForceMode.VelocityChange);
        }

        // Make bullet ignore its shooter (ownerTag) — same approach as Player bullets
        var hazard = bullet.GetComponent<UniversalHazard>();
        if (hazard != null)
        {
            hazard.SetupIgnoreTag(ownerTag);
            hazard.preserveOwnerOnEnable = true;
            hazard.ignoreOwnerInAOE = true;
        }

        // ignore collisions between bullet colliders and this shooter's colliders (extra safety)
        Collider[] projectileCols = bullet.GetComponentsInChildren<Collider>(true);
        Collider[] shooterCols = GetComponentsInParent<Collider>();
        foreach (var pCol in projectileCols)
        {
            if (pCol == null) continue;
            foreach (var sCol in shooterCols)
            {
                if (sCol == null) continue;
                Physics.IgnoreCollision(pCol, sCol, true);
            }
        }

        Destroy(bullet, 2f);
    }

    // Helper: set layer recursively (useful for prefabs with children)
    private void SetLayerRecursively(GameObject root, int layer)
    {
        if (root == null) return;
        root.layer = layer;
        foreach (Transform t in root.transform)
            SetLayerRecursively(t.gameObject, layer);
    }
}