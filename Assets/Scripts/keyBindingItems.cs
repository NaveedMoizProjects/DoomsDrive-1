using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class KeyBindingItems : MonoBehaviour
{
    public string actionName; // e.g. "p1_Forward" or "p2_Boost"
    public TextMeshProUGUI displayText;
    private bool waitingForKey = false;

    public void StartRebind()
    {
        if (!waitingForKey)
            StartCoroutine(WaitForKey());
    }

    private IEnumerator WaitForKey()
    {
        waitingForKey = true;
        displayText.text = "Press key...";

        while (!Input.anyKeyDown)
            yield return null;

        foreach (KeyCode k in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(k))
            {
                AssignKey(k);
                break;
            }
        }

        waitingForKey = false;
    }

    private void AssignKey(KeyCode newKey)
    {
        // Assign to manager
        var c = ControlSettingsManager.Instance;

        switch (actionName)
        {
            case "p1_Forward": c.p1_Forward = newKey; break;
            case "p1_Left": c.p1_Left = newKey; break;
            case "p1_Right": c.p1_Right = newKey; break;
            case "p1_Brake": c.p1_Brake = newKey; break;
            case "p1_Boost": c.p1_Boost = newKey; break;
            case "p1_Reset": c.p1_Reset = newKey; break;

            case "p2_Forward": c.p2_Forward = newKey; break;
            case "p2_Left": c.p2_Left = newKey; break;
            case "p2_Right": c.p2_Right = newKey; break;
            case "p2_Brake": c.p2_Brake = newKey; break;
            case "p2_Boost": c.p2_Boost = newKey; break;
            case "p2_Reset": c.p2_Reset = newKey; break;
        }

        // Update UI text
        displayText.text = newKey.ToString();
    }
}
