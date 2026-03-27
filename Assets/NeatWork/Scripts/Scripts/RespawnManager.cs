using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// RespawnManager: attach to the player vehicle (or a central manager and assign references).
[RequireComponent(typeof(DynamicCarController))]
public class RespawnManager : MonoBehaviour
{
    [Header("References (assign in inspector or auto-find)")]
    public DD1_LapHandler lapHandler;
    public WorldTriggerMessage.MessageType promptType = WorldTriggerMessage.MessageType.Warning;

    [Header("Prompt Settings")]
    public float promptTimeout = 20f; // auto-cancel after this

    [Header("Pause Settings")]
    [Tooltip("Realtime seconds to pause when showing respawn prompt. Inputs are queued and executed after this period.")]
    public float pauseDuration = 5f;
    [Tooltip("If true, the game will be paused (Time.timeScale = 0) while respawn prompt is active.")]
    public bool pauseOnPrompt = true;

    [Header("Respawn / Replacement")]
    [Tooltip("Optional: assign the original car prefab here. When set, respawn will instantiate a fresh copy and remove the old car.")]
    public GameObject carPrefab;

    [Header("Respawn Limits")]
    [Tooltip("Set to -1 for unlimited respawns")]
    public int maxRespawns = -1;
    private int respawnsUsed = 0;

    [Tooltip("Player invulnerability (unscaled seconds) after respawn")]
    public float invulnerableDuration = 2f;

    public KeyCode cancelKey = KeyCode.Escape;

    private DynamicCarController controller;
    private bool promptActive = false;
    private string currentReason = "";
    private Vector3 savedStartPos;
    private Quaternion savedStartRot;

    // pause state
    private float previousTimeScale = 1f;
    private bool pausedByRespawn = false;

    // queued input state
    private bool confirmQueued = false;
    private bool cancelQueued = false;

    // Snapshot of original hierarchy/poses for reattachment of separated body parts
    private class PartSnapshot
    {
        public string partName;        // DamageablePart.partName if available
        public string objectName;      // GameObject.name
        public string relativeParentPath; // path from car root to parent (empty => root)
        public Vector3 localPos;
        public Quaternion localRot;
    }
    private List<PartSnapshot> originalPartSnapshots = new List<PartSnapshot>();

    void Start()
    {
        controller = GetComponent<DynamicCarController>();
        savedStartPos = transform.position;
        savedStartRot = transform.rotation;

        if (lapHandler == null)
            lapHandler = FindObjectOfType<DD1_LapHandler>();

        // Capture snapshot of all DamageablePart transforms of the original vehicle so we can reattach detached pieces on respawn.
        CaptureOriginalPartSnapshots();
    }

    private void CaptureOriginalPartSnapshots()
    {
        originalPartSnapshots.Clear();
        var parts = GetComponentsInChildren<DamageablePart>(true);
        foreach (var p in parts)
        {
            // store parent path relative to vehicle root (exclude the part itself)
            string relPath = GetRelativePathToRoot(p.transform.parent, transform);
            originalPartSnapshots.Add(new PartSnapshot
            {
                partName = !string.IsNullOrEmpty(p.partName) ? p.partName : p.gameObject.name,
                objectName = p.gameObject.name,
                relativeParentPath = relPath,
                localPos = p.transform.localPosition,
                localRot = p.transform.localRotation
            });
        }
    }

    // return path from root to target (target can be null or root -> return empty)
    private string GetRelativePathToRoot(Transform target, Transform root)
    {
        if (target == null || target == root) return string.Empty;
        var stack = new Stack<string>();
        Transform cur = target;
        while (cur != null && cur != root)
        {
            stack.Push(cur.name);
            cur = cur.parent;
        }
        if (cur != root) // not a descendant
            return string.Empty;
        return string.Join("/", stack.ToArray());
    }

    // find child transform under root by relative path ("Body/Doors")
    private Transform FindChildByRelativePath(Transform root, string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return root;
        var parts = relativePath.Split('/');
        Transform cur = root;
        foreach (var part in parts)
        {
            if (cur == null) return null;
            cur = cur.Find(part);
        }
        return cur;
    }

