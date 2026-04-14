using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelFailedPanel : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Assign your Level Failed panel here.")]
    public GameObject levelFailedPanel;
    public string levelSelectionSceneName;
    public string mainMenuSceneName;
    public void RetryLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void LoadLevelSelection()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(levelSelectionSceneName);
    }

    public void ExitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();
        Debug.Log("Game Exited");
    }
}