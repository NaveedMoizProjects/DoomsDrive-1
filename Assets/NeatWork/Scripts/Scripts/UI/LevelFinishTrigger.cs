//using UnityEngine;
//using UnityEngine.SceneManagement;

//public class LevelFinishTrigger : MonoBehaviour
//{
//    public GameplayManager gm;

//    private void OnTriggerEnter(Collider other)
//    {
//        if (other.CompareTag("Player"))
//        {
//            gm.LevelCompleted();
//            Yahan aap win UI show kar sakte hain
//        }
//    }

//    public void GoBackToMenu()
//    {
//        SceneManager.LoadScene("MainMenu");
//    }
//}