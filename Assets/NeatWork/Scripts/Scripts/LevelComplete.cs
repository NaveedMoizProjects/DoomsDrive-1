using TMPro;
using UnityEngine;

public class LevelComplete : MonoBehaviour
{
    public static LevelComplete Instance { get; private set; }

    [Header("Level Complete Panel")]
    public GameObject levelCompletePanel;

    [Header("Panel message (optional)")]
    public TextMeshProUGUI panelMessageText;

    [Header("Trigger Tags")]
    [Tooltip("Tag that triggers a Win when it enters the trigger (leave empty to disable).")]
    public string winTag = "Player";
    [Tooltip("Tag that triggers a Lose when it enters the trigger (leave empty to disable).")]
    public string loseTag = "Enemy";

    [Header("Custom messages (optional)")]
    [Tooltip("Message shown on win. If empty, a default will be used.")]
    public string winMessage = "You win";
    [Tooltip("Message shown on lose. If empty, a default will be used.")]
    public string loseMessage = "You lose";

    // Prevent multiple triggers causing multiple panels / audio
    private bool isFinished = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this.gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void OnTriggerEnter(Collider other)
    {
        if (isFinished) return;

        string tag = other.tag;
        Debug.Log($"LevelComplete trigger hit by: {other.name} | Tag: {tag}");

        // Priority: explicit win/lose tags from inspector
        if (!string.IsNullOrEmpty(winTag) && other.CompareTag(winTag))
        {
            isFinished = true;
            ShowLevelComplete(string.IsNullOrEmpty(winMessage) ? "You win" : winMessage);
            return;
        }

        if (!string.IsNullOrEmpty(loseTag) && other.CompareTag(loseTag))
        {
            isFinished = true;
            ShowLevelComplete(string.IsNullOrEmpty(loseMessage) ? "You lose" : loseMessage);
            return;
        }

        // Fallback to the previous behavior (keeps existing triggers working)
        if (tag == "PlayerBullet" || tag == "AI" || tag == "Player")
        {
            isFinished = true;
            ShowLevelComplete(string.IsNullOrEmpty(winMessage) ? "You wins" : winMessage);
        }
    }

    // Shows final panel (use for win / final lose screens)
    public void ShowLevelComplete(string message)
    {
        if (panelMessageText != null)
            panelMessageText.text = message;

        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(true);

        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // Transient message (does not open final panel) — uses MessageUIManager if available
    public void ShowTransientMessage(string message, WorldTriggerMessage.MessageType msgType = WorldTriggerMessage.MessageType.Info, float duration = 2f)
    {
        if (MessageUIManager.Instance != null)
        {
            MessageUIManager.Instance.ProcessMessage(message, msgType, duration);
        }
        else
        {
            Debug.Log($"[TransientMessage] {message}");
        }
    }
}