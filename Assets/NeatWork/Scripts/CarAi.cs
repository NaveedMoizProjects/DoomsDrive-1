using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DynamicCarAI : MonoBehaviour
{
    public enum DriveType { AllWheelDrive, FrontWheelDrive, RearWheelDrive }
    public enum Axel { Front, Rear }
    enum DriveState { Drive, Decision, ForceForward, ForceReverse, Recover }

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
    public float waypointRadius = 15f;

    [Header("Car Performance")]
    public float motorTorque = 2200f;
    public float maxSteerAngle = 35f;
    public float brakeForce = 4000f;
    public float maxSpeedKmh = 60f;
    public float accelerationSmoothing = 0.25f;

    [Header("Stuck Timing Controls")]
    [Tooltip("How slow the car must be to be considered 'stuck'")]
    public float stuckSpeedThreshold = 0.8f;
    [Tooltip("Initial time stuck before the AI starts the recovery sequence")]
    public float stuckTimeLimit = 2.0f;
    [Tooltip("The 1-second 'mood' pause before taking action")]
    public float decisionWaitTime = 1.0f;
    [Tooltip("Time spent forcing the car forward to overcome obstacles")]
    public float forceForwardTime = 5.0f;
    [Tooltip("Time spent forcing the car in reverse if forward failed")]
    public float forceReverseTime = 4.0f;

    [Header("Safe Spot Memory (Track Bound)")]
    public string trackTag = "Track";
    public float recordInterval = 0.5f;
    private Vector3 lastSafePos;
    private Quaternion lastSafeRot;
    private float recordTimer = 0f;
    private bool isOnTrack = true;

    [Header("Recovery Settings")]
    public float rbReactivateDelay = 1.5f;
    public float sideRayDistance = 10f;
    public float rayOriginHeight = 0.8f;
    public LayerMask obstacleMask = ~0;

    Rigidbody carRb;
    int currentWaypoint;
    DriveState state = DriveState.Drive;

    float stuckTimer;
    float stateTimer;
    float reverseSteerDir;
    float currentTorqueVelocity;

    void Start()
    {
        carRb = GetComponent<Rigidbody>();
        carRb.centerOfMass = new Vector3(0, -0.8f, 0);
        lastSafePos = transform.position;
        lastSafeRot = transform.rotation;
    }

    void FixedUpdate()
    {
        if (waypoints.Length == 0) return;

        UpdateTrackMemory();
        UpdateStuckDetection();
        RunStateMachine();
    }

    void Update() => UpdateAllWheelVisuals();

    void UpdateTrackMemory()
    {
        if (isOnTrack)
        {
            recordTimer += Time.fixedDeltaTime;
            if (recordTimer >= recordInterval)
            {
                lastSafePos = transform.position;
                lastSafeRot = transform.rotation;
                recordTimer = 0f;
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(trackTag)) isOnTrack = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(trackTag)) isOnTrack = false;
    }

    void RunStateMachine()
    {
        switch (state)
        {
            case DriveState.Drive: StateDrive(); break;
            case DriveState.Decision: StateDecision(); break;
            case DriveState.ForceForward: StateForceForward(); break;
            case DriveState.ForceReverse: StateForceReverse(); break;
            case DriveState.Recover: StateRecover(); break;
        }
    }

    void StateDrive()
    {
        Transform target = waypoints[currentWaypoint];
        Vector3 direction = (target.position - transform.position);
        direction.y = 0f;
        float distance = direction.magnitude;
        direction.Normalize();

        float angle = Vector3.SignedAngle(transform.forward, direction, Vector3.up);
        float steerInput = Mathf.Clamp(angle / maxSteerAngle, -1f, 1f);

        float smoothedTorque = Mathf.SmoothDamp(carRb.velocity.magnitude > 0.5f ? motorTorque : 0, motorTorque, ref currentTorqueVelocity, accelerationSmoothing);
        ApplyToWheels(steerInput, smoothedTorque, 0);

        if (distance < waypointRadius) currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
    }

    void StateDecision()
    {
        ApplyToWheels(0, 0, brakeForce * 0.5f);
        stateTimer -= Time.fixedDeltaTime;
        if (stateTimer <= 0) TransitionTo(DriveState.ForceForward);
    }

    void StateForceForward()
    {
        stateTimer -= Time.fixedDeltaTime;
        // Exit if we successfully break free early
        if (carRb.velocity.magnitude > stuckSpeedThreshold + 1.5f)
        {
            TransitionTo(DriveState.Drive);
            return;
        }
        ApplyToWheels(0, motorTorque * 1.5f, 0);
        if (stateTimer <= 0) TransitionTo(DriveState.ForceReverse);
    }

    void StateForceReverse()
    {
        stateTimer -= Time.fixedDeltaTime;
        // Exit if we gain good reverse speed
        if (carRb.velocity.magnitude > 3.0f && stateTimer < forceReverseTime * 0.5f)
        {
            TransitionTo(DriveState.Recover);
            return;
        }
        ApplyReverseToWheels(-reverseSteerDir, motorTorque);
        if (stateTimer <= 0) TransitionTo(DriveState.Recover);
    }

    void StateRecover()
    {
        stateTimer -= Time.fixedDeltaTime;
        // FINAL SAFETY: If after all that time we are still stuck, warp to track.
        if (stateTimer <= 0 && carRb.velocity.magnitude < stuckSpeedThreshold)
        {
            PerformPlacementRecovery();
            return;
        }

        ApplyToWheels(reverseSteerDir, motorTorque * 0.8f, 0f);
        if (carRb.velocity.magnitude > 2.5f) TransitionTo(DriveState.Drive);
    }

    void PerformPlacementRecovery()
    {
        carRb.velocity = Vector3.zero;
        carRb.angularVelocity = Vector3.zero;

        transform.position = lastSafePos + Vector3.up * 1.5f;
        transform.rotation = lastSafeRot;

        StartCoroutine(TempKinematic());
        TransitionTo(DriveState.Drive);
    }

    IEnumerator TempKinematic()
    {
        carRb.isKinematic = true;
        yield return new WaitForSeconds(rbReactivateDelay);
        carRb.isKinematic = false;
    }

    void TransitionTo(DriveState next)
    {
        if (state == next) return;
        if (next == DriveState.Decision) stateTimer = decisionWaitTime;
        if (next == DriveState.ForceForward) stateTimer = forceForwardTime;
        if (next == DriveState.ForceReverse)
        {
            stateTimer = forceReverseTime;
            reverseSteerDir = ChooseClearSide();
        }
        if (next == DriveState.Recover) stateTimer = 2.5f;
        state = next;
        stuckTimer = 0f;
    }

    void UpdateStuckDetection()
    {
        if (state != DriveState.Drive) return;
        if (carRb.velocity.magnitude < stuckSpeedThreshold)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer >= stuckTimeLimit) TransitionTo(DriveState.Decision);
        }
        else
        {
            stuckTimer = 0f;
        }
    }

    float ChooseClearSide()
    {
        Vector3 origin = transform.position + Vector3.up * rayOriginHeight;
        float leftSum = 0, rightSum = 0;
        float[] angles = { 20f, 45f, 70f };
        foreach (float a in angles)
        {
            Vector3 lDir = transform.rotation * Quaternion.Euler(0, -a, 0) * Vector3.forward;
            Vector3 rDir = transform.rotation * Quaternion.Euler(0, a, 0) * Vector3.forward;
            leftSum += Physics.Raycast(origin, lDir, out RaycastHit lh, sideRayDistance, obstacleMask) ? lh.distance : sideRayDistance;
            rightSum += Physics.Raycast(origin, rDir, out RaycastHit rh, sideRayDistance, obstacleMask) ? rh.distance : sideRayDistance;
        }
        return (rightSum >= leftSum) ? 1f : -1f;
    }

    void ApplyToWheels(float steer, float torque, float brake)
    {
        foreach (var w in wheels)
        {
            if (w.wheelCollider == null) continue;
            if (w.axel == Axel.Front) w.wheelCollider.steerAngle = steer * maxSteerAngle;
            bool isPowered = driveMode == DriveType.AllWheelDrive || (driveMode == DriveType.FrontWheelDrive && w.axel == Axel.Front) || (driveMode == DriveType.RearWheelDrive && w.axel == Axel.Rear);
            w.wheelCollider.motorTorque = isPowered ? torque : 0f;
            w.wheelCollider.brakeTorque = brake;
        }
    }

    void ApplyReverseToWheels(float steer, float torque)
    {
        foreach (var w in wheels)
        {
            if (w.wheelCollider == null) continue;
            if (w.axel == Axel.Front) w.wheelCollider.steerAngle = steer * maxSteerAngle;
            w.wheelCollider.motorTorque = -torque;
            w.wheelCollider.brakeTorque = 0f;
        }
    }

    void UpdateAllWheelVisuals()
    {
        foreach (var w in wheels)
        {
            if (w.wheelModel == null || w.wheelCollider == null) continue;
            w.wheelCollider.GetWorldPose(out Vector3 pos, out Quaternion rot);
            w.wheelModel.transform.position = pos;
            w.wheelModel.transform.rotation = rot;
        }
    }
}