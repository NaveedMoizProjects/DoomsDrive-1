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

    [Header("References")]
    public GameObject pausePanel;
    //naveed here

    private Gun gun;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Find Gun by GameObject name
        GameObject gunObj = GameObject.Find("Gun");
        if (gunObj != null)
            gun = gunObj.GetComponent<Gun>();
        else
            Debug.LogWarning("LevelFailedManager: GameObject named 'Gun' not found in scene!");
    }

    public void NotifyPartDestroyed(DamageablePart.PartType partType)
    {
        if (failOnPartTypes == null || !failOnPartTypes.Contains(partType))
            return;

        var rm = RespawnManager.Instance;
        if (rm != null)
        {
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
            Debug.LogWarning("LevelFailedManager: RespawnManager.Instance not found — showing fail panel by fallback.");
            ShowLevelFailed();
        }
    }

    public void ShowLevelFailed()
    {
        // Hide pause panel immediately
        if (pausePanel != null)
            Destroy(pausePanel);

        // Disable mouse movement via Gun script
        if (gun != null)
            gun.enabled = false;

        // Show fail panel
        if (levelFailPanel != null)
            levelFailPanel.SetActive(true);

        // Display attempts info
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