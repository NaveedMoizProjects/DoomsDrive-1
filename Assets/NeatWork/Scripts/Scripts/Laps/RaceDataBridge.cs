using UnityEngine;

public class RaceDataBridge : MonoBehaviour
{
    private DD1_LapHandler lapHandler;
    public int lapsToWin = 3; // You can set this in the Inspector or via DamageManager

    // We use Reflection or simple public access to grab the private lapCount
    // But to keep it simple, let's just track the win condition here.

    void Start()
    {
        lapHandler = GetComponent<DD1_LapHandler>();

        // Tell the DamageManager how many laps we need to do
        if (DamageManager.Instance != null)
        {
            // You can set this here or in the Inspector of DamageManager
            DamageManager.Instance.lapsToWin = lapsToWin;
        }
    }

    // This method will be called whenever a lap is completed
    public void SyncLapToSingleton(int currentLap)
    {
        if (DamageManager.Instance != null)
        {
            DamageManager.Instance.currentLap = currentLap;
        }
    }
}