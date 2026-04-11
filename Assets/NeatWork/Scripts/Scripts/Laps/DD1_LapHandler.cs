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

    // Map every transform that belongs to a checkpoint (root + children) -> checkpoint index
    private Dictionary<Transform, int> transformToIndex;

    // Local reference to the car's player id for winner callback
    private CarMovement carMovement;

    public void Setup(List<Transform> cpList, TextMeshProUGUI lUI, TextMeshProUGUI cUI, UI_CheckpointMarker ptr)
    {
        checkpoints = cpList;
        lapText = lUI; cpText = cUI; marker = ptr;

        // cache CarMovement (this script is attached to the car instance)
        carMovement = GetComponent<CarMovement>();

        // Build mapping once for deterministic matching
        transformToIndex = new Dictionary<Transform, int>();
        if (checkpoints != null)
        {
            for (int i = 0; i < checkpoints.Count; i++)
            {
                var cp = checkpoints[i];
                if (cp == null) continue;

                // Map the root transform
                if (!transformToIndex.ContainsKey(cp)) transformToIndex.Add(cp, i);

                // Map all child transforms so colliders located on children still resolve to this index
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
        nextCPIndex = (lastCPIndex + 1 >= checkpoints.Count) ? 0 : lastCPIndex + 1;
        RefreshSystems(lastCPIndex);
    }

    private void OnTriggerEnter(Collider other)
    {
        // stop processing if game already finished via DamageManager
        if (DamageManager.Instance != null && DamageManager.Instance.IsGameOver) return;
        if (checkpoints == null || checkpoints.Count <= 1) return;

        Debug.Log($"DD1_LapHandler: OnTriggerEnter - collider '{other.name}', expected index {nextCPIndex}", this);

        if (!IsHit(other, nextCPIndex))
            return;

        if (nextCPIndex != 0)
        {
            int currentHit = nextCPIndex;
            RespawnManager.Instance.UpdateRespawnAnchor(currentHit);
            nextCPIndex = (nextCPIndex + 1 >= checkpoints.Count) ? 0 : nextCPIndex + 1;
            RefreshSystems(currentHit);
        }
        else
        {
            if (DamageManager.Instance != null)
            {
                DamageManager.Instance.currentLap++;
                RespawnManager.Instance.savedLap = DamageManager.Instance.currentLap;
            }

            int completed = (DamageManager.Instance != null) ? DamageManager.Instance.currentLap : 0;
            int target = (DamageManager.Instance != null) ? DamageManager.Instance.lapsToWin : 1;

            if (completed >= target)
            {
                // set short delay and declare winner via DamageManager (the project "game manager")
                if (DamageManager.Instance != null)
                    DamageManager.Instance.delayBeforeMenu = 2f;

                if (carMovement != null && DamageManager.Instance != null)
                {
                    DamageManager.Instance.DeclareWinner(carMovement.playerID);
                    return; // stop further processing
                }
                else
                {
                    Debug.LogWarning("DD1_LapHandler: Winner reached but missing CarMovement or DamageManager reference.");
                }
            }

            nextCPIndex = 1;
            RespawnManager.Instance.UpdateRespawnAnchor(0);
            RefreshSystems(0);
        }
    }

    private bool IsHit(Collider other, int idx)
    {
        if (checkpoints == null || idx < 0 || idx >= checkpoints.Count) return false;

        Transform t = other.transform;
        while (t != null)
        {
            if (transformToIndex != null && transformToIndex.TryGetValue(t, out int foundIdx))
            {
                bool match = (foundIdx == idx);
                if (match)
                    Debug.Log($"DD1_LapHandler: Checkpoint HIT idx={idx} name='{checkpoints[idx].name}' by collider '{other.name}' (mapped from '{t.name}')", this);
                else
                    Debug.Log($"DD1_LapHandler: Collider '{other.name}' mapped to checkpoint idx={foundIdx} but expected {idx}", this);

                return match;
            }
            t = t.parent;
        }

        return false;
    }

    private void RefreshSystems(int currentIdx)
    {
        int curLap = (DamageManager.Instance != null) ? DamageManager.Instance.currentLap : 0;
        if (lapText) lapText.text = $"Lap: {curLap} / {(DamageManager.Instance != null ? DamageManager.Instance.lapsToWin : 1)}";
        if (cpText) cpText.text = $"CP: {currentIdx} / {Mathf.Max(0, checkpoints.Count - 1)}";

        if (marker != null && checkpoints != null && nextCPIndex >= 0 && nextCPIndex < checkpoints.Count)
            marker.target = checkpoints[nextCPIndex];
    }
}