using UnityEngine;

public class NPCInteractable : MonoBehaviour
{
    [Header("Identitas NPC")]
    public string npcName = "Warga Sektor Bawah";

    [Header("Dialog Standar / Sebelum Quest")]
    [TextArea(2, 4)]
    public string[] initialDialogue = {
        "Hati-hati, penjaga bangsawan sedang merazia area ini!",
        "Jangan sampai ketahuan kalau kamu membantu pengguna teknologi."
    };

    [Header("Pengaturan Pemberi Quest Utama (Opsional)")]
    [Tooltip("Centang jika NPC ini memberikan Quest Utama pembuka Region")]
    public bool isQuestGiver = false;
    public string questTitle = "Terobos Sektor Bawah";
    public string questObjective = "Lumpuhkan & Tangkap Penjaga (Non-Lethal)";
    [Tooltip("Jumlah musuh yang harus di-Arrest untuk menyelesaikan quest ini")]
    public int requiredArrests = 2;
    [Tooltip("Tarik objek pagar/tembok penghalang Gerbang Region 2 ke sini agar terbuka saat quest selesai")]
    public GameObject gateBlockerToDisable;

    [Header("Dialog Saat Quest Sedang Berjalan & Setelah Selesai")]
    [TextArea(2, 4)]
    public string[] questInProgressDialogue = {
        "Lumpuhkan penjaga di depan sampai menyerah, lalu tekan F untuk mengamankan mereka!"
    };

    [TextArea(2, 4)]
    public string[] questCompletedDialogue = {
        "Kerja bagus, Lina! Jalur menuju Pasar Gelap di Distrik Perbatasan sudah aman dilalui."
    };

    private bool hasGivenQuest = false;

    public void TriggerInteraction()
    {
        if (DialogueAndQuestManager.Instance == null) return;

        // Jika bukan pemberi quest (NPC warga biasa), langsung putar dialog standar
        if (!isQuestGiver)
        {
            DialogueAndQuestManager.Instance.StartDialogue(npcName, initialDialogue, this);
            return;
        }

        // Jika NPC Pemberi Quest: cek status quest saat ini
        if (!hasGivenQuest)
        {
            DialogueAndQuestManager.Instance.StartDialogue(npcName, initialDialogue, this);
        }
        else if (!DialogueAndQuestManager.Instance.isMainQuestCompleted)
        {
            DialogueAndQuestManager.Instance.StartDialogue(npcName, questInProgressDialogue, null);
        }
        else
        {
            DialogueAndQuestManager.Instance.StartDialogue(npcName, questCompletedDialogue, null);
        }
    }

    // Dipanggil otomatis oleh Manager saat kalimat terakhir selesai dibaca
    public void OnDialogueFinished()
    {
        if (isQuestGiver && !hasGivenQuest)
        {
            hasGivenQuest = true;
            DialogueAndQuestManager.Instance.ActivateMainQuest(questTitle, questObjective, requiredArrests, gateBlockerToDisable);
        }
    }
}