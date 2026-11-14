using UnityEngine;
using UnityEngine.UI;

public class PlayerEnergy : MonoBehaviour
{
    [Header("Energy Settings")]
    public float maxEnergy = 100f;
    public float currentEnergy;
    public float energyRegenPerSecond = 15f;
    public float dashEnergyCost = 25f;

    [Header("Air Casting Drain Settings")]
    [Tooltip("Jumlah energi yang terkuras per detik saat menahan skill di udara")]
    public float airCastDrainPerSecond = 20f;

    [Header("UI Reference (HUD)")]
    public Slider energySlider;

    private PlayerMovement playerMovement;

    void Start()
    {
        currentEnergy = maxEnergy;
        playerMovement = GetComponent<PlayerMovement>();

        if (energySlider != null)
        {
            energySlider.maxValue = maxEnergy;
            energySlider.value = currentEnergy;
        }
    }

    void Update()
    {
        // Jangan regenerasi energi saat sedang Dash atau sedang menahan Casting di udara
        bool isAirCasting = playerMovement != null && 
                            playerMovement.currentState == PlayerMovement.MovementState.Casting && 
                            !playerMovement.IsGrounded();

        if (!isAirCasting && currentEnergy < maxEnergy)
        {
            currentEnergy += energyRegenPerSecond * Time.deltaTime;
            if (currentEnergy > maxEnergy) currentEnergy = maxEnergy;
            UpdateEnergyUI();
        }
    }

    public bool CanDash()
    {
        return currentEnergy >= dashEnergyCost;
    }

    public void UseDashEnergy()
    {
        currentEnergy = Mathf.Max(0f, currentEnergy - dashEnergyCost);
        UpdateEnergyUI();
    }

    // Fungsi baru untuk menguras energi sedikit demi sedikit saat menahan skill di udara
    // Mengembalikan nilai false jika energi habis
    public bool DrainEnergyOverTime(float drainRate)
    {
        if (currentEnergy <= 0f) return false;

        currentEnergy -= drainRate * Time.deltaTime;
        if (currentEnergy <= 0f)
        {
            currentEnergy = 0f;
            UpdateEnergyUI();
            return false; // Energi habis!
        }

        UpdateEnergyUI();
        return true;
    }

    void UpdateEnergyUI()
    {
        if (energySlider != null)
        {
            energySlider.value = currentEnergy;
        }
    }
}