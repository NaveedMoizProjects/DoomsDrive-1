
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Loading : MonoBehaviour
{

    public int levelToLoad;

    [SerializeField] private Slider loadingSlidor;
    [SerializeField] private GameObject loadingScreen;

    public void loadingAnim()
    {
        gameObject.transform.parent.parent.gameObject.SetActive(true);
        SceneManager.LoadScene(levelToLoad);
    }
}
