using UnityEngine;

public class MagicProjectile : MonoBehaviour
{
    private SkillData skillData;
    private Vector3 startPosition;
    private Vector3 moveDirection;
    private LayerMask enemyLayer;
    private LayerMask obstacleLayer;
    private bool hasExploded = false;
    private AudioSource flyAudioSource;

    public void Initialize(SkillData data, Vector3 direction, LayerMask enemyMask, LayerMask obstacleMask)
    {
        skillData = data;
        startPosition = transform.position;
        moveDirection = direction.normalized;
        enemyLayer = enemyMask;
        obstacleLayer = obstacleMask;

        if (moveDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(moveDirection);
        }
        transform.localScale = skillData.projectileScale;

        // Putar suara proyektil terbang secara Looping
        if (skillData.projectileFlySFX != null)
        {
            flyAudioSource = gameObject.AddComponent<AudioSource>();
            flyAudioSource.clip = skillData.projectileFlySFX;
            flyAudioSource.loop = true;
            flyAudioSource.volume = skillData.projectileFlyVolume;
            flyAudioSource.spatialBlend = 0.8f; // Efek suara 3D mengikuti posisi proyektil
            flyAudioSource.Play();
        }
    }

    void Update()
    {
        if (hasExploded || skillData == null) return;

        float moveDistance = skillData.projectileSpeed * Time.deltaTime;

        // 1. Deteksi tabrakan di jalur terbang
        LayerMask combinedMask = enemyLayer | obstacleLayer;
        if (Physics.SphereCast(transform.position, skillData.projectileHitboxRadius, moveDirection, out RaycastHit hit, moveDistance, combinedMask))
        {
            transform.position = hit.point;
            TriggerImpactAOE();
            return;
        }

        // 2. Gerakkan proyektil maju
        transform.position += moveDirection * moveDistance;

        // 3. Cek batas jarak lempar maksimal
        if (Vector3.Distance(startPosition, transform.position) >= skillData.maxRange)
        {
            TriggerImpactAOE();
        }
    }

    void TriggerImpactAOE()
    {
        if (hasExploded) return;
        hasExploded = true;

        // 1. Hentikan suara looping proyektil terbang seketika
        if (flyAudioSource != null && flyAudioSource.isPlaying)
        {
            flyAudioSource.Stop();
        }

        // 2. Putar suara ledakan Impact AOE dengan volume kuat walau berjarak jauh dari kamera
        if (skillData.impactAOESFX != null)
        {
            GameObject sfxObj = new GameObject("ImpactAOE_Audio");
            sfxObj.transform.position = transform.position;
            
            AudioSource impactSource = sfxObj.AddComponent<AudioSource>();
            impactSource.spatialBlend = 0.3f; // 0.3f agar suara tetap terdengar jelas walau meledak jauh dari kamera
            impactSource.minDistance = 10f;   // Jarak minimal sebelum suara mulai mengecil
            
            // PlayOneShot mendukung nilai volume di atas 1.0f (misal 1.5f atau 2.0f) untuk efek boost
            impactSource.PlayOneShot(skillData.impactAOESFX, skillData.impactAOEVolume);
            
            Destroy(sfxObj, skillData.impactAOESFX.length + 0.2f);
        }

        // 3. Munculkan Efek Visual Impact / AOE VFX
        if (skillData.impactAOEPrefab != null)
        {
            GameObject impactVFX = Instantiate(skillData.impactAOEPrefab, transform.position, Quaternion.identity);
            impactVFX.transform.localScale = skillData.impactVFXScale;
            Destroy(impactVFX, skillData.impactVFXDuration);
        }

        // 4. Hitung Damage & Stun ke seluruh musuh di dalam radius AOE
        Collider[] hitEnemies = Physics.OverlapSphere(transform.position, skillData.aoeRadius, enemyLayer);
        foreach (Collider enemy in hitEnemies)
        {
            EnemyDummy dummy = enemy.GetComponent<EnemyDummy>();
            if (dummy != null)
            {
                Vector3 knockbackDir = (enemy.transform.position - transform.position).normalized;
                knockbackDir.y = 0f;
                if (knockbackDir.sqrMagnitude < 0.01f) knockbackDir = moveDirection;

                dummy.TakeDamage(skillData.damage, skillData.stunDamage, knockbackDir, skillData.knockback);
            }
        }

        // 5. Hancurkan objek proyektil
        Destroy(gameObject);
    }

    void OnDrawGizmos()
    {
        if (skillData != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, skillData.aoeRadius);
        }
    }
}