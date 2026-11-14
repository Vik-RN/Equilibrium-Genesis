using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Stats")]
    public int maxHealth = 100;
    public int currentHealth;
    public bool isDead = false;

    [Header("i-Frame (Invulnerability) Status")]
    [Tooltip("Otomatis bernilai true saat karakter sedang Dash")]
    public bool isInvulnerable = false;

    [Header("UI References (HUD)")]
    public Slider hpSlider;
    public GameObject gameOverPanel; // Opsional: Panel UI saat kalah

    [Header("Animation Settings")]
    public string hitAnimName = "Hit_MC";
    public string dieAnimName = "Die_MC";

    private PlayerMovement playerMovement;
    private Animator animator;

    void Start()
    {
        currentHealth = maxHealth;
        playerMovement = GetComponent<PlayerMovement>();
        
        if (playerMovement != null)
        {
            animator = playerMovement.animator;
        }

        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHealth;
            hpSlider.value = currentHealth;
        }

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        // 1. CEK i-FRAME DASH: Jika sedang Dash, abaikan semua damage!
        if (isInvulnerable || (playerMovement != null && playerMovement.currentState == PlayerMovement.MovementState.Dashing))
        {
            Debug.Log("<color=cyan>DODGE! Serangan dihindari dengan i-Frame Dash!</color>");
            return;
        }

        // 2. Kurangi HP jika tidak berhasil dodge
        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;

        if (hpSlider != null) hpSlider.value = currentHealth;

        Debug.Log("Lina terkena serangan! Sisa HP: " + currentHealth);

        // 3. Cek Kematian atau Terkena Hit
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Batalkan rapalan sihir jika terkena pukul saat sedang Casting
            if (playerMovement != null) playerMovement.CancelAttack();
        }
    }

    public void Heal(int amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        if (hpSlider != null) hpSlider.value = currentHealth;
    }

    void Die()
    {
        isDead = true;
        Debug.Log("<color=red>Lina Tumbang! Game Over.</color>");

        if (playerMovement != null)
        {
            playerMovement.CancelAttack();
            playerMovement.enabled = false; // Matikan kontrol pergerakan saat kalah
        }

        if (animator != null && !string.IsNullOrEmpty(dieAnimName))
        {
            animator.CrossFadeInFixedTime(dieAnimName, 0.1f);
        }

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }
}