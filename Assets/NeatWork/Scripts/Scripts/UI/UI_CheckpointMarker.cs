using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_CheckpointMarker : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 2f, 0);
    public TextMeshProUGUI distanceText;

    [Header("Camera Assignment")]
    public Camera playerCam; // Drag the specific player's camera here!

    [Header("Edge Arrows")]
    public GameObject arrowUp;
    public GameObject arrowDown;
    public GameObject arrowLeft;
    public GameObject arrowRight;

    [Header("Settings")]
    public float margin = 50f;

    void Start()
    {
        if (playerCam == null) playerCam = Camera.main;
    }

    void LateUpdate()
    {
        if (target == null || playerCam == null) return;

        // 1. Get Screen Position relative to the FULL screen
        Vector3 screenPos = playerCam.WorldToScreenPoint(target.position + offset);

        // 2. Handle "Behind" Logic
        if (screenPos.z < 0)
        {
            screenPos *= -1f;
        }

        // 3. DEFINE THE HALF-SCREEN BOUNDARIES
        // playerCam.pixelRect gives us the exact box on the screen for this camera
        Rect camRect = playerCam.pixelRect;

        float minX = camRect.xMin + margin;
        float maxX = camRect.xMax - margin;
        float minY = camRect.yMin + margin;
        float maxY = camRect.yMax - margin;

        // 4. Clamp to the Camera's Viewport specifically
        screenPos.x = Mathf.Clamp(screenPos.x, minX, maxX);
        screenPos.y = Mathf.Clamp(screenPos.y, minY, maxY);

        // 5. Apply Position
        transform.position = new Vector3(screenPos.x, screenPos.y, 0);

        // 6. Arrow Logic (Using the new clamped bounds)
        UpdateArrows(screenPos, minX, maxX, minY, maxY);

        // 7. Distance Text
        if (distanceText != null)
        {
            float dist = Vector3.Distance(playerCam.transform.position, target.position);
            distanceText.text = Mathf.FloorToInt(dist) + "m";
        }
    }

    void UpdateArrows(Vector3 pos, float minX, float maxX, float minY, float maxY)
    {
        // Arrows trigger when touching the boundary of the HALF-screen
        if (arrowLeft) arrowLeft.SetActive(pos.x <= minX + 1f);
        if (arrowRight) arrowRight.SetActive(pos.x >= maxX - 1f);
        if (arrowUp) arrowUp.SetActive(pos.y >= maxY - 1f);
        if (arrowDown) arrowDown.SetActive(pos.y <= minY + 1f);
    }
}