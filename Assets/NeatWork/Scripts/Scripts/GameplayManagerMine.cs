using UnityEngine;

public class GameplayManagerMine : MonoBehaviour
{
    public static GameplayManagerMine instance;

    public GameObject levelFailPanel;

    void Awake()
    {
        instance = this;
    }

    public void LevelFail()
    {
        levelFailPanel.SetActive(true);
        Time.timeScale = 0f;
    }
}