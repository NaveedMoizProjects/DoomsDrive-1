using UnityEngine;

[RequireComponent(typeof(UI_CheckpointMarker))]
public class UIPointerAutoScanner : MonoBehaviour
{
    private UI_CheckpointMarker pointerScript;

    [Header("Settings")]
    public string carTag = "Player";
    public string gunNodeName = "GunPivot"; // The name of the transform the gun is on
    public float scanFrequency = 0.5f;

    void Awake()
    {
        pointerScript = GetComponent<UI_CheckpointMarker>();
    }

    void Start()
    {
        // Continuously check for the car in case of respawns
        InvokeRepeating(nameof(LocateGunnerTarget), 0f, scanFrequency);
    }

    void LocateGunnerTarget()
    {
        // If we already have a valid target, stop scanning
        if (pointerScript.target != null) return;

        // 1. Find ALL objects with the Tag "Player"
        GameObject[] potentialCars = GameObject.FindGameObjectsWithTag(carTag);

        foreach (GameObject car in potentialCars)
        {
            // 2. Check if the Car itself is the target (by name) 
            // OR look for the specific GunNode inside this car
            if (car.name == gunNodeName)
            {
                pointerScript.target = car.transform;
                Debug.Log($"Gunner UI: Locked onto {car.name} by Tag and Name.");
                return;
            }

            // 3. Search children of this specific 'Player' tagged object
            Transform gunNode = FindChildRecursive(car.transform, gunNodeName);

            if (gunNode != null)
            {
                pointerScript.target = gunNode;
                Debug.Log($"Gunner UI: Locked onto {gunNode.name} inside {car.name}.");
                return;
            }
        }
    }

    // Helper to find the gun node even if it's deep inside the car hierarchy
    private Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindChildRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }
}