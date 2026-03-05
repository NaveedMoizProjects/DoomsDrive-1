



using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;   // IMPORTANT
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Dropdown graphicsDrop;
    public TMP_Dropdown resoDrop;
    public Slider volumeSlider;

    void Start()
    {
        if (PlayerPrefs.GetInt("settingsSaved", 0) == 0)
        {
            PlayerPrefs.SetInt("graphics", 0);
            PlayerPrefs.SetInt("resolution", 0);
            PlayerPrefs.SetFloat("mastervolume", 1.0f);
            PlayerPrefs.SetInt("settingsSaved", 1);
            PlayerPrefs.Save();
        }

        ApplyGraphics();
        ApplyResolution();
        ApplyVolume();
    }

    // -------- GRAPHICS --------
    void ApplyGraphics()
    {
        int graphicsSetting = PlayerPrefs.GetInt("graphics", 0);
        graphicsDrop.value = graphicsSetting;
        QualitySettings.SetQualityLevel(graphicsSetting);
    }

    public void SetGraphics()
    {
        PlayerPrefs.SetInt("graphics", graphicsDrop.value);
        PlayerPrefs.Save();
        QualitySettings.SetQualityLevel(graphicsDrop.value);
    }

    // -------- RESOLUTION --------
    void ApplyResolution()
    {
        int resolutionSetting = PlayerPrefs.GetInt("resolution", 0);
        resoDrop.value = resolutionSetting;

        switch (resolutionSetting)
        {
            case 0:
                Screen.SetResolution(854, 480, true);
                break;
            case 1:
                Screen.SetResolution(1280, 720, true);
                break;
            case 2:
                Screen.SetResolution(1920, 1080, true);
                break;
        }
    }

    public void SetResolution()
    {
        PlayerPrefs.SetInt("resolution", resoDrop.value);
        PlayerPrefs.Save();
        ApplyResolution();
    }

    // -------- VOLUME --------
    void ApplyVolume()
    {
        float volume = PlayerPrefs.GetFloat("mastervolume", 1.0f);
        volumeSlider.value = volume;
        AudioListener.volume = volume;
    }

    public void SetVolume()
    {
        PlayerPrefs.SetFloat("mastervolume", volumeSlider.value);
        PlayerPrefs.Save();
        AudioListener.volume = volumeSlider.value;
    }

    public void saveSettings()
    {

        PlayerPrefs.SetInt("settingsSaved", 1);
        PlayerPrefs.Save();
    }

}