    void Update()
    {
        if (DamageManager.Instance == null || controller == null) return;

        if (promptActive)
        {
            // waiting for countdown/queue resolution; ignore further automatic checks.
            return;
        }

        // If respawn limit reached, do not prompt
        if (maxRespawns >= 0 && respawnsUsed >= maxRespawns)
            return;

        // 1) Core destroyed (health percentage <= 0)
        if (DamageManager.Instance.GetPlayerHealthPercentage() <= 0f)
        {
            ShowRespawnPrompt("Core destroyed. Respawn to last checkpoint? Press R to respawn, Esc to cancel.");
            return;
        }

        // 2) Any tyre detached: check wheels list for null collider
        foreach (var wheel in controller.wheels)
        {
            if (wheel.wheelCollider == null)
            {
                ShowRespawnPrompt("A tyre is detached. Racing is difficult — respawn to last checkpoint? Press R to respawn, Esc to cancel.");
                return;
            }
        }
    }

    private void ShowRespawnPrompt(string reason)
    {
        if (promptActive) return;

        // if out of respawns, show a short message and return
        if (maxRespawns >= 0 && respawnsUsed >= maxRespawns)
        {
            if (MessageUIManager.Instance != null)
                MessageUIManager.Instance.ProcessMessage("No respawns left.", WorldTriggerMessage.MessageType.Defeat, 3f);
            return;
        }

        currentReason = reason;
        promptActive = true;
        confirmQueued = false;
        cancelQueued = false;

        // Disable player control safely
        if (controller != null)
            controller.enabled = false;

        // Pause game if configured
        if (pauseOnPrompt)
            PauseGame();

        // Freeze HUD updates so UI won't change while paused
        if (DamageManager.Instance != null)
            DamageManager.Instance.hudPaused = true;

        // Use queued callbacks instead of direct Perform/Cancel so actions run after pauseDuration
        if (MessageUIManager.Instance != null)
        {
            MessageUIManager.Instance.ProcessPrompt(reason, promptType, pauseDuration, OnConfirmQueued, OnCancelQueued);
        }
        else
        {
            Debug.LogWarning("[RespawnManager] MessageUIManager not found. Prompt: " + reason);
        }

        // start the realtime countdown that executes the queued choice at the end
        StartCoroutine(PromptQueueCoroutine(pauseDuration));
    }

    // called by UI, queues confirm
    private void OnConfirmQueued()
    {
        confirmQueued = true;
    }

    // called by UI, queues cancel
    private void OnCancelQueued()
    {
        cancelQueued = true;
    }

    private IEnumerator PromptQueueCoroutine(float realtimeSeconds)
    {
        // wait in realtime while timescale can be 0
        yield return new WaitForSecondsRealtime(realtimeSeconds);

        // resolve the queued action in priority: confirm -> cancel -> timeout (cancel)
        if (confirmQueued)
            ExecuteQueuedPerform();
        else
            ExecuteQueuedCancel();
    }

    private void ExecuteQueuedPerform()
    {
        // perform respawn now
        PerformRespawn();
    }

    private void ExecuteQueuedCancel()
    {
        // cancel prompt and restore state
        CancelRespawn();
    }

    private void CancelRespawn()
    {
        // Called when queued cancel executed or when no action queued after countdown
        promptActive = false;
        currentReason = "";
        confirmQueued = false;
        cancelQueued = false;

        if (controller != null)
            controller.enabled = true;

        // Unpause if we paused
        if (pauseOnPrompt)
            UnpauseGame();

        // Unfreeze HUD
        if (DamageManager.Instance != null)
            DamageManager.Instance.hudPaused = false;

        if (MessageUIManager.Instance != null)
            MessageUIManager.Instance.ProcessMessage("Respawn cancelled.", WorldTriggerMessage.MessageType.Checkpoint, 2f);
    }

