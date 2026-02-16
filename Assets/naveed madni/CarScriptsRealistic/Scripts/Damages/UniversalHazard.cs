using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class UniversalHazard : MonoBehaviour
{
    public enum HazardType { Bullet, Explosive }
    public HazardType type;

    [Header("Damage Settings")]
    public float directDamage = 50f;
    public float explosionRadius = 5f;
    public float explosionForce = 1000f;

    void OnCollisionEnter(Collision collision)
    {
        Vector3 impactPoint = collision.contacts[0].point;

        if (type == HazardType.Bullet)
        {
            DamageablePart part = collision.collider.GetComponent<DamageablePart>();
            if (part != null) part.TakeDamage(directDamage);
        }
        else if (type == HazardType.Explosive)
        {
            // 1. Create the Visual Sphere (The "Smooth" part)
            GameObject waveObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            waveObj.transform.position = impactPoint;
            waveObj.transform.localScale = Vector3.zero; // Start at zero

            // Remove collider so the wave doesn't push the car physically
            Destroy(waveObj.GetComponent<Collider>());

            // Set Material to Transparent Red
            Renderer ren = waveObj.GetComponent<Renderer>();
            ren.material = new Material(Shader.Find("Transparent/Diffuse"));
            ren.material.color = new Color(1, 0, 0, 0.4f);

            // 2. Attach and Setup the Logic
            ExplosionShockwave wave = waveObj.AddComponent<ExplosionShockwave>();
            wave.Setup(explosionRadius, directDamage, explosionForce);
        }

        // Now the Rocket deletes itself, but the Wave stays behind to grow!
        Destroy(gameObject);
    }

    void CreateExplosionVisual(Vector3 point)
    {
        // Create a simple Unity Sphere
        GameObject visualSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visualSphere.transform.position = point;

        // Scale it to match your physics radius (Sphere radius is 0.5, so diameter is 1)
        // We multiply by 2 to make the mesh match the physics units
        visualSphere.transform.localScale = Vector3.one * (explosionRadius * 2f);

        // Remove the collider so it doesn't mess with the car
        Destroy(visualSphere.GetComponent<Collider>());

        // Make it look like a "Ghost" sphere
        Renderer ren = visualSphere.GetComponent<Renderer>();
        ren.material = new Material(Shader.Find("Transparent/Diffuse"));
        ren.material.color = new Color(1, 0, 0, 0.3f); // Semi-transparent Red

        // Destroy the visual after half a second
        Destroy(visualSphere, 0.5f);
    }
}