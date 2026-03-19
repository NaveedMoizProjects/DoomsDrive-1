using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections; // Required for Coroutines

public class Loading : MonoBehaviour
{
    // Changed from int to string
    public string sceneName;

    [SerializeField] private Slider loadingSlider;
    [SerializeField] private GameObject loadingScreen;

    public void loadingAnim()
    {
        // Activate the loading UI
        if (loadingScreen != null)
            loadingScreen.SetActive(true);

        // Start the background loading process
        StartCoroutine(LoadSceneAsync());
    }

    IEnumerator LoadSceneAsync()
    {
        // This runs the loading in the background
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        while (!operation.isDone)
        {
            // Clamp progress from 0 to 1
            float progress = Mathf.Clamp01(operation.progress / 0.9f);

            if (loadingSlider != null)
                loadingSlider.value = progress;

            yield return null;
        }
    }
}