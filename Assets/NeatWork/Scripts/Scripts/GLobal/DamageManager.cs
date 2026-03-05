using UnityEngine;
using System.Collections.Generic;

public class DamageManager : MonoBehaviour
{
    public static DamageManager Instance { get; private set; }
    public int currentLap = 0;
    public int lapsToWin = 3;

    public struct PartData
    {
        public float health;
        public DamageablePart.PartType type;
        public GameObject ownerCar;
    }

    // Key is now the unique InstanceID of the GameObject
    public Dictionary<int, PartData> carHealthRegistry = new Dictionary<int, PartData>();

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    public void UpdateHealth(int partID, float health, DamageablePart.PartType type, GameObject owner)
    {
        PartData data = new PartData { health = health, type = type, ownerCar = owner };
        carHealthRegistry[partID] = data;
    }

    public void DeclareWinner()
    {
        Debug.Log("We have a winner! Total Laps: " + currentLap);
        // Implement win logic here (e.g., show UI)
    }

    public float GetPartHealth(int partID)
    {
        return carHealthRegistry.ContainsKey(partID) ? carHealthRegistry[partID].health : 100f;
    }
}