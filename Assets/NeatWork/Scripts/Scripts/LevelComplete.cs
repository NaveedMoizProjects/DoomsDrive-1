using UnityEngine;

public class LevelComplete : MonoBehaviour
{
    [Header("Level Complete Panel")]
    public GameObject levelCompletePanel;

    void Start()
    {
        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        // Player car (tag: PlayerBullet), Player bullet, ya AI (enemy) trigger kare
        string tag = other.tag;
        Debug.Log($"LevelComplete trigger hit by: {other.name} | Tag: {tag}");

        if (tag == "PlayerBullet" || tag == "AI" || tag == "Player")
        {
            ShowLevelComplete();
        }
    }

    void ShowLevelComplete()
    {
        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(true);

        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}