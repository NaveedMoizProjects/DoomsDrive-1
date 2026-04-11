using UnityEngine;

/// <summary>
/// Enemy Car Chase AI with multi-ray obstacle avoidance.
/// Smoothly follows the player while steering around rocks, terrain bumps,
/// and other obstacles using a fan of forward-facing raycasts.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyCarChaseAI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  INSPECTOR SETTINGS
    // ─────────────────────────────────────────────────────────────

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
    public float maxSpeed = 40f;   // m/s

    [Header("Distance Settings")]
    public float followDistance = 8f;   // Stop closing in below this
    public float slowDistance = 12f;  // Start slowing below this

    [Header("Obstacle Avoidance")]
    [Tooltip("How far ahead each ray probes for obstacles")]
    public float raycastDistance = 10f;

    [Tooltip("Number of rays in the fan (odd number recommended)")]
    public int rayCount = 7;

    [Tooltip("Total spread angle of the ray fan in degrees")]
    public float raySpreadAngle = 90f;

    [Tooltip("Vertical offset for ray origin (above car pivot)")]
    public float rayOriginHeight = 0.5f;

    [Tooltip("Layers considered as obstacles (exclude Player layer)")]
    public LayerMask obstacleMask = ~0;   // All layers by default

    [Header("Avoidance Tuning")]
    [Tooltip("How strongly obstacle hits steer the car away")]
    public float avoidanceStrength = 1.8f;

    [Tooltip("Smooth time for steering interpolation")]
    public float steerSmoothTime = 0.12f;

    [Tooltip("Extra torque applied when actively avoiding")]
    public float avoidanceMotorBoost = 1.2f;

    // ─────────────────────────────────────────────────────────────
    //  PRIVATE STATE
    // ─────────────────────────────────────────────────────────────

    private Rigidbody rb;

    // Smoothed steer angle (SmoothDamp target)
    private float currentSteer;
    private float steerVelocity;    // used by SmoothDamp

    // True when any ray hit an obstacle last frame
    private bool isAvoiding;

    // Cached ray directions (local space), computed once
    private Vector3[] localRayDirs;

    // ─────────────────────────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        BuildRayDirections();
    }

    void OnEnable()
    {
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

        LocatePlayer();
    }

    void FixedUpdate()
    {
        if (playerCar == null)
        {
            LocatePlayer();
            if (playerCar == null) return;
        }

        float avoidSteer = ComputeAvoidanceSteering();
        float chaseSteering = ComputeChaseSteer();

        // Blend: when avoiding, avoidance wins; otherwise pure chase
        float rawTarget = isAvoiding
            ? Mathf.Clamp(avoidSteer, -maxSteerAngle, maxSteerAngle)
            : Mathf.Clamp(chaseSteering, -maxSteerAngle, maxSteerAngle);

        // Smooth the steer to prevent snappy oscillation
        currentSteer = Mathf.SmoothDamp(
            currentSteer, rawTarget,
            ref steerVelocity, steerSmoothTime);

        frontLeft.steerAngle = currentSteer;
        frontRight.steerAngle = currentSteer;

        DriveMotor();
    }

    // ─────────────────────────────────────────────────────────────
    //  OBSTACLE AVOIDANCE
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Casts a fan of rays in front of the car.
    /// Returns a weighted steer angle that pushes away from hits.
    /// </summary>
    float ComputeAvoidanceSteering()
    {
        Vector3 origin = transform.position + Vector3.up * rayOriginHeight;
        float weightedSteer = 0f;
        int hitCount = 0;

        for (int i = 0; i < rayCount; i++)
        {
            // Local direction → world direction
            Vector3 worldDir = transform.TransformDirection(localRayDirs[i]);

            if (Physics.Raycast(origin, worldDir, out RaycastHit hit,
                                raycastDistance, obstacleMask,
                                QueryTriggerInteraction.Ignore))
            {
                // Skip if we hit our own car or the player
                if (hit.collider.transform.IsChildOf(transform)) continue;
                if (playerCar != null && hit.collider.transform.IsChildOf(playerCar)) continue;

                // Proximity factor: closer = stronger response
                float proximity = 1f - (hit.distance / raycastDistance);

                // Ray signed angle: negative = left ray, positive = right ray
                float rayAngle = localRayDirs[i].x;   // x component encodes left/right

                // Steer AWAY from the side the ray hit
                weightedSteer -= rayAngle * proximity * avoidanceStrength * maxSteerAngle;
                hitCount++;

                // Debug visualisation (editor only)
                Debug.DrawRay(origin, worldDir * hit.distance, Color.red);
            }
            else
            {
                Debug.DrawRay(origin, worldDir * raycastDistance, Color.green);
            }
        }

        isAvoiding = hitCount > 0;

        if (hitCount == 0) return 0f;

        return Mathf.Clamp(weightedSteer / hitCount, -maxSteerAngle, maxSteerAngle);
    }

    /// <summary>
    /// Returns the raw steer angle needed to face the player.
    /// </summary>
    float ComputeChaseSteer()
    {
        Vector3 localTarget = transform.InverseTransformPoint(playerCar.position);
        float magnitude = localTarget.magnitude;

        if (magnitude < 0.001f) return 0f;

        return (localTarget.x / magnitude) * maxSteerAngle;
    }

    // ─────────────────────────────────────────────────────────────
    //  MOTOR / BRAKING
    // ─────────────────────────────────────────────────────────────

    void DriveMotor()
    {
        float distance = Vector3.Distance(transform.position, playerCar.position);
        float speedMs = rb.velocity.magnitude;
        float torqueScale = isAvoiding ? avoidanceMotorBoost : 1f;

        if (speedMs >= maxSpeed)
        {
            // Hit the speed cap
            ApplyMotor(0f);
            return;
        }

        if (distance > slowDistance)
        {
            ApplyMotor(motorTorque * torqueScale);
        }
        else if (distance > followDistance)
        {
            // Gradual slow-down in the approach zone
            float t = (distance - followDistance) / (slowDistance - followDistance);
            ApplyMotor(motorTorque * Mathf.Lerp(0.2f, 0.6f, t) * torqueScale);
        }
        else
        {
            // Inside follow bubble — hold position
            ApplyMotor(0f);
            ApplyBrake(1500f);
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  PLAYER LOCATION / RESPAWN
    // ─────────────────────────────────────────────────────────────

    void OnPlayerSpawned(GameObject newPlayer)
    {
        if (newPlayer != null)
        {
            playerCar = newPlayer.transform;
            Debug.Log($"EnemyCarChaseAI: player assigned from RespawnManager → {newPlayer.name}");
        }
    }

    void LocatePlayer()
    {
        if (RespawnManager.Instance != null && RespawnManager.Instance.currentCar != null)
        {
            playerCar = RespawnManager.Instance.currentCar.transform;
            RespawnManager.Instance.OnPlayerSpawned -= OnPlayerSpawned;
            RespawnManager.Instance.OnPlayerSpawned += OnPlayerSpawned;
            return;
        }

        GameObject found = GameObject.FindWithTag("Player");
        if (found != null)
            playerCar = found.transform;
    }

    // ─────────────────────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Pre-computes evenly spread ray directions in local car space.
    /// Rays are angled slightly downward to detect ground-level obstacles.
    /// </summary>
    void BuildRayDirections()
    {
        localRayDirs = new Vector3[rayCount];
        float halfSpread = raySpreadAngle * 0.5f;

        for (int i = 0; i < rayCount; i++)
        {
            float t = rayCount > 1 ? (float)i / (rayCount - 1) : 0.5f;
            float horizontal = Mathf.Lerp(-halfSpread, halfSpread, t);

            // Tilt slightly downward (-5°) so rays skim terrain bumps
            Quaternion rot = Quaternion.Euler(-5f, horizontal, 0f);
            localRayDirs[i] = rot * Vector3.forward;
        }
    }

    void ApplyMotor(float torque)
    {
        rearLeft.motorTorque = torque;
        rearRight.motorTorque = torque;
        rearLeft.brakeTorque = 0f;
        rearRight.brakeTorque = 0f;
    }

    void ApplyBrake(float brake)
    {
        rearLeft.brakeTorque = brake;
        rearRight.brakeTorque = brake;
        rearLeft.motorTorque = 0f;
        rearRight.motorTorque = 0f;
    }

    // ─────────────────────────────────────────────────────────────
    //  GIZMOS (Scene view debug)
    // ─────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (localRayDirs == null) BuildRayDirections();

        Vector3 origin = transform.position + Vector3.up * rayOriginHeight;
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);

        foreach (Vector3 localDir in localRayDirs)
        {
            Vector3 worldDir = transform.TransformDirection(localDir);
            Gizmos.DrawRay(origin, worldDir * raycastDistance);
        }

        // Follow / slow distance rings
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, followDistance);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, slowDistance);
    }
#endif
}