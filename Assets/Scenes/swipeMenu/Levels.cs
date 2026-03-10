using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using UnityEngine.SocialPlatforms;
using UnityEngine.UI;

public class Levels : MonoBehaviour
{

    int unlockLevels;

    public void levelIndexNumber(int levelNo)
    {

        PlayerPrefs.SetInt("LevelIndex", levelNo);

        print(PlayerPrefs.GetInt("levelIndex"));

    }


}