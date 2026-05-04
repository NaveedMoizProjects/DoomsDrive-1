using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Jab player jeete to yeh script cutscene scene par le jaata hai.
/// 
/// Setup:
///   1. Kisi bhi GameObject par yeh script lagao (gameplay scene mein)
///   2. Inspector mein Cutscene Scene Name fill karo
///   3. Woh scene File > Build Settings mein add karo
/// </summary>
public class LevelTransitionManager : MonoBehaviour
{
    public static LevelTransitionManager Instance { get; private set; }

    [Header("Cutscene Scene")]
    [Tooltip("Yahan apni cutscene ka exact scene name likho (Build Settings mein bhi add karo)")]
    public string cutsceneSceneName = "CutsceneLevelEnd";

    [Tooltip("Level complete hone ke kitni der baad scene switch ho (seconds)")]
    public float delayBeforeTransition = 0.5f;

    private bool _transitioning = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// LevelComplete.cs se automatically call hota hai jab player jeete.
    /// Cutscene scene load karta hai.
    /// </summary>
    public void GoToCutscene()
    {
        if (_transitioning) return;
        _transitioning = true;
        StartCoroutine(LoadCutscene());
    }

    private IEnumerator LoadCutscene()
    {
        // timeScale reset karo taake coroutine sahi kaam kare
        Time.timeScale = 1f;

        yield return new WaitForSecondsRealtime(delayBeforeTransition);

        if (string.IsNullOrEmpty(cutsceneSceneName))
        {
            Debug.LogWarning("[LevelTransitionManager] cutsceneSceneName empty hai! Inspector mein scene name assign karo.");
            yield break;
        }

        Debug.Log($"[LevelTransitionManager] Loading cutscene: {cutsceneSceneName}");
        SceneManager.LoadScene(cutsceneSceneName);
    }
}