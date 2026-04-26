using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Attach to an EMPTY parent GameObject (the "AI Spawner" object, NOT the car prefab).
/// Mirrors RespawnManager's exact dynamic spawn flow for AI vehicles:
///   - Instantiates prefab at runtime (dynamic, not static)
///   - Sets up DD1_LapHandler with master checkpoints
///   - Registers with DamageManager
///   - Freezes and re-enables Rigidbodies after placement
///   - Auto-respawns after respawnDelay seconds (no key press needed)
///   - Saves last checkpoint progress across respawns
/// </summary>
public class AIRespawnManager : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────────

    [Header("Identity")]
    public string aiName = "AI_01";

    [Header("Prefab & Spawn")]
    [Tooltip("The AI car prefab — same structure as player prefab but your AI variant.")]
    public GameObject aiCarPrefab;

    [Tooltip("Spawn offset above checkpoint to avoid ground clipping (matches player 1.5f).")]
    public float spawnHeightOffset = 1.5f;

    [Header("Checkpoints (leave empty to auto-share RespawnManager master list)")]
    [Tooltip("Leave empty and this AI will use RespawnManager.masterCheckpoints automatically.")]
    public List<Transform> overrideCheckpoints = new List<Transform>();

    [Header("Lap UI (optional — only assign if AI has its own HUD)")]
    public TextMeshProUGUI aiLapUI;
    public TextMeshProUGUI aiCheckpointUI;
    public UI_CheckpointMarker aiPointer;

    [Header("Respawn Settings")]
    public int maxRespawns = 3;

    [Tooltip("Seconds before AI auto-respawns. Real-time so player slow-mo does not freeze it.")]
    public float respawnDelay = 3f;

    [Header("Physics Settle (should match RespawnManager.rbReactivateDelay)")]
    public float rbReactivateDelay = 2f;

    // ── Runtime state ──────────────────────────────────────────────────────────

    private int respawnsRemaining;
    private int savedCPIndex = 0;
    private int savedLap = 1;
    private bool isDead = false;
    private bool respawnInProgress = false;

    public GameObject CurrentCar { get; private set; }
    public int RespawnsRemaining => respawnsRemaining;
    public bool IsAlive => CurrentCar != null && !isDead;

    // ── Events ─────────────────────────────────────────────────────────────────
    public event System.Action<AIRespawnManager> OnAISpawned;
    public event System.Action<AIRespawnManager> OnAIDestroyed;
    public event System.Action<AIRespawnManager> OnAIOutOfLives;

    // ── Active checkpoint list ─────────────────────────────────────────────────
    private List<Transform> ActiveCheckpoints
    {
        get
        {
            if (overrideCheckpoints != null && overrideCheckpoints.Count > 0)
                return overrideCheckpoints;

            if (RespawnManager.Instance != null
                && RespawnManager.Instance.masterCheckpoints != null
                && RespawnManager.Instance.masterCheckpoints.Count > 0)
                return RespawnManager.Instance.masterCheckpoints;

            return new List<Transform>();
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ──────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        respawnsRemaining = maxRespawns;
    }

    private void Start()
    {
        // First-time spawn — mirrors RespawnManager.Awake() → SpawnNewCar()
        SpawnAICar();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PUBLIC API
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Call from your AI health script when HP hits zero.
    /// Mirrors RespawnManager.OnPlayerZero() exactly.
    /// </summary>
    public void OnAIZero()
    {
        if (isDead || respawnInProgress) return;

        if (respawnsRemaining > 0)
        {
            respawnsRemaining--;
            TriggerAIDeath();
        }
        else
        {
            // No lives left — destroy and fire event, no respawn
            if (CurrentCar != null)
            {
                DamageManager.Instance?.ClearOwnerEntries(CurrentCar);
                OnAIDestroyed?.Invoke(this);
                Destroy(CurrentCar);
                CurrentCar = null;
            }

            isDead = true;
            Debug.Log($"[AIRespawnManager] {aiName} OUT OF LIVES.");
            OnAIOutOfLives?.Invoke(this);
        }
    }

    /// <summary>
    /// Save the last checkpoint the AI passed.
    /// Call from DD1_LapHandler or a checkpoint trigger on the AI car.
    /// Mirrors RespawnManager.UpdateRespawnAnchor().
    /// </summary>
    public void UpdateAICheckpoint(int cpIndex, int lap = 1)
    {
        var cps = ActiveCheckpoints;
        savedCPIndex = Mathf.Clamp(cpIndex, 0, Mathf.Max(0, cps.Count - 1));
        savedLap = lap;
        Debug.Log($"[AIRespawnManager] {aiName} checkpoint saved → CP {savedCPIndex} Lap {savedLap}");
    }

    /// <summary>
    /// Force an immediate respawn — useful for stuck detection.
    /// </summary>
    public void ForceRespawn(bool consumeLife = false)
    {
        if (respawnInProgress) return;

        if (consumeLife)
        {
            if (respawnsRemaining <= 0) { OnAIOutOfLives?.Invoke(this); return; }
            respawnsRemaining--;
        }

        StartCoroutine(RespawnSequence(instant: true));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // INTERNAL — DEATH & RESPAWN
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Mirrors RespawnManager.TriggerDeath() — destroys car, starts respawn timer.</summary>
    private void TriggerAIDeath()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"[AIRespawnManager] {aiName} died. Lives remaining: {respawnsRemaining}");

        if (CurrentCar != null)
        {
            DamageManager.Instance?.ClearOwnerEntries(CurrentCar);
            OnAIDestroyed?.Invoke(this);
            Destroy(CurrentCar);
            CurrentCar = null;
        }

        StartCoroutine(RespawnSequence());
    }

    /// <summary>
    /// Mirrors RespawnManager.RespawnSequence(fullRespawn: true).
    /// Counts down in real-time then spawns a fresh car.
    /// </summary>
    private IEnumerator RespawnSequence(bool instant = false)
    {
        respawnInProgress = true;

        if (!instant && respawnDelay > 0f)
        {
            for (int i = Mathf.CeilToInt(respawnDelay); i >= 1; i--)
            {
                Debug.Log($"[AIRespawnManager] {aiName} respawning in {i}s...");
                yield return new WaitForSecondsRealtime(1f);
            }
        }

        // Same purge the player's full-respawn path does
        DamageManager.Instance?.PurgeAllRegistryData();

        isDead = false;
        SpawnAICar();

        respawnInProgress = false;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // CORE SPAWN — 1:1 mirror of RespawnManager.SpawnNewCarAt()
    // ──────────────────────────────────────────────────────────────────────────

    private void SpawnAICar()
    {
        if (aiCarPrefab == null)
        {
            Debug.LogError($"[AIRespawnManager] {aiName}: aiCarPrefab not assigned!");
            return;
        }

        // 1. Safety — destroy any leftover car
        if (CurrentCar != null)
        {
            DamageManager.Instance?.ClearOwnerEntries(CurrentCar);
            Destroy(CurrentCar);
            CurrentCar = null;
        }

        // 2. Resolve spawn position from saved checkpoint (same as player)
        Vector3 spawnPos = GetCheckpointPos(savedCPIndex) + Vector3.up * spawnHeightOffset;
        Quaternion spawnRot = GetCheckpointRot(savedCPIndex);

        // 3. DYNAMIC Instantiate — no static scene object, mirrors SpawnNewCarAt()
        CurrentCar = Instantiate(aiCarPrefab, spawnPos, spawnRot, null);
        CurrentCar.name = $"{aiName}_Car";
        CurrentCar.tag = "AI";   // distinguishes from player "Player" tag

        // 4. Register with DamageManager — same call as player
        if (DamageManager.Instance != null)
            DamageManager.Instance.RefreshNewPlayerCar(CurrentCar);

        // 5. Setup DD1_LapHandler — same as player's lapHandler.Setup() + InitializeProgress()
        var lapHandler = CurrentCar.GetComponent<DD1_LapHandler>();
        if (lapHandler != null)
        {
            lapHandler.Setup(ActiveCheckpoints, aiLapUI, aiCheckpointUI, aiPointer);
            lapHandler.InitializeProgress(savedLap, savedCPIndex);
        }
        else
        {
            Debug.LogWarning($"[AIRespawnManager] {aiName}: DD1_LapHandler not found on prefab.");
        }

        // 6. Freeze Rigidbodies during placement — exact copy of player code
        Rigidbody[] rbs = CurrentCar.GetComponentsInChildren<Rigidbody>(true);
        bool[] prevKinematic = new bool[rbs.Length];

        for (int i = 0; i < rbs.Length; i++)
        {
            if (rbs[i] == null) continue;
            prevKinematic[i] = rbs[i].isKinematic;
            rbs[i].velocity = Vector3.zero;
            rbs[i].angularVelocity = Vector3.zero;
            rbs[i].isKinematic = true;
            rbs[i].Sleep();
        }

        StartCoroutine(ReenablePhysicsAfterPlacement(rbs, prevKinematic));

        // 7. Notify listeners
        OnAISpawned?.Invoke(this);

        Debug.Log($"[AIRespawnManager] {aiName} spawned at {spawnPos}  Lives: {respawnsRemaining}/{maxRespawns}");
    }

    // ── Exact copy of RespawnManager.ReenablePhysicsAfterPlacement() ──────────
    private IEnumerator ReenablePhysicsAfterPlacement(Rigidbody[] rbs, bool[] prevKinematic)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, rbReactivateDelay));

        for (int i = 0; i < rbs.Length; i++)
        {
            if (rbs[i] == null) continue;
            rbs[i].isKinematic = prevKinematic[i];
            rbs[i].WakeUp();
        }
    }

    // ── Checkpoint helpers ─────────────────────────────────────────────────────

    private Vector3 GetCheckpointPos(int index)
    {
        var cps = ActiveCheckpoints;
        if (cps == null || cps.Count == 0) return transform.position;
        return cps[Mathf.Clamp(index, 0, cps.Count - 1)].position;
    }

    private Quaternion GetCheckpointRot(int index)
    {
        var cps = ActiveCheckpoints;
        if (cps == null || cps.Count == 0) return transform.rotation;
        return cps[Mathf.Clamp(index, 0, cps.Count - 1)].rotation;
    }
}