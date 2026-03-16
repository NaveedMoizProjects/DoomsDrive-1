using UnityEngine;

public class AIHealth : MonoBehaviour
{
    public float health = 100f;
    public GameObject bloodVFX;

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("PlayerBullet"))
        {
            TakeDamage(10f, collision.contacts[0].point);
            Destroy(collision.gameObject);
        }
    }

    void TakeDamage(float damage, Vector3 hitPoint)
    {
        health -= damage;

        if (bloodVFX != null)
        {
            Instantiate(bloodVFX, hitPoint, Quaternion.identity);
        }

        if (health <= 0)
        {
            health = 0;
            Debug.Log("Enemy Destroyed");
            Destroy(gameObject);
        }
    }
}