    private void PerformRespawn()
    {
        // Called when queued confirm is executed
        promptActive = false;
        confirmQueued = false;
        cancelQueued = false;

        // consume a respawn
        if (maxRespawns >= 0)
            respawnsUsed++;

        // Reset any FinalizeGame flag
        if (DamageManager.Instance != null)
            DamageManager.Instance.isGameOver = false;

        // --- Determine target pose (try last passed checkpoint, fallback to start) ---
        Transform cp = null;

        if (lapHandler == null)
            lapHandler = FindObjectOfType<DD1_LapHandler>(); // defensive: try to find again

        if (lapHandler != null)
        {
            cp = lapHandler.GetLastPassedCheckpoint();

            // Defensive fallback: if GetLastPassedCheckpoint returned null but checkpoints exist, use index 0
            if (cp == null && lapHandler.checkpoints != null && lapHandler.checkpoints.Count > 0)
            {
                cp = lapHandler.checkpoints[0];
                Debug.LogWarning("[RespawnManager] GetLastPassedCheckpoint returned null — falling back to checkpoints[0].");
            }
        }

        if (lapHandler == null)
            Debug.LogWarning("[RespawnManager] lapHandler is null. Using saved start position for respawn.");
        if (cp == null)
            Debug.Log("[RespawnManager] No passed checkpoint found. Respawning at start position.");
        else
            Debug.Log($"[RespawnManager] Respawning to checkpoint: {cp.name} at {cp.position}");

        // If the checkpoint has a dedicated spawn child (common pattern), prefer it.
        Transform spawn = null;
        if (cp != null)
        {
            spawn = cp.Find("SpawnPoint") ?? cp.Find("RespawnPoint") ?? cp;
        }

        Vector3 targetPos = (spawn != null) ? spawn.position : savedStartPos;
        Quaternion targetRot = (spawn != null) ? spawn.rotation : savedStartRot;

        // If carPrefab is provided, fully replace the vehicle with a clean instance.
        if (carPrefab != null)
        {
            GameObject oldCar = this.gameObject;

            // Clear old car entries from DamageManager registry before destroying it
            if (DamageManager.Instance != null)
                DamageManager.Instance.ClearOwnerEntries(oldCar);

            // Instantiate new car prefab at target position
            GameObject newCar = GameObject.Instantiate(carPrefab, targetPos + Vector3.up * 0.5f, targetRot);
            newCar.name = carPrefab.name;

            // Ensure the spawned vehicle is tagged as Player (HUD & DamageManager filters rely on this)
            newCar.tag = "Player";

            // Repair & register every DamageablePart on the new car so DamageManager and HUD are populated immediately
            var newParts = newCar.GetComponentsInChildren<DamageablePart>(true);
            foreach (var p in newParts)
            {
                // Reset internal broken state and health
                p.RepairPart();

                // Force register with DamageManager using the new car as owner
                if (DamageManager.Instance != null)
                    DamageManager.Instance.UpdateHealth(p.GetInstanceID(), p.health, p.type, newCar);
            }

            // Recalculate baseline for HUD
            if (DamageManager.Instance != null)
                DamageManager.Instance.RecalculatePlayerMaxHealth(newCar);

            // Register new vehicle with RCC_SceneManager if present
            var newController = newCar.GetComponent<RCC_CarControllerV3>();
            if (newController != null && RCC_SceneManager.Instance != null)
            {
                RCC_SceneManager.Instance.RegisterPlayer(newController);
            }

            // Destroy old car gameObject
            Destroy(oldCar);

            // Unpause and unfreeze HUD
            if (pauseOnPrompt)
                UnpauseGame();
            if (DamageManager.Instance != null)
                DamageManager.Instance.hudPaused = false;

            // Make new car invulnerable briefly (DamageableParts will respect allowPlayerDamage)
            if (DamageManager.Instance != null)
            {
                DamageManager.Instance.allowPlayerDamage = false;
                StartCoroutine(EndInvulnerabilityAfterRealtime(invulnerableDuration));
            }

            if (MessageUIManager.Instance != null)
                MessageUIManager.Instance.ProcessMessage("Respawned: new vehicle spawned.", WorldTriggerMessage.MessageType.Checkpoint, 3f);
            else
                Debug.Log("<color=green>Respawned: new vehicle spawned.</color>");

            return;
        }

        // --- Fallback: in-place repair if no prefab assigned ---

        // Apply teleport and zero velocities
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = targetPos + Vector3.up * 0.5f;
            rb.rotation = targetRot;
            rb.Sleep();
        }
        else
        {
            transform.SetPositionAndRotation(targetPos + Vector3.up * 0.5f, targetRot);
        }

