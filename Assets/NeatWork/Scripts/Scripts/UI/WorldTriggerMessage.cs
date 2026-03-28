using UnityEngine;

public class WorldTriggerMessage : MonoBehaviour
{
    // Added Info and Error to the enum
    public enum MessageType { Neutral, Checkpoint, LapComplete, Victory, Defeat, Warning, Damage, Info, Error }

    [Header("Message Content")]
    public MessageType type;
    public string messageText = "ENTER MESSAGE HERE";
    public float duration = 3f;

    [Header("Logic")]
    public string targetTag = "Player";
    public bool triggerOnlyOnce = true;
    private bool hasTriggered = false;

    // Use this to trigger messages from OTHER scripts (like your Car Health script)
    public static void SendGlobalMessage(string text, MessageType msgType, float time = 3f)
    {
        if (MessageUIManager.Instance != null)
        {
            MessageUIManager.Instance.ProcessMessage(text, msgType, time);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        // Check if the object entering the trigger is the Player
        if (other.CompareTag(targetTag))
        {
            SendGlobalMessage(messageText, type, duration);
            if (triggerOnlyOnce) hasTriggered = true;
        }
    }
}