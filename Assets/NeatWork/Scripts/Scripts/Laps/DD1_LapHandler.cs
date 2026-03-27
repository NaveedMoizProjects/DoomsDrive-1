using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DD1_LapHandler : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    public List<Transform> checkpoints; // [0] = Start/Finish Line
    private int nextCPIndex = 1;

    [Header("UI")]
    public TextMeshProUGUI checkpointText;
    public TextMeshProUGUI lapText;

    [Header("Objective Marker")]
    public UI_CheckpointMarker marker; // Assign your UI Marker Image here

    private void Start()
    {
        UpdateUI();
        UpdateMarkerTarget(); // Set initial target
    }

    private void OnTriggerEnter(Collider other)
    {
        if (checkpoints.Count <= 1 || DamageManager.Instance == null) return;

        // 1. Logic for Normal Checkpoints
        if (nextCPIndex < checkpoints.Count && other.transform == checkpoints[nextCPIndex])
        {
            Debug.Log("Reached Checkpoint: " + nextCPIndex);
            nextCPIndex++;
            UpdateUI();
            UpdateMarkerTarget(); // Update marker to next CP
        }

        // 2. Logic for Finishing a Lap (Returning to checkpoints[0])
        else if (nextCPIndex == checkpoints.Count && other.transform == checkpoints[0])
        {
            DamageManager.Instance.currentLap++;
            Debug.Log("Lap Completed: " + DamageManager.Instance.currentLap);

            if (DamageManager.Instance.currentLap >= DamageManager.Instance.lapsToWin)
            {
                DamageManager.Instance.FinalizeGame("RACE COMPLETED: VICTORY!");
            }

            // Reset for next lap
            nextCPIndex = 1;
            UpdateUI();
            UpdateMarkerTarget(); // Point back to the first checkpoint
        }
    }

    private void UpdateMarkerTarget()
    {
        if (marker == null) return;

        // If we still have checkpoints left, point to the next one
        if (nextCPIndex < checkpoints.Count)
        {
            marker.target = checkpoints[nextCPIndex];
        }
        else
        {
            // If all intermediate checkpoints are hit, point to the Finish Line (0)
            marker.target = checkpoints[0];
        }
    }

    private void UpdateUI()
    {
        if (DamageManager.Instance == null) return;

        if (checkpointText != null)
        {
            checkpointText.text = $"Checkpoint: {nextCPIndex - 1} / {checkpoints.Count - 1}";
        }

        if (lapText != null)
        {
            lapText.text = $"Lap: {DamageManager.Instance.currentLap} / {DamageManager.Instance.lapsToWin}";
        }
    }

    // New utility: return the last checkpoint the player passed (safe access)
    public Transform GetLastPassedCheckpoint()
    {
        if (checkpoints == null || checkpoints.Count == 0)
            return null;

        int lastIndex = Mathf.Clamp(nextCPIndex - 1, 0, checkpoints.Count - 1);
        return checkpoints[lastIndex];
    }
}