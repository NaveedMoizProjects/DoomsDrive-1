using UnityEngine;

public class BulletDamage : MonoBehaviour
{
    public float damage = 20f;
    public string targetTag = "Player"; // jo tag damage receive kare

    void OnCollisionEnter(Collision collision)
    {
        // Check karo ke collided object ka tag targetTag se match karta hai
        if (collision.gameObject.CompareTag(targetTag))
        {
            HealthSystem health = collision.gameObject.GetComponent<HealthSystem>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }
        }

        Destroy(gameObject); // bullet destroy ho jaye
    }
}