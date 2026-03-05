using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Levels : MonoBehaviour
{
    public void levelIndexNumber(int levelNo)
    {
        PlayerPrefs.SetInt("levelIndex", levelNo);
        PlayerPrefs.Save();

        Debug.Log("Selected Level: " + levelNo);
    }
}