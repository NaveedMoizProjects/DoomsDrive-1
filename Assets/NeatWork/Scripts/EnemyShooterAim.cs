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

    void Update()
    {
        if (playerTarget == null) return;

        AimAtPlayer();
        Shoot();
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

        // Same rotation fix as player gun
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