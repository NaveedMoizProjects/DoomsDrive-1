/*
    This is a singleton class
*/

using UnityEngine;

public class CarSettings : MonoBehaviour
{
    // This is the global "address" other scripts use to find this one
    public static CarSettings Instance;

    [Header("Global Car Tuning")]
    [Range(0.1f, 5f)] public float globalAccelerationMultiplier = 1.0f;
    [Range(0f, 2f)] public float airDamping = 0.5f;
    [Range(0f, 1f)] public float suspensionHeight = 0.2f;
    [Range(0f, 1f)]public float stabilityExtra = 1.0f; // Use this to move CoM via UI

    void Awake()
    {
        // Initialization Logic:
        if (Instance == null)
        {
            Instance = this;
            // Optional: Keeps settings alive if you change levels
            // DontDestroyOnLoad(gameObject); 
        }
        else
        {
            // Prevents having two managers in one scene
            Destroy(gameObject);
        }
    }
}