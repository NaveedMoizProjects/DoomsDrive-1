using UnityEngine;

public class TrackBoundCheck : MonoBehaviour
{
    [Header("Settings")]
    public string trackTag = "Track";
    public KeyCode offTrackKey = KeyCode.G;
    public KeyCode libertyKey = KeyCode.G;

    [Header("Safe Spot Memory")]
    private Vector3 lastSafePos;
    private Quaternion lastSafeRot;
    private float recordTimer = 0f;
    private float recordInterval = 0.5f; // How often to save the position

    [Header("State")]
    public bool isOnTrack = true;
    private float messageTimer = 0f;

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(trackTag))
        {
            isOnTrack = true;

            // Record position while on track
            recordTimer += Time.deltaTime;
            if (recordTimer >= recordInterval)
            {
                lastSafePos = transform.position;
                lastSafeRot = transform.rotation;
                recordTimer = 0f;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(trackTag)) isOnTrack = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(libertyKey)) PerformPreciseRecovery();

        if (!isOnTrack)
        {
            HandleOffTrackMessage();
            if (Input.GetKeyDown(offTrackKey)) PerformPreciseRecovery();
        }
    }

    void HandleOffTrackMessage()
    {
        messageTimer -= Time.deltaTime;
        if (messageTimer <= 0f)
        {
            if (MessageUIManager.Instance != null)
                MessageUIManager.Instance.ProcessMessage($"OFF TRACK!\nPress [{offTrackKey}] to Recover", WorldTriggerMessage.MessageType.Warning, 2f);
            messageTimer = 2f;
        }
    }

    void PerformPreciseRecovery()
    {
        if (RespawnManager.Instance != null)
        {
            // We call a new function that uses the SPECIFIC last safe spot
            RespawnManager.Instance.PlacementRecovery(this.gameObject, lastSafePos, lastSafeRot);
            isOnTrack = true;
        }
    }
}