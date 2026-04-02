using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance { get; private set; }

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

        // Restore old logic: place at nearest Track collider surface (ClosestPoint), otherwise use saved checkpoint's closest point,
        // otherwise pick nearest nearby collider's closest point, otherwise fallback to provided position.
        Collider chosenCollider = null;
        Vector3 referencePoint = (car != null) ? car.transform.position : targetPos;

        // 1) Search for the nearest 'Track' collider in a larger radius (prefer the main track)
        float searchRadius = 20f;
        Collider[] hits = Physics.OverlapSphere(targetPos, searchRadius);
        float bestSqr = Mathf.Infinity;
        for (int i = 0; i < hits.Length; i++)
        {
            var c = hits[i];
            if (c == null) continue;
            if (!c.enabled) continue;
            // prefer track-tagged colliders first
            if (c.CompareTag("Track"))
            {
                Vector3 cp = c.ClosestPoint(referencePoint);
                float d = (cp - referencePoint).sqrMagnitude;
                if (d < bestSqr)
                {
                    bestSqr = d;
                    chosenCollider = c;
                }
            }
        }

        // 2) If no Track collider was found, prefer saved checkpoint collider (use its closest point)
        if (chosenCollider == null && masterCheckpoints != null && savedCPIndex >= 0 && savedCPIndex < masterCheckpoints.Count)
        {
            Transform cp = masterCheckpoints[savedCPIndex];
            if (cp != null)
                chosenCollider = cp.GetComponent<Collider>() ?? cp.GetComponentInChildren<Collider>() ?? cp.GetComponentInParent<Collider>();
        }

        // 3) If still none, pick nearest collider from a small radius (previous behavior)
        if (chosenCollider == null)
        {
            Collider[] nearby = Physics.OverlapSphere(targetPos, 2f);
            float closestDist = Mathf.Infinity;
            foreach (var col in nearby)
            {
                if (col == null) continue;
                if (!col.enabled) continue;
                Vector3 cp = col.ClosestPoint(referencePoint);
                float d = (cp - referencePoint).sqrMagnitude;
                if (d < closestDist)
                {
                    closestDist = d;
                    chosenCollider = col;
                }
            }
        }

        // 4) Place at the collider's closest surface point (nearest track behavior). If none found, fallback to provided targetPos/targetRot.
        if (chosenCollider != null)
        {
            Vector3 spawnPoint = chosenCollider.ClosestPoint(referencePoint) + Vector3.up * 0.5f;
            Quaternion spawnRot = chosenCollider.transform.rotation;

            car.transform.position = spawnPoint;
            car.transform.rotation = spawnRot;
        }
        else
        {
            car.transform.position = targetPos + Vector3.up * 0.5f;
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