using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelFailedManager : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Assign your Level Failed panel here.")]
    public GameObject levelFailedPanel;

    [Tooltip("Assign your Pause panel here — it will be destroyed when Level Failed triggers.")]
    public GameObject pausePanel;

    [Header("Settings")]
    [Tooltip("Pause the game when Level Failed panel appears.")]
    public bool pauseOnFail = true;

    [Header("Tags to Monitor")]
    [Tooltip("All tags listed here will be monitored — if ANY are destroyed, Level Failed triggers.")]
    public string[] tagsToMonitor = new string[] { "Player", "Body", "Tyre" };

    private GameObject[] trackedObjects;
    private bool levelFailed = false;

    void Start()
    {
        if (levelFailedPanel != null)
            levelFailedPanel.SetActive(false);

        trackedObjects = FindAllTaggedObjects();

        if (trackedObjects.Length == 0)
            Debug.LogWarning("LevelFailedManager: No GameObjects found for any of the monitored tags!");
    }

    GameObject[] FindAllTaggedObjects()
    {
        System.Collections.Generic.List<GameObject> found = new System.Collections.Generic.List<GameObject>();

        foreach (string tag in tagsToMonitor)
        {
            try
            {
                GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
                if (objects.Length == 0)
                    Debug.LogWarning("LevelFailedManager: No GameObject found with tag '" + tag + "'");
                else
                    found.AddRange(objects);
            }
            catch
            {
                Debug.LogWarning("LevelFailedManager: Tag '" + tag + "' does not exist in Tag Manager. Please add it.");
            }
        }

        return found.ToArray();
    }

    void Update()
    {
        if (levelFailed) return;

        foreach (GameObject obj in trackedObjects)
        {
            if (obj == null)
            {
                TriggerLevelFailed();
                return;
            }
        }
    }

    void TriggerLevelFailed()
    {
        if (levelFailed) return;
        levelFailed = true;

        Debug.Log("LevelFailedManager: A critical GameObject was destroyed — showing Level Failed panel.");

        // Destroy the pause panel if it exists
        if (pausePanel != null)
            Destroy(pausePanel);

        if (levelFailedPanel != null)
            levelFailedPanel.SetActive(true);
        else
            Debug.LogWarning("LevelFailedManager: Level Failed Panel is not assigned!");

        if (pauseOnFail)
            Time.timeScale = 0f;
    }

    // Call this from a Retry button in your UI
    public void RetryLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Call this from a Main Menu button in your UI
    public void GoToMainMenu(int menuSceneIndex = 0)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneIndex);
    }
}