using UnityEngine;

public class CarRespawner : MonoBehaviour
{
    [Header("Respawn Settings")]
    public Transform startPoint;                 // Assigned in Inspector as default
    private Transform currentCheckpoint;         // Updated during gameplay
    public float respawnDelay = 2f;

    [Header("Detection Tags")]
    public string trackTag = "Track";
    public string checkpointTag = "Checkpoint";

    private Rigidbody rb;
    private bool isOffTrack = false;
    private float offTrackTimer = 0f;

    [Header("Player Settings")]
    public CarMovement.PlayerID playerID;        // Must match the player's CarMovement setting

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // If startPoint not assigned, fallback to current position
        if (startPoint == null)
        {
            GameObject defaultStart = new GameObject("DefaultStartPoint");
            defaultStart.transform.position = transform.position;
            defaultStart.transform.rotation = transform.rotation;
            startPoint = defaultStart.transform;
        }

        currentCheckpoint = startPoint;
        RespawnCar();
    }

    void Update()
    {
        // Off-track timer logic
        if (isOffTrack)
        {
            offTrackTimer += Time.deltaTime;
            if (offTrackTimer >= respawnDelay)
            {
                RespawnCar();
            }
        }

        // Manual reset input based on player
        if (playerID == CarMovement.PlayerID.Player1 && Input.GetKeyDown(ControlSettingsManager.Instance.p1_Reset))
        {
            RespawnCar();
        }
        else if (playerID == CarMovement.PlayerID.Player2 && Input.GetKeyDown(ControlSettingsManager.Instance.p2_Reset))
        {
            RespawnCar();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(trackTag))
        {
            isOffTrack = true;
            offTrackTimer = 0f;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(trackTag))
        {
            isOffTrack = false;
            offTrackTimer = 0f;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(checkpointTag))
        {
            currentCheckpoint = other.transform;
        }
    }

    public void RespawnCar()
    {
        if (!rb || !currentCheckpoint) return;

        rb.isKinematic = true;

        // Offset logic: Player 1 spawns slightly left, Player 2 slightly right
        Vector3 offset = Vector3.zero;
        if (playerID == CarMovement.PlayerID.Player1)
            offset = -currentCheckpoint.right * 1.5f; // 1.5 units to the left
        else if (playerID == CarMovement.PlayerID.Player2)
            offset = currentCheckpoint.right * 1.5f;  // 1.5 units to the right

        // Move to checkpoint position + offset
        transform.position = currentCheckpoint.position + offset;
        transform.rotation = currentCheckpoint.rotation;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        StartCoroutine(ReactivateRigidbody());

        isOffTrack = false;
        offTrackTimer = 0f;
    }
    private System.Collections.IEnumerator ReactivateRigidbody()
    {
        yield return new WaitForFixedUpdate();
        rb.isKinematic = false;
    }
}
