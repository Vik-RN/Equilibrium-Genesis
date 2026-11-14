using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class JusticePointManager : MonoBehaviour
{
    public static JusticePointManager Instance;

    [Header("Justice Point Data")]
    public int currentJusticePoints = 0;

    [Header("HUD & Upgrade UI References")]
    public TextMeshProUGUI justicePointHUDText;
    public GameObject upgradeMenuPanel;
    public TextMeshProUGUI hpUpgradeInfoText;

    [Header("4 Skills Upgrade Setup (0=Z, 1=X, 2=C, 3=V)")]
    public SkillData[] skillsToUpgrade = new SkillData[4];
    public TextMeshProUGUI[] skillInfoTexts = new TextMeshProUGUI[4];

    [Header("Max HP Upgrade Setup")]
    public PlayerHealth playerHealth;
    public int hpUpgradeCost = 10;
    public int hpIncreaseAmount = 20;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        UpdateUI();
        if (upgradeMenuPanel != null) upgradeMenuPanel.SetActive(false);
    }

    void Update()
    {
        // Tekan tombol TAB atau U untuk membuka/menutup Menu Upgrade
        if (Keyboard.current != null && (Keyboard.current.tabKey.wasPressedThisFrame || Keyboard.current.uKey.wasPressedThisFrame))
        {
            ToggleUpgradeMenu();
        }
    }

    public void AddJusticePoints(int amount)
    {
        currentJusticePoints += amount;
        UpdateUI();
    }

    // Dipanggil oleh Tombol UI Upgrade Skill (Masukkan angka 0, 1, 2, atau 3 di Inspector Button)
    public void UpgradeSkillByIndex(int index)
    {
        if (index < 0 || index >= skillsToUpgrade.Length) return;
        SkillData skill = skillsToUpgrade[index];
        if (skill == null) return;

        if (currentJusticePoints >= skill.upgradeCost)
        {
            currentJusticePoints -= skill.upgradeCost;
            skill.UpgradeSkill();
            UpdateUI();
        }
        else
        {
            Debug.Log("Justice Point tidak cukup!");
        }
    }

    public void UpgradeMaxHPButton()
    {
        if (playerHealth == null) return;

        if (currentJusticePoints >= hpUpgradeCost)
        {
            currentJusticePoints -= hpUpgradeCost;
            playerHealth.maxHealth += hpIncreaseAmount;
            playerHealth.Heal(hpIncreaseAmount);
            hpUpgradeCost += 5;

            if (playerHealth.hpSlider != null)
            {
                playerHealth.hpSlider.maxValue = playerHealth.maxHealth;
                playerHealth.hpSlider.value = playerHealth.currentHealth;
            }
            UpdateUI();
        }
    }

    public void ToggleUpgradeMenu()
    {
        if (upgradeMenuPanel == null) return;
        bool isOpening = !upgradeMenuPanel.activeSelf;
        upgradeMenuPanel.SetActive(isOpening);

        // Pause game saat menu terbuka agar bisa klik tombol dengan aman
        Time.timeScale = isOpening ? 0f : 1f;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (justicePointHUDText != null)
            justicePointHUDText.text = "Justice Points: " + currentJusticePoints;

        if (hpUpgradeInfoText != null && playerHealth != null)
            hpUpgradeInfoText.text = $"Max HP: {playerHealth.maxHealth} (+{hpIncreaseAmount})\nBiaya: {hpUpgradeCost} JP";

        string[] keyLabels = { "Z", "X", "C", "V" };
        for (int i = 0; i < 4; i++)
        {
            if (skillInfoTexts != null && i < skillInfoTexts.Length && skillInfoTexts[i] != null && skillsToUpgrade[i] != null)
            {
                SkillData s = skillsToUpgrade[i];
                skillInfoTexts[i].text = $"[{keyLabels[i]}] {s.skillName} (Lv.{s.skillLevel})\n" +
                                         $"DMG: {s.damage} | STUN: {s.stunDamage} | CD: {s.cooldown}s\n" +
                                         $"Biaya: {s.upgradeCost} JP";
            }
        }
    }
}