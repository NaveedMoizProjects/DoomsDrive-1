using UnityEngine;

public class ExplosionShockwave : MonoBehaviour
{
    private float maxRadius;
    private float currentRadius = 0f;
    private float expansionSpeed;

    private string ownerTag = "";

    // Backwards-compatible Setup overloads
    public void Setup(float radius, float damage, float force)
    {
        Setup(radius, damage, force, "");
    }

    // New: accept ownerTag so AOE can ignore owner
    public void Setup(float radius, float damage, float force, string ownerToIgnore)
    {
        maxRadius = radius;
        expansionSpeed = radius * 5f;
        ownerTag = ownerToIgnore;

        // Apply the Damage and Force immediately using your helper (owner-aware)
        DamageGiver.SendExplosionDamage(transform.position, radius, damage, force, ownerTag);
    }

    void Update()
    {
        if (currentRadius < maxRadius)
        {
            currentRadius += expansionSpeed * Time.deltaTime;
            transform.localScale = Vector3.one * (currentRadius * 2f);

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
            Destroy(gameObject);
        }
    }
}