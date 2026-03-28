using UnityEngine;

public class DamageablePart : MonoBehaviour
{
    public string partName;
    public float health = 100f;
    public float maxHealth = 100f;
    public enum PartType { Wheel, Door, Body, Core,player,enemy }
    public PartType type;

    private GameObject rootCar;
    private bool isBroken = false;

    void Start()
    {
        rootCar = transform.root != null ? transform.root.gameObject : null;
        if (maxHealth <= 0f) maxHealth = health;
        SyncWithManager();
    }

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (rootCar != null && rootCar.CompareTag("Player") && DamageManager.Instance != null && !DamageManager.Instance.allowPlayerDamage)
            return;

        if (health <= 0 || isBroken) return;

        health -= amount;
        SyncWithManager();

        if (type == PartType.Wheel)
        {
            DynamicCarController controller = GetComponentInParent<DynamicCarController>();
            if (controller != null) controller.ApplyDamageToWheel(partName, amount);
        }

        if (health <= 0) BreakPart();
    }

    void BreakPart()
    {
        if (isBroken) return;
        isBroken = true;

        // Use the SINGLETON Instance instead of GetComponentInParent
        if (type == PartType.Core)
        {
            if (RespawnManager.Instance != null)
                RespawnManager.Instance.TriggerDeath("ENGINE TOTALED");
            return;
        }

        if (type == PartType.Wheel)
        {
            DynamicCarController controller = GetComponentInParent<DynamicCarController>();
            if (controller != null)
            {
                controller.DisconnectWheel(partName);
                // Trigger the R/ESC menu when a wheel falls off
                if (RespawnManager.Instance != null)
                    RespawnManager.Instance.TriggerDeath("CRITICAL WHEEL LOSS");
            }
            this.enabled = false;
            return;
        }

        // Standard body part physics
        transform.SetParent(null);
        if (!GetComponent<Rigidbody>()) gameObject.AddComponent<Rigidbody>();
        this.enabled = false;
    }

    void SyncWithManager()
    {
        if (DamageManager.Instance != null)
        {
            rootCar = transform.root != null ? transform.root.gameObject : rootCar;
            DamageManager.Instance.UpdateHealth(gameObject.GetInstanceID(), Mathf.Max(0, health), type, rootCar);
        }
    }
}