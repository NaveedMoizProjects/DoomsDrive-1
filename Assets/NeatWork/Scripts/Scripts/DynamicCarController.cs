using System.Collections.Generic;
using UnityEngine;

public class DynamicCarController : MonoBehaviour
{
    public enum DriveType { AllWheelDrive, FrontWheelDrive, RearWheelDrive }
    public enum WheelPosition { FrontLeft, FrontRight, RearLeft, RearRight }

    [System.Serializable]
    public struct Wheel
    {
        public string wheelName;
        public WheelCollider wheelCollider;
        public GameObject wheelModel;
        public WheelPosition position;
        public float health;
        [Tooltip("Optional: Dust/Smoke particles. Leave empty if none.")]
        public ParticleSystem sandParticles;
    }

    [Header("Anti-Flip & Physics")]
    public float centerOfMassY = -1.5f;
    public float extraGravity = 2.0f;
    public float downforceAmount = 500f;

    [Header("Steering Smoothness")]
    public float maxSteerAngle = 30f;
    [Tooltip("Higher = faster steering response. Recommended: 3 to 6")]
    public float steeringSpeed = 5f;

    [Header("Friction Settings")]
    [Range(1f, 5f)] public float forwardFrictionStiffness = 2.0f;
    [Range(1f, 5f)] public float sidewaysFrictionStiffness = 2.5f;

    [Header("Drive Settings")]
    public DriveType driveMode = DriveType.AllWheelDrive;
    public List<Wheel> wheels;
    public float baseMotorTorque = 2000f;
    public float brakeForce = 4000f;
    public float scrapingFriction = 500f;

    // Private
    private Rigidbody carRb;
    private CarSettings carSettings;
    private float currentSteerAngle = 0f; // Track actual angle, not normalized input

    void Start()
    {
        carRb = GetComponent<Rigidbody>();
        carSettings = CarSettings.Instance;

        carRb.centerOfMass = new Vector3(0, centerOfMassY, 0.1f);
        UpdateWheelFriction();
    }

    void FixedUpdate()
    {
        carRb.centerOfMass = new Vector3(0, centerOfMassY, 0.1f);
        carRb.AddForce(Vector3.down * carRb.mass * extraGravity);

        UpdateWheelFriction();
        HandlePhysics();
        ApplyDownforce();
        ApplyAirDrag();
        ApplyScrapingDrag();
    }

    void HandlePhysics()
    {
        // ✅ FIX: Use GetAxisRaw to avoid Unity's built-in double-smoothing
        float moveInput = Input.GetAxisRaw("Vertical");
        float rawSteer = Input.GetAxisRaw("Horizontal");
        bool isBraking = Input.GetKey(KeyCode.Space);

        // ✅ FIX: Smoothly interpolate the actual steer ANGLE directly
        float targetAngle = rawSteer * maxSteerAngle;
        currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetAngle, Time.fixedDeltaTime * steeringSpeed);

        float accelMultiplier = carSettings != null ? carSettings.globalAccelerationMultiplier : 1f;
        float suspHeight = carSettings != null ? carSettings.suspensionHeight : 0.2f;

