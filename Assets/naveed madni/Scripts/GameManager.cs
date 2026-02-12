using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int lapsToWin = 1;
    private bool gameOver = false;

    [Header("Scene Settings")]
    public float delayBeforeMenu = 5f;
    public string mainMenuSceneName = "MainMenu";

    [Header("Audio")]
    public AudioSource applauseAudio;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void DeclareWinner(CarMovement.PlayerID winner)
    {
        if (gameOver) return;
        gameOver = true;

        RaceUIHandler.Instance.ShowResults(winner);
        Time.timeScale = 0f;

        if (applauseAudio != null)
            applauseAudio.Play();

        Debug.Log("Game Over! Winner: " + winner);
        StartCoroutine(BackToMenuAfterDelay());
    }

    private System.Collections.IEnumerator BackToMenuAfterDelay()
    {
        yield return new WaitForSecondsRealtime(delayBeforeMenu);
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
