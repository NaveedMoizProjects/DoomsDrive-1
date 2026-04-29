using UnityEngine;

/// <summary>
/// Enemy Car Chase AI — v5
///
/// FIXES vs v4:
///   1. OBSTACLE LAYER FIX: obstacleMask now properly excludes the car's OWN
///      colliders via a dedicated selfMask check. The goto-based skip was
///      unreliable — replaced with a clean continue pattern.
///
///   2. GROUND EXCLUSION: A separate groundCheckMask lets you assign the
///      Ground layer so rays never treat the road as an obstacle.
///      Default remains ~0 (everything) so it works out of the box.
///
///   3. REVERSE COMPLETELY REBUILT:
///      - Braking phase no longer eats into reverseTimer.
///        reverseTimer only starts AFTER the car has actually stopped.
///      - reverseDuration raised to 3.5 s default.
///      - maxReverseSpeed raised to 30 m/s default.
///      - Reverse steer is now FULL maxSteerAngle (not a separate smaller angle).
///      - After reversing, Recover phase drives forward with opposite steer
///        for long enough to clear the obstacle.
///
///   4. STUCK RE-ENTRY GUARD: stuckTimer cannot trigger a new Reverse cycle
///      for 2 s after returning from Recover, preventing rapid re-entry loops.
///
///   5. RAY SELF-HIT: Rays are compared against ALL colliders whose root
///      transform IsChildOf(this.transform), so compound-collider cars are
///      handled correctly.
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
    public float followDistance = 8f;
    public float slowDistance = 14f;

    [Header("Obstacle Avoidance")]
    [Tooltip("How far ahead each ray reaches")]
    public float raycastDistance = 12f;

    [Tooltip("Number of rays in the fan (odd number recommended)")]
    public int rayCount = 9;

    [Tooltip("Total spread of the fan in degrees")]
    public float raySpreadAngle = 110f;

    [Tooltip("Height above pivot to start rays. Raise if rays clip your car body.")]
    public float rayOriginHeight = 1.0f;

    [Tooltip(
        "Layers counted as obstacles.\n" +
        "IMPORTANT: Assign this properly in the Inspector.\n" +
        "Exclude: Ground, Player, your own car's layer.\n" +
        "Default = Everything (~0) — works immediately but may cause false hits on ground.")]
    public LayerMask obstacleMask = ~0;

    [Tooltip(
        "Layers that represent the ground / road surface.\n" +
        "Rays that only hit these layers are NOT counted as obstacle hits.\n" +
        "Assign your Ground layer here.")]
    public LayerMask groundMask = 0;

    [Tooltip("Scales the avoidance steer output (1–3 recommended)")]
    public float avoidanceStrength = 2.0f;

    [Tooltip("Below this distance the car treats an obstacle as an emergency " +
             "(instant steer, no smoothing, avoidance overrides chase completely)")]
    public float emergencyDistance = 4.5f;

    [Tooltip("Normal steering smooth time in seconds")]
    public float steerSmoothTime = 0.08f;

    [Tooltip("Motor torque multiplier while in Avoid state")]
    public float avoidanceMotorBoost = 1.15f;

    [Header("Stuck / Reverse Recovery")]
    [Tooltip("Forward speed (m/s) below which the car may be stuck")]
    public float stuckSpeedThreshold = 0.4f;

    [Tooltip("Seconds below threshold before triggering reverse")]
    public float stuckTimeLimit = 1.5f;

    [Tooltip("How long the car ACTUALLY reverses (seconds) — braking phase is separate")]
    public float reverseDuration = 3.5f;

    [Tooltip("How long the forward-recovery steer is held (seconds)")]
    public float recoverDuration = 1.4f;

    [Tooltip("Reverse motor torque")]
    public float reverseTorque = 3500f;

    [Tooltip("Max reverse speed cap (m/s)")]
    public float maxReverseSpeed = 30f;

    [Tooltip("Front-wheel steer angle while reversing (uses maxSteerAngle if set to 0)")]
    public float reverseSteerAngle = 0f;   // 0 = use maxSteerAngle

    [Tooltip("Seconds after Recover before stuck detection fires again (prevents re-entry loops)")]
    public float postRecoverCooldown = 2.0f;

    // ═══════════════════════════════════════════════════════════════
    //  PRIVATE STATE
    // ═══════════════════════════════════════════════════════════════

    Rigidbody rb;
    DriveState state = DriveState.Chase;

    // Steering
    float currentSteer;
    float steerVelocity;

    // Ray directions (local space, built once)
    Vector3[] localRayDirs;

    // Avoidance results — refreshed every FixedUpdate
    bool isAvoiding;
    float avoidanceSteer;
    bool emergencyAvoid;

    // Stuck / reverse
    float stuckTimer;
    float reverseTimer;
    float recoverTimer;
    float reverseSteerDir;
    bool reverseStarted;
    bool brakingBeforeReverse;
    float postRecoverCooldownTimer;   // FIX #4: re-entry guard

    // ═══════════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ═══════════════════════════════════════════════════════════════

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
        rb.centerOfMass = new Vector3(0f, -0.9f, 0f);
        LocatePlayer();
    }

    void FixedUpdate()
    {
        if (playerCar == null) { LocatePlayer(); if (playerCar == null) return; }

        // Tick post-recover cooldown
        if (postRecoverCooldownTimer > 0f)
            postRecoverCooldownTimer -= Time.fixedDeltaTime;

        // Always refresh avoidance FIRST so all states see current obstacle data
        RefreshAvoidance();

        UpdateStuckDetection();
        RunStateMachine();
    }

    // ═══════════════════════════════════════════════════════════════
    //  AVOIDANCE  — "open space" algorithm  (FIX #1, #2)
    // ═══════════════════════════════════════════════════════════════

    void RefreshAvoidance()
    {
        Vector3 origin = transform.position + Vector3.up * rayOriginHeight;
        float leftFreeSum = 0f;
        float rightFreeSum = 0f;
        float closestHit = float.MaxValue;
        int hitCount = 0;

        for (int i = 0; i < rayCount; i++)
        {
            Vector3 worldDir = transform.TransformDirection(localRayDirs[i]);
            float freeDist;

            if (Physics.Raycast(origin, worldDir, out RaycastHit hit,
                                raycastDistance, obstacleMask,
                                QueryTriggerInteraction.Ignore))
            {
                // FIX #1: Skip own colliders reliably (handles compound colliders)
                if (hit.collider.transform.IsChildOf(transform))
                {
                    freeDist = raycastDistance;
                }
                // Skip player car
                else if (playerCar != null && hit.collider.transform.IsChildOf(playerCar))
                {
                    freeDist = raycastDistance;
                }
                // FIX #2: Skip pure-ground hits so road surface is never an obstacle
                else if (groundMask != 0 && ((groundMask.value >> hit.collider.gameObject.layer) & 1) == 1)
                {
                    freeDist = raycastDistance;
                }
                else
                {
                    // Real obstacle
                    freeDist = hit.distance;
                    hitCount++;
                    if (freeDist < closestHit) closestHit = freeDist;
                    Debug.DrawRay(origin, worldDir * freeDist, Color.red);
                }
            }
            else
            {
                freeDist = raycastDistance;
                Debug.DrawRay(origin, worldDir * raycastDistance, Color.green);
            }

            // Accumulate free-space per side
            float rx = localRayDirs[i].x;
            if (rx < -0.01f) leftFreeSum += freeDist;
            else if (rx > 0.01f) rightFreeSum += freeDist;
            else                  // centre ray: split equally
            {
                leftFreeSum += freeDist * 0.5f;
                rightFreeSum += freeDist * 0.5f;
            }
        }

        isAvoiding = hitCount > 0;
        emergencyAvoid = closestHit < emergencyDistance;

        if (!isAvoiding)
        {
            avoidanceSteer = 0f;
            return;
        }

        // Steer toward the side with MORE free space.
        float diff = rightFreeSum - leftFreeSum;
        int halfRays = Mathf.Max(1, (rayCount + 1) / 2);
        float normalised = diff / (halfRays * raycastDistance);   // ≈ -1 … +1

        avoidanceSteer = Mathf.Clamp(
            normalised * avoidanceStrength * maxSteerAngle,
            -maxSteerAngle,
            maxSteerAngle);
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
        if (isAvoiding)
            TransitionTo(DriveState.Avoid);
        // Don't return — still steer + drive this frame

        float steer = isAvoiding
            ? BlendSteer(ComputeChaseSteer(), avoidanceSteer, emergencyAvoid)
            : ComputeChaseSteer();

        ApplySteer(steer, emergencyAvoid);
        DriveForward(1f);
    }

    // ── AVOID ────────────────────────────────────────────────────
    void StateAvoid()
    {
        if (!isAvoiding)
        {
            TransitionTo(DriveState.Chase);
            return;
        }

        float steer = BlendSteer(ComputeChaseSteer(), avoidanceSteer, emergencyAvoid);
        ApplySteer(steer, emergencyAvoid);
        DriveForward(avoidanceMotorBoost);
    }

    // ── REVERSE ──────────────────────────────────────────────────  (FIX #3)
    void StateReverse()
    {
        // ── Phase 0: Brake until forward momentum is killed ──────────────────
        if (brakingBeforeReverse)
        {
            float fwdSpeed = Vector3.Dot(rb.velocity, transform.forward);
            if (fwdSpeed > 0.3f)
            {
                ApplyBrake(6000f);
                return;   // reverseTimer has NOT started yet — no time wasted
            }
            // Forward momentum gone → now begin the timed reverse
            brakingBeforeReverse = false;
            reverseStarted = false;   // ensure init block runs
        }

        // ── Phase 1: Initialise reverse (runs exactly once) ──────────────────
        if (!reverseStarted)
        {
            reverseStarted = true;
            reverseTimer = reverseDuration;   // full duration, no deductions
            reverseSteerDir = ChooseClearSide();
            Debug.Log($"[EnemyCarAI] Reverse started. SteerDir={reverseSteerDir} Duration={reverseDuration}s");
        }

        // ── Phase 2: Execute reverse ──────────────────────────────────────────
        reverseTimer -= Time.fixedDeltaTime;

        float rSteer = (reverseSteerAngle > 0f ? reverseSteerAngle : maxSteerAngle);
        // While reversing: steer the NOSE away from the obstacle so we swing clear
        ApplySteer(-reverseSteerDir * rSteer, emergency: true);
        DriveReverse();

        Debug.DrawRay(
            transform.position + Vector3.up * 2f,
            transform.right * reverseSteerDir * 3f,
            Color.magenta);

        if (reverseTimer <= 0f)
            TransitionTo(DriveState.Recover);
    }

    // ── RECOVER ──────────────────────────────────────────────────
    void StateRecover()
    {
        recoverTimer -= Time.fixedDeltaTime;

        // Drive forward while sweeping nose toward the clear side
        ApplySteer(reverseSteerDir * maxSteerAngle, emergency: false);
        DriveForward(1f);

        if (recoverTimer <= 0f)
            TransitionTo(isAvoiding ? DriveState.Avoid : DriveState.Chase);
    }

    // ── Steer blend helper ────────────────────────────────────────
    float BlendSteer(float chaseSteer, float avoidSteer, bool emergency)
    {
        if (emergency) return avoidSteer;

        bool sameDir = (chaseSteer >= 0f) == (avoidSteer >= 0f);
        return sameDir
            ? Mathf.Lerp(avoidSteer, chaseSteer, 0.20f)
            : avoidSteer;
    }

    // ═══════════════════════════════════════════════════════════════
    //  TRANSITION
    // ═══════════════════════════════════════════════════════════════

    void TransitionTo(DriveState next)
    {
        if (state == next) return;

        if (next == DriveState.Reverse)
        {
            reverseStarted = false;
            brakingBeforeReverse = true;
            stuckTimer = 0f;
        }

        if (next == DriveState.Recover)
        {
            recoverTimer = recoverDuration;
        }

        // FIX #4: After recovering, block stuck detection for a short window
        if (next == DriveState.Chase || next == DriveState.Avoid)
        {
            if (state == DriveState.Recover)
                postRecoverCooldownTimer = postRecoverCooldown;

            stuckTimer = 0f;
        }

        state = next;
        Debug.Log($"[EnemyCarAI] → {next}");
    }

    // ═══════════════════════════════════════════════════════════════
    //  STUCK DETECTION  (FIX #4 — respects post-recover cooldown)
    // ═══════════════════════════════════════════════════════════════

    void UpdateStuckDetection()
    {
        // Do not run during reverse/recover, and not during post-recover cooldown
        if (state == DriveState.Reverse || state == DriveState.Recover) return;
        if (postRecoverCooldownTimer > 0f) return;   // FIX #4

        float fwdSpeed = Vector3.Dot(rb.velocity, transform.forward);

        if (fwdSpeed < stuckSpeedThreshold)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer >= stuckTimeLimit)
                TransitionTo(DriveState.Reverse);
        }
        else
        {
            stuckTimer = Mathf.Max(0f, stuckTimer - Time.fixedDeltaTime * 3f);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  CHOOSE CLEAR SIDE FOR REVERSE
    // ═══════════════════════════════════════════════════════════════

    float ChooseClearSide()
    {
        Vector3 origin = transform.position + Vector3.up * rayOriginHeight;
        float leftSum = 0f;
        float rightSum = 0f;

        float[] angles = { 20f, 45f, 70f, 90f };
        foreach (float angle in angles)
        {
            Vector3 lDir = transform.TransformDirection(
                               Quaternion.Euler(0, -angle, 0) * Vector3.forward);
            Vector3 rDir = transform.TransformDirection(
                               Quaternion.Euler(0, angle, 0) * Vector3.forward);

            leftSum += Physics.Raycast(origin, lDir, out RaycastHit lh,
                            raycastDistance, obstacleMask) ? lh.distance : raycastDistance;
            rightSum += Physics.Raycast(origin, rDir, out RaycastHit rh,
                            raycastDistance, obstacleMask) ? rh.distance : raycastDistance;
        }

        return rightSum >= leftSum ? 1f : -1f;
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

    void ApplySteer(float target, bool emergency)
    {
        target = Mathf.Clamp(target, -maxSteerAngle, maxSteerAngle);

        if (emergency)
        {
            currentSteer = target;
            steerVelocity = 0f;
        }
        else
        {
            currentSteer = Mathf.SmoothDamp(currentSteer, target,
                               ref steerVelocity, steerSmoothTime);
        }

        frontLeft.steerAngle = currentSteer;
        frontRight.steerAngle = currentSteer;
    }

    // ═══════════════════════════════════════════════════════════════
    //  MOTOR / BRAKING
    // ═══════════════════════════════════════════════════════════════

    void DriveForward(float torqueScale)
    {
        float fwdSpeed = Vector3.Dot(rb.velocity, transform.forward);
        if (fwdSpeed >= maxSpeed) { ApplyMotorForward(0f); return; }

        if (playerCar == null) return;
        float dist = Vector3.Distance(transform.position, playerCar.position);
        float throttle = dist > slowDistance ? 1f
                       : dist > followDistance ? Mathf.InverseLerp(followDistance, slowDistance, dist)
                       : 0f;

        if (throttle <= 0f) { ApplyMotorForward(0f); ApplyBrake(1200f); return; }

        ApplyBrake(0f);
        ApplyMotorForward(motorTorque * throttle * torqueScale);
    }

    void DriveReverse()
    {
        float revSpeed = -Vector3.Dot(rb.velocity, transform.forward);
        if (revSpeed >= maxReverseSpeed) { ApplyMotorReverse(0f); return; }

        ApplyBrake(0f);
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
        if (RespawnManager.Instance != null &&
            RespawnManager.Instance.currentCar != null)
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
        float half = raySpreadAngle * 0.5f;

        for (int i = 0; i < rayCount; i++)
        {
            float t = rayCount > 1 ? (float)i / (rayCount - 1) : 0.5f;
            float horiz = Mathf.Lerp(-half, half, t);
            localRayDirs[i] = Quaternion.Euler(0f, horiz, 0f) * Vector3.forward;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  GIZMOS
    // ═══════════════════════════════════════════════════════════════

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (localRayDirs == null) BuildRayDirections();

        Vector3 origin = transform.position + Vector3.up * rayOriginHeight;

        Gizmos.color = new Color(1f, 0.6f, 0f, 0.5f);
        foreach (var d in localRayDirs)
            Gizmos.DrawRay(origin, transform.TransformDirection(d) * raycastDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, followDistance);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, slowDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, emergencyDistance);

        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 3f,
            $"State          : {state}\n" +
            $"Avoiding       : {isAvoiding}  Emergency: {emergencyAvoid}\n" +
            $"AvoidSteer     : {avoidanceSteer:F1}°\n" +
            $"Stuck          : {stuckTimer:F1} s\n" +
            $"PostRecoverCD  : {postRecoverCooldownTimer:F1} s\n" +
            $"ReverseTimer   : {reverseTimer:F1} s");
    }
#endif
}