using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class JusticePointManager : MonoBehaviour
{
    public static JusticePointManager Instance;

    [Header("Justice Point Data")]
    public int currentJusticePoints = 0;

    [Header("UI References")]
    public TextMeshProUGUI justicePointHUDText;
    public GameObject upgradeMenuPanel;
    [Tooltip("Teks tunggal di tengah panel untuk menampilkan deskripsi item yang diklik")]
    public TextMeshProUGUI deskripsiText; 

    [Header("Selection Highlight (Kedap-Kedip)")]
    [Tooltip("Masukkan kelima Image Highlight_Blink secara berurutan: 0=HP, 1=Z, 2=X, 3=C, 4=V")]
    public Image[] selectionHighlights = new Image[5];
    
    [Tooltip("Kecepatan kedip diperlambat agar tidak puyeng (Standar: 1.5f)")]
    public float blinkSpeed = 1.5f;
    
    // Variabel untuk melacak status mana yang sedang dipilih (-1 berarti tidak ada)
    private int selectedIndex = -1; 

    [Header("Max HP Upgrade Setup")]
    public PlayerHealth playerHealth;
    public int hpUpgradeCost = 10;
    public int hpIncreaseAmount = 20;

    [Header("4 Skills Upgrade Setup")]
    public SkillData[] skillsToUpgrade = new SkillData[4];

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        UpdateHUD();
        if (upgradeMenuPanel != null) upgradeMenuPanel.SetActive(false);
    }

    void Update()
    {
        // Buka/Tutup Menu dengan tombol Tab / U
        if (Keyboard.current != null && (Keyboard.current.tabKey.wasPressedThisFrame || Keyboard.current.uKey.wasPressedThisFrame))
        {
            ToggleUpgradeMenu();
        }

        // Jalankan Efek Kedap-Kedip Putih Transparan pada kotak yang dipilih
        if (upgradeMenuPanel.activeSelf && selectedIndex != -1)
        {
            for (int i = 0; i < selectionHighlights.Length; i++)
            {
                if (selectionHighlights[i] != null)
                {
                    if (i == selectedIndex)
                    {
                        selectionHighlights[i].gameObject.SetActive(true);
                        
                        // PingPong menghasilkan nilai naik-turun dari 0 ke 1 dengan mulus
                        float pingPong = Mathf.PingPong(Time.unscaledTime * blinkSpeed, 1f);
                        
                        // Lerp Alpha (Transparansi) dari 0.1f (sangat pudar) ke 0.6f (putih jelas tapi tidak menutupi tulisan)
                        float alphaValue = Mathf.Lerp(0.1f, 0.6f, pingPong);
                        
                        // Set warna menjadi Putih dengan Alpha yang berkedip
                        selectionHighlights[i].color = new Color(1f, 1f, 1f, alphaValue);
                    }
                    else
                    {
                        selectionHighlights[i].gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    public void AddJusticePoints(int amount)
    {
        currentJusticePoints += amount;
        UpdateHUD();
    }

    public void ToggleUpgradeMenu()
    {
        if (upgradeMenuPanel == null) return;
        bool isOpening = !upgradeMenuPanel.activeSelf;
        upgradeMenuPanel.SetActive(isOpening);

        Time.timeScale = isOpening ? 0f : 1f;
        
        // Reset pilihan setiap kali menu dibuka
        if (isOpening)
        {
            SelectSlot(-1); 
        }
        UpdateHUD();
    }

    // Dipanggil oleh kelima tombol Slot (HP, Z, X, C, V)
    public void SelectSlot(int index)
    {
        selectedIndex = index;

        // Matikan semua highlight jika dibatalkan
        if (selectedIndex == -1)
        {
            foreach (Image img in selectionHighlights) { if (img != null) img.gameObject.SetActive(false); }
            if (deskripsiText != null) deskripsiText.text = "Pilih kotak di bawah untuk melihat detail Upgrade";
            return;
        }

        // Perbarui teks di tengah berdasarkan tombol yang diklik
        if (deskripsiText != null)
        {
            if (selectedIndex == 0)
            {
                deskripsiText.text = $"[MAX HP]\nLevel Max HP: {playerHealth.maxHealth}\nPenambahan: +{hpIncreaseAmount} HP\n\nBiaya Upgrade: <color=yellow>{hpUpgradeCost} JP</color>";
            }
            else
            {
                int skillIndex = selectedIndex - 1; // Ubah indeks 1-4 menjadi 0-3 untuk array skill
                if (skillsToUpgrade[skillIndex] != null)
                {
                    SkillData s = skillsToUpgrade[skillIndex];
                    deskripsiText.text = $"[{s.skillName}]\nLevel Saat Ini: {s.skillLevel}\nDMG: {s.damage}  |  STUN: {s.stunDamage}\n\nBiaya Upgrade: <color=yellow>{s.upgradeCost} JP</color>";
                }
            }
        }
    }

    // Dipanggil oleh SATU tombol "Upgrade" yang berada di atas panel
    public void ExecuteUpgrade()
    {
        if (selectedIndex == -1) 
        {
            Debug.Log("Pilih slot terlebih dahulu sebelum klik Upgrade!");
            return;
        }

        if (selectedIndex == 0) // Jika HP yang sedang dipilih
        {
            if (currentJusticePoints >= hpUpgradeCost)
            {
                currentJusticePoints -= hpUpgradeCost;
                playerHealth.maxHealth += hpIncreaseAmount;
                playerHealth.Heal(hpIncreaseAmount);
                hpUpgradeCost += 5;
                if (playerHealth.hpSlider != null) { playerHealth.hpSlider.maxValue = playerHealth.maxHealth; playerHealth.hpSlider.value = playerHealth.currentHealth; }
                
                SelectSlot(0); // Refresh teks deskripsi
            }
        }
        else // Jika Skill yang sedang dipilih (indeks 1 sampai 4)
        {
            int skillIndex = selectedIndex - 1;
            SkillData skill = skillsToUpgrade[skillIndex];
            
            if (skill != null && currentJusticePoints >= skill.upgradeCost)
            {
                currentJusticePoints -= skill.upgradeCost;
                skill.UpgradeSkill();
                
                SelectSlot(selectedIndex); // Refresh teks deskripsi
            }
        }
        
        UpdateHUD();
    }

    void UpdateHUD()
    {
        if (justicePointHUDText != null)
            justicePointHUDText.text = "Justice Points: " + currentJusticePoints;
    }
}