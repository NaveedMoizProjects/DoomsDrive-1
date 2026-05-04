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

        if (!string.IsNullOrEmpty(winTag) && other.CompareTag(winTag))
        {
            isFinished = true;
            TriggerWin();
            return;
        }

        if (!string.IsNullOrEmpty(loseTag) && other.CompareTag(loseTag))
        {
            isFinished = true;
            TriggerLose();
            return;
        }

        // Fallback
        if (tag == "PlayerBullet" || tag == "AI" || tag == "Player")
        {
            isFinished = true;
            TriggerWin();
        }
    }

    // ── Player jeeta: cutscene par le jaao ──
    public void TriggerWin()
    {
        string msg = string.IsNullOrEmpty(winMessage) ? "You win" : winMessage;
        Debug.Log($"[LevelComplete] WIN — cutscene load ho raha hai.");

        if (LevelTransitionManager.Instance != null)
        {
            LevelTransitionManager.Instance.GoToCutscene();
        }
        else
        {
            Debug.LogWarning("[LevelComplete] LevelTransitionManager nahi mila! Panel fallback.");
            ShowPanel(msg);
        }
    }

    // ── Player mara: sirf panel dikhao, cutscene nahi ──
    public void TriggerLose()
    {
        string msg = string.IsNullOrEmpty(loseMessage) ? "You lose" : loseMessage;
        Debug.Log($"[LevelComplete] LOSE — panel show ho raha hai.");
        ShowPanel(msg);
    }

    private void ShowPanel(string message)
    {
        if (panelMessageText != null)
            panelMessageText.text = message;

        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(true);

        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // Backward compatibility: but now uses enums instead of fragile string check
    public enum ResultType { Win, Lose }

    public void ShowLevelComplete(ResultType result)
    {
        if (result == ResultType.Lose)
            TriggerLose();
        else
            TriggerWin();
    }

    // Transient message
    public void ShowTransientMessage(string message, WorldTriggerMessage.MessageType msgType = WorldTriggerMessage.MessageType.Info, float duration = 2f)
    {
        if (MessageUIManager.Instance != null)
            MessageUIManager.Instance.ProcessMessage(message, msgType, duration);
        else
            Debug.Log($"[TransientMessage] {message}");
    }
}
