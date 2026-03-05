using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameplayManager : MonoBehaviour
{
    public GameObject[] Levels;
    public int levelNo;

    void Start()
    {
        levelNo = PlayerPrefs.GetInt("levelIndex", 0);

        for (int i = 0; i < Levels.Length; i++)
        {
            if (levelNo == i)
            {
                Debug.Log("This is Level No: " + levelNo);
                Levels[i].SetActive(true);
            }
            else
            {
                Levels[i].SetActive(false);
            }
        }
    }

    public void CompleteLevel()
    {
        int current = PlayerPrefs.GetInt("unlockedLevels", 1);
        PlayerPrefs.SetInt("unlockedLevels", current + 1);
        PlayerPrefs.Save();

        Debug.Log("Next Level Unlocked");
    }
}