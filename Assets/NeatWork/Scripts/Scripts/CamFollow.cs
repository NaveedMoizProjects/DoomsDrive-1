using UnityEngine;

public class CamFollow : MonoBehaviour
{
    public Transform followTarget;     // This should be a reference point (like a child of car)
    public float smoothSpeed = 5f;     // Smoothing factor for position
    public float rotationSpeed = 5f;   // Smoothing for rotation

    void LateUpdate()
    {
        if (followTarget == null) return;

        // Smooth position
        transform.position = Vector3.Lerp(transform.position, followTarget.position, smoothSpeed * Time.deltaTime);

        // Smooth rotation
        transform.rotation = Quaternion.Slerp(transform.rotation, followTarget.rotation, rotationSpeed * Time.deltaTime);
    }
}
