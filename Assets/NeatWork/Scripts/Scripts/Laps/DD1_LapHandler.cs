using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DD1_LapHandler : MonoBehaviour
{
    private List<Transform> checkpoints;
    private TextMeshProUGUI lapText;
    private TextMeshProUGUI cpText;
    private UI_CheckpointMarker marker;
    private int nextCPIndex = 1;

    public void Setup(List<Transform> cpList, TextMeshProUGUI lUI, TextMeshProUGUI cUI, UI_CheckpointMarker ptr)
    {
        checkpoints = cpList; lapText = lUI; cpText = cUI; marker = ptr;
    }

    public void InitializeProgress(int lap, int lastCPIndex)
    {
        nextCPIndex = (lastCPIndex + 1 >= checkpoints.Count) ? 0 : lastCPIndex + 1;
        RefreshSystems(lastCPIndex);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (checkpoints == null || checkpoints.Count <= 1) return;

        // --- THE VALIDATION GATE ---
        // Only proceed if the hit object IS the next specific checkpoint in our list
        if (!IsHit(other, nextCPIndex))
        {
            return; // Exit immediately. No message, no update, total silence.
        }

        // 1. Handle Normal Checkpoints (1 to Max)
        if (nextCPIndex != 0)
        {
            int currentHit = nextCPIndex;

            // Save this as the new "Safe Spot" in the Manager
            RespawnManager.Instance.UpdateRespawnAnchor(currentHit);

            // Set the next target
            nextCPIndex = (nextCPIndex + 1 >= checkpoints.Count) ? 0 : nextCPIndex + 1;

            RefreshSystems(currentHit);
        }
        // 2. Handle Finish Line (Index 0)
        else
        {
            if (DamageManager.Instance != null)
            {
                DamageManager.Instance.currentLap++;
                RespawnManager.Instance.savedLap = DamageManager.Instance.currentLap;
            }

            nextCPIndex = 1;
            RespawnManager.Instance.UpdateRespawnAnchor(0);
            RefreshSystems(0);
        }
    }

    private bool IsHit(Collider other, int idx)
    {
        // Check if the collider belongs to the specific checkpoint transform at this index
        return other.transform == checkpoints[idx] || other.transform.IsChildOf(checkpoints[idx]);
    }

    private void RefreshSystems(int currentIdx)
    {
        int curLap = (DamageManager.Instance != null) ? DamageManager.Instance.currentLap : 1;
        if (lapText) lapText.text = $"Lap: {curLap}";
        if (cpText) cpText.text = $"CP: {currentIdx} / {checkpoints.Count - 1}";

        // Update the pointer to show the ONLY checkpoint that matters now
        if (marker != null) marker.target = checkpoints[nextCPIndex];
    }
}