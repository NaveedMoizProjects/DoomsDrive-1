using UnityEngine;

public class DamageablePart : MonoBehaviour
{
    public string partName;
    public float health = 100f;
    public bool isInvincible = false;

    public enum PartType { Wheel, Door, Body, Core }
    public PartType type;

    private DynamicCarController controller;
    public bool isPlayer = false;

    void Start()
    {
        controller = GetComponentInParent<DynamicCarController>();

        // Register using InstanceID to prevent data collision between AI and Player
        if (DamageManager.Instance != null)
        {
            DamageManager.Instance.UpdateHealth(gameObject.GetInstanceID(), health, type, transform.root.gameObject);
        }
    }

    public void TakeDamage(float amount)
    {
        if (isInvincible || health <= 0) return;

        health -= amount;

        // 1. Sync with Physics Controller (if it's a wheel)
        if (type == PartType.Wheel && controller != null)
        {
            controller.ApplyDamageToWheel(partName, amount);
        }

        // 2. Sync with Global Manager using UNIQUE ID
        if (DamageManager.Instance != null)
        {
            DamageManager.Instance.UpdateHealth(gameObject.GetInstanceID(), health, type, transform.root.gameObject);
        }

        // 3. Trigger effects ONLY on the car this part belongs to
        CarEffectsManager effects = GetComponentInParent<CarEffectsManager>();
        if (effects != null)
        {
            effects.RefreshEffects();
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
            Debug.Log($"<color=red><b>CORE DESTROYED ON {transform.root.name}</b></color>");
            return;
        }

        // If it's a wheel, we usually handle that in the CarController, 
        // but if you want it to fall off, remove the 'return' below.
        if (type == PartType.Wheel) return;

        // DISCONNECT Logic
        ConfigurableJoint joint = GetComponent<ConfigurableJoint>();
        if (joint != null) Destroy(joint);

        transform.SetParent(null); // Separate from the car

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        rb.isKinematic = false;
        rb.useGravity = true;

        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = false;

        // Pop it outward so it doesn't collide with the car frame
        rb.AddForce((transform.position - transform.root.position).normalized * 5f, ForceMode.Impulse);

        this.enabled = false;
    }
}