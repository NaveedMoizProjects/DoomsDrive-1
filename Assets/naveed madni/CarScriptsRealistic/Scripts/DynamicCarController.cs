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
        public string wheelName; // e.g., "FrontLeft"
        public WheelCollider wheelCollider;
        public GameObject wheelModel;
        public Axel axel;
        public float health; // New: 100 is healthy, 0 is destroyed
    }

    // --- Configuration ---
    [Header("Drive Settings")]
    public DriveType driveMode = DriveType.AllWheelDrive;
    public List<Wheel> wheels;

    [Header("Physics Constants")]
    public float baseMotorTorque = 1500f;
    public float maxSteerAngle = 30f;
    public float brakeForce = 3000f;

    private Rigidbody carRb;

    void Start()
    {
        carRb = GetComponent<Rigidbody>();

        // Safety check for the Singleton we created earlier
        if (CarSettings.Instance == null)
        {
            Debug.LogError("CarSettings Singleton missing! Please add a CarSettings script to the scene.");
        }

        // Move the weight way down to prevent flipping
        // Adjust the -1.0f value. Lower = more stable.
        carRb.centerOfMass = new Vector3(0, -1.0f, 0);
    }

    void FixedUpdate()
    {
        // This allows you to adjust how "flippable" the car is from your UI sliders
        carRb.centerOfMass = new Vector3(0, -CarSettings.Instance.stabilityExtra, 0);
        HandlePhysics();
        ApplyAirDrag();
    }

    void HandlePhysics()
    {
        // 1. Get Inputs
        float moveInput = Input.GetAxis("Vertical");
        float steerInput = Input.GetAxis("Horizontal");
        bool isBraking = Input.GetKey(KeyCode.Space);

        foreach (var wheel in wheels)
        {
            // REQUIREMENT: Dynamic check for missing/broken wheels
            if (wheel.wheelCollider == null) continue;

            // 2. Handle Steering
            if (wheel.axel == Axel.Front)
            {
                // Smoothly steer
                wheel.wheelCollider.steerAngle = steerInput * maxSteerAngle;
            }

            // 3. Handle Acceleration (Based on Drive Mode)
            bool isPowered = false;
            if (driveMode == DriveType.AllWheelDrive) isPowered = true;
            else if (driveMode == DriveType.FrontWheelDrive && wheel.axel == Axel.Front) isPowered = true;
            else if (driveMode == DriveType.RearWheelDrive && wheel.axel == Axel.Rear) isPowered = true;

            if (isPowered)
            {
                wheel.wheelCollider.motorTorque = moveInput * baseMotorTorque * CarSettings.Instance.globalAccelerationMultiplier;
            }
            else
            {
                wheel.wheelCollider.motorTorque = 0;
            }

            // 4. Handle Braking
            wheel.wheelCollider.brakeTorque = isBraking ? brakeForce : 0;

            // 5. OPTIONAL: Hydraulics (Suspension Height)
            // Note: targetPosition 0 = Fully Extended, 1 = Fully Compressed
            var spring = wheel.wheelCollider.suspensionSpring;
            spring.targetPosition = CarSettings.Instance.suspensionHeight;
            wheel.wheelCollider.suspensionSpring = spring;

            // 6. REQUIREMENT: Update Visuals (Make the 3D model match the physics)
            UpdateVisuals(wheel);
        }
    }

    void UpdateVisuals(Wheel wheel)
    {
        if (wheel.wheelModel == null) return;

        Vector3 pos;
        Quaternion rot;
        wheel.wheelCollider.GetWorldPose(out pos, out rot);

        wheel.wheelModel.transform.position = pos;
        wheel.wheelModel.transform.rotation = rot;
    }

    // for testing now
    public void DestroySpecificWheel(string targetName)
    {
        for (int i = 0; i < wheels.Count; i++)
        {
            if (wheels[i].wheelName == targetName)
            {
                // 1. Disable Physics
                if (wheels[i].wheelCollider != null) wheels[i].wheelCollider.enabled = false;

                // 2. Hide Mesh
                if (wheels[i].wheelModel != null) wheels[i].wheelModel.SetActive(false);

                Debug.Log($"Relational Link Success: {targetName} is now offline.");
                return;
            }
        }
    }
    public void DisconnectWheel(string targetName)
    {
        for (int i = 0; i < wheels.Count; i++)
        {
            // 1. Find the wheel in our list
            if (wheels[i].wheelName == targetName)
            {
                Wheel wheel = wheels[i];

                if (wheel.wheelCollider == null) return;

                // 2. Disable the physics 'logic' (so the car stops powered this corner)
                wheel.wheelCollider.enabled = false;

                // 3. Make the visual mesh a physical object in the world
                if (wheel.wheelModel != null)
                {
                    // Disconnect from the car parent
                    wheel.wheelModel.transform.SetParent(null);

                    // Add physics to the loose wheel
                    Rigidbody wheelRb = wheel.wheelModel.AddComponent<Rigidbody>();
                    MeshCollider wheelCol = wheel.wheelModel.AddComponent<MeshCollider>();
                    wheelCol.convex = true; // Required for physics

                    // Give it a little "pop" force so it flies away from the car
                    wheelRb.AddForce(transform.right * 5f + Vector3.up * 2f, ForceMode.Impulse);
                    wheelRb.AddRelativeTorque(Vector3.right * 10f, ForceMode.Impulse);
                }

                Debug.Log($"Wheel {targetName} has been disconnected and is now physics-active!");

                // 4. Important: Set references to null so the Controller loop skips this wheel
                // We use a local variable to update the struct then put it back
                wheel.wheelCollider = null;
                wheels[i] = wheel;

                return;
            }
        }
    }
    void ApplyAirDrag()
    {
        // REQUIREMENT: Custom Damping/Air Resistance
        // Applying force opposite to velocity based on the square of speed
        if (CarSettings.Instance == null) return;

        float speed = carRb.velocity.magnitude;
        Vector3 dragForce = -carRb.velocity * (speed * CarSettings.Instance.airDamping);
        carRb.AddForce(dragForce);
    }
}