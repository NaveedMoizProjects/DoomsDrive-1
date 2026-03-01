using UnityEngine;

public class TurretController : MonoBehaviour
{
    [Header("Assignments")]
    public Transform gunTransform;
    public Transform playerTransform;
    public Transform turretBase;
    public Transform camTargetPoint; // Drag your 'Target Child' here

    [Header("Settings")]
    public float sensitivity = 2f;
    public float radius = 1.5f;
    public float heightOffset = 0f;
    public float minVertical = -20f;
    public float maxVertical = 45f;

    [Header("Raycast Aiming")]
    public float maxAimDistance = 100f;
    public LayerMask aimLayers; // Set this to ignore the Player/Turret layers

    private float _yaw = 0f;
    private float _pitch = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        _yaw = turretBase != null ? turretBase.eulerAngles.y : 0f;

        if (playerTransform != null && turretBase != null)
            heightOffset = playerTransform.position.y - turretBase.position.y;
    }

    void Update()
    {
        // 1. Mouse Input drives the Master Angles
        _yaw += Input.GetAxis("Mouse X") * sensitivity;
        _pitch -= Input.GetAxis("Mouse Y") * sensitivity;
        _pitch = Mathf.Clamp(_pitch, minVertical, maxVertical);

        // 2. Rotate Player (Y only) and Orbit
        playerTransform.rotation = Quaternion.Euler(0f, _yaw, 0f);
        float angleInRad = _yaw * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Sin(angleInRad), 0, Mathf.Cos(angleInRad)) * -radius;
        playerTransform.position = new Vector3(turretBase.position.x + offset.x, turretBase.position.y + heightOffset, turretBase.position.z + offset.z);

        // 3. Rotate Turret Base (Yaw) and Gun (Pitch + Yaw)
        if (turretBase != null) turretBase.rotation = Quaternion.Euler(0f, _yaw, 0f);
        if (gunTransform != null) gunTransform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);

        // 4. Dynamic Raycast Aiming
        UpdateCameraTarget();
    }

    void UpdateCameraTarget()
    {
        if (gunTransform == null || camTargetPoint == null) return;

        RaycastHit hit;
        // Cast ray forward from the gun
        if (Physics.Raycast(gunTransform.position, gunTransform.forward, out hit, maxAimDistance, aimLayers))
        {
            // If we hit something, move the target to the hit point
            camTargetPoint.position = hit.point;
        }
        else
        {
            // If we hit nothing (sky), put the target at max distance
            camTargetPoint.position = gunTransform.position + (gunTransform.forward * maxAimDistance);
        }
    }
}