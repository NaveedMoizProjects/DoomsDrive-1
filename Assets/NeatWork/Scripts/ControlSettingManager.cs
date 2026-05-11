using UnityEngine;

public class ControlSettingsManager : MonoBehaviour
{
    public static ControlSettingsManager Instance;

    // Player 1 Controls
    public KeyCode p1_Forward = KeyCode.W;
    public KeyCode p1_Left = KeyCode.A;
    public KeyCode p1_Right = KeyCode.D;
    public KeyCode p1_Brake = KeyCode.S;
    public KeyCode p1_Boost = KeyCode.Space;
    public KeyCode p1_Reset = KeyCode.R;

    // Player 2 Controls
    public KeyCode p2_Forward = KeyCode.UpArrow;
    public KeyCode p2_Left = KeyCode.LeftArrow;
    public KeyCode p2_Right = KeyCode.RightArrow;
    public KeyCode p2_Brake = KeyCode.DownArrow;
    public KeyCode p2_Boost = KeyCode.B;
    public KeyCode p2_Reset = KeyCode.Y;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
