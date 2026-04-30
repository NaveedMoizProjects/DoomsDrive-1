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

    private Dictionary<Transform, int> transformToIndex;
    private CarMovement carMovement;

    public void Setup(List<Transform> cpList, TextMeshProUGUI lUI, TextMeshProUGUI cUI, UI_CheckpointMarker ptr)
    {
        checkpoints = cpList;
        lapText = lUI; cpText = cUI; marker = ptr;
        carMovement = GetComponent<CarMovement>();

        transformToIndex = new Dictionary<Transform, int>();
        if (checkpoints != null)
        {
            for (int i = 0; i < checkpoints.Count; i++)
            {
                var cp = checkpoints[i];
                if (cp == null) continue;

                if (!transformToIndex.ContainsKey(cp)) transformToIndex.Add(cp, i);

                foreach (var child in cp.GetComponentsInChildren<Transform>(true))
                {
                    if (child == null) continue;
                    if (!transformToIndex.ContainsKey(child)) transformToIndex.Add(child, i);
                }
            }
        }
    }

    public void InitializeProgress(int lap, int lastCPIndex)
    {
        if (checkpoints == null || checkpoints.Count == 0) return;
        // Strictly (0 -> 1 -> 2 -> 0)
        nextCPIndex = (lastCPIndex + 1) % checkpoints.Count;
        RefreshSystems(lastCPIndex);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (DamageManager.Instance != null && DamageManager.Instance.IsGameOver) return;
        if (checkpoints == null || checkpoints.Count <= 1) return;

        if (!IsHit(other, nextCPIndex)) return;

        int currentHit = nextCPIndex;

        // Update Respawn bookmark
        RespawnManager.Instance.UpdateRespawnAnchor(currentHit);

        // TRIGGER AT END OF LIST (e.g., Index 2)
        if (currentHit == checkpoints.Count - 1)
        {
            SpawnObjectAtFinish();
        }

        // TRIGGER AT START/FINISH (Index 0)
        if (currentHit == 0)
        {
            HandleLapCompletion();
        }

        // Rotate index: 0->1->2->0
        nextCPIndex = (nextCPIndex + 1) % checkpoints.Count;

        RefreshSystems(currentHit);
    }

    private void SpawnObjectAtFinish()
    {
        // PULL FROM DAMAGE MANAGER
        if (DamageManager.Instance != null && DamageManager.Instance.objectToSpawn != null)
        {
            if (checkpoints != null && checkpoints.Count > 0)
            {
                Transform finishLine = checkpoints[0];
                Instantiate(DamageManager.Instance.objectToSpawn, finishLine.position, finishLine.rotation);
                Debug.Log("Last CP reached: Prefab from DamageManager spawned at CP 0.");
            }
        }
    }

    private void HandleLapCompletion()
    {
        if (DamageManager.Instance != null)
        {
            DamageManager.Instance.currentLap++;
            RespawnManager.Instance.savedLap = DamageManager.Instance.currentLap;

            if (DamageManager.Instance.currentLap >= DamageManager.Instance.lapsToWin)
            {
                if (carMovement != null)
                {
                    DamageManager.Instance.DeclareWinner(carMovement.playerID);
                }
            }
        }
    }

    private bool IsHit(Collider other, int idx)
    {
        Transform t = other.transform;
        while (t != null)
        {
            if (transformToIndex != null && transformToIndex.TryGetValue(t, out int foundIdx))
            {
                return (foundIdx == idx);
            }
            t = t.parent;
        }
        return false;
    }

    private void RefreshSystems(int currentIdx)
    {
        int curLap = (DamageManager.Instance != null) ? DamageManager.Instance.currentLap : 0;
        if (lapText) lapText.text = $"Lap: {curLap} / {(DamageManager.Instance != null ? DamageManager.Instance.lapsToWin : 1)}";
        if (cpText) cpText.text = $"CP: {currentIdx} / {checkpoints.Count - 1}";

        if (marker != null && checkpoints != null)
            marker.target = checkpoints[nextCPIndex];
    }
}