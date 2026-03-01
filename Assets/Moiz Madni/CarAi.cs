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

    private Rigidbody carRb;
    private int currentWaypoint;
    private bool isFinished = false; // Flag to stop the car

    void Start()
    {
        carRb = GetComponent<Rigidbody>();
        carRb.centerOfMass = new Vector3(0, -1f, 0);
    }

    void FixedUpdate()
    {
        if (waypoints.Length == 0) return;

        // If we reached the end, apply brakes and stop processing
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

        // Slow down on sharp turns
        if (Mathf.Abs(angle) > slowDownAngle)
        {
            targetTorque *= 0.5f;
        }

        // Speed limit
        if (speed > maxSpeed)
        {
            targetTorque = 0;
        }

        ApplyToWheels(steerInput, targetTorque, 0f);

        if (distance < waypointRadius)
        {
            // Check if there are more waypoints
            if (currentWaypoint < waypoints.Length - 1)
            {
                currentWaypoint++;
            }
            else
            {
                // Reached the last one
                isFinished = true;
            }
        }
    }

    // Added 'brake' parameter to apply the brakeForce when finished
    void ApplyToWheels(float steerInput, float torque, float brake)
    {
        for (int i = 0; i < wheels.Count; i++)
        {
            if (wheels[i].wheelCollider == null) continue;

            // Steering
            if (wheels[i].axel == Axel.Front)
            {
                wheels[i].wheelCollider.steerAngle = steerInput * maxSteerAngle;
            }

            // Drive type logic
            bool isPowered = false;

            if (driveMode == DriveType.AllWheelDrive) isPowered = true;
            else if (driveMode == DriveType.FrontWheelDrive && wheels[i].axel == Axel.Front) isPowered = true;
            else if (driveMode == DriveType.RearWheelDrive && wheels[i].axel == Axel.Rear) isPowered = true;

            wheels[i].wheelCollider.motorTorque = isPowered ? torque : 0f;
            wheels[i].wheelCollider.brakeTorque = brake; // Uses the brake parameter

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
}