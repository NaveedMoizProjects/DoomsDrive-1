using UnityEngine;

public class DamageablePart : MonoBehaviour
{
    public string partName;
    public float health = 100f;
    public bool isInvincible = false; // Toggle for parts that show damage but don't break

    public enum PartType { Wheel, Door, Body, Core } // Added Core
    public PartType type;

    private DynamicCarController controller;
    public bool isPlayer = false;

    void Start()
    {
        controller = GetComponentInParent<DynamicCarController>();
        if (controller == null)
            Debug.LogError($"{gameObject.name} can't find DynamicCarController!");
        if (DamageManager.Instance != null)
        {
            DamageManager.Instance.UpdateHealth(partName, health);
        }
    }

    public void TakeDamage(float amount)
    {
        if (isInvincible || health <= 0) return;

        health -= amount;

        // SYNC: Tell the controller to update the health in its internal list
        if (type == PartType.Wheel && controller != null)
        {
            controller.ApplyDamageToWheel(partName, amount);
        }

        if (DamageManager.Instance != null)
        {
            DamageManager.Instance.UpdateHealth(partName, health);
        }
        // Only send to the HUD if this is the Player's car
        if (isPlayer && DamageManager.Instance != null)
        {
            DamageManager.Instance.UpdateHealth(partName, health);
        }

        if (health <= 0)
        {
            health = 0;
            BreakPart();
        }
    }

    void BreakPart()
    {
        if (type == PartType.Core)
        {
            Debug.Log("<color=red><b>CORE DESTROYED</b></color>");
            return;
        }

        if (type == PartType.Wheel) return;

        Debug.Log($"Breaking Joint for: {gameObject.name}");

        // 1. DISCONNECT THE JOINT (This is the "Glue")
        ConfigurableJoint joint = GetComponent<ConfigurableJoint>();
        if (joint != null)
        {
            Destroy(joint);
        }

        // 2. Unparent so it is no longer a child of the Jeep
        transform.SetParent(null);

        // 3. Ensure it has physics to fall
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        rb.isKinematic = false;
        rb.useGravity = true;

        // Ensure the collider is active and physical
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = false;

        // 4. Give it a push so it doesn't get stuck in the car's frame
        rb.AddForce(-transform.forward * 5f, ForceMode.Impulse);

        this.enabled = false;
    }
}