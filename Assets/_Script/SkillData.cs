using UnityEngine;

[CreateAssetMenu(fileName = "SkillData", menuName = "Scriptable Objects/SkillData")]
public class SkillData : ScriptableObject
{
    [Header("Skill Identity")]
    public string skillName = "New Spell";
    public int skillLevel = 1;

    [Header("Animations")]
    public string castAnimName = "Cast_Z_MC";     
    public string executeAnimName = "Shoot_Z_MC"; 

    [Header("Spell Requirements & Cooldown")]
    public float castTime = 1.0f;
    public float cooldown = 5.0f;

    [Header("1. Cast VFX (Lingkaran Sihir di Tangan)")]
    public GameObject magicCirclePrefab; 
    public Vector3 magicCircleOffset = Vector3.zero;       
    public Vector3 magicCircleRotation = Vector3.zero;     
    public Vector3 magicCircleScale = Vector3.one;         

    [Header("2. Projectile VFX (Saat Sihir Dilempar)")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 20f;
    [Tooltip("Jarak terbang maksimal sebelum meledak otomatis")]
    public float maxRange = 12f;
    [Tooltip("Ukuran fisik bola proyektil untuk deteksi tabrakan")]
    public float projectileHitboxRadius = 0.3f;
    public Vector3 projectileScale = Vector3.one;

    [Header("3. Impact / AOE VFX (Saat Kena Musuh / Ujung Jarak)")]
    public GameObject impactAOEPrefab;
    [Tooltip("Radius ledakan Area of Effect (AOE) masing-masing skill")]
    public float aoeRadius = 3.5f;
    public Vector3 impactVFXScale = Vector3.one;
    public float impactVFXDuration = 2.0f;

    [Header("Combat Stats (Base)")]
    public int damage = 25;
    public int stunDamage = 35; 
    public float knockback = 10f;

    [Header("Upgrade Settings (Justice Point)")]
    public int upgradeCost = 10;
    public int damageIncreasePerLevel = 5;
    public int stunIncreasePerLevel = 10;

    [Header("Execution Animation Timing")]
    public float duration = 0.8f;

    [Header("Skill Audio (SFX)")]
    [Tooltip("Suara saat menahan/merapal lingkaran sihir di tangan")]
    public AudioClip castSFX;
    public bool loopCastSFX = true;
    [Range(0f, 1f)] public float castVolume = 0.8f;

    [Tooltip("Suara saat proyektil terbang di udara (otomatis looping)")]
    public AudioClip projectileFlySFX;
    [Range(0f, 1f)] public float projectileFlyVolume = 0.8f;

    [Tooltip("Suara ledakan saat proyektil mengenai target atau mencapai batas jarak")]
    public AudioClip impactAOESFX;
    [Tooltip("Bisa diatur lebih dari 1 (hingga 2) untuk mem-boost suara ledakan yang pelan")]
    [Range(0f, 2f)] public float impactAOEVolume = 1.5f;

    public void UpgradeSkill()
    {
        skillLevel++;
        damage += damageIncreasePerLevel;
        stunDamage += stunIncreasePerLevel;
        upgradeCost += 5;
    }
}
