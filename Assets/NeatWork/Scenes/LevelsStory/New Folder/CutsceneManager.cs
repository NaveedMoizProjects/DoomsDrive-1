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
    public Image characterImage; // Optional character portrait
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
        if(line.PlayerSprite == null)
        {
            characterImage.gameObject.SetActive(false);
        }
        else
        {
            characterImage.gameObject.SetActive(true);
        }
        // 1. Update Visuals
        if (line.backgroundSprite != null)
        { 
            backgroundImage.sprite = line.backgroundSprite;
            characterImage.sprite = line.PlayerSprite;
        }

        textBoxPanel.SetActive(line.showTextBox);

        if (nameText != null)
        { 
            nameText.text = line.characterName; 
        }

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


    [Header("End of Cutscene UI")]
    public GameObject levelCompleteUIPanel; // Inspector mein Level Complete Panel yahan drag karein

    void EndCutscene()
    {
        Debug.Log("Cutscene Finished.");

        // Agar humne panel assign kiya hai, to usay show karein
        if (levelCompleteUIPanel != null)
        {
            levelCompleteUIPanel.SetActive(true);

            // Mouse cursor dikhane ke liye (agar gameplay mein hidden tha)
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // TextBox aur characters ko hide kar dein taake piche sirf background dikhay
            if (textBoxPanel != null) textBoxPanel.SetActive(false);
            if (characterImage != null) characterImage.gameObject.SetActive(false);
        }
        else if (!string.IsNullOrEmpty(currentCutscene.nextSceneName))
        {
            // Agar panel nahi hai, to purana scene loading logic chale
            SceneManager.LoadScene(currentCutscene.nextSceneName);
        }
        else
        {
            Debug.LogWarning("Na Panel assign hai na Next Scene Name!");
            gameObject.SetActive(false);
        }
    }

}