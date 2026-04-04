using UnityEngine;
using TMPro;

public class PlayerHealthDamage : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;

    private TMP_Text healthText;

    void Start()
    {
        currentHealth = maxHealth;

        // Auto find TextMeshPro in scene
        healthText = GameObject.Find("PlayerHealthText").GetComponent<TMP_Text>();

        UpdateHealthUI();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerBullet"))
        {
            TakeDamage(10);
            Destroy(other.gameObject);
        }
    }

    void TakeDamage(int damage)
    {
        currentHealth -= damage;
        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void UpdateHealthUI()
    {
        if (healthText != null)
            healthText.text = "Health: " + currentHealth;
    }

    void Die()
    {
        Debug.Log("Player Died");
    }
}