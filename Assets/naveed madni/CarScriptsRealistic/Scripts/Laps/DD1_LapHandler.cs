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

    private void Start()
    {
        // Initialize UI using data from our Safe Path (DamageManager)
        UpdateUI();
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
        }

        // 2. Logic for Finishing a Lap (Returning to checkpoints[0])
        // Must have hit all other checkpoints first
        else if (nextCPIndex == checkpoints.Count && other.transform == checkpoints[0])
        {
            DamageManager.Instance.currentLap++;
            Debug.Log("Lap Completed: " + DamageManager.Instance.currentLap);

            // Check Win Condition
            if (DamageManager.Instance.currentLap >= DamageManager.Instance.lapsToWin)
            {
                DamageManager.Instance.DeclareWinner();
            }

            // Reset for next lap
            nextCPIndex = 1;
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (DamageManager.Instance == null) return;

        if (checkpointText != null)
        {
            // Shows "Checkpoint: 1 / 5" etc.
            checkpointText.text = $"Checkpoint: {nextCPIndex - 1} / {checkpoints.Count - 1}";
        }

        if (lapText != null)
        {
            lapText.text = $"Lap: {DamageManager.Instance.currentLap} / {DamageManager.Instance.lapsToWin}";
        }
    }
}