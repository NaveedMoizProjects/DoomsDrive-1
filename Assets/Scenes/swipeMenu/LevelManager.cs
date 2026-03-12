using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameplayManager : MonoBehaviour
{
    public GameObject[] Levels;
    public int levelNo;
    void Start()
    {

        levelNo = PlayerPrefs.GetInt("levelIndex");


        for (int i = 0; i < Levels.Length; i++)
        {

            if (levelNo == i)
            {
                print("This Is the Level No" + levelNo);
                Levels[i].SetActive(true);
            }
            else
            {

                print("This Is the Level No" + levelNo);
                Levels[i].SetActive(false);
            }

        }
    }

    public void CompleteLevel()
    {
        PlayerPrefs.SetInt(("unlockedLevels"), +1);
    }

    public static implicit operator GameplayManager(GameplayManagerMine v)
    {
        throw new NotImplementedException();
    }
}
