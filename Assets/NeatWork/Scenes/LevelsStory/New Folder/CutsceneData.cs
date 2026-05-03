using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    public string characterName;
    [TextArea(3, 10)]
    public string text;
    public Sprite backgroundSprite;
    public Sprite PlayerSprite;
    public float typingSpeed = 0.02f; // Default speed
    public bool showTextBox = true;
}

[CreateAssetMenu(fileName = "NewCutscene", menuName = "Cutscene/Sequence")]
public class CutsceneData : ScriptableObject
{
    public DialogueLine[] lines;
    [Header("Transition Settings")]
    public string nextSceneName; // The scene to load when finished
}