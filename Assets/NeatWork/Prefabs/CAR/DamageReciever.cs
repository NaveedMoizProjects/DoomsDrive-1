using UnityEngine;

public class DamageReceiver : MonoBehaviour
{
    public int health = 100;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerBullet"))
        {
            TakeDamage(20);

            // Destroy bullet after hit
            Destroy(other.gameObject);
        }
    }

    void TakeDamage(int damage)
    {
        health -= damage;

        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log(gameObject.name + " died");
        Destroy(gameObject);
    }
}