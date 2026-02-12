using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LapHandler : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    public List<Transform> checkpoints; // [0] = Start, [1...n] = checkpoints
    private int currentCheckpointIndex = 1; // Start from checkpoint 1 (skip start)

    [Header("Lap Settings")]
    private int lapCount = 0;

    [Header("UI")]
    public TextMeshProUGUI checkpointText;
    public TextMeshProUGUI lapText;

    private CarMovement carMovement;

    private void Start()
    {
        carMovement = GetComponent<CarMovement>();

        // ✅ target laps now pulled from GameManager
        // (you no longer need a public int here)
        UpdateCheckpointUI();
        UpdateLapUI();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (checkpoints.Count <= 1) return;

        // Case: Returning to start to finish lap
        if (currentCheckpointIndex == checkpoints.Count && other.transform == checkpoints[0])
        {
            lapCount++;
            Debug.Log($"{carMovement.playerID} completed lap {lapCount}");

            UpdateLapUI();

            // ✅ use GameManager.Instance.lapsToWin
            if (lapCount >= GameManager.Instance.lapsToWin)
            {
                GameManager.Instance.DeclareWinner(carMovement.playerID);
            }

            currentCheckpointIndex = 1;
            UpdateCheckpointUI();
            return;
        }

        // Case: Normal checkpoint hit
        if (currentCheckpointIndex < checkpoints.Count && other.transform == checkpoints[currentCheckpointIndex])
        {
            Debug.Log($"{carMovement.playerID} reached checkpoint {currentCheckpointIndex}");
            currentCheckpointIndex++;
            UpdateCheckpointUI();
        }
    }

    private void UpdateCheckpointUI()
    {
        if (checkpointText != null)
        {
            int shown = Mathf.Clamp(currentCheckpointIndex - 1, 0, checkpoints.Count - 1);
            checkpointText.text = $"Checkpoint: {shown} / {checkpoints.Count - 1}";
        }
    }

    private void UpdateLapUI()
    {
        if (lapText != null)
        {
            lapText.text = "Lap: " + lapCount + " / " + GameManager.Instance.lapsToWin;
        }
    }
}
