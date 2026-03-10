using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LockingUnlocking : MonoBehaviour
{
    public Button[] buttons;
    public Image[] lock1;      // lock icon
    public Image[] extraPic;   // new picture

    int unlockLevels;

    private void Start()
    {
        unlockLevels = PlayerPrefs.GetInt("unlockedLevels");
        print(unlockLevels);

        for (int i = 0; i < buttons.Length; i++)
        {
            if (i < unlockLevels + 1)
            {
                buttons[i].interactable = true;

                lock1[i].gameObject.SetActive(false);
                extraPic[i].gameObject.SetActive(false); // unlock hone par picture bhi off

                print("buttons if statement " + i);
            }
            else
            {
                buttons[i].interactable = false;

                lock1[i].gameObject.SetActive(true);
                extraPic[i].gameObject.SetActive(true); // lock hone par picture on

                print("buttons else statement " + i);
            }
        }
    }

    public void LoadLevel(int levelID)
    {
        
        SceneManager.LoadScene(levelID);
        print("LevelLoaded " + levelID);
    }
}