using UnityEngine;

public class ExplosionShockwave : MonoBehaviour
{
    private float maxRadius;
    private float currentRadius = 0f;
    private float expansionSpeed;

    public void Setup(float radius, float damage, float force)
    {
        maxRadius = radius;
        // Adjust expansionSpeed for how fast the red sphere grows
        expansionSpeed = radius * 5f;

        // Apply the Damage and Force immediately using your helper
        DamageGiver.SendExplosionDamage(transform.position, radius, damage, force);
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