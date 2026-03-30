using UnityEngine;

public class Gun : MonoBehaviour
{
    [Header("Assignments")]
    public string bulletPoolTag = "Bullet";
    public string ownerTag = "Player";
    public Transform spawnPoint;
    public ParticleSystem muzzleFlash;
    public AudioSource gunAudioSource;

    [Header("Settings")]
    public float launchForce = 200f; // Updated to your preferred speed
    public float fireRate = 0.5f;
    private float nextTimeToShoot = 0f;

    private Rigidbody carRb; // Cached reference to the car's physics

    void Start()
    {
        // Find the Rigidbody of the car this gun is attached to
        carRb = GetComponentInParent<Rigidbody>();
    }

    void Update()
    {
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

        // Standard 90-degree offset for horizontal capsule/cylinder meshes
        Quaternion bulletRotation = spawnPoint.rotation * Quaternion.Euler(90, 0, 0);

        GameObject projectile = ObjectPooler.Instance.SpawnFromPool(bulletPoolTag, spawnPoint.position, bulletRotation);

        if (projectile != null)
        {
            // 1. Setup Hazard logic
            UniversalHazard hazard = projectile.GetComponent<UniversalHazard>();
            if (hazard != null) hazard.SetupIgnoreTag(ownerTag);

            // 2. Ignore collisions with the shooter
            Collider bulletCol = projectile.GetComponent<Collider>();
            Collider carCol = GetComponentInParent<Collider>();
            if (bulletCol != null && carCol != null)
            {
                Physics.IgnoreCollision(bulletCol, carCol, true);
            }

            // 3. Physics Handling
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Clear old pooling forces
                rb.angularVelocity = Vector3.zero;

                // INHERIT SPEED: If the car is moving at 50 units, 
                // and we shoot at 200, the bullet should move at 250 units.
                Vector3 carVelocity = (carRb != null) ? carRb.velocity : Vector3.zero;

                // Directly set velocity for maximum precision at high speeds
                rb.velocity = carVelocity + (spawnPoint.forward * launchForce);
            }
        }
    }
}