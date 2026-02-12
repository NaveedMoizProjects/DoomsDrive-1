using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ControlRebindButton : MonoBehaviour
{
    public enum PlayerControlType { Player1, Player2 }
    public enum Action { Forward, Left, Right, Brake, Boost, Reset }

    public PlayerControlType playerType;
    public Action actionType;

    private Button button;
    private Text buttonText;
    private bool waitingForInput = false;

    private void Start()
    {
        button = GetComponent<Button>();
        buttonText = GetComponentInChildren<Text>();

        UpdateButtonText();
        button.onClick.AddListener(() => StartCoroutine(WaitForKey()));
    }

    private IEnumerator WaitForKey()
    {
        waitingForInput = true;
        buttonText.text = "Press Key...";

        yield return null;

        while (!Input.anyKeyDown)
            yield return null;

        foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(key))
            {
                SetKey(key);
                break;
            }
        }

        waitingForInput = false;
        UpdateButtonText();
    }

    private void SetKey(KeyCode newKey)
    {
        var ctrl = ControlSettingsManager.Instance;

        if (playerType == PlayerControlType.Player1)
        {
            switch (actionType)
            {
                case Action.Forward: ctrl.p1_Forward = newKey; break;
                case Action.Left: ctrl.p1_Left = newKey; break;
                case Action.Right: ctrl.p1_Right = newKey; break;
                case Action.Brake: ctrl.p1_Brake = newKey; break;
                case Action.Boost: ctrl.p1_Boost = newKey; break;
                case Action.Reset: ctrl.p1_Reset = newKey; break;
            }
        }
        else
        {
            switch (actionType)
            {
                case Action.Forward: ctrl.p2_Forward = newKey; break;
                case Action.Left: ctrl.p2_Left = newKey; break;
                case Action.Right: ctrl.p2_Right = newKey; break;
                case Action.Brake: ctrl.p2_Brake = newKey; break;
                case Action.Boost: ctrl.p2_Boost = newKey; break;
                case Action.Reset: ctrl.p2_Reset = newKey; break;
            }
        }
    }

    private void UpdateButtonText()
    {
        KeyCode key = KeyCode.None;
        var ctrl = ControlSettingsManager.Instance;

        if (playerType == PlayerControlType.Player1)
        {
            switch (actionType)
            {
                case Action.Forward: key = ctrl.p1_Forward; break;
                case Action.Left: key = ctrl.p1_Left; break;
                case Action.Right: key = ctrl.p1_Right; break;
                case Action.Brake: key = ctrl.p1_Brake; break;
                case Action.Boost: key = ctrl.p1_Boost; break;
                case Action.Reset: key = ctrl.p1_Reset; break;
            }
        }
        else
        {
            switch (actionType)
            {
                case Action.Forward: key = ctrl.p2_Forward; break;
                case Action.Left: key = ctrl.p2_Left; break;
                case Action.Right: key = ctrl.p2_Right; break;
                case Action.Brake: key = ctrl.p2_Brake; break;
                case Action.Boost: key = ctrl.p2_Boost; break;
                case Action.Reset: key = ctrl.p2_Reset; break;
            }
        }

        buttonText.text = key.ToString();
    }
}
