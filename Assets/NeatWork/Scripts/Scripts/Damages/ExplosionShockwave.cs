using UnityEngine;

public class ExplosionShockwave : MonoBehaviour
{
    private float maxRadius;
    private float currentRadius = 0f;
    private float expansionSpeed;
    private bool hasDealtDamage = false;

    // We initialize this from the Rocket
    public void Setup(float radius, float damage, float force)
    {
        maxRadius = radius;
        //expansionSpeed = radius * 5f; // Adjust for "smoothness"
        expansionSpeed = 1;

        // Apply the Damage and Force immediately (Physics logic)
        DamageGiver.SendExplosionDamage(transform.position, radius, damage, force);
    }

    void Update()
    {
        // Smoothly grow the sphere visually
        if (currentRadius < maxRadius)
        {
            currentRadius += expansionSpeed * Time.deltaTime;
            transform.localScale = Vector3.one * (currentRadius * 2f); // Scale is Diameter

            // Optional: Fade out the color as it grows
            Renderer ren = GetComponent<Renderer>();
            if (ren != null)
            {
                Color c = ren.material.color;
                c.a = Mathf.Lerp(0.4f, 0f, currentRadius / maxRadius);
                ren.material.color = c;
            }
        }
        else
        {
            Destroy(gameObject); // Delete the sphere once it reaches max size
        }
    }
}