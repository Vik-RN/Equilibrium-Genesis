using UnityEngine;
using TMPro;

public class DialogueAndQuestManager : MonoBehaviour
{
    public static DialogueAndQuestManager Instance;

    [Header("1. UI Interaction Prompt ([F] Nama)")]
    public GameObject interactPromptUI;
    public TextMeshProUGUI interactPromptText;

    [Header("2. UI Dialogue Panel")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueBodyText;

    [Header("3. UI Quest Tracker Panel")]
    public GameObject questTrackerUI;
    public TextMeshProUGUI questTitleText;
    public TextMeshProUGUI questObjectiveText;

    [Header("Status Dialog & Quest Saat Ini")]
    public bool isDialogueActive = false;
    public bool hasActiveMainQuest = false;
    public bool isMainQuestCompleted = false;

    private string activeQuestTitle;
    private string activeQuestDescription;
    private int targetArrestCount = 0;
    private int currentArrestCount = 0;
    private GameObject gateToUnlock; // Objek penghalang/gerbang menuju Region berikutnya

    // Data antrean kalimat dialog yang sedang berlangsung
    private string[] currentLines;
    private int currentLineIndex = 0;
    private NPCInteractable currentTalkingNPC;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (interactPromptUI != null) interactPromptUI.SetActive(false);
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        UpdateQuestUI();
    }

    // --- FUNGSI PROMPT [F] ---
    public void ShowInteractPrompt(string promptMessage)
    {
        if (isDialogueActive)
        {
            if (interactPromptUI != null) interactPromptUI.SetActive(false);
            return;
        }

        if (interactPromptUI != null)
        {
            interactPromptUI.SetActive(true);
            if (interactPromptText != null) interactPromptText.text = "[F] " + promptMessage;
        }
    }

    public void HideInteractPrompt()
    {
        if (interactPromptUI != null) interactPromptUI.SetActive(false);
    }

    // --- FUNGSI DIALOG ---
    public void StartDialogue(string speakerName, string[] lines, NPCInteractable npcSource = null)
    {
        if (lines == null || lines.Length == 0) return;

        isDialogueActive = true;
        currentTalkingNPC = npcSource;
        currentLines = lines;
        currentLineIndex = 0;

        HideInteractPrompt();
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (speakerNameText != null) speakerNameText.text = speakerName;
        if (dialogueBodyText != null) dialogueBodyText.text = currentLines[currentLineIndex];
    }

    public void AdvanceDialogue()
    {
        if (!isDialogueActive) return;

        currentLineIndex++;
        if (currentLineIndex < currentLines.Length)
        {
            if (dialogueBodyText != null) dialogueBodyText.text = currentLines[currentLineIndex];
        }
        else
        {
            EndDialogue();
        }
    }

    void EndDialogue()
    {
        isDialogueActive = false;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        // Jika NPC ini memberikan Quest Utama setelah selesai bicara
        if (currentTalkingNPC != null)
        {
            currentTalkingNPC.OnDialogueFinished();
            currentTalkingNPC = null;
        }
    }

    // --- FUNGSI QUEST UTAMA ---
    public void ActivateMainQuest(string title, string description, int requiredArrests, GameObject regionGateBlocker)
    {
        hasActiveMainQuest = true;
        isMainQuestCompleted = false;
        activeQuestTitle = title;
        activeQuestDescription = description;
        targetArrestCount = requiredArrests;
        currentArrestCount = 0;
        gateToUnlock = regionGateBlocker;

        // Jika quest hanya butuh bicara (target tangkap = 0), langsung selesai & buka gerbang
        if (targetArrestCount <= 0)
        {
            CompleteMainQuest();
        }
        else
        {
            UpdateQuestUI();
        }
    }

    // Dipanggil otomatis saat pemain menekan F (Arrest) pada musuh
    public void AddArrestQuestProgress()
    {
        if (!hasActiveMainQuest || isMainQuestCompleted) return;

        currentArrestCount++;
        if (currentArrestCount >= targetArrestCount)
        {
            currentArrestCount = targetArrestCount;
            CompleteMainQuest();
        }
        else
        {
            UpdateQuestUI();
        }
    }

    void CompleteMainQuest()
    {
        isMainQuestCompleted = true;
        UpdateQuestUI();

        // Buka penghalang/gerbang menuju Region 2!
        if (gateToUnlock != null)
        {
            gateToUnlock.SetActive(false);
            Debug.Log("<color=green>QUEST UTAMA SELESAI! Gerbang menuju Region berikutnya telah terbuka!</color>");
        }
    }

    void UpdateQuestUI()
    {
        if (questTrackerUI == null) return;

        if (!hasActiveMainQuest)
        {
            if (questTitleText != null) questTitleText.text = "Eksplorasi Area";
            if (questObjectiveText != null) questObjectiveText.text = "- Cari dan bicara dengan pemberi petunjuk";
            return;
        }

        if (questTitleText != null) questTitleText.text = activeQuestTitle;

        if (questObjectiveText != null)
        {
            if (isMainQuestCompleted)
                questObjectiveText.text = "<color=#00FF88>Selesai! Pergi ke Gerbang Transisi Region.</color>";
            else if (targetArrestCount > 0)
                questObjectiveText.text = $"- {activeQuestDescription} ({currentArrestCount}/{targetArrestCount})";
            else
                questObjectiveText.text = $"- {activeQuestDescription}";
        }
    }
}