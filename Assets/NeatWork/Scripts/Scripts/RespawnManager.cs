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

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this.gameObject); return; }
        Instance = this;

        respawnsRemaining = maxRespawns;

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
        if (isDead && Input.GetKeyDown(KeyCode.R))
        {
            if (currentMode == GameMode.DragRace || respawnsRemaining > 0)
            {
                if (currentMode == GameMode.CombatStory) respawnsRemaining--;
                PerformRespawn();
            }
        }

        // NEW: handle prompt input without destroying the car
        if (promptActive)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                // Respawn (soft): teleport current car to respawn anchor and fully heal
                if (currentCar != null)
                {
                    if (currentMode == GameMode.CombatStory) respawnsRemaining = Mathf.Max(0, respawnsRemaining - 1);
                    SoftResetCar(currentCar);
                }
                promptActive = false;
                Time.timeScale = 1f;
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

    void PerformRespawn()
    {
        if (DamageManager.Instance != null) DamageManager.Instance.PurgeAllRegistryData();

        transform.position = respawnPos;
        transform.rotation = respawnRot;

        Time.timeScale = 1f;
        isDead = false;
        SpawnNewCar();
        UpdateLivesUI();
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

        // Notify listeners that a player car was spawned
        OnPlayerSpawned?.Invoke(currentCar);
    }

    void UpdateLivesUI()
    {
        if (livesUI == null) return;
        livesUI.text = (currentMode == GameMode.CombatStory) ? $"LIVES: {respawnsRemaining}" : "MODE: DRAG RACE";
    }

    // --- RECOVERY FUNCTIONS (FIXES THE MISMATCH ERROR) ---

    public void PlacementRecovery(GameObject car, Vector3 targetPos, Quaternion targetRot)
    {
        Rigidbody rb = car.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        Collider chosenCollider = null;
        Vector3 referencePoint = (car != null) ? car.transform.position : targetPos;

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
            // Use bounds.center to get the absolute middle of the "Range Cube"
            Vector3 spawnPoint = chosenCollider.bounds.center;

            // Offset upward based on the collider's height so you don't spawn inside the floor
            // (Half the height of the cube + a little extra for the car)
            float heightOffset = (chosenCollider.bounds.extents.y) + 1.5f;
            spawnPoint.y = chosenCollider.bounds.min.y + heightOffset;

            car.transform.position = spawnPoint;

            // Align rotation to the track piece itself
            car.transform.rotation = chosenCollider.transform.rotation;
        }
        else
        {
            // Fallback to the last recorded safe spot if no track piece is nearby
            car.transform.position = targetPos + Vector3.up * 1.5f;
            car.transform.rotation = targetRot;
        }

        StartCoroutine(ReenablePhysicsAfterPlacement(rb));
    }
    private IEnumerator ReenablePhysicsAfterPlacement(Rigidbody rb)
    {
        yield return new WaitForFixedUpdate();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.WakeUp();
        }
    }

    public void SoftResetCar(GameObject car)
    {
        Rigidbody rb = car.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        car.transform.position = respawnPos + Vector3.up * 1.5f;
        car.transform.rotation = respawnRot;

        if (DamageManager.Instance != null) DamageManager.Instance.ForceFullHeal(car);

        StartCoroutine(ReenablePhysicsAfterPlacement(rb));
    }
}