        // 1) Reset meshes (MeshDeformer)
        var deformers = GetComponentsInChildren<MeshDeformer>(true);
        foreach (var d in deformers)
            d.ResetDeformation();

        // 2) Attempt to reattach separated body parts found in the scene that originally belonged to this vehicle
        ReattachSeparatedParts();

        // 3) Restore parts health and update DamageManager registry (now includes reattached parts)
        var parts = GetComponentsInChildren<DamageablePart>(true);
        foreach (var p in parts)
        {
            // Use RepairPart to reset internal broken state and health
            p.RepairPart();

            // Ensure DamageManager knows the part is restored (owner is root)
            if (DamageManager.Instance != null)
                DamageManager.Instance.UpdateHealth(p.GetInstanceID(), p.health, p.type, p.transform.root.gameObject);
        }

        // Recalculate the player's health baseline so HUD and GetPlayerHealthPercentage are correct
        if (DamageManager.Instance != null)
            DamageManager.Instance.RecalculatePlayerMaxHealth(this.gameObject);

        // 4) Attempt to reattach wheel colliders and wheel models
        for (int i = 0; i < controller.wheels.Count; i++)
        {
            var w = controller.wheels[i];

            if (w.wheelCollider == null)
            {
                // find any WheelCollider under the vehicle (include inactive)
                WheelCollider found = null;
                var allColliders = GetComponentsInChildren<WheelCollider>(true);
                foreach (var c in allColliders)
                {
                    // match using part name contained in collider name (we set this when disconnecting)
                    if (!string.IsNullOrEmpty(w.wheelName) && c.gameObject.name.Contains(w.wheelName))
                    {
                        found = c;
                        break;
                    }
                }

                // Fallback: pick first unused collider (not referenced by any wheel)
                if (found == null)
                {
                    foreach (var c in allColliders)
                    {
                        bool used = false;
                        foreach (var other in controller.wheels)
                        {
                            if (other.wheelCollider == c) { used = true; break; }
                        }
                        if (!used) { found = c; break; }
                    }
                }

                if (found != null)
                {
                    found.gameObject.SetActive(true);
                    found.enabled = true;

                    // reassign
                    w.wheelCollider = found;

                    // try to reparent wheel model back under collider and remove physics added at detach
                    if (w.wheelModel != null)
                    {
                        // remove Rigidbody & MeshCollider added during disconnect
                        var rbPart = w.wheelModel.GetComponent<Rigidbody>();
                        if (rbPart != null) Destroy(rbPart);
                        var mc = w.wheelModel.GetComponent<MeshCollider>();
                        if (mc != null && mc.convex) Destroy(mc);

                        // parent it back under the collider and snap to collider world pose
                        w.wheelModel.transform.SetParent(found.transform, true);

                        Vector3 pos;
                        Quaternion rot;
                        found.GetWorldPose(out pos, out rot);
                        w.wheelModel.transform.position = pos;
                        w.wheelModel.transform.rotation = rot;
                    }

                    controller.wheels[i] = w;
                }
            }
        }

        // Re-enable control
        if (controller != null)
            controller.enabled = true;

        // Unpause if we paused when showing prompt
        if (pauseOnPrompt)
            UnpauseGame();

        // Unfreeze HUD
        if (DamageManager.Instance != null)
            DamageManager.Instance.hudPaused = false;

        // make player invulnerable for a short realtime duration
        if (DamageManager.Instance != null)
        {
            DamageManager.Instance.allowPlayerDamage = false;
            StartCoroutine(EndInvulnerabilityAfterRealtime(invulnerableDuration));
        }

