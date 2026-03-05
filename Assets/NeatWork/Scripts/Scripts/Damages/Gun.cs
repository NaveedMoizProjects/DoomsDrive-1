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
        if ((Input.GetKey(KeyCode.F) || Input.GetMouseButton(0)) && Time.time >= nextTimeToShoot)
        {
            nextTimeToShoot = Time.time + fireRate;
            Shoot();
        }
    }

    void Shoot()
    {
        if (muzzleFlash != null) muzzleFlash.Play();
        if (gunAudioSource != null) gunAudioSource.Play();

        // 1. Rotation Fix: 
        // Hum spawnPoint ki rotation letay hain aur usay X-axis par 90 degrees rotate kar detay hain.
        Quaternion bulletRotation = spawnPoint.rotation * Quaternion.Euler(90, 0, 0);

        // 2. Create the projectile with the NEW rotation
        GameObject projectile = Instantiate(projectilePrefab, spawnPoint.position, bulletRotation);

        // 3. Add physics force
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Force hamesha spawnPoint.forward ki taraf hi lagni chahiye
            rb.AddForce(spawnPoint.forward * launchForce, ForceMode.VelocityChange);
        }

        Destroy(projectile, 2f);
    }
}