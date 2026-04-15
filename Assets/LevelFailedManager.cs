using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LevelFailedManager : MonoBehaviour
{
    public static LevelFailedManager Instance { get; private set; }

    [Header("Failure configuration")]
    [Tooltip("If any of these PartTypes are destroyed, level fail will trigger.")]
    public List<DamageablePart.PartType> failOnPartTypes = new List<DamageablePart.PartType>()
    {
        DamageablePart.PartType.Core,
        DamageablePart.PartType.Wheel
    };

    [Header("UI")]
    public GameObject levelFailPanel;
    public TextMeshProUGUI attemptsText;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Call from anywhere when a part is destroyed (DamageablePart will call this)
    // Now only shows fail panel when configured PartType is destroyed AND respawn attempts are exhausted.
    public void NotifyPartDestroyed(DamageablePart.PartType partType)
    {
        if (failOnPartTypes == null || !failOnPartTypes.Contains(partType))
            return;

        var rm = RespawnManager.Instance;
        if (rm != null)
        {
            // Show fail panel only when no respawns remain
            if (rm.RespawnsRemaining <= 0)
            {
                ShowLevelFailed();
            }
            else
            {
                Debug.Log($"LevelFailedManager: '{partType}' destroyed but respawns remain ({rm.RespawnsRemaining}). Not failing level.");
            }
        }
        else
        {
            // If RespawnManager missing, fallback to showing panel (safer) — adjust if you prefer different behavior.
            Debug.LogWarning("LevelFailedManager: RespawnManager.Instance not found — showing fail panel by fallback.");
            ShowLevelFailed();
        }
    }

    // Explicit show API (callable from other systems)
    public void ShowLevelFailed()
    {
        if (levelFailPanel != null) levelFailPanel.SetActive(true);

        // Display attempts info if RespawnManager is present
        if (attemptsText != null && RespawnManager.Instance != null)
        {
            int max = RespawnManager.Instance.MaxRespawns;
            int remaining = RespawnManager.Instance.RespawnsRemaining;
            int used = Mathf.Max(0, max - remaining);
            attemptsText.text = $"Attempts used: {used} / {max}";
        }

        Time.timeScale = 0f;
    }
}