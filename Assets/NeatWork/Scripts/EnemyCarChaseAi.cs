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

    void OnEnable()
    {
        // Subscribe if RespawnManager exists now
        if (RespawnManager.Instance != null)
            RespawnManager.Instance.OnPlayerSpawned += OnPlayerSpawned;
    }

    void OnDisable()
    {
        if (RespawnManager.Instance != null)
            RespawnManager.Instance.OnPlayerSpawned -= OnPlayerSpawned;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.8f, 0);

        // Try to acquire player at start
        LocatePlayer();
    }

    void FixedUpdate()
    {
        // If we don't have a player, try to find one (covers startup ordering / missed events)
        if (playerCar == null)
        {
            LocatePlayer();
            // still no player -> nothing to do this frame
            if (playerCar == null) return;
        }

        ChasePlayer();
    }

    void OnPlayerSpawned(GameObject newPlayer)
    {
        if (newPlayer != null)
        {
            playerCar = newPlayer.transform;
            Debug.Log($"EnemyCarChaseAI: assigned player from RespawnManager: {newPlayer.name}");
        }
    }

    void LocatePlayer()
    {
        // Prefer RespawnManager's currentCar if available
        if (RespawnManager.Instance != null && RespawnManager.Instance.currentCar != null)
        {
            playerCar = RespawnManager.Instance.currentCar.transform;
            Debug.Log("EnemyCarChaseAI: located player via RespawnManager.currentCar");
            // Ensure subscription (in case Awake order prevented OnEnable subscription)
            RespawnManager.Instance.OnPlayerSpawned -= OnPlayerSpawned;
            RespawnManager.Instance.OnPlayerSpawned += OnPlayerSpawned;
            return;
        }

        // Fallback: find by tag
        GameObject found = GameObject.FindWithTag("Player");
        if (found != null)
        {
            playerCar = found.transform;
            Debug.Log($"EnemyCarChaseAI: located player by tag: {found.name}");
            return;
        }

        // Not found yet — will try again next FixedUpdate
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