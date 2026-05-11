using UnityEngine;

public class RaceUIHandler : MonoBehaviour
{
    public static RaceUIHandler Instance;

    public GameObject player1WinImage;
    public GameObject player2WinImage;
    public GameObject player1LoseImage;
    public GameObject player2LoseImage;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        player1WinImage.SetActive(false);
        player2WinImage.SetActive(false);
        player1LoseImage.SetActive(false);
        player2LoseImage.SetActive(false);
    }

    public void ShowResults(CarMovement.PlayerID winner)
    {
        switch (winner)
        {
            case CarMovement.PlayerID.Player1:
                player1WinImage.SetActive(true);
                player2LoseImage.SetActive(true);
                break;
            case CarMovement.PlayerID.Player2:
                player2WinImage.SetActive(true);
                player1LoseImage.SetActive(true);
                break;
        }
    }
}
