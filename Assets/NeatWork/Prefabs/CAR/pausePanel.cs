using UnityEngine;
using UnityEngine.SceneManagement;

public class GamePauseManager : MonoBehaviour
{
    public GameObject pauseMenu;
    public string mainMenuSceneName;
    public string levelSelectionSceneName;

    private bool isPaused = false;

    // List of scripts to disable when paused (optional, drag here)
   // public MonoBehaviour[] scriptsToDisableOnPause;

    void Start()
    {
        ResumeGame(); // Ensure game starts unpaused
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        pauseMenu.SetActive(true);
        Time.timeScale = 0f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        //// Disable all scripts that might take input
        //foreach (var script in scriptsToDisableOnPause)
        //{
        //    if (script != null)
        //        script.enabled = false;
        //}

        isPaused = true;
    }

    public void ResumeGame()
    {
        pauseMenu.SetActive(false);
        Time.timeScale = 1f;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        //// Enable input scripts back
        //foreach (var script in scriptsToDisableOnPause)
        //{
        //    if (script != null)
        //        script.enabled = true;
        //}

        isPaused = false;
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

    public void ReloadLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ExitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();
        Debug.Log("Game Exited");
    }
}