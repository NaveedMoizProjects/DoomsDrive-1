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

    public struct PartData
    {
        public float health;
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
        carHealthRegistry[partID] = new PartData { health = clampedHealth, type = type, ownerCar = owner };

        if (!healthInitialized && Time.timeSinceLevelLoad > 1f && owner.CompareTag("Player"))
        {
            maxPlayerHealth = carHealthRegistry.Values
                .Where(p => p.ownerCar != null && p.ownerCar.CompareTag("Player"))
                .Sum(p => p.health);
            healthInitialized = true;
        }

        if (type == DamageablePart.PartType.Core && clampedHealth <= 0 && owner.CompareTag("Player"))
        {
            FinalizeGame("MISSION FAILED: CORE DESTROYED");
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