using UnityEngine;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    public float health = 100f;

    [Header("UI")]
    public TMP_Text healthText;

    [Header("VFX")]
    public GameObject bloodVFX;

    void Start()
    {
        UpdateHealthUI();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("EnemyBullet"))
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

        UpdateHealthUI();

        if (health <= 0)
        {
            health = 0;
            Debug.Log("Player Dead");
            // yahan game over logic laga sakte ho
        }
    }

    void UpdateHealthUI()
    {
        if (healthText != null)
        {
            healthText.text = "Health : " + health.ToString("0");
        }
    }
}