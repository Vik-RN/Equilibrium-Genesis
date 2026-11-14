using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyDummy : MonoBehaviour
{
    public enum EnemyState { Patrol, Chase, Attack, Surrender, Arrested, Dead }

    [Header("FSM State")]
    public EnemyState currentState = EnemyState.Patrol;

    [Header("Combat Status (Dibaca oleh PlayerMovement)")]
    public bool isSurrendered = false;
    public bool isArrested = false;

    [Header("Justice Point Reward")]
    public int justicePointReward = 10;

    [Header("AI Target & Navigation")]
    [Tooltip("Tarik objek Player_Base ke sini (Otomatis dicari jika kosong)")]
    public Transform playerTarget;
    [Tooltip("Daftar titik koordinat untuk rute patroli musuh")]
    public Transform[] patrolPoints;
    public float patrolSpeed = 2.0f;
    public float chaseSpeed = 4.0f;
    public float patrolWaitTime = 2.0f;
    private int currentPatrolIndex = 0;
    private float waitTimer = 0f;

    [Header("Detection & Attack Settings")]
    [Tooltip("Jarak musuh mulai menyadari keberadaan Lina")]
    public float detectionRange = 8.0f;
    [Tooltip("Jarak musuh berhenti mengejar dan mulai memukul")]
    public float attackRange = 1.8f;
    public float attackCooldown = 2.0f;
    public int attackDamage = 15;
    public Transform enemyAttackPoint;
    public float enemyAttackRadius = 1.0f;
    public LayerMask playerLayer;
    private float lastAttackTime = 0f;

    [Header("Health & Stun Settings (Non-Lethal)")]
    public int maxHealth = 100;
    private int currentHealth;
    public int maxStun = 100;
    private int currentStun = 0;

    [Header("Regeneration Settings")]
    public float regenDelay = 3f;
    public float hpRegenPerSecond = 2f;
    public float stunRecoveryPerSecond = 5f;
    private float timeSinceLastHit = 0f;
    private float hpRegenAccumulator = 0f;
    private float stunRegenAccumulator = 0f;

    [Header("UI & Animation References")]
    public Slider hpSlider;    
    public Slider stunSlider;  
    public GameObject arrestPromptUI; 
    public Animator animator;  

    [Header("Animation State/Parameter Names")]
    [Tooltip("Nama parameter Float di Animator untuk transisi Idle (0) ke Walk/Run (1)")]
    public string moveSpeedParam = "Speed"; 
    public string attackTrigger = "Attack";
    public string hitTrigger = "Hit";
    public string surrenderTrigger = "Surrender";
    public string arrestedTrigger = "Arrested";
    public string dieTrigger = "Die";

    [Header("Knockback Settings")]
    public float knockbackDecay = 5f; 
    private Vector3 knockbackVelocity;

    private CharacterController controller;
    private NavMeshAgent agent;
    private Camera mainCamera;

    void Start()
    {
        currentHealth = maxHealth;
        currentStun = 0;
        
        controller = GetComponent<CharacterController>();
        agent = GetComponent<NavMeshAgent>();
        mainCamera = Camera.main;

        // Otomatis cari Player jika lupa dimasukkan di Inspector
        if (playerTarget == null)
        {
            PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
            if (player != null) playerTarget = player.transform;
        }

        if (hpSlider != null) { hpSlider.maxValue = maxHealth; hpSlider.value = currentHealth; }
        if (stunSlider != null) { stunSlider.maxValue = maxStun; stunSlider.value = currentStun; }
        if (arrestPromptUI != null) arrestPromptUI.SetActive(false);

        // Mulai rute patroli jika ada titik yang didaftarkan
        if (patrolPoints != null && patrolPoints.Length > 0 && agent.isOnNavMesh)
        {
            agent.speed = patrolSpeed;
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }
    }

    void Update()
    {
        if (currentState == EnemyState.Arrested || currentState == EnemyState.Dead) return;

        // 1. Proses dorongan Knockback saat terkena serangan
        if (knockbackVelocity.magnitude > 0.1f)
        {
            // Hentikan sementara NavMeshAgent agar tidak berbenturan dengan CharacterController
            if (agent.isOnNavMesh) agent.isStopped = true;
            
            controller.Move(knockbackVelocity * Time.deltaTime);
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, knockbackDecay * Time.deltaTime);
            
            // Sinkronisasi posisi NavMeshAgent dengan posisi baru setelah terdorong
            if (agent.isOnNavMesh) agent.nextPosition = transform.position;
            return; 
        }

        // 2. Jika tidak sedang menyerah, jalankan Regen dan State Machine AI
        if (currentState != EnemyState.Surrender)
        {
            HandleRegeneration();
            UpdateFSM();
        }
    }

    void UpdateFSM()
    {
        if (playerTarget == null || !agent.isOnNavMesh) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        switch (currentState)
        {
            case EnemyState.Patrol:
                HandlePatrolState(distanceToPlayer);
                break;
            case EnemyState.Chase:
                HandleChaseState(distanceToPlayer);
                break;
            case EnemyState.Attack:
                HandleAttackState(distanceToPlayer);
                break;
        }

        // Update parameter kecepatan jalan di Animator (jika menggunakan Blend Tree / Float Speed)
        if (animator != null && !string.IsNullOrEmpty(moveSpeedParam))
        {
            float currentVel = agent.isStopped ? 0f : agent.velocity.magnitude;
            animator.SetFloat(moveSpeedParam, currentVel);
        }
    }

    // --- LOGIKA TIAP STATE ---
    void HandlePatrolState(float distanceToPlayer)
    {
        // Transisi ke Chase jika pemain masuk jangkauan deteksi
        if (distanceToPlayer <= detectionRange)
        {
            currentState = EnemyState.Chase;
            return;
        }

        agent.speed = patrolSpeed;

        // Jika tidak punya titik patroli, diam di tempat (jaga pos)
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            agent.isStopped = true;
            return;
        }

        agent.isStopped = false;

        // Cek apakah sudah sampai di titik waypoint saat ini
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= patrolWaitTime)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                agent.SetDestination(patrolPoints[currentPatrolIndex].position);
                waitTimer = 0f;
            }
        }
    }

    void HandleChaseState(float distanceToPlayer)
    {
        // Jika pemain kabur terlalu jauh (1.5x jarak deteksi), kembali berpatroli
        if (distanceToPlayer > detectionRange * 1.5f)
        {
            currentState = EnemyState.Patrol;
            if (patrolPoints != null && patrolPoints.Length > 0)
                agent.SetDestination(patrolPoints[currentPatrolIndex].position);
            return;
        }

        // Jika sudah masuk jarak pukul, pindah ke state Attack
        if (distanceToPlayer <= attackRange)
        {
            currentState = EnemyState.Attack;
            agent.isStopped = true;
            return;
        }

        // Kejar posisi pemain
        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.SetDestination(playerTarget.position);
    }

    void HandleAttackState(float distanceToPlayer)
    {
        agent.isStopped = true;

        // Jika pemain menjauh sedikit dari jarak pukul, kejar lagi
        if (distanceToPlayer > attackRange + 0.4f)
        {
            currentState = EnemyState.Chase;
            return;
        }

        // Putar badan musuh agar selalu menghadap ke arah pemain saat menyerang
        Vector3 lookDir = (playerTarget.position - transform.position).normalized;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), 10f * Time.deltaTime);
        }

        // Eksekusi serangan berdasarkan Cooldown
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            if (animator != null) animator.SetTrigger(attackTrigger);
            
            // Hitung perkenaan serangan setelah jeda 0.3 detik (menyesuaikan ayunan tangan)
            Invoke(nameof(ExecuteEnemyHitbox), 0.3f);
        }
    }

    void ExecuteEnemyHitbox()
    {
        // Batalkan serangan jika musuh keburu menyerah atau mati saat mengayunkan tangan
        if (currentState != EnemyState.Attack) return;

        Transform hitOrigin = enemyAttackPoint != null ? enemyAttackPoint : transform;
        Collider[] hitPlayers = Physics.OverlapSphere(hitOrigin.position, enemyAttackRadius, playerLayer);

        foreach (Collider player in hitPlayers)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
        }
    }

    // --- LOGIKA DAMAGE, REGEN, & SURRENDER ---
    public void TakeDamage(int damage, int stunDamage, Vector3 hitDirection, float knockbackForce)
    {
        if (isSurrendered || isArrested || currentState == EnemyState.Dead) return;

        timeSinceLastHit = 0f;
        hpRegenAccumulator = 0f;
        stunRegenAccumulator = 0f;

        currentHealth -= damage;
        currentStun += stunDamage; 
        
        UpdateUI();

        // Jika diserang dari luar jangkauan saat berpatroli, otomatis sadar dan mengejar pemain
        if (currentState == EnemyState.Patrol)
        {
            currentState = EnemyState.Chase;
        }

        if (currentStun >= maxStun) Surrender();
        else if (currentHealth <= 0) Die();
        else
        {
            if (animator != null) animator.SetTrigger(hitTrigger); 
            knockbackVelocity = hitDirection * knockbackForce;
        }
    }

    void HandleRegeneration()
    {
        timeSinceLastHit += Time.deltaTime;
        if (timeSinceLastHit >= regenDelay)
        {
            bool needsUIUpdate = false;

            if (currentHealth < maxHealth && hpRegenPerSecond > 0)
            {
                hpRegenAccumulator += hpRegenPerSecond * Time.deltaTime;
                if (hpRegenAccumulator >= 1f)
                {
                    int regenAmount = Mathf.FloorToInt(hpRegenAccumulator);
                    currentHealth = Mathf.Min(currentHealth + regenAmount, maxHealth);
                    hpRegenAccumulator -= regenAmount;
                    needsUIUpdate = true;
                }
            }

            if (currentStun > 0 && stunRecoveryPerSecond > 0)
            {
                stunRegenAccumulator += stunRecoveryPerSecond * Time.deltaTime;
                if (stunRegenAccumulator >= 1f)
                {
                    int recoveryAmount = Mathf.FloorToInt(stunRegenAccumulator);
                    currentStun = Mathf.Max(currentStun - recoveryAmount, 0);
                    stunRegenAccumulator -= recoveryAmount;
                    needsUIUpdate = true;
                }
            }

            if (needsUIUpdate) UpdateUI();
        }
    }

    void Surrender()
    {
        currentState = EnemyState.Surrender;
        isSurrendered = true;
        currentStun = maxStun; 
        UpdateUI();

        knockbackVelocity = Vector3.zero; 
        CancelInvoke(nameof(ExecuteEnemyHitbox));

        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (animator != null)
        {
            if (!string.IsNullOrEmpty(moveSpeedParam)) animator.SetFloat(moveSpeedParam, 0f);
            animator.SetTrigger(surrenderTrigger); 
        }
        if (arrestPromptUI != null) arrestPromptUI.SetActive(true); 
    }

    public void Arrest()
    {
        if (!isSurrendered || isArrested) return;

        currentState = EnemyState.Arrested;
        isArrested = true;
        
        if (arrestPromptUI != null) arrestPromptUI.SetActive(false);
        if (hpSlider != null) hpSlider.transform.parent.gameObject.SetActive(false); 

        if (animator != null) animator.SetTrigger(arrestedTrigger);
        Debug.Log("Musuh berhasil diamankan! +10 Justice Points (Equilibrium)");

        if (JusticePointManager.Instance != null)
        {
            JusticePointManager.Instance.AddJusticePoints(justicePointReward);
        }

        if (DialogueAndQuestManager.Instance != null)
        {
            DialogueAndQuestManager.Instance.AddArrestQuestProgress();
        }

        Destroy(gameObject, 2f);
    }

    void Die()
    {
        currentState = EnemyState.Dead;
        currentHealth = 0; 
        UpdateUI();
        
        CancelInvoke(nameof(ExecuteEnemyHitbox));
        if (agent.isOnNavMesh) agent.isStopped = true;

        if (animator != null) animator.SetTrigger(dieTrigger);
        Debug.Log("Musuh kehabisan HP (Lethal Finish - Tidak mendapat Justice Point).");
        
        GetComponent<Collider>().enabled = false;
        Destroy(gameObject, 3f);
    }

    void UpdateUI()
    {
        if (hpSlider != null) hpSlider.value = currentHealth;
        if (stunSlider != null) stunSlider.value = currentStun;
    }

    void LateUpdate()
    {
        if (hpSlider != null && mainCamera != null)
        {
            hpSlider.transform.parent.LookAt(hpSlider.transform.parent.position + mainCamera.transform.rotation * Vector3.forward,
                                             mainCamera.transform.rotation * Vector3.up);
        }
    }

    // Visualisasi area deteksi dan jangkauan serang di Scene View
    void OnDrawGizmosSelected()
    {
        // Lingkaran Kuning: Jangkauan Deteksi (Mulai Mengejar)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Lingkaran Merah: Jangkauan Berhenti & Memukul
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Lingkaran Oranye: Hitbox Pukulan Musuh
        if (enemyAttackPoint != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireSphere(enemyAttackPoint.position, enemyAttackRadius);
        }
    }
}