        if (MessageUIManager.Instance != null)
            MessageUIManager.Instance.ProcessMessage("Respawned at last checkpoint.", WorldTriggerMessage.MessageType.Checkpoint, 3f);
        else
            Debug.Log("<color=green>Respawned at last checkpoint.</color>");
    }

    // Replace only the ReattachSeparatedParts method in RespawnManager.cs
    private void ReattachSeparatedParts()
    {
        // Gather all DamageablePart instances in scene (including detached ones)
        var allParts = FindObjectsOfType<DamageablePart>(true);

        foreach (var snap in originalPartSnapshots)
        {
            // Prefer matching by DamageablePart.partName first, fallback to GameObject.name
            var match = allParts.FirstOrDefault(p =>
                (!string.IsNullOrEmpty(p.partName) && p.partName == snap.partName) ||
                p.gameObject.name == snap.objectName
            );

            if (match == null) continue;

            // If it's already part of this vehicle, skip
            if (match.transform.root == this.gameObject) continue;

            // Reparent to stored path (if exists), else to root
            Transform parentTarget = FindChildByRelativePath(this.transform, snap.relativeParentPath) ?? this.transform;

            // Attach and restore local transform
            match.transform.SetParent(parentTarget, false);
            match.transform.localPosition = snap.localPos;
            match.transform.localRotation = snap.localRot;

            // If this detached object is NOT an RCC detachable part, remove physics artifacts (they were added at detach time)
            var detachable = match.GetComponent<RCC_DetachablePart>();
            if (detachable == null)
            {
                // IMPORTANT: destroy any ConfigurableJoint first — Unity will error if you remove the Rigidbody while joint exists.
                var strayCJ = match.GetComponent<ConfigurableJoint>();
                if (strayCJ != null)
                    Destroy(strayCJ);

                var rbPart = match.GetComponent<Rigidbody>();
                if (rbPart != null)
                    Destroy(rbPart);

                var mc = match.GetComponent<MeshCollider>();
                if (mc != null)
                    Destroy(mc);
            }
            else
            {
                // For RCC_DetachablePart: ensure part collider disabled (it will be managed by the detachable logic)
                if (detachable.partCollider != null)
                    detachable.partCollider.enabled = false;

                // Call OnRepair to recreate / restore joint and internal state
                detachable.OnRepair();

                // Ensure the recreated joint connects to this vehicle's Rigidbody
                var ownerRb = this.GetComponent<Rigidbody>();
                if (ownerRb != null && detachable.Joint != null)
                    detachable.Joint.connectedBody = ownerRb;

                // Place joint transform under the expected parent so Unity's joint coordinates remain correct
                if (detachable.Joint != null)
                {
                    detachable.Joint.transform.SetParent(parentTarget, false);
                    detachable.Joint.transform.localPosition = snap.localPos;
                    detachable.Joint.transform.localRotation = snap.localRot;
                }
            }

            match.gameObject.SetActive(true);

            // Repair & re-register so DamageManager stops counting detached object's old owner
            match.RepairPart();

            // Ensure DamageManager uses this car as owner (this updates registry entry owner)
            if (DamageManager.Instance != null)
                DamageManager.Instance.UpdateHealth(match.GetInstanceID(), match.health, match.type, this.gameObject);
        }
    }

    private IEnumerator EndInvulnerabilityAfterRealtime(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        if (DamageManager.Instance != null)
            DamageManager.Instance.allowPlayerDamage = true;
    }

    private void PauseGame()
    {
        if (pausedByRespawn) return;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        pausedByRespawn = true;
    }

    private void UnpauseGame()
    {
        if (!pausedByRespawn) return;
        Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;
        AudioListener.pause = false;
        pausedByRespawn = false;
    }
    private void Awake()
    {
        // Ensure we have the lap handler as early as possible (in case checkpoints update before Start).
        if (lapHandler == null)
            lapHandler = FindObjectOfType<DD1_LapHandler>();

        if (lapHandler == null)
            Debug.LogWarning("[RespawnManager] DD1_LapHandler not found in scene. Assign lapHandler in inspector or ensure the component exists and is active.");
        else
            Debug.Log($"[RespawnManager] Found DD1_LapHandler with {(lapHandler.checkpoints != null ? lapHandler.checkpoints.Count : 0)} checkpoints.");
    }
}