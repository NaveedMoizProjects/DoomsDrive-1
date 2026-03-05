using UnityEngine;
using UnityEngine.UI;

public class Audio_setting : MonoBehaviour
{
    public Slider volumeSlider;
    public AudioSource musicSource;

    private static bool instanceExists;

    void Awake()
    {
        if (!instanceExists)
        {
            DontDestroyOnLoad(gameObject);
            instanceExists = true;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    void Start()
    {
        float savedVolume = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
        volumeSlider.value = savedVolume;
        musicSource.volume = savedVolume;

        // ✅ Ensure the slider updates volume live
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    }

    public void OnVolumeChanged(float value)
    {
        musicSource.volume = value;
        PlayerPrefs.SetFloat("MusicVolume", value);
    }
}
