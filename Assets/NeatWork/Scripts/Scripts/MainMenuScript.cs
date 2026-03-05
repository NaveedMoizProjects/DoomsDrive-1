using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuScript : MonoBehaviour
{
    public GameObject levelSelectPanel;
    public GameObject levelListPanel;
    public GameObject optionsPanel;
    
    // Called by Start Button
    public void OnStartClicked()
    {
        levelSelectPanel.SetActive(true);
    }

    // Called by Options Button
    public void OnOptionsClicked()
    {
        optionsPanel.SetActive(true);
    }

    // Called by Exit Button
    public void OnExitClicked()
    {
        Application.Quit();
        Debug.Log("Game Exiting..."); // Won’t quit in editor, just logs
    }

    // Called by Back buttons inside panels
    public void OnBackToMenu()
    {
        levelSelectPanel.SetActive(false);
        levelListPanel.SetActive(false);
        optionsPanel.SetActive(false);
    }

    // Called by level buttons

    public void OnLapSelected()
    {
        levelListPanel.SetActive(true);
        levelSelectPanel.SetActive(false);
        optionsPanel.SetActive(false);
    }
    public void LoadLevel(string levelName)
    {
        SceneManager.LoadScene(levelName);
    }
}