using UnityEngine;

public class OptionsPanelManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject soundSettingsPanel;
    public GameObject controlSettingsPanel;

    public void ShowSoundSettings()
    {
        soundSettingsPanel.SetActive(true);
        controlSettingsPanel.SetActive(false);
    }

    public void ShowControlSettings()
    {
        soundSettingsPanel.SetActive(false);
        controlSettingsPanel.SetActive(true);
    }

    private void Start()
    {
        // Default: show sound settings
        ShowSoundSettings();
    }
}
