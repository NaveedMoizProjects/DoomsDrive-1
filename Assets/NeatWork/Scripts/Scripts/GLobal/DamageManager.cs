using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DamageManager : MonoBehaviour
{
    public static DamageManager Instance { get; private set; }

    [Header("Game State")]
    public bool isGameOver = false;
    public int currentLap = 1;
    public int lapsToWin = 3;

    [Header("Health Settings")]
    private float maxPlayerHealth = 0f;
    private bool healthInitialized = false;

    // New control flags
    [HideInInspector] public bool hudPaused = false;         // whether HUD should freeze
    [HideInInspector] public bool allowPlayerDamage = true;  // whether player parts can take damage

    public struct PartData
    {
        public float health;
        public float maxHealth; // added: track each part's maximum (baseline)
        public DamageablePart.PartType type;
        public GameObject ownerCar;
    }

    public Dictionary<int, PartData> carHealthRegistry = new Dictionary<int, PartData>();

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    // --- LAP LOGIC ---
    public void OnLapComplete(GameObject car)
    {
        if (isGameOver) return;

        if (car.CompareTag("Player"))
        {
            if (currentLap >= lapsToWin)
            {
                FinalizeGame("VICTORY: RACE FINISHED!");
            }
            else
            {
                currentLap++;
                Debug.Log($"<color=green>Lap {currentLap} Started!</color>");
            }
        }
    }

    // --- HEALTH LOGIC ---
    public void UpdateHealth(int partID, float health, DamageablePart.PartType type, GameObject owner)
    {
        if (isGameOver) return;

        float clampedHealth = Mathf.Max(0, health);

        // Determine maxHealth for this part (keep previous max if present, otherwise use current value)
        float maxHealthForPart = clampedHealth;
        if (carHealthRegistry.TryGetValue(partID, out PartData existing))
        {
            // Preserve the highest-seen maxHealth (so baseline won't shrink when part is damaged)
            maxHealthForPart = Mathf.Max(existing.maxHealth > 0f ? existing.maxHealth : clampedHealth, clampedHealth);
        }

        carHealthRegistry[partID] = new PartData { health = clampedHealth, maxHealth = maxHealthForPart, type = type, ownerCar = owner };

        // If this is a player-owned part, recalculate player's total max baseline so percentage/HUD stays correct
        if (owner != null && owner.CompareTag("Player"))
        {
            RecalculatePlayerMaxHealth(owner);
        }

        if (type == DamageablePart.PartType.Core && clampedHealth <= 0 && owner != null && owner.CompareTag("Player"))
        {
            FinalizeGame("MISSION FAILED: CORE DESTROYED");
        }
    }

    // Remove all registry entries that belong to the specified owner and recalculate baselines.
    public void ClearOwnerEntries(GameObject owner)
    {
        if (owner == null) return;

        var keysToRemove = carHealthRegistry
            .Where(kv => kv.Value.ownerCar == owner)
            .Select(kv => kv.Key)
            .ToList();

        foreach (var k in keysToRemove)
            carHealthRegistry.Remove(k);

        // Recalculate baseline for that owner (and global player baseline)
        RecalculatePlayerMaxHealth(owner);
    }

    // Recalculates and caches the maximum possible health for the given owner player vehicle.
    // If owner is null, it will compute for all player-owned parts across registry.
    public void RecalculatePlayerMaxHealth(GameObject owner = null)
    {
        // Sum maxHealth for parts that belong to the player (or all player parts if owner == null)
        float totalMax = 0f;

        foreach (var kv in carHealthRegistry.Values)
        {
            if (kv.ownerCar == null) continue;
            if (!kv.ownerCar.CompareTag("Player")) continue;
            if (owner != null && kv.ownerCar != owner) continue;
            totalMax += kv.maxHealth;
        }

        // If we couldn't compute a baseline (e.g. registry not populated yet), keep previous behaviour and defer initialization.
        if (totalMax > 0f)
        {
            maxPlayerHealth = totalMax;
            healthInitialized = true;
        }
    }

    public float GetPlayerHealthPercentage()
    {
        if (!healthInitialized || maxPlayerHealth <= 0) return 100f;

        float currentTotal = carHealthRegistry.Values
            .Where(p => p.ownerCar != null && p.ownerCar.CompareTag("Player"))
            .Sum(p => p.health);

        return Mathf.Clamp((currentTotal / maxPlayerHealth) * 100f, 0f, 100f);
    }

    public void FinalizeGame(string reason)
    {
        if (isGameOver) return;
        isGameOver = true;

        Debug.Log($"<color=red><b>{reason}</b></color>");

        // SceneManager.LoadScene("StatsScene");
    }
}