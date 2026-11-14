using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class PlayerAnimationNames
{
    [Header("Normal Stance (Tanpa Sihir)")]
    public string idleNormal = "Idle_Normal_MC";
    public string runNormal = "Run_Normal_MC";
    public string jumpNormal = "Jump_Normal_MC";
    public string jumpAirNormal1 = "JumpAir_Nrm1_MC";
    public string jumpAirNormal2 = "JumpAir_Nrm2_MC";
    public string dashNormal1 = "Dash1_Normal_MC";
    public string dashNormal2 = "Dash2_Normal_MC";

    [Header("Combat Stance (Kuda-kuda Sihir)")]
    public string idleCombat = "Idle_Combat_MC";
    public string runCombat = "Run_Combat_MC";
    public string jumpCombat = "Jump_Combat_MC";
    public string jumpAirCombat1 = "JumpAir_Cmb1_MC";
    public string jumpAirCombat2 = "JumpAir_Cmb2_MC";
    public string dashCombat1 = "Dash1_Combat_MC";
    public string dashCombat2 = "Dash2_Combat_MC";

    [Header("Basic Attack (Magic Burst)")]
    public string[] basicAttacks = { "BA1_MC", "BA2_MC", "BA3_MC", "BA4_MC" };
}


[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerEnergy))]
public class PlayerMovement : MonoBehaviour
{
    public enum MovementState { Grounded, MidAir, Dashing, Attacking, Casting, SkillExecuting }

    [Header("State Machine")]
    public MovementState currentState = MovementState.Grounded;

    [Header("References")]
    private CharacterController controller;
    private PlayerEnergy energyManager;
    private PlayerHealth healthManager;
    private PlayerControls playerControls;
    public Transform cameraTransform; 
    private Camera mainCamera; 
    public Animator animator;

    [Header("Animation Settings")]
    public PlayerAnimationNames animNames;

    [Header("Magic Casting Setup")]
    public Transform attackPoint;      
    public Transform castHandPoint;    

    [Header("Combat Stance Settings")]
    public bool isInCombatStance = false; 
    public float stanceTimeout = 5f; 
    private float lastInputTime;

    [Header("Basic Attack Hitbox Settings")]
    public float attackRadius = 1.2f;  
    public LayerMask enemyLayer;
    [Tooltip("Pilih layer Default / Ground / Environment agar sihir meledak saat kena lantai/tembok")]
    public LayerMask obstacleLayer = 1; // 1 = Default Layer
    public LayerMask npcLayer;

    [Header("Player Audio Settings (SFX)")]
    public AudioSource playerAudioSource; // Untuk Basic Attack & efek sekali putar
    public AudioSource castingAudioSource; // Khusus untuk suara merapal sihir di tangan
    public AudioClip footstepSFX;
    [Tooltip("Jeda waktu antar langkah kaki saat berlari (detik)")]
    public float footstepInterval = 0.35f;
    [Range(0f, 1f)] public float footstepVolume = 0.5f;
    public AudioClip basicAttackSFX; // Satu suara yang sama untuk semua combo Basic Attack
    [Range(0f, 1f)] public float basicAttackVolume = 0.8f;
    private float footstepTimer = 0f;    

    [Header("Basic Attack Combo (Magic Burst)")]
    public float[] attackDurations = { 1.0f, 1.0f, 1.0f, 1.5f }; 
    public float[] comboWindows = { 0.5f, 0.5f, 0.5f, 0f }; 
    public float[] attackForwardSpeeds = { 1f, 1f, 1f, 2f }; 
    public float[] attackMoveDurations = { 0.15f, 0.15f, 0.15f, 0.25f };
    public float[] attackHitTimes = { 0.2f, 0.2f, 0.2f, 0.3f };
    public int[] attackDamages = { 10, 10, 15, 25 };
    public int[] attackStunDamages = { 8, 8, 12, 20 };
    public float[] attackKnockbacks = { 2f, 2f, 2f, 5f };

    [Header("Air Combat (Levitation)")]
    public bool floatDuringAirAttack = true;
    [Range(0f, 1f)] public float airAttackGravityScale = 0.02f; 

    [Header("Equipped Magic Skills (0=Z, 1=X, 2=C, 3=V)")]
    public SkillData[] equippedSkills = new SkillData[4];

