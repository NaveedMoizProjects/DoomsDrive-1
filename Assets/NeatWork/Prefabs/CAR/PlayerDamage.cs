//using UnityEngine;
//using TMPro;

//public class PlayerHealthDamage : MonoBehaviour
//{
//    public int maxHealth = 100;
//    public int currentHealth;

//    private TMP_Text healthText;

//    void OnEnable()
//    {
//        if (RespawnManager.Instance != null)
//            RespawnManager.Instance.OnPlayerSpawned += OnPlayerSpawned;
//    }

//    void OnDisable()
//    {
//        if (RespawnManager.Instance != null)
//            RespawnManager.Instance.OnPlayerSpawned -= OnPlayerSpawned;
//    }

//    void Start()
//    {
//        currentHealth = maxHealth;

//        LocateHealthText();
//        UpdateHealthUI();
//    }

//    void OnPlayerSpawned(GameObject newPlayer)
//    {
//        Jab player respawn ho to health reset
//        if (newPlayer == gameObject)
//        {
//            currentHealth = maxHealth;
//            LocateHealthText();
//            UpdateHealthUI();

//            Debug.Log("PlayerHealthDamage: Reset on respawn");
//        }
//    }

//    void LocateHealthText()
//    {
//        Try find by name
//        GameObject textObj = GameObject.Find("PlayerHealthText");

//        if (textObj != null)
//        {
//            healthText = textObj.GetComponent<TMP_Text>();
//            Debug.Log("Health UI Found");
//        }
//        else
//        {
//            Debug.LogWarning("PlayerHealthText not found!");
//        }
//    }
//    private void OnCollisionEnter(Collision collision)
//    {
//        if (collision.gameObject.CompareTag("EnemyBullet"))
//        {
//            if (CompareTag("Player") || CompareTag("Player"))
//            {
//                TakeDamage(10);
//                Destroy(collision.gameObject);
//            }
//        }
//    }
//    public void TakeDamage(int damage)
//    {
//        currentHealth -= damage;
//        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

//        UpdateHealthUI();

//        if (currentHealth <= 0)
//        {
//            Die();
//        }
//    }

//    public void UpdateHealthUI()
//    {
//        if (healthText == null)
//        {
//            LocateHealthText();
//        }

//        if (healthText != null)
//        {
//            healthText.text = "HealthMoiz: " + currentHealth;
//        }
//    }

//    void Die()
//    {
//        Debug.Log("Player Died");

//    Optional: disable player instead of destroy
//        gameObject.SetActive(false);
//    }
//}