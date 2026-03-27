using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Events;

public class MessageUIManager : MonoBehaviour
{
    public static MessageUIManager Instance;

    [Header("UI Components")]
    public TextMeshProUGUI uiText;
    public GameObject uiPanel;

    [Header("Prompt Keys")]
    public KeyCode promptConfirmKey = KeyCode.R;
    public KeyCode promptCancelKey = KeyCode.Escape;

    // Internal prompt state
    private bool promptActive = false;
    private float promptTimer = 0f;
    private UnityAction onPromptConfirm;
    private UnityAction onPromptCancel;

    void Awake()
    {
        Instance = this;
        if (uiPanel) uiPanel.SetActive(false);
    }

    void Update()
    {
        if (!promptActive) return;

        // Confirm
        if (Input.GetKeyDown(promptConfirmKey))
        {
            ConfirmPrompt();
            return;
        }

        // Cancel
        if (Input.GetKeyDown(promptCancelKey))
        {
            CancelPrompt();
            return;
        }
    }

    public void ProcessMessage(string text, WorldTriggerMessage.MessageType type, float duration)
    {
        StopAllCoroutines(); // Clear previous message timers

        string processedText = text;
        Color targetColor = Color.white;

        // Apply your specific style rules
        switch (type)
        {
            case WorldTriggerMessage.MessageType.Checkpoint:
                targetColor = Color.cyan; // Blueish
                break;
            case WorldTriggerMessage.MessageType.LapComplete:
                targetColor = Color.green;
                break;
            case WorldTriggerMessage.MessageType.Victory:
                processedText = "<b>" + text + "</b>"; // Bold
                targetColor = Color.green;
                break;
            case WorldTriggerMessage.MessageType.Defeat:
                processedText = "<b>" + text + "</b>"; // Bold
                targetColor = Color.red;
                break;
            case WorldTriggerMessage.MessageType.Warning:
                targetColor = new Color(1f, 0.5f, 0f); // Orange
                break;
            case WorldTriggerMessage.MessageType.Damage:
                targetColor = new Color(1f, 0.3f, 0.3f); // Light Red
                break;
        }

        uiText.text = processedText;
        uiText.color = targetColor;

        if (uiPanel) uiPanel.SetActive(true);
        StartCoroutine(HideTimer(duration));
    }

    IEnumerator HideTimer(float delay)
    {
        yield return new WaitForSeconds(delay);
        // Only hide if not currently a prompt awaiting input
        if (!promptActive)
        {
            uiText.text = "";
            if (uiPanel) uiPanel.SetActive(false);
        }
    }

    // New API: Show an interactive prompt. Confirm via promptConfirmKey (default R), cancel via promptCancelKey (default Esc).
    // onConfirm / onCancel callbacks are optional.
    public void ProcessPrompt(string text, WorldTriggerMessage.MessageType type, float timeout, UnityAction onConfirm = null, UnityAction onCancel = null)
    {
        // Cancel any existing non-prompt coroutines / messages
        StopAllCoroutines();

        // Set prompt text & color using same styling rules as ProcessMessage
        string processedText = text;
        Color targetColor = Color.white;

        switch (type)
        {
            case WorldTriggerMessage.MessageType.Checkpoint:
                targetColor = Color.cyan;
                break;
            case WorldTriggerMessage.MessageType.LapComplete:
                targetColor = Color.green;
                break;
            case WorldTriggerMessage.MessageType.Victory:
                processedText = "<b>" + text + "</b>";
                targetColor = Color.green;
                break;
            case WorldTriggerMessage.MessageType.Defeat:
                processedText = "<b>" + text + "</b>";
                targetColor = Color.red;
                break;
            case WorldTriggerMessage.MessageType.Warning:
                targetColor = new Color(1f, 0.5f, 0f);
                break;
            case WorldTriggerMessage.MessageType.Damage:
                targetColor = new Color(1f, 0.3f, 0.3f);
                break;
        }

        uiText.text = processedText;
        uiText.color = targetColor;

        if (uiPanel) uiPanel.SetActive(true);

        // set prompt state and callbacks
        promptActive = true;
        promptTimer = timeout;
        onPromptConfirm = onConfirm;
        onPromptCancel = onCancel;

        // start the timeout coroutine that will cancel prompt when time is up
        StartCoroutine(PromptTimeoutCoroutine(timeout));
    }

    IEnumerator PromptTimeoutCoroutine(float timeout)
    {
        float timer = timeout;
        while (timer > 0f && promptActive)
        {
            yield return null;
            timer -= Time.unscaledDeltaTime;
        }

        if (promptActive)
        {
            // Timeout -> cancel
            CancelPrompt();
        }
    }

    // Confirm and Cancel methods exposed for other scripts or UI buttons
    public void ConfirmPrompt()
    {
        if (!promptActive) return;

        promptActive = false;
        promptTimer = 0f;

        onPromptConfirm?.Invoke();
        onPromptConfirm = null;
        onPromptCancel = null;

        uiText.text = "";
        if (uiPanel) uiPanel.SetActive(false);
    }

    public void CancelPrompt()
    {
        if (!promptActive) return;

        promptActive = false;
        promptTimer = 0f;

        onPromptCancel?.Invoke();
        onPromptConfirm = null;
        onPromptCancel = null;

        uiText.text = "";
        if (uiPanel) uiPanel.SetActive(false);
    }
}