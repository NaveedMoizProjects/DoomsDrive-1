using System.Collections.Generic;
using UnityEngine;

public class DynamicCarController : MonoBehaviour
{
    public enum DriveType { AllWheelDrive, FrontWheelDrive, RearWheelDrive }

    public enum WheelPosition
    {
        FrontLeft,
        FrontRight,
        RearLeft,
        RearRight
    }

    [System.Serializable]
    public struct Wheel
    {
        public string wheelName;
        public WheelCollider wheelCollider;
        public GameObject wheelModel;
        public WheelPosition position; // 👈 updated
        public float health;
        public ParticleSystem sandParticles;
    }

    [Header("Drive Settings")]
    public DriveType driveMode = DriveType.AllWheelDrive;
    public List<Wheel> wheels;

    [Header("Physics Constants")]
    public float baseMotorTorque = 1500f;
    public float maxSteerAngle = 30f;
    public float brakeForce = 3000f;
    public float scrapingFriction = 500f;

    private Rigidbody carRb;
    private CarSettings carSettings;

    void Start()
    {
        carRb = GetComponent<Rigidbody>();
        carSettings = CarSettings.Instance;

        if (carSettings == null)
            Debug.LogWarning("CarSettings Singleton missing! Using fallback defaults.");

        carRb.centerOfMass = new Vector3(0, -1.0f, 0);
    }

    void FixedUpdate()
    {
        if (carSettings != null)
        {
            Vector3 targetCoM = new Vector3(0, -carSettings.stabilityExtra, 0.1f);
            carRb.centerOfMass = Vector3.Lerp(carRb.centerOfMass, targetCoM, Time.fixedDeltaTime * 3f);
        }

        HandlePhysics();
        ApplyAirDrag();
        ApplyScrapingDrag();
    }

    void HandlePhysics()
    {
        float moveInput = Input.GetAxis("Vertical");
        float steerInput = Input.GetAxis("Horizontal");
        bool isBraking = Input.GetKey(KeyCode.Space);

        float accelMultiplier = carSettings != null ? carSettings.globalAccelerationMultiplier : 1f;
        float suspHeight = carSettings != null ? carSettings.suspensionHeight : 0.2f;

        for (int i = 0; i < wheels.Count; i++)
        {
            if (wheels[i].wheelCollider == null) continue;

            // ✅ Steering (ONLY front wheels)
            if (wheels[i].position == WheelPosition.FrontLeft ||
                wheels[i].position == WheelPosition.FrontRight)
            {
                wheels[i].wheelCollider.steerAngle = steerInput * maxSteerAngle;
            }

            // ✅ Drive Power
            bool isPowered = false;

            if (driveMode == DriveType.AllWheelDrive)
                isPowered = true;
            else if (driveMode == DriveType.FrontWheelDrive &&
                (wheels[i].position == WheelPosition.FrontLeft || wheels[i].position == WheelPosition.FrontRight))
                isPowered = true;
            else if (driveMode == DriveType.RearWheelDrive &&
                (wheels[i].position == WheelPosition.RearLeft || wheels[i].position == WheelPosition.RearRight))
                isPowered = true;

            if (isPowered)
                wheels[i].wheelCollider.motorTorque = moveInput * baseMotorTorque * accelMultiplier;
            else
                wheels[i].wheelCollider.motorTorque = 0;

            // ✅ Brake (same as before)
            wheels[i].wheelCollider.brakeTorque = isBraking ? brakeForce : 0;

            // Suspension
            var spring = wheels[i].wheelCollider.suspensionSpring;
            spring.targetPosition = suspHeight;
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

    public void ApplyDamageToWheel(string targetName, float amount)
    {
        for (int i = 0; i < wheels.Count; i++)
        {
            if (wheels[i].wheelName == targetName)
            {
                Wheel wheel = wheels[i];
                wheel.health -= amount;
                wheels[i] = wheel;
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

                if (wheel.wheelCollider == null)
                    return;

                try
                {
                    if (wheel.wheelCollider.gameObject != null && !string.IsNullOrEmpty(wheel.wheelName))
                        wheel.wheelCollider.gameObject.name = wheel.wheelName + "_Collider";
                }
                catch { }

                wheel.wheelCollider.enabled = false;
                wheel.wheelCollider.transform.SetParent(this.transform);
                wheel.wheelCollider.gameObject.SetActive(false);

                if (wheel.wheelModel != null)
                {
                    GameObject meshObj = wheel.wheelModel;

                    try
                    {
                        if (!string.IsNullOrEmpty(wheel.wheelName))
                            meshObj.name = wheel.wheelName + "_Model";
                    }
                    catch { }

                    Vector3 launchVelocity = carRb != null ? carRb.velocity : Vector3.zero;

                    meshObj.transform.SetParent(null);

                    Rigidbody tireRb = meshObj.GetComponent<Rigidbody>();
                    if (tireRb == null)
                        tireRb = meshObj.AddComponent<Rigidbody>();

                    tireRb.isKinematic = false;
                    tireRb.useGravity = true;
                    tireRb.mass = 20f;
                    tireRb.velocity = launchVelocity;

                    MeshCollider tireCol = meshObj.GetComponent<MeshCollider>();
                    if (tireCol == null)
                        tireCol = meshObj.AddComponent<MeshCollider>();

                    tireCol.convex = true;

                    Collider carCol = GetComponent<Collider>();
                    if (carCol != null && tireCol != null)
                        Physics.IgnoreCollision(tireCol, carCol);

                    // 👉 Side kick based on LEFT/RIGHT
                    Vector3 sideDir = (wheel.position == WheelPosition.FrontLeft || wheel.position == WheelPosition.RearLeft)
                        ? -transform.right
                        : transform.right;

                    tireRb.AddForce(sideDir * 5f, ForceMode.Impulse);
                }

                wheel.wheelCollider = null;
                wheels[i] = wheel;

                Debug.Log($"<color=cyan>{targetName} disconnected successfully.</color>");
                return;
            }
        }
    }

    void ApplyScrapingDrag()
    {
        foreach (var wheel in wheels)
        {
            if (wheel.wheelCollider == null)
            {
                Vector3 offset;

                if (wheel.position == WheelPosition.FrontLeft)
                    offset = new Vector3(-0.8f, 0, 1.5f);
                else if (wheel.position == WheelPosition.FrontRight)
                    offset = new Vector3(0.8f, 0, 1.5f);
                else if (wheel.position == WheelPosition.RearLeft)
                    offset = new Vector3(-0.8f, 0, -1.5f);
                else
                    offset = new Vector3(0.8f, 0, -1.5f);

                Vector3 dragPos = transform.TransformPoint(offset);

                carRb.AddForceAtPosition(Vector3.down * 1000f, dragPos);
                Vector3 dragDir = -carRb.velocity.normalized;
                carRb.AddForceAtPosition(dragDir * (carRb.velocity.magnitude * scrapingFriction), dragPos);
            }
        }
    }

    void ApplyAirDrag()
    {
        float airDamp = carSettings != null ? carSettings.airDamping : 0.5f;
        float speed = carRb.velocity.magnitude;
        Vector3 dragForce = -carRb.velocity * (speed * airDamp);
        carRb.AddForce(dragForce);
    }
}