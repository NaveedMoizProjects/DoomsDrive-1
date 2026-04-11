using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DamageManager : MonoBehaviour
{
    public static DamageManager Instance { get; private set; }

    [Header("Lap Settings")]
    public int currentLap = 0; // start at 0 = no laps completed
    public int lapsToWin = 3;

    [Header("Game State")]
    public bool isGameOver = false;
    // Read-only accessor for other systems
    public bool IsGameOver => isGameOver;

    [Header("Scene / End-level")]
    [Tooltip("Seconds to wait on results screen before returning to MainMenu")]
    public float delayBeforeMenu = 5f;
    [Tooltip("Scene name to load after race end")]
    public string mainMenuSceneName = "MainMenu";
    [Header("Audio")]
    public AudioSource applauseAudio;

    [Header("Health")]
    private float maxPlayerHealth = 0f;
    private bool healthInitialized = false;

    [HideInInspector] public bool hudPaused = false;
    [HideInInspector] public bool allowPlayerDamage = true;

    public struct PartData
    {
        public float health;
        public float maxHealth;
        public DamageablePart.PartType type;
        public GameObject ownerCar;
    }

    public Dictionary<int, PartData> carHealthRegistry = new();

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    // --- NEW: Race end / declare winner handled here ---
    public void DeclareWinner(CarMovement.PlayerID winner)
    {
        if (isGameOver) return;
        isGameOver = true;

        if (RaceUIHandler.Instance != null)
            RaceUIHandler.Instance.ShowResults(winner);

        Time.timeScale = 0f;

        if (applauseAudio != null)
            applauseAudio.Play();

        Debug.Log("Game Over! Winner: " + winner);
        StartCoroutine(BackToMenuAfterDelay());
    }

    private System.Collections.IEnumerator BackToMenuAfterDelay()
    {
        yield return new WaitForSecondsRealtime(delayBeforeMenu);
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(mainMenuSceneName))
            SceneManager.LoadScene(mainMenuSceneName);
    }

    // --- EXISTING: registry, health, respawn helpers ---
    // --- NEW: PURGE OLD DATA BEFORE RESPAWN ---
    public void PurgeAllRegistryData()
    {
        carHealthRegistry.Clear();
        maxPlayerHealth = 0f;
        healthInitialized = false;
        Debug.Log("[DamageManager] Registry Purged for fresh Respawn.");
    }

    // --- NEW: SCAN NEW CAR PARTS ---
    public void RefreshNewPlayerCar(GameObject newCar)
    {
        // Force every DamageablePart on the new car to register
        DamageablePart[] parts = newCar.GetComponentsInChildren<DamageablePart>();
        foreach (var part in parts)
        {
            UpdateHealth(part.gameObject.GetInstanceID(), part.health, part.type, newCar);
        }

        RecalculatePlayerMaxHealth(newCar);
        ForceFullHeal(newCar);
        healthInitialized = true;
        hudPaused = false;
    }

    public void UpdateHealth(int id, float health, DamageablePart.PartType type, GameObject owner)
    {
        if (isGameOver) return;
        float clamped = Mathf.Max(0, health);
        float max = clamped;

        if (carHealthRegistry.TryGetValue(id, out var existing))
            max = Mathf.Max(existing.maxHealth, clamped);

        carHealthRegistry[id] = new PartData { health = clamped, maxHealth = max, type = type, ownerCar = owner };

        if (owner != null && owner.CompareTag("Player")) RecalculatePlayerMaxHealth(owner);
    }

    public void ClearOwnerEntries(GameObject owner)
    {
        var keys = carHealthRegistry.Where(k => k.Value.ownerCar == owner || k.Value.ownerCar == null).Select(k => k.Key).ToList();
        foreach (var k in keys) carHealthRegistry.Remove(k);
    }

    public void RecalculatePlayerMaxHealth(GameObject owner)
    {
        float total = carHealthRegistry.Values.Where(p => p.ownerCar == owner).Sum(p => p.maxHealth);
        if (total > 0) { maxPlayerHealth = total; healthInitialized = true; }
    }

    public float GetPlayerHealthPercentage()
    {
        if (!healthInitialized || maxPlayerHealth <= 0) return 100f;
        float current = carHealthRegistry.Values.Where(p => p.ownerCar != null && p.ownerCar.CompareTag("Player")).Sum(p => p.health);
        return Mathf.Clamp((current / maxPlayerHealth) * 100f, 0f, 100f);
    }

    public void ForceFullHeal(GameObject owner)
    {
        var keys = carHealthRegistry.Keys.ToList();
        foreach (var k in keys)
        {
            var data = carHealthRegistry[k];
            if (data.ownerCar == owner) { data.health = data.maxHealth; carHealthRegistry[k] = data; }
        }
    }

    public void FinalizeGame(string reason)
    {
        if (isGameOver) return;
        isGameOver = true;
        Debug.Log(reason);
    }
}