        for (int i = 0; i < wheels.Count; i++)
        {
            if (wheels[i].wheelCollider == null) continue;

            // Apply smoothed steer angle to front wheels
            if (wheels[i].position == WheelPosition.FrontLeft ||
                wheels[i].position == WheelPosition.FrontRight)
            {
                wheels[i].wheelCollider.steerAngle = currentSteerAngle;
            }

            // Drive mode logic
            bool isPowered = false;
            switch (driveMode)
            {
                case DriveType.AllWheelDrive:
                    isPowered = true;
                    break;
                case DriveType.FrontWheelDrive:
                    isPowered = wheels[i].position == WheelPosition.FrontLeft ||
                                wheels[i].position == WheelPosition.FrontRight;
                    break;
                case DriveType.RearWheelDrive:
                    isPowered = wheels[i].position == WheelPosition.RearLeft ||
                                wheels[i].position == WheelPosition.RearRight;
                    break;
            }

            wheels[i].wheelCollider.motorTorque = isPowered ? moveInput * baseMotorTorque * accelMultiplier : 0f;
            wheels[i].wheelCollider.brakeTorque = isBraking ? brakeForce : 0f;

            // Suspension
            var spring = wheels[i].wheelCollider.suspensionSpring;
            spring.targetPosition = suspHeight;
            wheels[i].wheelCollider.suspensionSpring = spring;

            UpdateVisuals(wheels[i]);
        }
    }

    void UpdateWheelFriction()
    {
        foreach (var wheel in wheels)
        {
            if (wheel.wheelCollider == null) continue;

            WheelFrictionCurve fF = wheel.wheelCollider.forwardFriction;
            fF.stiffness = forwardFrictionStiffness;
            wheel.wheelCollider.forwardFriction = fF;

            WheelFrictionCurve sF = wheel.wheelCollider.sidewaysFriction;
            sF.stiffness = sidewaysFrictionStiffness;
            wheel.wheelCollider.sidewaysFriction = sF;
        }
    }

    void UpdateVisuals(Wheel wheel)
    {
        if (wheel.wheelModel == null || wheel.wheelCollider == null) return;
        wheel.wheelCollider.GetWorldPose(out Vector3 pos, out Quaternion rot);
        wheel.wheelModel.transform.position = pos;
        wheel.wheelModel.transform.rotation = rot;
    }

    void ApplyDownforce()
    {
        float speed = carRb.velocity.magnitude;
        carRb.AddForce(-transform.up * downforceAmount * speed);
    }

    void ApplyAirDrag()
    {
        float airDamp = carSettings != null ? carSettings.airDamping : 0.5f;
        carRb.AddForce(-carRb.velocity * (carRb.velocity.magnitude * airDamp));
    }

    void ApplyScrapingDrag()
    {
        foreach (var wheel in wheels)
        {
            if (wheel.wheelCollider != null) continue; // Skip wheels that are intact

            Vector3 offset = wheel.position == WheelPosition.FrontLeft ? new Vector3(-0.8f, 0, 1.5f) :
                             wheel.position == WheelPosition.FrontRight ? new Vector3(0.8f, 0, 1.5f) :
                             wheel.position == WheelPosition.RearLeft ? new Vector3(-0.8f, 0, -1.5f) :
                                                                           new Vector3(0.8f, 0, -1.5f);

            Vector3 dragPos = transform.TransformPoint(offset);
            carRb.AddForceAtPosition(Vector3.down * 1000f, dragPos);
            carRb.AddForceAtPosition(-carRb.velocity.normalized * (carRb.velocity.magnitude * scrapingFriction), dragPos);
        }
    }

    // ─── Damage & Wheel Disconnect ───────────────────────────────────────────

    public void ApplyDamageToWheel(string targetName, float amount)
    {
        for (int i = 0; i < wheels.Count; i++)
        {
            if (wheels[i].wheelName != targetName) continue;

            Wheel wheel = wheels[i];
            wheel.health -= amount;
            wheels[i] = wheel;

            Debug.Log($"[Car] Wheel '{targetName}' took {amount} damage. Health: {wheel.health}");
            return;
        }
    }

    public void DisconnectWheel(string targetName)
    {
        for (int i = 0; i < wheels.Count; i++)
        {
            if (wheels[i].wheelName != targetName) continue;

            Wheel wheel = wheels[i];
            if (wheel.wheelCollider == null) return;

            wheel.wheelCollider.enabled = false;
            wheel.wheelCollider.gameObject.SetActive(false);

            if (wheel.wheelModel != null)
            {
                GameObject meshObj = wheel.wheelModel;
                meshObj.transform.SetParent(null);

                Rigidbody tireRb = meshObj.GetComponent<Rigidbody>() ?? meshObj.AddComponent<Rigidbody>();
                tireRb.isKinematic = false;
                tireRb.useGravity = true;
                tireRb.mass = 20f;
                tireRb.velocity = carRb.velocity;

                MeshCollider tireCol = meshObj.GetComponent<MeshCollider>() ?? meshObj.AddComponent<MeshCollider>();
                tireCol.convex = true;

                Vector3 sideDir = (wheel.position == WheelPosition.FrontLeft ||
                                   wheel.position == WheelPosition.RearLeft)
                                   ? -transform.right : transform.right;

                tireRb.AddForce(sideDir * 5f, ForceMode.Impulse);
            }

            wheel.wheelCollider = null;
            wheels[i] = wheel;
            return;
        }
    }
}