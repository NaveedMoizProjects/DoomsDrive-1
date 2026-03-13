using UnityEngine;

public class DamageablePart : MonoBehaviour
{
    public string partName;
    public float health = 100f;
    public enum PartType { Wheel, Door, Body, Core }
    public PartType type;

    private GameObject rootCar;
    private bool isBroken = false; // Prevents double-triggering BreakPart

    void Start()
    {
        rootCar = transform.root.gameObject;
        SyncWithManager();
    }

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
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
    }

    void SyncWithManager()
    {
        if (DamageManager.Instance != null)
        {
            // Ensure health is clamped at 0 for the HUD registry
            float clampedHealth = Mathf.Max(0, health);
            DamageManager.Instance.UpdateHealth(gameObject.GetInstanceID(), clampedHealth, type, rootCar);
        }
    }
}