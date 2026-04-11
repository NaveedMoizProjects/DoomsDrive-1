using System.Collections;
using UnityEngine;

/// <summary>
/// Enemy Car Chase AI — v2
/// • Multi-ray fan obstacle avoidance
/// • Stuck detection with automatic reverse + re-steer recovery
/// • Smooth state transitions: CHASE → AVOID → REVERSE → RECOVER
/// • Realistic WheelCollider physics throughout
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyCarChaseAI : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════
    //  ENUMS
    // ═══════════════════════════════════════════════════════════════

    enum DriveState { Chase, Avoid, Reverse, Recover }

    // ═══════════════════════════════════════════════════════════════
    //  INSPECTOR
    // ═══════════════════════════════════════════════════════════════

    [Header("─── Target ───────────────────────────────────")]
    public Transform playerCar;

    [Header("─── Wheel Colliders ──────────────────────────")]
    public WheelCollider frontLeft;
    public WheelCollider frontRight;
    public WheelCollider rearLeft;
    public WheelCollider rearRight;

    [Header("─── Chase Settings ───────────────────────────")]
    public float motorTorque = 2000f;
    public float maxSteerAngle = 30f;
    public float maxSpeed = 40f;          // m/s forward cap

    [Header("─── Distance Settings ─────────────────────────")]
    public float followDistance = 8f;          // stop closing below this
    public float slowDistance = 12f;         // start slowing below this

    [Header("─── Obstacle Avoidance ─────────────────────────")]
    [Tooltip("How far ahead each ray probes")]
    public float raycastDistance = 10f;
    [Tooltip("Odd number recommended")]
    public int rayCount = 7;
    [Tooltip("Total fan spread in degrees")]
    public float raySpreadAngle = 90f;
    [Tooltip("Ray origin height above pivot")]
    public float rayOriginHeight = 0.5f;
    [Tooltip("Obstacle layers — exclude Player layer!")]
    public LayerMask obstacleMask = ~0;
    public float avoidanceStrength = 1.8f;
    public float steerSmoothTime = 0.12f;
    public float avoidanceMotorBoost = 1.2f;

    [Header("─── Stuck / Reverse Recovery ──────────────────")]
    [Tooltip("Speed (m/s) below which the car is considered possibly stuck")]
    public float stuckSpeedThreshold = 0.5f;
    [Tooltip("Seconds below stuckSpeedThreshold before triggering reverse")]
    public float stuckTimeLimit = 1.8f;
    [Tooltip("How long the car reverses")]
    public float reverseDuration = 1.4f;
    [Tooltip("How long the car steers away before resuming chase")]
    public float recoverDuration = 0.8f;
    [Tooltip("Torque used while reversing")]
    public float reverseTorque = 1400f;
    [Tooltip("Speed cap while reversing (m/s)")]
    public float maxReverseSpeed = 12f;
    [Tooltip("Steer angle applied while reversing to escape the obstacle")]
    public float reverseSteerAngle = 25f;

    // ═══════════════════════════════════════════════════════════════
    //  PRIVATE STATE
    // ═══════════════════════════════════════════════════════════════

    Rigidbody rb;
    DriveState state = DriveState.Chase;

    // Steering
    float currentSteer;
    float steerVelocity;

    // Avoidance
    Vector3[] localRayDirs;
    bool isAvoiding;

    // Stuck detection
    float stuckTimer;
    float reverseTimer;
    float recoverTimer;
    float reverseSteerDir; // -1 left, +1 right (chosen at reverse start)

    // One-shot flag so reverse init runs once
    bool reverseStarted;

    // ═══════════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ═══════════════════════════════════════════════════════════════

    void Awake() => BuildRayDirections();

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
        if (playerCar == null) { LocatePlayer(); if (playerCar == null) return; }

        UpdateStuckDetection();
        RunStateMachine();
    }

    // ═══════════════════════════════════════════════════════════════
    //  STATE MACHINE
    // ═══════════════════════════════════════════════════════════════

    void RunStateMachine()
    {
        switch (state)
        {
            case DriveState.Chase: StateChase(); break;
            case DriveState.Avoid: StateAvoid(); break;
            case DriveState.Reverse: StateReverse(); break;
            case DriveState.Recover: StateRecover(); break;
        }
    }

    // ── CHASE ────────────────────────────────────────────────────
    void StateChase()
    {
        ComputeAvoidanceSteering(); // refresh isAvoiding

        if (isAvoiding)
        {
            TransitionTo(DriveState.Avoid);
            return;
        }

        SetSmoothedSteer(ComputeChaseSteer());
        DriveForward(1f);
    }

    // ── AVOID ────────────────────────────────────────────────────
    void StateAvoid()
    {
        float avoidSteer = ComputeAvoidanceSteering();

        if (!isAvoiding)
        {
            TransitionTo(DriveState.Chase);
            return;
        }

        // 75% avoidance + 25% chase so the car doesn't lose the player
        float blended = Mathf.Lerp(ComputeChaseSteer(), avoidSteer, 0.75f);
        SetSmoothedSteer(blended);
        DriveForward(avoidanceMotorBoost);
    }

    // ── REVERSE ──────────────────────────────────────────────────
    void StateReverse()
    {
        if (!reverseStarted)
        {
            reverseStarted = true;
            reverseTimer = reverseDuration;
            reverseSteerDir = ChooseReverseSteerDir();
        }

        reverseTimer -= Time.fixedDeltaTime;

        // Counter-steer while reversing to swing the nose away from obstacle
        SetSmoothedSteer(-reverseSteerDir * reverseSteerAngle);
        DriveReverse();

        if (reverseTimer <= 0f)
            TransitionTo(DriveState.Recover);
    }

    // ── RECOVER ──────────────────────────────────────────────────
    void StateRecover()
    {
        recoverTimer -= Time.fixedDeltaTime;

        // Drive forward with a steer bias to face clear space
        SetSmoothedSteer(reverseSteerDir * maxSteerAngle);
        DriveForward(1f);

        ComputeAvoidanceSteering(); // refresh isAvoiding

        if (recoverTimer <= 0f)
            TransitionTo(isAvoiding ? DriveState.Avoid : DriveState.Chase);
    }

    // ─── Transition helper ────────────────────────────────────────
    void TransitionTo(DriveState next)
    {
        if (state == next) return;

        if (next == DriveState.Reverse)
        {
            reverseStarted = false;
            stuckTimer = 0f;
        }
        if (next == DriveState.Recover)
        {
            recoverTimer = recoverDuration;
        }

        state = next;
        Debug.Log($"[EnemyCarAI] State → {next}");
    }

    // ═══════════════════════════════════════════════════════════════
    //  STUCK DETECTION
    // ═══════════════════════════════════════════════════════════════

    void UpdateStuckDetection()
    {
        // Only monitor while trying to move forward
        if (state == DriveState.Reverse || state == DriveState.Recover) return;

        if (rb.velocity.magnitude < stuckSpeedThreshold)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer >= stuckTimeLimit)
                TransitionTo(DriveState.Reverse);
        }
        else
        {
            // Decay quickly once moving again
            stuckTimer = Mathf.Max(0f, stuckTimer - Time.fixedDeltaTime * 2f);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  OBSTACLE AVOIDANCE  (ray fan)
    // ═══════════════════════════════════════════════════════════════

    float ComputeAvoidanceSteering()
    {
        Vector3 origin = transform.position + Vector3.up * rayOriginHeight;
        float weightedSteer = 0f;
        int hitCount = 0;

        for (int i = 0; i < rayCount; i++)
        {
            Vector3 worldDir = transform.TransformDirection(localRayDirs[i]);

            if (Physics.Raycast(origin, worldDir, out RaycastHit hit,
                                raycastDistance, obstacleMask,
                                QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.IsChildOf(transform)) continue;
                if (playerCar != null && hit.collider.transform.IsChildOf(playerCar)) continue;

                float proximity = 1f - (hit.distance / raycastDistance);
                float rayAngle = localRayDirs[i].x;
                weightedSteer -= rayAngle * proximity * avoidanceStrength * maxSteerAngle;
                hitCount++;

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
    /// Compares open space on left vs right and returns the clearer side (+1 right, -1 left).
    /// Used to pick which way to swing out during reverse.
    /// </summary>
    float ChooseReverseSteerDir()
    {
        Vector3 origin = transform.position + Vector3.up * rayOriginHeight;
        float leftDist = raycastDistance;
        float rightDist = raycastDistance;

        Vector3 leftDir = transform.TransformDirection(Quaternion.Euler(0, -45f, 0) * Vector3.forward);
        Vector3 rightDir = transform.TransformDirection(Quaternion.Euler(0, 45f, 0) * Vector3.forward);

        if (Physics.Raycast(origin, leftDir, out RaycastHit lh, raycastDistance, obstacleMask))
            leftDist = lh.distance;
        if (Physics.Raycast(origin, rightDir, out RaycastHit rh, raycastDistance, obstacleMask))
            rightDist = rh.distance;

        // Swing toward the more open side
        return rightDist >= leftDist ? 1f : -1f;
    }

    // ═══════════════════════════════════════════════════════════════
    //  STEERING
    // ═══════════════════════════════════════════════════════════════

    float ComputeChaseSteer()
    {
        Vector3 local = transform.InverseTransformPoint(playerCar.position);
        if (local.magnitude < 0.001f) return 0f;
        return (local.x / local.magnitude) * maxSteerAngle;
    }

    void SetSmoothedSteer(float target)
    {
        target = Mathf.Clamp(target, -maxSteerAngle, maxSteerAngle);
        currentSteer = Mathf.SmoothDamp(currentSteer, target, ref steerVelocity, steerSmoothTime);
        frontLeft.steerAngle = currentSteer;
        frontRight.steerAngle = currentSteer;
    }

    // ═══════════════════════════════════════════════════════════════
    //  MOTOR / BRAKING
    // ═══════════════════════════════════════════════════════════════

    void DriveForward(float torqueScale)
    {
        if (rb.velocity.magnitude >= maxSpeed) { ApplyMotorForward(0f); return; }

        float distance = Vector3.Distance(transform.position, playerCar.position);
        float t = Mathf.InverseLerp(followDistance, slowDistance, distance);

        float throttle = distance > slowDistance ? 1f
                       : distance > followDistance ? Mathf.Lerp(0.2f, 0.6f, t)
                       : 0f;

        if (throttle <= 0f) { ApplyMotorForward(0f); ApplyBrake(1500f); return; }

        ApplyMotorForward(motorTorque * throttle * torqueScale);
    }

    void DriveReverse()
    {
        float reverseSpeedMs = -Vector3.Dot(rb.velocity, transform.forward);
        if (reverseSpeedMs >= maxReverseSpeed) { ApplyMotorReverse(0f); return; }
        ApplyMotorReverse(reverseTorque);
    }

    void ApplyMotorForward(float torque)
    {
        rearLeft.motorTorque = torque;
        rearRight.motorTorque = torque;
        rearLeft.brakeTorque = 0f;
        rearRight.brakeTorque = 0f;
    }

    void ApplyMotorReverse(float torque)
    {
        rearLeft.motorTorque = -torque;
        rearRight.motorTorque = -torque;
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

    // ═══════════════════════════════════════════════════════════════
    //  PLAYER LOCATION / RESPAWN
    // ═══════════════════════════════════════════════════════════════

    void OnPlayerSpawned(GameObject newPlayer)
    {
        if (newPlayer != null) playerCar = newPlayer.transform;
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
        if (found != null) playerCar = found.transform;
    }

    // ═══════════════════════════════════════════════════════════════
    //  RAY SETUP
    // ═══════════════════════════════════════════════════════════════

    void BuildRayDirections()
    {
        localRayDirs = new Vector3[rayCount];
        float halfSpread = raySpreadAngle * 0.5f;

        for (int i = 0; i < rayCount; i++)
        {
            float t = rayCount > 1 ? (float)i / (rayCount - 1) : 0.5f;
            float horizontal = Mathf.Lerp(-halfSpread, halfSpread, t);
            Quaternion rot = Quaternion.Euler(-5f, horizontal, 0f);
            localRayDirs[i] = rot * Vector3.forward;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  GIZMOS  (Scene view debug)
    // ═══════════════════════════════════════════════════════════════

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (localRayDirs == null) BuildRayDirections();

        Vector3 origin = transform.position + Vector3.up * rayOriginHeight;

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.45f);
        foreach (var d in localRayDirs)
            Gizmos.DrawRay(origin, transform.TransformDirection(d) * raycastDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, followDistance);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, slowDistance);

        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2.5f,
            $"State : {state}\nStuck : {stuckTimer:F1} s");
    }
#endif
}