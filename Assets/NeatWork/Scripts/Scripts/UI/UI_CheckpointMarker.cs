using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_CheckpointMarker : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 2f, 0);
    public TextMeshProUGUI distanceText;

    [Header("Edge Arrows")]
    public GameObject arrowUp;
    public GameObject arrowDown;
    public GameObject arrowLeft;
    public GameObject arrowRight;

    [Header("Settings")]
    public float margin = 50f; // How far from the edge to stay

    private Camera cam;
    private RectTransform rectTransform;

    void Start()
    {
        cam = Camera.main;
        rectTransform = GetComponent<RectTransform>();
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Get Screen Position
        Vector3 screenPos = cam.WorldToScreenPoint(target.position + offset);

        // 2. Handle "Behind" Logic
        // If screenPos.z is negative, the target is behind the camera.
        if (screenPos.z < 0)
        {
            screenPos *= -1f;
        }

        // 3. Clamp to Screen Edges
        // We restrict the icon to stay within the screen resolution minus our margin
        float minX = margin;
        float maxX = Screen.width - margin;
        float minY = margin;
        float maxY = Screen.height - margin;

        // Check if we are touching edges to trigger arrows
        bool isOffScreenX = screenPos.x <= minX || screenPos.x >= maxX;
        bool isOffScreenY = screenPos.y <= minY || screenPos.y >= maxY;

        screenPos.x = Mathf.Clamp(screenPos.x, minX, maxX);
        screenPos.y = Mathf.Clamp(screenPos.y, minY, maxY);

        // 4. Set Position
        transform.position = screenPos;

        // 5. Arrow Logic
        UpdateArrows(screenPos, minX, maxX, minY, maxY);

        // 6. Distance Text
        if (distanceText != null)
        {
            float dist = Vector3.Distance(cam.transform.position, target.position);
            distanceText.text = Mathf.FloorToInt(dist) + "m";
        }
    }

    void UpdateArrows(Vector3 pos, float minX, float maxX, float minY, float maxY)
    {
        // Only show arrows if the marker is at the very edge
        if (arrowLeft) arrowLeft.SetActive(pos.x <= minX);
        if (arrowRight) arrowRight.SetActive(pos.x >= maxX);
        if (arrowUp) arrowUp.SetActive(pos.y >= maxY);
        if (arrowDown) arrowDown.SetActive(pos.y <= minY);
    }
}