using UnityEngine;
using System.Collections.Generic;

public class DynamicCarController : MonoBehaviour
{
    // ... Enums & Structs remain the same ...
    public enum DriveType { AllWheelDrive, FrontWheelDrive, RearWheelDrive }
    public enum Axel { Front, Rear }

    [System.Serializable]
    public struct Wheel
    {
        public string wheelName;
        public WheelCollider wheelCollider;
        public GameObject wheelModel;
        public Axel axel;
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

    void Start()
    {
        carRb = GetComponent<Rigidbody>();
        if (CarSettings.Instance == null)
            Debug.LogError("CarSettings Singleton missing!");

        carRb.centerOfMass = new Vector3(0, -1.0f, 0);
    }

    void FixedUpdate()
    {
        if (CarSettings.Instance != null)
        {
            Vector3 targetCoM = new Vector3(0, -CarSettings.Instance.stabilityExtra, 0.1f);
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

        for (int i = 0; i < wheels.Count; i++)
        {
            // CRITICAL: Stop all logic if collider is null
            if (wheels[i].wheelCollider == null) continue;

            // Steering
            if (wheels[i].axel == Axel.Front)
                wheels[i].wheelCollider.steerAngle = steerInput * maxSteerAngle;

            // Drive Power
            bool isPowered = false;
            if (driveMode == DriveType.AllWheelDrive) isPowered = true;
            else if (driveMode == DriveType.FrontWheelDrive && wheels[i].axel == Axel.Front) isPowered = true;
            else if (driveMode == DriveType.RearWheelDrive && wheels[i].axel == Axel.Rear) isPowered = true;

            if (isPowered)
                wheels[i].wheelCollider.motorTorque = moveInput * baseMotorTorque * CarSettings.Instance.globalAccelerationMultiplier;
            else
                wheels[i].wheelCollider.motorTorque = 0;

            wheels[i].wheelCollider.brakeTorque = isBraking ? brakeForce : 0;

            // Suspension
            var spring = wheels[i].wheelCollider.suspensionSpring;
            spring.targetPosition = CarSettings.Instance.suspensionHeight;
            wheels[i].wheelCollider.suspensionSpring = spring;

            UpdateVisuals(wheels[i]);
        }
    }

    void UpdateVisuals(Wheel wheel)
    {
        // Double check both exist before moving anything
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

                // SAFETY: If already disconnected, exit
                if (wheel.wheelCollider == null) 
                    return;

                // 2. DISABLE PHYSICS ON THE CAR
                // Disable the collider FIRST so it stops asking for a Rigidbody
                wheel.wheelCollider.enabled = false;
                wheel.wheelCollider.transform.SetParent(this.transform); // Reparent to car
                wheel.wheelCollider.gameObject.SetActive(false);

                // 3. HANDLE THE MESH
                if (wheel.wheelModel != null)
                {
                    GameObject meshObj = wheel.wheelModel;

                    // Store velocity before we break the hierarchy
                    Vector3 launchVelocity = carRb != null ? carRb.velocity : Vector3.zero;

                    meshObj.transform.SetParent(null); // Now it's a free object

                    // 4. RIGIDBODY SAFETY CHECK
                    Rigidbody tireRb = meshObj.GetComponent<Rigidbody>();
                    if (tireRb == null)
                    {
                        tireRb = meshObj.AddComponent<Rigidbody>();
                    }

                    // Now that we ARE SURE tireRb is not null, we can apply settings
                    tireRb.isKinematic = false;
                    tireRb.useGravity = true;
                    tireRb.mass = 20f;
                    tireRb.velocity = launchVelocity;

                    // 5. COLLIDER SAFETY CHECK
                    MeshCollider tireCol = meshObj.GetComponent<MeshCollider>();
                    if (tireCol == null)
                    {
                        tireCol = meshObj.AddComponent<MeshCollider>();
                    }
                    tireCol.convex = true;

                    // Ignore car body
                    Collider carCol = GetComponent<Collider>();
                    if (carCol != null && tireCol != null)
                    {
                        Physics.IgnoreCollision(tireCol, carCol);
                    }

                    // Final Kick
                    Vector3 sideDir = (wheel.axel == Axel.Front) ? transform.right : -transform.right;
                    tireRb.AddForce(sideDir * 5f, ForceMode.Impulse);
                }

                // 6. UPDATE THE LIST
                // We set the collider to null so the loop knows this wheel is "dead"
                wheel.wheelCollider = null;
                wheels[i] = wheel;

                Debug.Log($"<color=cyan>{targetName} disconnected successfully.</color>");
                return; // Breakpoint here should work now
            }
        }
    }

    void ApplyScrapingDrag()
    {
        foreach (var wheel in wheels)
        {
            // If the collider is gone but we have a "positional marker" (the empty spot)
            // Note: If wheelModel was unparented, we need to track the "Spawn Point"
            if (wheel.wheelCollider == null)
            {
                // Pull down at the corner of the car
                // (Using a generic offset since model is gone)
                Vector3 dragPos = transform.TransformPoint(wheel.axel == Axel.Front ? new Vector3(0.8f, 0, 1.5f) : new Vector3(0.8f, 0, -1.5f));
                
                carRb.AddForceAtPosition(Vector3.down * 1000f, dragPos);
                Vector3 dragDir = -carRb.velocity.normalized;
                carRb.AddForceAtPosition(dragDir * (carRb.velocity.magnitude * scrapingFriction), dragPos);
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