using UnityEngine;

public class TurretController : MonoBehaviour
{
    [Header("Assignments")]
    public Transform turretBase;
    public Transform playerTransform;
    public Transform gunTransform;
    public Transform camTargetPoint;
    public Transform raycastOrigin;

    [Header("Settings")]
    public float sensitivity = 2f;
    public float radius = 1.5f;
    public float manualHeightAdjustment = 0f;
    public float minVertical = -20f;
    public float maxVertical = 45f;
    public float maxAimDistance = 100f;
    public LayerMask aimLayers;

    private float _yaw = 0f;
    private float _pitch = 0f;
    private float _initialLocalHeight;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        if (playerTransform != null)
            _initialLocalHeight = playerTransform.localPosition.y;
        if (turretBase != null)
            _yaw = turretBase.localEulerAngles.y;
        if (raycastOrigin == null) raycastOrigin = gunTransform;
    }

    void Update()
    {
        // ✅ Pause ya death pe koi bhi input/movement block
        if (Time.timeScale == 0f || GamePauseManager.IsPlayerDead) return;

        // 1. INPUT
        _yaw += Input.GetAxis("Mouse X") * sensitivity;
        _pitch -= Input.GetAxis("Mouse Y") * sensitivity;
        _pitch = Mathf.Clamp(_pitch, minVertical, maxVertical);

        // 2. ROTATE TURRET BASE (Horizontal Master)
        if (turretBase != null)
            turretBase.localRotation = Quaternion.Euler(0f, _yaw, 0f);

        // 3. POSITION PLAYER (The Orbit)
        if (playerTransform != null && turretBase != null)
        {
            float angleInRad = _yaw * Mathf.Deg2Rad;
            Vector3 localDir = new Vector3(Mathf.Sin(angleInRad), 0, Mathf.Cos(angleInRad));
            Vector3 localPos = turretBase.localPosition + (localDir * -radius);
            localPos.y = _initialLocalHeight + manualHeightAdjustment;
            playerTransform.localPosition = localPos;
            playerTransform.localRotation = turretBase.localRotation;
        }

        // 4. ROTATE GUN
        if (gunTransform != null)
        {
            gunTransform.localRotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        UpdateCameraTarget();
    }

    void UpdateCameraTarget()
    {
        if (camTargetPoint == null) return;
        Transform origin = raycastOrigin != null ? raycastOrigin : gunTransform;
        RaycastHit hit;
        if (Physics.Raycast(origin.position, origin.forward, out hit, maxAimDistance, aimLayers))
            camTargetPoint.position = hit.point;
        else
            camTargetPoint.position = origin.position + (origin.forward * maxAimDistance);
    }
}