using UnityEngine;
using System.Collections.Generic;

public class DynamicCarController : MonoBehaviour
{
    // --- Enums & Structs ---
    public enum DriveType { AllWheelDrive, FrontWheelDrive, RearWheelDrive }
    public enum Axel { Front, Rear }

    [System.Serializable]
    public struct Wheel
    {
        public string wheelName;
        public WheelCollider wheelCollider;
        public GameObject wheelModel;
        public Axel axel;
        public float health; // 100 = Healthy, 0 = Pop out
    }

    // --- Configuration ---
    [Header("Drive Settings")]
    public DriveType driveMode = DriveType.AllWheelDrive;
    public List<Wheel> wheels;

    [Header("Physics Constants")]
    public float baseMotorTorque = 1500f;
    public float maxSteerAngle = 30f;
    public float brakeForce = 3000f;
    public float scrapingFriction = 500f; // Drag applied when a wheel is missing

    private Rigidbody carRb;

    void Start()
    {
        carRb = GetComponent<Rigidbody>();

        if (CarSettings.Instance == null)
        {
            Debug.LogError("CarSettings Singleton missing! Please add a CarSettings script to the scene.");
        }

        // Initial Center of Mass setup
        carRb.centerOfMass = new Vector3(0, -1.0f, 0);
    }

    void FixedUpdate()
    {
        // Keep car stable based on UI Settings
        if (CarSettings.Instance != null)
        {
            carRb.centerOfMass = new Vector3(0, -CarSettings.Instance.stabilityExtra, 0);
        }

        HandlePhysics();
        ApplyAirDrag();
        ApplyScrapingDrag(); // NEW: Handle dragging on the ground for missing wheels
    }

    void HandlePhysics()
    {
        float moveInput = Input.GetAxis("Vertical");
        float steerInput = Input.GetAxis("Horizontal");
        bool isBraking = Input.GetKey(KeyCode.Space);

        for (int i = 0; i < wheels.Count; i++)
        {
            // Skip logic if wheel is already gone
            if (wheels[i].wheelCollider == null) continue;

            // --- TIRE DAMAGE CHECK ---
            // If health drops to 0, trigger physical disconnect automatically
            if (wheels[i].health <= 0)
            {
                DisconnectWheel(wheels[i].wheelName);
                continue;
            }

            // Steering
            if (wheels[i].axel == Axel.Front)
            {
                wheels[i].wheelCollider.steerAngle = steerInput * maxSteerAngle;
            }

            // Drive Power
            bool isPowered = false;
            if (driveMode == DriveType.AllWheelDrive) isPowered = true;
            else if (driveMode == DriveType.FrontWheelDrive && wheels[i].axel == Axel.Front) isPowered = true;
            else if (driveMode == DriveType.RearWheelDrive && wheels[i].axel == Axel.Rear) isPowered = true;

            if (isPowered)
            {
                wheels[i].wheelCollider.motorTorque = moveInput * baseMotorTorque * CarSettings.Instance.globalAccelerationMultiplier;
            }
            else
            {
                wheels[i].wheelCollider.motorTorque = 0;
            }

            wheels[i].wheelCollider.brakeTorque = isBraking ? brakeForce : 0;

            // Suspension/Hydraulics
            var spring = wheels[i].wheelCollider.suspensionSpring;
            spring.targetPosition = CarSettings.Instance.suspensionHeight;
            wheels[i].wheelCollider.suspensionSpring = spring;

            UpdateVisuals(wheels[i]);
        }
    }

    void UpdateVisuals(Wheel wheel)
    {
        if (wheel.wheelModel == null || wheel.wheelCollider == null) return;

        Vector3 pos;
        Quaternion rot;
        wheel.wheelCollider.GetWorldPose(out pos, out rot);

        wheel.wheelModel.transform.position = pos;
        wheel.wheelModel.transform.rotation = rot;
    }

    // Call this from UI to simulate damage
    public void ApplyDamageToWheel(string targetName, float amount)
    {
        for (int i = 0; i < wheels.Count; i++)
        {
            if (wheels[i].wheelName == targetName)
            {
                Wheel w = wheels[i];
                w.health -= amount;
                wheels[i] = w; // Update the list

                // The FixedUpdate will see this health change 
                // and call DisconnectWheel() on the next frame!
                return;
            }
        }
    }

    public void DisconnectWheel(string targetName)
    {
        for (int i = 0; i < wheels.Count; i++)
        {
            if (wheels[i].wheelName == targetName)
            {
                Wheel wheel = wheels[i];
                if (wheel.wheelCollider == null) return;

                // Disable physics logic on the car
                wheel.wheelCollider.enabled = false;

                if (wheel.wheelModel != null)
                {
                    // POPCORN PREVENTION: Unparent first
                    wheel.wheelModel.transform.SetParent(null);

                    // Physical Tire Setup
                    Rigidbody wheelRb = wheel.wheelModel.AddComponent<Rigidbody>();
                    wheelRb.mass = 20f; // Heavy tire
                    wheelRb.velocity = carRb.velocity; // Inherit car speed

                    MeshCollider wheelCol = wheel.wheelModel.AddComponent<MeshCollider>();
                    wheelCol.convex = true;

                    // Stop the wheel from exploding away from the car body
                    // FIX: Find ALL colliders on the car (body, other wheels, etc.)
                    Collider[] carColliders = GetComponentsInChildren<Collider>();
                    foreach (var carCol in carColliders)
                    {
                        // Don't ignore itself if it's still there, but ignore everything else
                        if (carCol != wheelCol)
                            Physics.IgnoreCollision(wheelCol, carCol);
                    }
                    // Add Friction/Rubber material
                    PhysicMaterial tireMat = new PhysicMaterial();
                    tireMat.dynamicFriction = 0.8f;
                    tireMat.bounciness = 0.2f;
                    wheelCol.material = tireMat;

                    // Small side-pop
                    Vector3 sideDir = (wheel.axel == Axel.Front) ? transform.right : -transform.right;
                    wheelRb.AddForce(sideDir * 3f, ForceMode.Impulse);
                }

                // Null out collider to signal 'Missing Wheel' to the rest of the script
                wheel.wheelCollider = null;
                wheels[i] = wheel;
                return;
            }
        }
    }

    void ApplyScrapingDrag()
    {
        foreach (var wheel in wheels)
        {
            // If the wheel is missing, apply drag at that specific corner
            if (wheel.wheelCollider == null && wheel.wheelModel != null)
            {
                // Pull the car down on the broken side
                carRb.AddForceAtPosition(Vector3.down * 1000f, wheel.wheelModel.transform.position);
                // Apply drag based on speed
                Vector3 dragDir = -carRb.velocity.normalized;
                carRb.AddForceAtPosition(dragDir * (carRb.velocity.magnitude * scrapingFriction), wheel.wheelModel.transform.position);
            }
        }
    }

    void ApplyAirDrag()
    {
        if (CarSettings.Instance == null) return;
        float speed = carRb.velocity.magnitude;
        Vector3 dragForce = -carRb.velocity * (speed * CarSettings.Instance.airDamping);
        carRb.AddForce(dragForce);
    }
}