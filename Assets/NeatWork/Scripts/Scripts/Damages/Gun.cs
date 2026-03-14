using UnityEngine;

public class Gun : MonoBehaviour
{
    [Header("Assignments")]
    public string bulletPoolTag = "Bullet"; // Matches the tag in ObjectPooler
    public string ownerTag = "Player";    // Tag to ignore (e.g., "Player" or "AI")
    public Transform spawnPoint;
    public ParticleSystem muzzleFlash;
    public AudioSource gunAudioSource;

    [Header("Settings")]
    public float launchForce = 50f;
    public float fireRate = 0.5f;
    private float nextTimeToShoot = 0f;

    void Update()
    {
        // Support for both Keyboard and Mouse
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

        // 90-degree offset often needed for cylinder/capsule bullet meshes
        Quaternion bulletRotation = spawnPoint.rotation * Quaternion.Euler(90, 0, 0);

        // Pull from Pool
        GameObject projectile = ObjectPooler.Instance.SpawnFromPool(bulletPoolTag, spawnPoint.position, bulletRotation);

        if (projectile != null)
        {
            // --- THE LINK: Tell the bullet to ignore the shooter ---
            UniversalHazard hazard = projectile.GetComponent<UniversalHazard>();
            if (hazard != null)
            {
                hazard.SetupIgnoreTag(ownerTag);
            }

            Collider bulletCol = projectile.GetComponent<Collider>();
            Collider carCol = GetComponentInParent<Collider>(); // Assumes Gun is on a car

            if (bulletCol != null && carCol != null)
            {
                Physics.IgnoreCollision(bulletCol, carCol, true);
            }

            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Reset velocity for pooling accuracy
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.AddForce(spawnPoint.forward * launchForce, ForceMode.VelocityChange);
            }
        }
    }
}