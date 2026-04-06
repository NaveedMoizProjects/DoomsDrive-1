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

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(firePoint.forward * launchForce, ForceMode.VelocityChange);
        }

        Destroy(bullet, 2f);
    }
}