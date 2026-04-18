using TMPro;
using UnityEngine;

public class LevelComplete : MonoBehaviour
{
    public static LevelComplete Instance { get; private set; }

    [Header("Level Complete Panel")]
    public GameObject levelCompletePanel;

    [Header("Panel message (optional)")]
    public TextMeshProUGUI panelMessageText;

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
        // Player car (tag: PlayerBullet), Player bullet, or AI (enemy) trigger
        string tag = other.tag;
        Debug.Log($"LevelComplete trigger hit by: {other.name} | Tag: {tag}");

        if (tag == "PlayerBullet" || tag == "AI" || tag == "Player")
        {
            // level finished — use final panel with win message
            ShowLevelComplete("You wins");
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