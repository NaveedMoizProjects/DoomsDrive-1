using UnityEngine;

public class TrackBoundCheck : MonoBehaviour
{
    [Header("Settings")]
    public string trackTag = "Track";
    public KeyCode offTrackKey = KeyCode.R; // Warning key (only when off track)
    public KeyCode libertyKey = KeyCode.G;  // Liberty key (always active)

    [Header("State")]
    public bool isOnTrack = true;
    private float messageTimer = 0f;

    // 1. CONSTANT STAY CHECK
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(trackTag))
        {
            isOnTrack = true;
        }
    }

    // 2. EXIT CHECK
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(trackTag))
        {
            isOnTrack = false;
        }
    }

    private void Update()
    {
        // --- LIBERTY LOGIC (ALWAYS ON) ---
        // Player can press G at any time to replace themselves
        if (Input.GetKeyDown(libertyKey))
        {
            RecoverCar("Liberty Recovery");
        }

        // --- WARNING LOGIC (ONLY OFF TRACK) ---
        if (!isOnTrack)
        {
            HandleOffTrackLogic();
        }
        else
        {
            messageTimer = 0f;
        }
    }

    void HandleOffTrackLogic()
    {
        messageTimer -= Time.deltaTime;
        if (messageTimer <= 0f)
        {
            if (MessageUIManager.Instance != null)
            {
                // Tells them to press R (offTrackKey)
                MessageUIManager.Instance.ProcessMessage($"OFF TRACK!\nPress [{offTrackKey}] to Recover", WorldTriggerMessage.MessageType.Warning, 2f);
            }
            messageTimer = 2f;
        }

        // Check for the specific Off-Track recovery button
        if (Input.GetKeyDown(offTrackKey))
        {
            RecoverCar("Off-Track Recovery");
        }
    }

    void RecoverCar(string logReason)
    {
        if (RespawnManager.Instance != null)
        {
            RespawnManager.Instance.SoftResetCar(this.gameObject);
            isOnTrack = true;
            Debug.Log(logReason + " Triggered.");
        }
    }
}