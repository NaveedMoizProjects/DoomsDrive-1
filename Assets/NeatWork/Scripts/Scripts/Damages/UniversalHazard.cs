using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class UniversalHazard : MonoBehaviour
{
    public enum HazardType { Bullet, Explosive, LandMine }
    public HazardType type;

    [Header("Damage Settings")]
    public float directDamage = 50f;
    public float explosionRadius = 5f;
    public float explosionForce = 1000f;
    public float delay = 0.0f;
    public GameObject explosionEffect; // Assign in Inspector

    void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();

        if (type == HazardType.LandMine)
        {
            // 1. Let it fall initially
            rb.useGravity = true;
            rb.isKinematic = false;

            // 2. Start as a physical object so it hits the floor
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = false;
        }
    }

    // --- TRIGGER LOGIC (For LandMines) ---
    private void OnTriggerEnter(Collider other)
    {
        if (type == HazardType.LandMine)
        {
            if (other.CompareTag("Player") || other.CompareTag("AI"))
            {
                // FIX: Triggers don't have 'contacts'. Use the mine's position.
                Vector3 impactPoint = transform.position;

                Explode(impactPoint);
                StartCoroutine(EndAfterDelay(other.gameObject));

                // Disable visual and collider immediately so it doesn't double-trigger
                GetComponent<Collider>().enabled = false;
                if (GetComponent<Renderer>()) GetComponent<Renderer>().enabled = false;
            }
        }
    }

    // --- COLLISION LOGIC (For Bullets & Rockets) ---
    void OnCollisionEnter(Collision collision)
    {
        if (type == HazardType.LandMine && collision.collider.CompareTag("Terrain"))
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.useGravity = false; // Stop pulling down
            rb.isKinematic = true; // Lock it in place

            // Switch to Trigger mode so the car can "overlap" it to explode
            GetComponent<Collider>().isTrigger = true;

            return; // Don't explode yet!
        }

        Vector3 impactPoint = collision.contacts[0].point;

        if (type == HazardType.Bullet)
        {
            DamageablePart part = collision.collider.GetComponent<DamageablePart>();
            if (part != null) 
                part.TakeDamage(directDamage, collision.contacts[0].point, collision.contacts[0].normal);
        }
        else if (type == HazardType.Explosive)
        {
            Explode(impactPoint);
        }

        // Bullets and Explosives destroy themselves on impact
        if (type != HazardType.LandMine)
        {
            Destroy(gameObject);
        }
    }

    void Explode(Vector3 impactPoint)
    {
        if (explosionEffect != null)
            Instantiate(explosionEffect, impactPoint, Quaternion.identity);

        // Create the Visual Sphere Shockwave
        GameObject waveObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        waveObj.transform.position = impactPoint;
        waveObj.transform.localScale = Vector3.zero;

        Destroy(waveObj.GetComponent<Collider>());

        Renderer ren = waveObj.GetComponent<Renderer>();
        ren.material = new Material(Shader.Find("Transparent/Diffuse"));
        ren.material.color = new Color(1, 0, 0, 0.4f);

        // Attach Logic
        ExplosionShockwave wave = waveObj.AddComponent<ExplosionShockwave>();
        wave.Setup(explosionRadius, directDamage, explosionForce);
    }

    IEnumerator EndAfterDelay(GameObject car)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log($"<color=red><b>CAR DESTROYED BY {type}!</b></color>");

        // If it's a LandMine, we destroy the mine object after the delay
        if (type == HazardType.LandMine) Destroy(gameObject);
    }
}