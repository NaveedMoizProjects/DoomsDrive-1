using UnityEngine;

public class Gun : MonoBehaviour
{
    [Header("Assignments")]
    public GameObject projectilePrefab; // The bullet/rocket object
    public Transform spawnPoint;         // Empty object at the end of the barrel
    public ParticleSystem muzzleFlash;
    public AudioSource gunAudioSource;

    [Header("Settings")]
    public float launchForce = 50f;     // Speed of the projectile
    public float fireRate = 0.5f;       // Seconds between shots
    private float nextTimeToShoot = 0f;

    void Update()
    {
        // Check for 'F' key or Mouse Left Click
        if ((Input.GetKeyDown(KeyCode.F) || Input.GetMouseButtonDown(0)) && Time.time >= nextTimeToShoot)
        {
            nextTimeToShoot = Time.time + fireRate;
            Shoot();
        }
    }

    void Shoot()
    {
        if (muzzleFlash != null) muzzleFlash.Play();
        if (gunAudioSource != null) gunAudioSource.Play();

        // 1. Create the projectile at the spawn point
        GameObject projectile = Instantiate(projectilePrefab, spawnPoint.position, spawnPoint.rotation);

        // 2. Add physics force to make it fly
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Use VelocityChange to ignore mass for consistent speed
            rb.AddForce(spawnPoint.forward * launchForce, ForceMode.VelocityChange);
        }

        // 3. Optional: Auto-destroy the bullet after 5 seconds so the scene stays clean
        Destroy(projectile, 5f);
    }
}