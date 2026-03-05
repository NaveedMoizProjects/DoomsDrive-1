using UnityEngine;
using System.Collections.Generic;

public class DamageManager : MonoBehaviour
{
    public static DamageManager Instance { get; private set; }

    // Store the health AND the type
    public struct PartData
    {
        public float health;
        public DamageablePart.PartType type;
    }

    // This dictionary stores: PartName -> HealthValue and Type(along part data but type used)
    public Dictionary<string, PartData> carHealthRegistry = new Dictionary<string, PartData>();

    [Header("Race Settings")]
    public int lapsToWin = 3;
    public int currentLap = 0;
    public int currentCheckpoint = 0;

    public void DeclareWinner()
    {
        Debug.Log("Race Finished! Security System: Vehicle Lockdown Engaged.");
    }
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //// Call this whenever a part takes damage
    //public void UpdateHealth(string partName, float health)
    //{
    //    if (carHealthRegistry.ContainsKey(partName))
    //        carHealthRegistry[partName] = health;
    //    else
    //        carHealthRegistry.Add(partName, health);

    //    // Security check: You can trigger a "Security Alert" here if health < 20
    //    if (health <= 0) Debug.Log($"Warning: {partName} has been breached!");
    //}
    public void UpdateHealth(string partName, float health, DamageablePart.PartType type)
    {
        PartData data = new PartData { health = health, type = type };

        if (carHealthRegistry.ContainsKey(partName))
            carHealthRegistry[partName] = data;
        else
            carHealthRegistry.Add(partName, data);
    }

    public float GetPartHealth(string partName)
    {
        return carHealthRegistry.ContainsKey(partName) ? (carHealthRegistry[partName].health)  : 000 ;
    }
}