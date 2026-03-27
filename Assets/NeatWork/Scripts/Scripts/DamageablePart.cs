using UnityEngine;

public class DamageablePart : MonoBehaviour
{
    public string partName;
    public float health = 100f;
    public float maxHealth = 100f; // explicit per-part baseline
    public enum PartType { Wheel, Door, Body, Core }
    public PartType type;

    private GameObject rootCar;
    private bool isBroken = false; // Prevents double-triggering BreakPart

    void OnEnable()
    {
        // Ensure owner is current when enabled
        rootCar = transform.root != null ? transform.root.gameObject : null;
        SyncWithManager();
    }

    void Start()
    {
        // initialize owner and values
        rootCar = transform.root != null ? transform.root.gameObject : null;

        // ensure maxHealth is set from initial health on start
        if (maxHealth <= 0f)
            maxHealth = health;
        else
            health = Mathf.Min(health, maxHealth);

        SyncWithManager();
    }

    // Called by Unity when the transform's parent changes at runtime (e.g., reattach)
    void OnTransformParentChanged()
    {
        rootCar = transform.root != null ? transform.root.gameObject : null;
        SyncWithManager();
    }

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        // If owner is player and damage disabled globally, skip
        if (rootCar != null && rootCar.CompareTag("Player") && DamageManager.Instance != null && !DamageManager.Instance.allowPlayerDamage)
            return;

        // 1. If already dead or broken, stop everything
        if (health <= 0 || isBroken) return;

        health -= amount;

        // 2. Sync with the Global Manager (Laps/HUD/Total %)
        SyncWithManager();

        // 3. Handle specific part logic
        if (type == PartType.Wheel)
        {
            DynamicCarController controller = GetComponentInParent<DynamicCarController>();
            if (controller != null)
            {
                // We ONLY update the number here. 
                // We do NOT let the controller decide to disconnect.
                controller.ApplyDamageToWheel(partName, amount);
            }
        }
        else
        {
            // Visual denting for non-wheel parts
            MeshDeformer deformer = GetComponent<MeshDeformer>();
            if (deformer != null) deformer.DeformMesh(hitPoint, 1.5f, 0.2f, -hitNormal);
        }

        // 4. Check for death
        if (health <= 0)
        {
            BreakPart();
        }
    }

    void BreakPart()
    {
        if (isBroken) return; // Hard safety lock
        isBroken = true;

        if (type == PartType.Core) return;

        if (type == PartType.Wheel)
        {
            DynamicCarController controller = GetComponentInParent<DynamicCarController>();
            if (controller != null)
            {
                // This is now the ONLY place in the whole project 
                // that triggers the wheel pop.
                controller.DisconnectWheel(partName);
            }
            this.enabled = false;
            return;
        }

        // --- Logic for Doors/Bumpers/Body Parts ---
        transform.SetParent(null);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.mass = 5f;

        // Apply ejection force relative to the car's position
        Vector3 explosionSource = transform.position - Vector3.up;
        rb.AddForce((transform.position - explosionSource).normalized * 15f, ForceMode.Impulse);

        this.enabled = false;

        // Update owner immediately (detached => no owner)
        rootCar = transform.root != null ? transform.root.gameObject : null;
        SyncWithManager();
    }

    // Public repair API used by RespawnManager
    public void RepairPart()
    {
        // Reset broken flag and restore health to max
        isBroken = false;
        health = Mathf.Max(1f, maxHealth); // ensure > 0
        this.enabled = true;

        // Update owner in case parent changed
        rootCar = transform.root != null ? transform.root.gameObject : null;

        // Re-sync with manager
        SyncWithManager();
    }

    // Called from RespawnManager when a detached part is reparented to a new car instance
    public void SetOwnerAndSync(GameObject newOwner)
    {
        rootCar = newOwner;
        // If reparent wasn't done by caller, ensure transform root matches owner
        if (rootCar != null && transform.root != rootCar.transform)
        {
            transform.SetParent(rootCar.transform, true);
        }

        // Update manager registry with new owner
        SyncWithManager();
    }

    void SyncWithManager()
    {
        if (DamageManager.Instance != null)
        {
            // Ensure rootCar reference is up to date
            rootCar = transform.root != null ? transform.root.gameObject : rootCar;

            // Ensure health is clamped at 0 for the HUD registry
            float clampedHealth = Mathf.Max(0, health);

            // Use the object's instance id as registry key (registry will be replaced/updated on reattach/spawn)
            DamageManager.Instance.UpdateHealth(gameObject.GetInstanceID(), clampedHealth, type, rootCar);
        }
    }
}