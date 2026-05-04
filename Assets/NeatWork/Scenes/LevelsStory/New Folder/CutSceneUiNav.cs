using UnityEngine;
using UnityEngine.SceneManagement;

public class CutsceneUINav : MonoBehaviour
{
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu"); // Apne menu scene ka naam likhein
    }

    public void GoToLevelSelect()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("LevelSelection");
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}