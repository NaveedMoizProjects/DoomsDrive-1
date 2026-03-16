using UnityEngine;

public class LevelUnlockCheckpoint : MonoBehaviour
{
    public GameObject Trigger;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            int currentLevel = PlayerPrefs.GetInt("LevelIndex", 1);
            int unlockedLevel = PlayerPrefs.GetInt("unlockedLevels", 0);

            if (currentLevel > unlockedLevel)
            {
                PlayerPrefs.SetInt("unlockedLevels", currentLevel);
                PlayerPrefs.Save();
                Trigger.SetActive(false);
                Debug.Log("Next Level Unlocked: Level " + (currentLevel + 1));
            }
        }
    }
}