    [Header("Skill HUD Cooldown UI (0=Z, 1=X, 2=C, 3=V)")]
    [Tooltip("Teks TMP di atas ikon skill untuk menampilkan sisa detik cooldown")]
    public TextMeshProUGUI[] skillCooldownTexts = new TextMeshProUGUI[4];
    [Tooltip("Opsional: Image hitam transparan (Fill Radial 360) untuk efek gelap saat cooldown")]
    public Image[] skillCooldownOverlays = new Image[4];

    // Variabel Operasional Skill & Cooldown
    private float[] cooldownTimers = new float[4];
    private int activeSkillIndex = -1;
    private SkillData currentSkill;
    private GameObject spawnedMagicCircle; 
    private float skillTimer = 0f;
    private float currentCastTime = 0f; 
    private bool hasDealtSkillDamage = false;

    private int currentCombo = 0;
    private float attackTimer = 0f;
    private bool isComboQueued = false;
    private bool hasDealtDamage = false; 
    private bool isAirAttacking = false; 

    private string currentAnim;       
    private bool useDash2 = false;    
    private bool useAirJump2 = false; 
    private float animLockTimer = 0f; 

    [Header("Movement Settings")]
    public float moveSpeed = 7f;
    public float rotationSpeed = 25f; 
    public float gravity = -20f;
    private Vector3 velocity;
    private Vector2 moveInput;

