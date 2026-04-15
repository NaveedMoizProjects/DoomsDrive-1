using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance { get; private set; }

    // Event fired whenever a new player car is spawned.
    public event Action<GameObject> OnPlayerSpawned;

    // Optional: event for when player car is destroyed
    public event Action<GameObject> OnPlayerDestroyed;

    public enum GameMode { DragRace, CombatStory }

    [Header("Game Mode Settings")]
    public GameMode currentMode = GameMode.DragRace;
    public int maxRespawns = 3; // Only used in Combat/Story
    private int respawnsRemaining;

    [Header("Master Track & UI")]
    public List<Transform> masterCheckpoints;
    public TextMeshProUGUI lapUI;
    public TextMeshProUGUI checkpointUI;
    public TextMeshProUGUI livesUI;
    public UI_CheckpointMarker pointer;

    [Header("Prefabs & Cameras")]
    public GameObject carPrefab;
    public CinemachineVirtualCamera vCam;

    [Header("Saved Progress")]
    public int savedLap = 1;
    public int savedCPIndex = 0;
    private Vector3 respawnPos;
    private Quaternion respawnRot;

    public GameObject currentCar;
    private bool isDead = false;

    // NEW: wheel-loss prompt state (non-destructive)
    private bool promptActive = false;

    [Header("Prompt Behavior")]
    [Tooltip("When a prompt is active (wheel loss), pressing R will perform a full respawn if true, or a soft reset (teleport + heal) if false.")]
    public bool promptFullRespawn = true;

    [Header("Timing / Cleanup")]
    [Tooltip("Seconds to wait before re-enabling Rigidbodies after relocations/spawns.")]
    public float rbReactivateDelay = 2f;
    [Tooltip("Seconds after which detached/destroyed fragments are removed.")]
    public float destroyedCleanupDelay = 10f;
    [Tooltip("When true, detached parts created by DamageablePart will get auto-cleanup component.")]
    public bool autoCleanupDetached = true;

    [Header("Respawn Countdown")]
    [Tooltip("Enable countdown (real seconds) before respawn.")]
    public bool useRespawnCountdown = true;
    [Tooltip("Countdown length in seconds used for the 3-2-1 messages.")]
    public int respawnCountdownSeconds = 3;

    // internal
    private bool respawnInProgress = false;

    // Exposed read-only properties for other systems (LevelFailedManager, UI, etc.)
    public int RespawnsRemaining => respawnsRemaining;
    public int MaxRespawns => maxRespawns;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this.gameObject); return; }
        Instance = this;

        respawnsRemaining = maxRespawns;

        // guard null list to avoid NREs if inspector not set
        if (masterCheckpoints == null) masterCheckpoints = new List<Transform>();

        if (masterCheckpoints.Count > 0)
        {
            UpdateRespawnAnchor(0);
            transform.position = respawnPos;
            transform.rotation = respawnRot;
        }

        SpawnNewCar();
        UpdateLivesUI();
    }

    public void UpdateRespawnAnchor(int index)
    {
        if (masterCheckpoints == null || masterCheckpoints.Count == 0) return;
        index = Mathf.Clamp(index, 0, masterCheckpoints.Count - 1);
        respawnPos = masterCheckpoints[index].position;
        respawnRot = masterCheckpoints[index].rotation;
        savedCPIndex = index;
    }

    public void TriggerDeath(string reason)
    {
        if (isDead) return;
        isDead = true;
        Time.timeScale = 0.5f;

        string message = reason;

        if (currentMode == GameMode.CombatStory)
        {
            if (respawnsRemaining > 0)
                message += $"\n{respawnsRemaining} LIVES LEFT\n[R] RESPAWN";
            else
            {
                message += "\nMISSION FAILED\nOUT OF LIVES";
                if (DamageManager.Instance != null) DamageManager.Instance.FinalizeGame("OUT OF LIVES");
            }
        }
        else
        {
            message += "\n[R] RESPAWN";
        }

        if (MessageUIManager.Instance != null)
            MessageUIManager.Instance.ProcessMessage(message, WorldTriggerMessage.MessageType.Warning, 10f);

        if (currentCar != null)
        {
            DamageManager.Instance?.ClearOwnerEntries(currentCar);

            // notify listeners about destruction
            OnPlayerDestroyed?.Invoke(currentCar);

            Destroy(currentCar);
            currentCar = null;
        }
    }

    // NEW API: called when a DamageablePart with PartType.player reaches zero
    public void OnPlayerZero()
    {
        if (isDead || respawnInProgress) return;

        // Combat/Story mode: consume respawn if available, otherwise level fail
        if (currentMode == GameMode.CombatStory)
        {
            if (respawnsRemaining > 0)
            {
                respawnsRemaining = Mathf.Max(0, respawnsRemaining - 1);
                // Use existing death flow so UI/messages remain consistent
                TriggerDeath("PLAYER KILLED");
            }
            else
            {
                // No respawns left — destroy player and show level failed UI
                if (currentCar != null)
                {
                    DamageManager.Instance?.ClearOwnerEntries(currentCar);
                    OnPlayerDestroyed?.Invoke(currentCar);
                    Destroy(currentCar);
                    currentCar = null;
                }

                // Show level failed panel (if present)
                LevelFailedManager.Instance?.ShowLevelFailed();

                // ensure timeScale is 0 (LevelFailedManager handles it but defensive)
                Time.timeScale = 0f;
            }
        }
        else
        {
            // For DragRace or other modes, use regular death handling
            TriggerDeath("PLAYER KILLED");
        }

        UpdateLivesUI();
    }

    // NEW: non-destructive prompt showing message and waiting for R (respawn) or C (continue)
    public void PromptRespawn(string reason)
    {
        if (promptActive || isDead) return;
        promptActive = true;
        Time.timeScale = 0.5f;

        string message = reason + "\n[R] RESPAWN    [C] CONTINUE";

        if (MessageUIManager.Instance != null)
            MessageUIManager.Instance.ProcessMessage(message, WorldTriggerMessage.MessageType.Warning, 10f);
    }

    private void Update()
    {
        // Keep existing death handling
        if (isDead && Input.GetKeyDown(KeyCode.R) && !respawnInProgress)
        {
            if (currentMode == GameMode.DragRace || respawnsRemaining > 0)
            {
                if (currentMode == GameMode.CombatStory) respawnsRemaining--;
                // start respawn (with countdown if enabled)
                StartCoroutine(RespawnSequence(fullRespawn: true, callerIsPrompt: false));
            }
        }

        // NEW: handle prompt input without destroying the car (or optionally perform full respawn)
        if (promptActive && !respawnInProgress)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                // decrement lives for combat mode
                if (currentMode == GameMode.CombatStory) respawnsRemaining = Mathf.Max(0, respawnsRemaining - 1);

                if (promptFullRespawn)
                {
                    // schedule full respawn via countdown coroutine
                    StartCoroutine(RespawnSequence(fullRespawn: true, callerIsPrompt: true));
                }
                else
                {
                    // immediate soft reset (now robust) — keep prompt behavior fast
                    SoftResetCar(currentCar);
                    promptActive = false;
                    Time.timeScale = 1f;
                    UpdateLivesUI();
                }
            }
            else if (Input.GetKeyDown(KeyCode.C))
            {
                // Continue playing; keep current car as-is
                promptActive = false;
                Time.timeScale = 1f;
                if (MessageUIManager.Instance != null)
                    MessageUIManager.Instance.ProcessMessage("Continuing", WorldTriggerMessage.MessageType.Info, 2f);
            }
        }
    }

    // Orchestrates the countdown and the actual respawn (shared for death and prompt full-respawn)
    private IEnumerator RespawnSequence(bool fullRespawn, bool callerIsPrompt)
    {
        respawnInProgress = true;

        // If countdown enabled, show 3-2-1 messages in realtime
        if (useRespawnCountdown && respawnCountdownSeconds > 0)
        {
            for (int i = respawnCountdownSeconds; i >= 1; i--)
            {
                if (MessageUIManager.Instance != null)
                    MessageUIManager.Instance.ProcessMessage($"RESPAWN IN\n{i}", WorldTriggerMessage.MessageType.Info, 1f);

                yield return new WaitForSecondsRealtime(1f);
            }
        }

        // final message / small pause
        if (MessageUIManager.Instance != null)
            MessageUIManager.Instance.ProcessMessage($"RESPAWNING...", WorldTriggerMessage.MessageType.Info, 0.8f);

        yield return new WaitForSecondsRealtime(0.2f);

        // Full respawn path: destroy current car (if present), purge damage registry, spawn new
        if (fullRespawn)
        {
            if (currentCar != null)
            {
                DamageManager.Instance?.ClearOwnerEntries(currentCar);
                OnPlayerDestroyed?.Invoke(currentCar);
                Destroy(currentCar);
                currentCar = null;
            }

            if (DamageManager.Instance != null) DamageManager.Instance.PurgeAllRegistryData();

            Time.timeScale = 1f;
            isDead = false;

            SpawnNewCarAt(respawnPos, respawnRot);
            UpdateLivesUI();
        }

        // cleanup state
        promptActive = false;
        respawnInProgress = false;
    }

    // Backwards-compatible SpawnNewCar using manager transform (kept for initial Awake usage).
    void SpawnNewCar()
    {
        currentCar = Instantiate(carPrefab, transform.position + Vector3.up * 1.5f, transform.rotation, null);
        currentCar.tag = "Player";

        if (DamageManager.Instance != null) DamageManager.Instance.RefreshNewPlayerCar(currentCar);

        var lapHandler = currentCar.GetComponent<DD1_LapHandler>();
        if (lapHandler != null)
        {
            lapHandler.Setup(masterCheckpoints, lapUI, checkpointUI, pointer);
            lapHandler.InitializeProgress(savedLap, savedCPIndex);
        }

        if (vCam)
        {
            vCam.Follow = currentCar.transform;
            vCam.LookAt = currentCar.transform;
            vCam.OnTargetObjectWarped(currentCar.transform, currentCar.transform.position - vCam.transform.position);
        }

        // Notify listeners that a player car was spawned
        OnPlayerSpawned?.Invoke(currentCar);

        // Ensure new car's rigidbodies don't fight initial placement
        Rigidbody[] newRbs = currentCar.GetComponentsInChildren<Rigidbody>(true);
        bool[] prevKinematic = new bool[newRbs.Length];
        for (int i = 0; i < newRbs.Length; i++)
        {
            if (newRbs[i] == null) continue;
            prevKinematic[i] = newRbs[i].isKinematic;
            newRbs[i].velocity = Vector3.zero;
            newRbs[i].angularVelocity = Vector3.zero;
            newRbs[i].isKinematic = true;
            newRbs[i].Sleep();
        }
        StartCoroutine(ReenablePhysicsAfterPlacement(newRbs, prevKinematic));
    }

    // New explicit spawn helper — always used for real respawns so relocation logic can't hijack it.
    void SpawnNewCarAt(Vector3 position, Quaternion rotation)
    {
        currentCar = Instantiate(carPrefab, position + Vector3.up * 1.5f, rotation, null);
        currentCar.tag = "Player";

        if (DamageManager.Instance != null) DamageManager.Instance.RefreshNewPlayerCar(currentCar);

        var lapHandler = currentCar.GetComponent<DD1_LapHandler>();
        if (lapHandler != null)
        {
            lapHandler.Setup(masterCheckpoints, lapUI, checkpointUI, pointer);
            lapHandler.InitializeProgress(savedLap, savedCPIndex);
        }

        if (vCam)
        {
            vCam.Follow = currentCar.transform;
            vCam.LookAt = currentCar.transform;
            vCam.OnTargetObjectWarped(currentCar.transform, currentCar.transform.position - vCam.transform.position);
        }

        // Notify listeners that a player car was spawned
        OnPlayerSpawned?.Invoke(currentCar);

        // Freeze child Rigidbodies and restore after configured delay
        Rigidbody[] newRbs = currentCar.GetComponentsInChildren<Rigidbody>(true);
        bool[] prevKinematic = new bool[newRbs.Length];
        for (int i = 0; i < newRbs.Length; i++)
        {
            if (newRbs[i] == null) continue;
            prevKinematic[i] = newRbs[i].isKinematic;
            newRbs[i].velocity = Vector3.zero;
            newRbs[i].angularVelocity = Vector3.zero;
            newRbs[i].isKinematic = true;
            newRbs[i].Sleep();
        }
        StartCoroutine(ReenablePhysicsAfterPlacement(newRbs, prevKinematic));
    }

    void UpdateLivesUI()
    {
        if (livesUI == null) return;
        livesUI.text = (currentMode == GameMode.CombatStory) ? $"LIVES: {respawnsRemaining}" : "MODE: DRAG RACE";
    }

    // --- RECOVERY FUNCTIONS (FIXES THE MISMATCH ERROR) ---
    // NOTE: these now operate on the car ROOT and temporarily lock ALL rigidbodies under it
    public void PlacementRecovery(GameObject car, Vector3 targetPos, Quaternion targetRot)
    {
        // Operate on the root vehicle to avoid partial child movement
        GameObject root = (car != null && car.transform.root != null) ? car.transform.root.gameObject : car;
        if (root == null) return;

        Rigidbody[] rbs = root.GetComponentsInChildren<Rigidbody>(true);
        bool[] prevKinematic = new bool[rbs.Length];

        // Freeze physics on all rigidbodies (store previous kinematic state)
        for (int i = 0; i < rbs.Length; i++)
        {
            if (rbs[i] == null) continue;
            prevKinematic[i] = rbs[i].isKinematic;
            rbs[i].velocity = Vector3.zero;
            rbs[i].angularVelocity = Vector3.zero;
            rbs[i].isKinematic = true;
            rbs[i].Sleep();
        }

        Collider chosenCollider = null;
        Vector3 referencePoint = root.transform.position;

        // 1) Find the nearest 'Track' collider
        float searchRadius = 30f;
        Collider[] hits = Physics.OverlapSphere(targetPos, searchRadius);
        float bestSqr = Mathf.Infinity;

        foreach (var c in hits)
        {
            if (c == null || !c.enabled) continue;
            if (c.CompareTag("Track"))
            {
                float d = (c.bounds.center - referencePoint).sqrMagnitude;
                if (d < bestSqr)
                {
                    bestSqr = d;
                    chosenCollider = c;
                }
            }
        }

        // 2) If found, move to CENTER of the collider, not the edge
        if (chosenCollider != null)
        {
            Vector3 spawnPoint = chosenCollider.bounds.center;
            float heightOffset = (chosenCollider.bounds.extents.y) + 1.5f;
            spawnPoint.y = chosenCollider.bounds.min.y + heightOffset;

            root.transform.position = spawnPoint;
            root.transform.rotation = chosenCollider.transform.rotation;
        }
        else
        {
            root.transform.position = targetPos + Vector3.up * 1.5f;
            root.transform.rotation = targetRot;
        }

        StartCoroutine(ReenablePhysicsAfterPlacement(rbs, prevKinematic));
    }

    private IEnumerator ReenablePhysicsAfterPlacement(Rigidbody[] rbs, bool[] prevKinematic)
    {
        // Wait configured realtime delay so transforms have settled (ignores Time.timeScale)
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, rbReactivateDelay));

        for (int i = 0; i < rbs.Length; i++)
        {
            if (rbs[i] == null) continue;
            // restore previous kinematic state
            rbs[i].isKinematic = prevKinematic[i];
            rbs[i].WakeUp();
        }
    }

    public void SoftResetCar(GameObject car)
    {
        // Operate on root to avoid partial movement
        GameObject root = (car != null && car.transform.root != null) ? car.transform.root.gameObject : car;
        if (root == null) return;

        Rigidbody[] rbs = root.GetComponentsInChildren<Rigidbody>(true);
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

        root.transform.position = respawnPos + Vector3.up * 1.5f;
        root.transform.rotation = respawnRot;

        if (DamageManager.Instance != null) DamageManager.Instance.ForceFullHeal(root);

        StartCoroutine(ReenablePhysicsAfterPlacement(rbs, prevKinematic));
    }
}