using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyCarChaseAI : MonoBehaviour
{
    [Header("Target")]
    public Transform playerCar;

    [Header("Wheel Colliders")]
    public WheelCollider frontLeft;
    public WheelCollider frontRight;
    public WheelCollider rearLeft;
    public WheelCollider rearRight;

    [Header("Chase Settings")]
    public float motorTorque = 2000f;
    public float maxSteerAngle = 30f;
    public float maxSpeed = 40f;

    [Header("Distance Settings")]
    public float followDistance = 8f;   // AI kitna distance maintain kare
    public float slowDistance = 12f;    // is distance par slow hona start kare

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.8f, 0);
    }

    void FixedUpdate()
    {
        if (playerCar == null) return;

        ChasePlayer();
    }

    void ChasePlayer()
    {
        float distance = Vector3.Distance(transform.position, playerCar.position);

        Vector3 localTarget = transform.InverseTransformPoint(playerCar.position);

        float steer = (localTarget.x / localTarget.magnitude) * maxSteerAngle;

        frontLeft.steerAngle = steer;
        frontRight.steerAngle = steer;

        // ----- SPEED CONTROL -----

        if (distance > slowDistance)
        {
            // Full speed chase
            ApplyMotor(motorTorque);
        }
        else if (distance > followDistance)
        {
            // Slow chase
            ApplyMotor(motorTorque * 0.4f);
        }
        else
        {
            // Maintain distance
            ApplyMotor(0);
            ApplyBrake(1500f);
        }

        // Max speed limiter
        if (rb.velocity.magnitude > maxSpeed)
        {
            ApplyMotor(0);
        }
    }

    void ApplyMotor(float torque)
    {
        rearLeft.motorTorque = torque;
        rearRight.motorTorque = torque;

        rearLeft.brakeTorque = 0;
        rearRight.brakeTorque = 0;
    }

    void ApplyBrake(float brake)
    {
        rearLeft.brakeTorque = brake;
        rearRight.brakeTorque = brake;

        rearLeft.motorTorque = 0;
        rearRight.motorTorque = 0;
    }
}