using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LockingUnlocking : MonoBehaviour
{
    public Button[] levelButtons;
    public GameObject[] lockImages;
    public GameObject[] lockbgImages;

    int unlockedLevels;

    void Start()
    {
        unlockedLevels = PlayerPrefs.GetInt("unlockedLevels", 1);

        for (int i = 0; i < levelButtons.Length; i++)
        {
            if (i < unlockedLevels)
            {
                levelButtons[i].interactable = true;
                lockImages[i].SetActive(false);
                lockbgImages[i].SetActive(false);
            }
            else
            {
                levelButtons[i].interactable = false;
                lockImages[i].SetActive(true);
                lockbgImages[i].SetActive(true);
            }
        }
    }

    public void LoadLevel(int levelID)
    {
        SceneManager.LoadScene("level" + levelID);
    }
}