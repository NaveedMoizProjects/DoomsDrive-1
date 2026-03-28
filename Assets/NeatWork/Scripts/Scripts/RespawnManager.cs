using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Cinemachine;

public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance { get; private set; }

    [Header("Master Track & UI")]
    public List<Transform> masterCheckpoints;
    public TextMeshProUGUI lapUI;
    public TextMeshProUGUI checkpointUI;
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

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this.gameObject); return; }
        Instance = this;

        if (masterCheckpoints.Count > 0)
        {
            UpdateRespawnAnchor(0);
            transform.position = respawnPos;
            transform.rotation = respawnRot;
        }

        SpawnNewCar();
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

        if (MessageUIManager.Instance != null)
            MessageUIManager.Instance.ProcessMessage($"{reason}\n[R] RESPAWN", WorldTriggerMessage.MessageType.Warning, 10f);

        if (currentCar != null)
        {
            DamageManager.Instance?.ClearOwnerEntries(currentCar);
            Destroy(currentCar);
            currentCar = null;
        }
    }

    private void Update()
    {
        if (isDead && Input.GetKeyDown(KeyCode.R)) PerformRespawn();
    }

    void PerformRespawn()
    {
        if (DamageManager.Instance != null) DamageManager.Instance.PurgeAllRegistryData();

        transform.position = respawnPos;
        transform.rotation = respawnRot;

        Time.timeScale = 1f;
        isDead = false;
        SpawnNewCar();
    }

    void SpawnNewCar()
    {
        // Spawning as sibling (null parent)
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
    // Add these functions inside your existing RespawnManager.cs

    public void SoftResetCar(GameObject car)
    {
        Rigidbody rb = car.GetComponent<Rigidbody>();

        // 1. Kill all velocity so the car doesn't "fly" when moved
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true; // Temporarily stop physics calculations
        }

        // 2. Place the car at the last saved checkpoint (the sibling anchor)
        // We use respawnPos and respawnRot which were updated by DD1_LapHandler
        car.transform.position = respawnPos + Vector3.up * 1.5f;
        car.transform.rotation = respawnRot;

        // 3. Optional: Add a tiny delay or just re-enable physics
        StartCoroutine(ReenablePhysics(rb));

        Debug.Log("Car recovered to last checkpoint.");
    }

    private System.Collections.IEnumerator ReenablePhysics(Rigidbody rb)
    {
        yield return new WaitForFixedUpdate(); // Wait one physics frame
        if (rb != null)
        {
            rb.isKinematic = false;
        }
    }
}