using UnityEngine;

public class CarDamageHandler : MonoBehaviour
{
    private DynamicCarController controller;

    void Start()
    {
        controller = GetComponent<DynamicCarController>();
    }

    // Call this from a UI Button or a Bullet collision
    public void DamageWheel(int wheelIndex, float damageAmount)
    {
        if (wheelIndex < 0 || wheelIndex >= controller.wheels.Count) return;

        // Get a reference to the specific wheel
        var wheel = controller.wheels[wheelIndex];

        // Subtract health
        wheel.health -= damageAmount;
        Debug.Log($"{wheel.wheelName} took damage! Current HP: {wheel.health}");

        if (wheel.health <= 0)
        {
            DestroyWheel(wheelIndex);
        }

        // Update the list with the new health values
        controller.wheels[wheelIndex] = wheel;
    }

    void DestroyWheel(int index)
    {
        var wheel = controller.wheels[index];

        Debug.LogWarning($"WHEEL DESTROYED: {wheel.wheelName}");

        // 1. Physically disable the physics
        if (wheel.wheelCollider != null)
        {
            wheel.wheelCollider.enabled = false;
            // Or use Destroy(wheel.wheelCollider); if you want it gone forever
        }

        // 2. Visually hide the wheel (or swap to a broken rim model)
        if (wheel.wheelModel != null)
        {
            wheel.wheelModel.SetActive(false);
        }

        // 3. Optional: Trigger a particle effect (smoke/sparks)
        // Instantiate(explosionPrefab, wheel.wheelCollider.transform.position, Quaternion.identity);
    }
}