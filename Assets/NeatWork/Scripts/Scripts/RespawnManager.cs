using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance { get; private set; }

    public event Action<GameObject> OnPlayerSpawned;
    public event Action<GameObject> OnPlayerDestroyed;

    public enum GameMode { DragRace, CombatStory }

    [Header("Game Mode Settings")]
    public GameMode currentMode = GameMode.DragRace;
    public int maxRespawns = 3;
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

    private bool promptActive = false;

    [Header("Prompt Behavior")]
    public bool promptFullRespawn = true;

    [Header("Timing / Cleanup")]
    public float rbReactivateDelay = 2f;
    public float destroyedCleanupDelay = 10f;
    public bool autoCleanupDetached = true;

    [Header("Respawn Countdown")]
    public bool useRespawnCountdown = true;
    public int respawnCountdownSeconds = 3;

    private bool respawnInProgress = false;

    public int RespawnsRemaining => respawnsRemaining;
    public int MaxRespawns => maxRespawns;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this.gameObject); return; }
        Instance = this;

        respawnsRemaining = maxRespawns;

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

        bool outOfLives = false;

        if (currentMode == GameMode.CombatStory)
        {
            if (respawnsRemaining > 0)
            {
                message += $"\n{respawnsRemaining} LIVES LEFT\n[R] RESPAWN";
            }
            else
            {
                outOfLives = true;
                message += "\nMISSION FAILED\nOUT OF LIVES";
                if (DamageManager.Instance != null) DamageManager.Instance.FinalizeGame("OUT OF LIVES");

                // final screen (lose)
                LevelComplete.Instance?.ShowLevelComplete("You lost");
            }
        }
        else
        {
            message += "\n[R] RESPAWN";
        }

        if (MessageUIManager.Instance != null)
            MessageUIManager.Instance.ProcessMessage(message, WorldTriggerMessage.MessageType.Warning, 10f);

        // Transient death notice when NOT out of lives
        if (!outOfLives)
        {
            LevelComplete.Instance?.ShowTransientMessage("You died", WorldTriggerMessage.MessageType.Defeat, 2f);
        }

        if (currentCar != null)
        {
            DamageManager.Instance?.ClearOwnerEntries(currentCar);
            OnPlayerDestroyed?.Invoke(currentCar);
            Destroy(currentCar);
            currentCar = null;
        }
    }

    public void OnPlayerZero()
    {
        if (isDead || respawnInProgress) return;

        if (currentMode == GameMode.CombatStory)
        {
            if (respawnsRemaining > 0)
            {
                respawnsRemaining = Mathf.Max(0, respawnsRemaining - 1);
                TriggerDeath("PLAYER KILLED");
            }
            else
            {
                // No respawns left — destroy player and show final fail screen
                if (currentCar != null)
                {
                    DamageManager.Instance?.ClearOwnerEntries(currentCar);
                    OnPlayerDestroyed?.Invoke(currentCar);
                    Destroy(currentCar);
                    currentCar = null;
                }

                if (DamageManager.Instance != null) DamageManager.Instance.FinalizeGame("OUT OF LIVES");

                // final screen (lose)
                LevelComplete.Instance?.ShowLevelComplete("You lost");

                Time.timeScale = 0f;
            }
        }
        else
        {
            // DragRace mode — regular death flow
            TriggerDeath("PLAYER KILLED");
        }

        UpdateLivesUI();
    }

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
        // Block R key if pause panel is showing due to death
        if (GamePauseManager.IsPlayerDead) return;

        if (isDead && Input.GetKeyDown(KeyCode.R) && !respawnInProgress)
        {
            if (currentMode == GameMode.DragRace || respawnsRemaining > 0)
            {
                if (currentMode == GameMode.CombatStory) respawnsRemaining--;
                StartCoroutine(RespawnSequence(fullRespawn: true, callerIsPrompt: false));
            }
        }

        if (promptActive && !respawnInProgress)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                if (currentMode == GameMode.CombatStory) respawnsRemaining = Mathf.Max(0, respawnsRemaining - 1);

                if (promptFullRespawn)
                {
                    StartCoroutine(RespawnSequence(fullRespawn: true, callerIsPrompt: true));
                }
                else
                {
                    SoftResetCar(currentCar);
                    promptActive = false;
                    Time.timeScale = 1f;
                    UpdateLivesUI();
                }
            }
            else if (Input.GetKeyDown(KeyCode.C))
            {
                promptActive = false;
                Time.timeScale = 1f;
                if (MessageUIManager.Instance != null)
                    MessageUIManager.Instance.ProcessMessage("Continuing", WorldTriggerMessage.MessageType.Info, 2f);
            }
        }
    }

    private IEnumerator RespawnSequence(bool fullRespawn, bool callerIsPrompt)
    {
        respawnInProgress = true;

        if (useRespawnCountdown && respawnCountdownSeconds > 0)
        {
            for (int i = respawnCountdownSeconds; i >= 1; i--)
            {
                if (MessageUIManager.Instance != null)
                    MessageUIManager.Instance.ProcessMessage($"RESPAWN IN\n{i}", WorldTriggerMessage.MessageType.Info, 1f);

                yield return new WaitForSecondsRealtime(1f);
            }
        }

        if (MessageUIManager.Instance != null)
            MessageUIManager.Instance.ProcessMessage($"RESPAWNING...", WorldTriggerMessage.MessageType.Info, 0.8f);

        yield return new WaitForSecondsRealtime(0.2f);

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

        promptActive = false;
        respawnInProgress = false;
    }

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

        OnPlayerSpawned?.Invoke(currentCar);

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

        OnPlayerSpawned?.Invoke(currentCar);

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

    public void PlacementRecovery(GameObject car, Vector3 targetPos, Quaternion targetRot)
    {
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

        Collider chosenCollider = null;
        Vector3 referencePoint = root.transform.position;

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
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, rbReactivateDelay));

        for (int i = 0; i < rbs.Length; i++)
        {
            if (rbs[i] == null) continue;
            rbs[i].isKinematic = prevKinematic[i];
            rbs[i].WakeUp();
        }
    }

    public void SoftResetCar(GameObject car)
    {
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