    [Header("Jump & Dash Settings")]
    public float jumpHeight = 3f;
    public int maxAirJumps = 2; 
    private int airJumpCount = 0;
    public float dashSpeed = 25f;
    public float dashDuration = 0.2f; 
    public float dashAnimDuration = 0.6f; 
    private float dashTimer;
    private Vector3 dashDirection;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        energyManager = GetComponent<PlayerEnergy>();
        healthManager = GetComponent<PlayerHealth>();
        playerControls = new PlayerControls();
        if (playerAudioSource == null) playerAudioSource = gameObject.AddComponent<AudioSource>();
        if (castingAudioSource == null) castingAudioSource = gameObject.AddComponent<AudioSource>();

    }

    void OnEnable()
    {
        playerControls.Gameplay.Enable();
        playerControls.Gameplay.Jump.performed += OnJumpPerformed;
        playerControls.Gameplay.Dash.performed += OnDashPerformed;
        playerControls.Gameplay.BasicAttack.performed += OnBasicAttackPerformed;
        playerControls.Gameplay.Interact.performed += OnInteractPerformed;

        // Input 4 Slot Skill (Z, X, C, V)
        playerControls.Gameplay.Skill_Z.started += ctx => StartSkillCast(0);
        playerControls.Gameplay.Skill_Z.canceled += ctx => ReleaseSkillCast(0);

        playerControls.Gameplay.Skill_X.started += ctx => StartSkillCast(1);
        playerControls.Gameplay.Skill_X.canceled += ctx => ReleaseSkillCast(1);

        playerControls.Gameplay.Skill_C.started += ctx => StartSkillCast(2);
        playerControls.Gameplay.Skill_C.canceled += ctx => ReleaseSkillCast(2);

        playerControls.Gameplay.Skill_V.started += ctx => StartSkillCast(3);
        playerControls.Gameplay.Skill_V.canceled += ctx => ReleaseSkillCast(3);
    }

    void OnDisable()
    {
        playerControls.Gameplay.Jump.performed -= OnJumpPerformed;
        playerControls.Gameplay.Dash.performed -= OnDashPerformed;
        playerControls.Gameplay.BasicAttack.performed -= OnBasicAttackPerformed;
        playerControls.Gameplay.Interact.performed -= OnInteractPerformed;
        playerControls.Gameplay.Disable();
    }

    void Start()
    {
        lastInputTime = Time.time;
        if (cameraTransform != null) mainCamera = cameraTransform.GetComponent<Camera>();
        if (mainCamera == null) mainCamera = Camera.main; 
        UpdateSkillHUD();
    }

    void Update()
    {   
        // Selalu hitung mundur cooldown meskipun sedang bergerak/menyerang
        UpdateSkillCooldowns();

        CheckNearbyInteractables();

        // Hentikan input gerak/serang jika Menu Upgrade sedang dibuka (Time.timeScale == 0)
        if (Time.timeScale == 0f) return;

        if (DialogueAndQuestManager.Instance != null && DialogueAndQuestManager.Instance.isDialogueActive)
        {
            moveInput = Vector2.zero;
            ChangeAnimation(isInCombatStance ? animNames.idleCombat : animNames.idleNormal);
            ApplyGravity(); 
            return;
        }

        moveInput = playerControls.Gameplay.Move.ReadValue<Vector2>();
        if (moveInput.magnitude > 0.1f || playerControls.Gameplay.CameraPanHold.ReadValue<float>() > 0.1f) RegisterInput();

        if (isInCombatStance && Time.time - lastInputTime > stanceTimeout && currentState != MovementState.Attacking && currentState != MovementState.Casting && currentState != MovementState.SkillExecuting)
        {
            isInCombatStance = false;
        }

        if (animLockTimer > 0) animLockTimer -= Time.deltaTime;
        if (currentState != MovementState.Dashing && currentState != MovementState.SkillExecuting) HandleRotation();

        switch (currentState)
        {
            case MovementState.Grounded: HandleGroundedState(); break;
            case MovementState.MidAir: HandleMidAirState(); break;
            case MovementState.Dashing: HandleDashingState(); break;
            case MovementState.Attacking: HandleAttackingState(); break;
            case MovementState.Casting: HandleCastingState(); break;
            case MovementState.SkillExecuting: HandleSkillExecutingState(); break;
        }
    }

    // --- MANAJEMEN COOLDOWN & HUD ---
    void UpdateSkillCooldowns()
    {
        for (int i = 0; i < cooldownTimers.Length; i++)
        {
            if (cooldownTimers[i] > 0f)
            {
                cooldownTimers[i] -= Time.deltaTime;
                if (cooldownTimers[i] < 0f) cooldownTimers[i] = 0f;
            }
        }
        UpdateSkillHUD();
    }

    void UpdateSkillHUD()
    {
        for (int i = 0; i < 4; i++)
        {
            float remaining = cooldownTimers[i];

            if (skillCooldownTexts != null && i < skillCooldownTexts.Length && skillCooldownTexts[i] != null)
            {
                // Ambil nama skill dari ScriptableObject, jika kosong tulis "Empty"
                string sName = (equippedSkills[i] != null) ? equippedSkills[i].skillName : "Empty";
                
                // Jika masih cooldown tampilkan angka detik, jika siap tampilkan nama skillnya!
                skillCooldownTexts[i].text = remaining > 0f ? remaining.ToString("F1") : sName;
            }

            if (skillCooldownOverlays != null && i < skillCooldownOverlays.Length && skillCooldownOverlays[i] != null)
            {
                if (equippedSkills[i] != null && equippedSkills[i].cooldown > 0f)
                    skillCooldownOverlays[i].fillAmount = remaining / equippedSkills[i].cooldown;
                else
                    skillCooldownOverlays[i].fillAmount = 0f;
            }
        }
    }

    // --- SISTEM CASTING 4 SKILL (Z=0, X=1, C=2, V=3) ---
    void StartSkillCast(int slotIndex)
    {
        if (DialogueAndQuestManager.Instance != null && DialogueAndQuestManager.Instance.isDialogueActive) return;
        if (Time.timeScale == 0f) return;
        if (slotIndex < 0 || slotIndex >= equippedSkills.Length) return;

        SkillData selectedSkill = equippedSkills[slotIndex];
        if (selectedSkill == null) return;

        // Cek apakah skill masih cooldown
        if (cooldownTimers[slotIndex] > 0f)
        {
            Debug.Log($"Skill {selectedSkill.skillName} masih Cooldown ({cooldownTimers[slotIndex]:F1}s)!");
            return;
        }

        if (currentState == MovementState.Dashing || currentState == MovementState.Attacking || currentState == MovementState.Casting || currentState == MovementState.SkillExecuting) return;

        RegisterInput(); 
        isInCombatStance = true; 
        activeSkillIndex = slotIndex;
        currentSkill = selectedSkill; 
        currentState = MovementState.Casting; 
        currentCastTime = 0f; 
        
        ChangeAnimation(currentSkill.castAnimName, 0.1f); 
        
        if (currentSkill.magicCirclePrefab != null && castHandPoint != null)
        {
            spawnedMagicCircle = Instantiate(currentSkill.magicCirclePrefab, castHandPoint);
            spawnedMagicCircle.transform.localPosition = currentSkill.magicCircleOffset;
            spawnedMagicCircle.transform.localRotation = Quaternion.Euler(currentSkill.magicCircleRotation);
            spawnedMagicCircle.transform.localScale = currentSkill.magicCircleScale;

            foreach (ParticleSystem ps in spawnedMagicCircle.GetComponentsInChildren<ParticleSystem>())
            {
                var main = ps.main; main.loop = true; main.playOnAwake = true; ps.Play(); 
            }
        }
        velocity.x = 0; velocity.z = 0;

        // Putar suara Cast di tangan
        if (currentSkill.castSFX != null && castingAudioSource != null)
        {
            castingAudioSource.clip = currentSkill.castSFX;
            castingAudioSource.loop = currentSkill.loopCastSFX;
            castingAudioSource.volume = currentSkill.castVolume;
            castingAudioSource.Play();
        }
    }

    void HandleCastingState() 
    { 
        currentCastTime += Time.deltaTime; 

        if (!controller.isGrounded)
        {
            // Kunci posisi agar melayang diam di udara
            velocity = Vector3.zero;

            // Kuras energi sedikit demi sedikit selama menahan skill di udara
            if (energyManager != null)
            {
                bool hasEnergy = energyManager.DrainEnergyOverTime(energyManager.airCastDrainPerSecond);
                if (!hasEnergy)
                {
                    Debug.Log("<color=orange>Energi habis saat menahan skill di udara! Rapalan gagal & Cooldown.</color>");
                    CancelAttack(); // Otomatis gagal & langsung kena cooldown!
                }
            }
        }
        else
        {
            // Jika di darat, tetap gunakan gravitasi normal agar menapak tanah
            ApplyGravity();
        }
    }

    Vector3 GetCursorAimDirection(Vector3 spawnPos)
    {
        if (mainCamera == null) return transform.forward;

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        LayerMask aimMask = enemyLayer | obstacleLayer;

        Vector3 targetPoint;
        // Jika kursor mengenai musuh, tanah, atau tembok
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, aimMask))
        {
            targetPoint = hit.point;
            // Jika mengenai musuh, arahkan sedikit ke tengah badan (bukan ke telapak kaki)
            if (((1 << hit.collider.gameObject.layer) & enemyLayer) != 0)
            {
                targetPoint += Vector3.up * 0.8f;
            }
        }
        else
        {
            // Jika kursor diarahkan ke langit/ruang kosong, ambil titik lurus searah kursor
            Plane groundPlane = new Plane(Vector3.up, new Vector3(0, spawnPos.y, 0));
            if (groundPlane.Raycast(ray, out float distance))
                targetPoint = ray.GetPoint(distance);
            else
                targetPoint = ray.GetPoint(currentSkill != null ? currentSkill.maxRange : 15f);
        }

        Vector3 dir = (targetPoint - spawnPos).normalized;
        return dir.sqrMagnitude > 0.001f ? dir : transform.forward;
    }

    void ReleaseSkillCast(int slotIndex)
    {
        if (currentState == MovementState.Casting && currentSkill != null && activeSkillIndex == slotIndex)
        {
            if (currentCastTime >= currentSkill.castTime)
            {
                cooldownTimers[slotIndex] = currentSkill.cooldown;

                currentState = MovementState.SkillExecuting;
                ChangeAnimation(currentSkill.executeAnimName, 0.05f); 
                skillTimer = currentSkill.duration; 
                
                // 1. Hapus Efek Cast (Magic Circle di tangan)
                if (spawnedMagicCircle != null) Destroy(spawnedMagicCircle);
                if (castingAudioSource != null && castingAudioSource.isPlaying) castingAudioSource.Stop();

                // 2. Hitung arah ke pointer kursor & putar badan Lina menghadap arah lemparan
                Transform spawnOrigin = attackPoint != null ? attackPoint : transform;
                Vector3 aimDirection = GetCursorAimDirection(spawnOrigin.position);

                Vector3 flatLook = new Vector3(aimDirection.x, 0f, aimDirection.z);
                if (flatLook.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(flatLook.normalized);
                }

                // 3. Munculkan Efek Projectile & luncurkan!
                if (currentSkill.projectilePrefab != null)
                {
                    GameObject projObj = Instantiate(currentSkill.projectilePrefab, spawnOrigin.position, Quaternion.LookRotation(aimDirection));
                    MagicProjectile projScript = projObj.GetComponent<MagicProjectile>();
                    if (projScript == null) projScript = projObj.AddComponent<MagicProjectile>();

                    projScript.Initialize(currentSkill, aimDirection, enemyLayer, obstacleLayer);
                }
            }
            else 
            { 
                CancelAttack();
            }
        }
    }

    void HandleSkillExecutingState()
    {
        ApplyGravity(); 
        skillTimer -= Time.deltaTime;

        if (skillTimer <= 0) 
        { 
            currentSkill = null; 
            activeSkillIndex = -1; 
            currentState = controller.isGrounded ? MovementState.Grounded : MovementState.MidAir; 
        }
    }

    void HandleRotation()
    {
        if (mainCamera == null) return;
        if (playerControls.Gameplay.CameraPanHold.ReadValue<float>() > 0.1f)
        {
            Vector3 camForward = cameraTransform.forward; camForward.y = 0f;
            if (camForward.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(camForward.normalized), rotationSpeed * Time.deltaTime);
        }
        else
        {
            Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            Plane groundPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));
            if (groundPlane.Raycast(ray, out float distance))
            {
                Vector3 lookDirection = ray.GetPoint(distance) - transform.position; lookDirection.y = 0f; 
                if (lookDirection.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection.normalized), rotationSpeed * Time.deltaTime);
            }
        }
    }

    void OnBasicAttackPerformed(InputAction.CallbackContext context)
    {
        if (DialogueAndQuestManager.Instance != null && DialogueAndQuestManager.Instance.isDialogueActive) return;
        if (Time.timeScale == 0f) return;
        if (currentState == MovementState.Casting || currentState == MovementState.SkillExecuting) return;
        RegisterInput(); isInCombatStance = true;

        if (currentState == MovementState.Dashing) return;
        if (currentState != MovementState.Attacking) StartAttack(1);
        else if (currentCombo < 4) isComboQueued = true;
    }

    void StartAttack(int comboStep)
    {
        currentState = MovementState.Attacking; currentCombo = comboStep; isComboQueued = false; hasDealtDamage = false; 
        isAirAttacking = !controller.isGrounded;
        if (isAirAttacking && floatDuringAirAttack) velocity.y = 0f; 
        
        ChangeAnimation(animNames.basicAttacks[currentCombo - 1], 0.05f); 
        attackTimer = attackDurations[currentCombo - 1]; velocity.x = 0; velocity.z = 0;

        // Putar suara Basic Attack (sama untuk semua combo 1-4)
        if (basicAttackSFX != null && playerAudioSource != null)
        {
            playerAudioSource.PlayOneShot(basicAttackSFX, basicAttackVolume);
        }
    }

    void HandleAttackingState()
    {
        if (!isAirAttacking || !floatDuringAirAttack) ApplyGravity();
        else { velocity.y += (gravity * airAttackGravityScale) * Time.deltaTime; controller.Move(velocity * Time.deltaTime); }

        attackTimer -= Time.deltaTime; int comboIndex = currentCombo - 1; float elapsedTime = attackDurations[comboIndex] - attackTimer;

        if (elapsedTime < attackMoveDurations[comboIndex]) controller.Move(transform.forward * attackForwardSpeeds[comboIndex] * Time.deltaTime); 
        if (!hasDealtDamage && elapsedTime >= attackHitTimes[comboIndex]) { DealDamage(comboIndex); hasDealtDamage = true; }

        if (isComboQueued && attackTimer <= comboWindows[comboIndex]) { StartAttack(currentCombo + 1); return; }
        if (attackTimer <= 0) { currentCombo = 0; currentState = controller.isGrounded ? MovementState.Grounded : MovementState.MidAir; }
    }

    void DealDamage(int comboIndex)
    {
        if (attackPoint == null) return;
        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRadius, enemyLayer);
        foreach (Collider enemy in hitEnemies)
        {
            EnemyDummy dummy = enemy.GetComponent<EnemyDummy>();
            if (dummy != null)
            {
                Vector3 knockbackDir = (enemy.transform.position - transform.position).normalized; knockbackDir.y = 0f; 
                int stunVal = (attackStunDamages != null && comboIndex < attackStunDamages.Length) ? attackStunDamages[comboIndex] : 10;
                dummy.TakeDamage(attackDamages[comboIndex], stunVal, knockbackDir, attackKnockbacks[comboIndex]);
            }
        }
    }

    public void CancelAttack()
    {
        // JIKA GAGAL SAAT SEDANG MERAPAL (CASTING), LANGSUNG MASUKKAN KE COOLDOWN!
        if (currentState == MovementState.Casting && currentSkill != null && activeSkillIndex >= 0)
        {
            cooldownTimers[activeSkillIndex] = currentSkill.cooldown;
            Debug.Log($"Rapalan {currentSkill.skillName} gagal/dibatalkan! Terkena penalti Cooldown {currentSkill.cooldown}s.");
        }

        currentCombo = 0; isComboQueued = false; attackTimer = 0; isAirAttacking = false; 
        if (spawnedMagicCircle != null) Destroy(spawnedMagicCircle); 
        currentSkill = null; activeSkillIndex = -1;
        
        if (currentState == MovementState.Attacking || currentState == MovementState.Casting || currentState == MovementState.SkillExecuting) 
            currentState = controller.isGrounded ? MovementState.Grounded : MovementState.MidAir;

        if (castingAudioSource != null && castingAudioSource.isPlaying) castingAudioSource.Stop();
    }

    void RegisterInput() { lastInputTime = Time.time; }

    void HandleGroundedState()
    {
        if (controller.isGrounded && velocity.y < 0) { velocity.y = -2f; airJumpCount = 0; }
        MoveCharacter(moveSpeed); ApplyGravity();

        if (moveInput.magnitude > 0.1f)
        {
            if (animLockTimer <= 0) ChangeAnimation(isInCombatStance ? animNames.runCombat : animNames.runNormal);

            // Logika suara langkah kaki
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                if (footstepSFX != null && playerAudioSource != null)
                {
                    playerAudioSource.PlayOneShot(footstepSFX, footstepVolume);
                }
                footstepTimer = footstepInterval;
            }
        }
        else
        {
            if (animLockTimer <= 0) ChangeAnimation(isInCombatStance ? animNames.idleCombat : animNames.idleNormal);
            footstepTimer = 0f; // Reset agar saat mulai jalan langsung berbunyi
        }

        if (!controller.isGrounded) currentState = MovementState.MidAir;
    }

    void HandleMidAirState() { MoveCharacter(moveSpeed); ApplyGravity(); if (controller.isGrounded) currentState = MovementState.Grounded; }

    void HandleDashingState() 
    { 
        controller.Move(dashDirection * dashSpeed * Time.deltaTime); 
        dashTimer -= Time.deltaTime; 
        if (dashTimer <= 0) 
        {
            if (healthManager != null) healthManager.isInvulnerable = false;
            currentState = controller.isGrounded ? MovementState.Grounded : MovementState.MidAir; 
        }
    }

    void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (DialogueAndQuestManager.Instance != null && DialogueAndQuestManager.Instance.isDialogueActive) return;
        if (Time.timeScale == 0f) return;
        RegisterInput(); 
        if (currentState == MovementState.Dashing || currentState == MovementState.Attacking || currentState == MovementState.Casting || currentState == MovementState.SkillExecuting) return;
        if (currentState == MovementState.Grounded) ExecuteJump();
        else if (currentState == MovementState.MidAir && airJumpCount < maxAirJumps) { ExecuteJump(); airJumpCount++; }
    }

    void OnDashPerformed(InputAction.CallbackContext context)
    {
        if (DialogueAndQuestManager.Instance != null && DialogueAndQuestManager.Instance.isDialogueActive) return;
        if (Time.timeScale == 0f) return;
        RegisterInput(); 
        if (currentState == MovementState.Attacking || currentState == MovementState.Casting || currentState == MovementState.SkillExecuting) CancelAttack();
        if (currentState != MovementState.Dashing && energyManager.CanDash()) { energyManager.UseDashEnergy(); StartDash(); }
    }

    void MoveCharacter(float speed)
    {
        if (cameraTransform == null) return;
        Vector3 camForward = cameraTransform.forward; camForward.y = 0f; camForward.Normalize();
        Vector3 camRight = cameraTransform.right; camRight.y = 0f; camRight.Normalize();
        controller.Move((camForward * moveInput.y + camRight * moveInput.x) * speed * Time.deltaTime);
    }

    void ApplyGravity() { velocity.y += gravity * Time.deltaTime; controller.Move(velocity * Time.deltaTime); }

    void ExecuteJump()
    {
        velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        if (currentState == MovementState.Grounded) { currentState = MovementState.MidAir; ChangeAnimation(isInCombatStance ? animNames.jumpCombat : animNames.jumpNormal); }
        else { ChangeAnimation(isInCombatStance ? (useAirJump2 ? animNames.jumpAirCombat2 : animNames.jumpAirCombat1) : (useAirJump2 ? animNames.jumpAirNormal2 : animNames.jumpAirNormal1)); useAirJump2 = !useAirJump2; }
    }

    void StartDash()
    {
        currentState = MovementState.Dashing; dashTimer = dashDuration; animLockTimer = dashAnimDuration; 
        if (healthManager != null) healthManager.isInvulnerable = true;

        if (cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward; camForward.y = 0f; camForward.Normalize();
            Vector3 camRight = cameraTransform.right; camRight.y = 0f; camRight.Normalize();
            Vector3 moveDir = camForward * moveInput.y + camRight * moveInput.x;
            dashDirection = moveDir.magnitude > 0 ? moveDir.normalized : transform.forward;
        }
        else dashDirection = transform.forward;

        velocity.y = 0; 
        ChangeAnimation(isInCombatStance ? (useDash2 ? animNames.dashCombat2 : animNames.dashCombat1) : (useDash2 ? animNames.dashNormal2 : animNames.dashNormal1));
        useDash2 = !useDash2; 
    }

    void ChangeAnimation(string newAnim, float transitionDuration = 0.1f)
    {
        if (animator == null || currentAnim == newAnim) return;
        animator.CrossFadeInFixedTime(newAnim, transitionDuration); currentAnim = newAnim;
    }

    void CheckNearbyInteractables()
    {
        if (DialogueAndQuestManager.Instance == null) return;
        if (DialogueAndQuestManager.Instance.isDialogueActive) return;

        float interactRange = 2.5f;

        // 1. Prioritas Pertama: Cek apakah ada musuh yang sedang Surrender di dekat kita
        Collider[] nearbyEnemies = Physics.OverlapSphere(transform.position, interactRange, enemyLayer);
        foreach (Collider enemy in nearbyEnemies)
        {
            EnemyDummy dummy = enemy.GetComponent<EnemyDummy>();
            if (dummy != null && dummy.isSurrendered && !dummy.isArrested)
            {
                DialogueAndQuestManager.Instance.ShowInteractPrompt("Tangkap Penjaga");
                return;
            }
        }

        // 2. Prioritas Kedua: Cek apakah ada NPC di dekat kita
        Collider[] nearbyNPCs = Physics.OverlapSphere(transform.position, interactRange, npcLayer);
        foreach (Collider npcCol in nearbyNPCs)
        {
            NPCInteractable npc = npcCol.GetComponent<NPCInteractable>();
            if (npc != null)
            {
                DialogueAndQuestManager.Instance.ShowInteractPrompt(npc.npcName);
                return;
            }
        }

        // Jika tidak ada siapa-siapa di dekat Lina, sembunyikan prompt [F]
        DialogueAndQuestManager.Instance.HideInteractPrompt();
    }

    void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (Time.timeScale == 0f) return;

        // 1. Jika sedang membuka dialog, tombol F berfungsi untuk lanjut ke kalimat berikutnya!
        if (DialogueAndQuestManager.Instance != null && DialogueAndQuestManager.Instance.isDialogueActive)
        {
            DialogueAndQuestManager.Instance.AdvanceDialogue();
            return;
        }

        float interactRange = 2.5f; 

        // 2. Cek tangkap musuh terlebih dahulu
        Collider[] nearbyEnemies = Physics.OverlapSphere(transform.position, interactRange, enemyLayer);
        foreach (Collider enemy in nearbyEnemies)
        {
            EnemyDummy dummy = enemy.GetComponent<EnemyDummy>();
            if (dummy != null && dummy.isSurrendered && !dummy.isArrested)
            {
                dummy.Arrest();
                return; 
            }
        }

        // 3. Jika tidak ada musuh menyerah, cek bicara dengan NPC terdekat
        Collider[] nearbyNPCs = Physics.OverlapSphere(transform.position, interactRange, npcLayer);
        foreach (Collider npcCol in nearbyNPCs)
        {
            NPCInteractable npc = npcCol.GetComponent<NPCInteractable>();
            if (npc != null)
            {
                npc.TriggerInteraction();
                return;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
    }

    public bool IsGrounded()
    {
        return controller != null && controller.isGrounded;
    }
}