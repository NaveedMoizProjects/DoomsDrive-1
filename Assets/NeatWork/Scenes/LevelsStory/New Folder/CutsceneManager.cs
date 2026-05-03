using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class CutsceneManager : MonoBehaviour
{
    [Header("References")]
    public CutsceneData currentCutscene;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI nameText; // Optional character name
    public Image backgroundImage;
    public GameObject textBoxPanel;

    [Header("Settings")]
    public bool autoPlay = false;
    public float autoPlayDelay = 2.0f;

    private int lineIndex = 0;
    private bool isTyping = false;
    private string fullText;
    private Coroutine typingCoroutine;

    void Start()
    {
        if (currentCutscene != null && currentCutscene.lines.Length > 0)
        {
            UpdateUI();
        }
        else
        {
            Debug.LogError("No Cutscene Data assigned!");
        }
    }

    // Logic for: Clicking Background OR pressing Space
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnBackgroundClick();
        }
    }

    public void OnBackgroundClick()
    {
        if (isTyping)
        {
            // COMPLETE TEXT IMMEDIATELY
            CompleteLineInstantly();
        }
        else
        {
            // MOVE TO NEXT DIALOGUE
            AdvanceDialogue();
        }
    }

    private void AdvanceDialogue()
    {
        lineIndex++;
        if (lineIndex < currentCutscene.lines.Length)
        {
            UpdateUI();
        }
        else
        {
            EndCutscene();
        }
    }

    private void CompleteLineInstantly()
    {
        StopCoroutine(typingCoroutine);
        dialogueText.text = fullText;
        isTyping = false;
    }

    // Function to Skip the ENTIRE cutscene immediately
    public void SkipAll()
    {
        EndCutscene();
    }

    void UpdateUI()
    {
        DialogueLine line = currentCutscene.lines[lineIndex];

        // 1. Update Visuals
        if (line.backgroundSprite != null)
            backgroundImage.sprite = line.backgroundSprite;

        textBoxPanel.SetActive(line.showTextBox);

        if (nameText != null)
            nameText.text = line.characterName;

        fullText = line.text;

        // 2. Restart Typing
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(line));
    }

    IEnumerator TypeText(DialogueLine line)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char letter in line.text.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(line.typingSpeed);
        }

        isTyping = false;

        // Optional: Auto-advance logic
        if (autoPlay)
        {
            yield return new WaitForSeconds(autoPlayDelay);
            if (!isTyping) AdvanceDialogue();
        }
    }

    void EndCutscene()
    {
        Debug.Log("Cutscene Finished. Loading: " + currentCutscene.nextSceneName);

        if (!string.IsNullOrEmpty(currentCutscene.nextSceneName))
        {
            SceneManager.LoadScene(currentCutscene.nextSceneName);
        }
        else
        {
            Debug.LogWarning("Next Scene Name is empty in CutsceneData!");
            // Alternatively, deactivate the UI if you stay in the same scene
            gameObject.SetActive(false);
        }
    }
}