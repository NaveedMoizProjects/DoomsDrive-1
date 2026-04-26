using UnityEngine;
using System.Collections.Generic;

public class DynamicCarAI : MonoBehaviour
{
    public enum DriveType { AllWheelDrive, FrontWheelDrive, RearWheelDrive }
    public enum Axel { Front, Rear }

    [System.Serializable]
    public struct Wheel
    {
        public string wheelName;
        public WheelCollider wheelCollider;
        public GameObject wheelModel;
        public Axel axel;
    }

    [Header("Drive Settings")]
    public DriveType driveMode = DriveType.AllWheelDrive;
    public List<Wheel> wheels;

    [Header("AI Waypoints")]
    public Transform[] waypoints;
    public float waypointRadius = 10f;

    [Header("Car Performance")]
    public float motorTorque = 1500f;
    public float maxSteerAngle = 30f;
    public float brakeForce = 3000f;
    public float maxSpeed = 120f;
    public float slowDownAngle = 50f;

    [Header("Wheel Visual Smoothing")]
    [Tooltip("How fast the wheel visual position follows suspension. Lower = smoother but laggy")]
    public float suspensionSmoothSpeed = 20f;

    private Rigidbody carRb;
    private int currentWaypoint;
    private bool isFinished = false;

    // Store individual wheel rotation angles for smooth spinning
    private float[] wheelRotationAngles;

    void Start()
    {
        carRb = GetComponent<Rigidbody>();
        carRb.centerOfMass = new Vector3(0, -1f, 0);

        // Initialize rotation trackers for each wheel
        wheelRotationAngles = new float[wheels.Count];
        for (int i = 0; i < wheelRotationAngles.Length; i++)
            wheelRotationAngles[i] = 0f;
    }

    void FixedUpdate()
    {
        if (waypoints.Length == 0) return;

        if (isFinished)
        {
            ApplyToWheels(0, 0, brakeForce);
            return;
        }

        Transform target = waypoints[currentWaypoint];
        Vector3 direction = target.position - transform.position;
        direction.y = 0f;
        float distance = direction.magnitude;
        direction.Normalize();

        float angle = Vector3.SignedAngle(transform.forward, direction, Vector3.up);
        float steerInput = Mathf.Clamp(angle / 45f, -1f, 1f);
        float speed = carRb.velocity.magnitude * 3.6f; // km/h

        float targetTorque = motorTorque;

        if (Mathf.Abs(angle) > slowDownAngle)
            targetTorque *= 0.5f;

        if (speed > maxSpeed)
            targetTorque = 0;

        ApplyToWheels(steerInput, targetTorque, 0f);

        if (distance < waypointRadius)
        {
            if (currentWaypoint < waypoints.Length - 1)
                currentWaypoint++;
            else
                isFinished = true;
        }
    }

    void Update()
    {
        // Visual update runs in Update (not FixedUpdate) for smoother rendering
        UpdateAllWheelVisuals();
    }

    void ApplyToWheels(float steerInput, float torque, float brake)
    {
        for (int i = 0; i < wheels.Count; i++)
        {
            if (wheels[i].wheelCollider == null) continue;

            if (wheels[i].axel == Axel.Front)
                wheels[i].wheelCollider.steerAngle = steerInput * maxSteerAngle;

            bool isPowered = false;
            if (driveMode == DriveType.AllWheelDrive) isPowered = true;
            else if (driveMode == DriveType.FrontWheelDrive && wheels[i].axel == Axel.Front) isPowered = true;
            else if (driveMode == DriveType.RearWheelDrive && wheels[i].axel == Axel.Rear) isPowered = true;

            wheels[i].wheelCollider.motorTorque = isPowered ? torque : 0f;
            wheels[i].wheelCollider.brakeTorque = brake;
        }
    }

    void UpdateAllWheelVisuals()
    {
        for (int i = 0; i < wheels.Count; i++)
        {
            if (wheels[i].wheelModel == null || wheels[i].wheelCollider == null) continue;
            UpdateWheelVisual(wheels[i], i);
        }
    }

    void UpdateWheelVisual(Wheel wheel, int index)
    {
        WheelCollider col = wheel.wheelCollider;
        GameObject model = wheel.wheelModel;

        // --- POSITION: Follow suspension travel smoothly ---
        // Get the collider's world pose (this has the correct suspension position)
        Vector3 colliderWorldPos;
        Quaternion colliderWorldRot;
        col.GetWorldPose(out colliderWorldPos, out colliderWorldRot);

        // Smoothly interpolate position to prevent jitter
        model.transform.position = Vector3.Lerp(
            model.transform.position,
            colliderWorldPos,
            Time.deltaTime * suspensionSmoothSpeed
        );

        // --- ROTATION: Calculate manually for clean spin & steer ---
        // Step 1: Accumulate spin rotation from wheel's RPM
        float rpm = col.rpm;
        float degreesPerSecond = rpm * 6f; // 360 degrees / 60 seconds = 6
        wheelRotationAngles[index] += degreesPerSecond * Time.deltaTime;
        wheelRotationAngles[index] = Mathf.Repeat(wheelRotationAngles[index], 360f); // Keep in 0-360

        // Step 2: Get steer angle
        float steerAngle = col.steerAngle;

        // Step 3: Build rotation — parent space:
        // Start from the car's rotation, add steering on Y, then add spin on X
        Quaternion steerRot = Quaternion.Euler(0f, steerAngle, 0f);
        Quaternion spinRot = Quaternion.Euler(wheelRotationAngles[index], 0f, 0f);

        model.transform.rotation = transform.rotation * steerRot * spinRot;
    }
}