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
    public float motorTorque = 2500f;
    public float maxSteerAngle = 30f;
    public float maxSpeed = 45f;

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
        Vector3 localTarget = transform.InverseTransformPoint(playerCar.position);

        float steer = (localTarget.x / localTarget.magnitude) * maxSteerAngle;

        frontLeft.steerAngle = steer;
        frontRight.steerAngle = steer;

        if (rb.velocity.magnitude < maxSpeed)
        {
            rearLeft.motorTorque = motorTorque;
            rearRight.motorTorque = motorTorque;
        }
        else
        {
            rearLeft.motorTorque = 0;
            rearRight.motorTorque = 0;
        }
    }
}