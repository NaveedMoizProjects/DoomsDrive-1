using UnityEngine;
using TMPro;
using System.Collections;

public class MessageUIManager : MonoBehaviour
{
    public static MessageUIManager Instance;

    [Header("UI Components")]
    public TextMeshProUGUI uiText;
    public GameObject uiPanel;

    void Awake()
    {
        Instance = this;
        if (uiPanel) uiPanel.SetActive(false);
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
        uiText.text = "";
        if (uiPanel) uiPanel.SetActive(false);
    }
}