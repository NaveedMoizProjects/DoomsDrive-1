using UnityEngine;
using UnityEngine.SceneManagement;

public class GamePauseManager : MonoBehaviour
{
    public GameObject pauseMenu;
    public string mainMenuSceneName;
    public string levelSelectionSceneName;
    private bool isPaused = false;

    [Header("References")]
    public Gun gun;
    public TurretController turretController;

    public static bool IsPlayerDead = false;

    void Start()
    {
        IsPlayerDead = false;
        ResumeGame();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsPlayerDead) return;

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
        isPaused = true;

      if (gun != null) gun.enabled = false;
        if (turretController != null) turretController.enabled = false;
    }

    public void ResumeGame()
    {
        pauseMenu.SetActive(false);
        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        isPaused = false;

      if (gun != null) gun.enabled = true;
        if (turretController != null) turretController.enabled = true;
    }

    public static void ShowOnPlayerDeath()
    {
        IsPlayerDead = true;

        GamePauseManager instance = FindObjectOfType<GamePauseManager>();
        if (instance != null)
        {
            instance.pauseMenu.SetActive(true);
            Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            instance.isPaused = true;

           if (instance.gun != null) instance.gun.enabled = false;
            if (instance.turretController != null) instance.turretController.enabled = false;
        }
    }

    public void LoadMainMenu()
    {
        IsPlayerDead = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void LoadLevelSelection()
    {
        IsPlayerDead = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(levelSelectionSceneName);
    }

    public void ReloadLevel()
    {
        IsPlayerDead = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ExitGame()
    {
        IsPlayerDead = false;
        Time.timeScale = 1f;
        Application.Quit();
        Debug.Log("Game Exited");
    }
}