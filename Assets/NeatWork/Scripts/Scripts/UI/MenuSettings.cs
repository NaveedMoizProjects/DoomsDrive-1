using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuSettings : MonoBehaviour
{
    public GameObject MenuPanel;

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            OnResume();
        }
    }
    public void OnResume()
    {
        MenuPanel.SetActive(false);
        Time.timeScale = 1.0f;
    }
    public void OnPauseClick()
    {
        MenuPanel.SetActive(true);
        Time.timeScale = 0f;
    }
    public void GotoMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
    public void OnRestart()
    {
        Time.timeScale = 1f;
        MenuPanel.SetActive(false);
        // load this scene
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }
}
