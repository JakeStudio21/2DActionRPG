using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    // [SerializeField] private int startingHealth = 3; - 몬스터 체력을 시작할때 체력으로 세팅한 경우(이전버전)
    [Header("Health & Damage")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float knockBackThrust = 15f;
    [SerializeField] private GameObject deathVFXPrefab;

    [Header("Experience")]
    [SerializeField] private int experienceGiven = 10; // 몬스터가 주는 경험치

    private int currentHealth;
    private Knockback knockback;
    private Flash flash;

    public bool isBoss = false; // Inspector에서 체크

    public string bossId = ""; // Inspector에서 "BossA", "BossB" 등으로 설정
    public bool isMiniBoss = false;  // Inspector에서 "BossA", "BossB" 등으로 설정

    public bool isDead = false;

    public FinalBossSpawner bossSpawner; // Inspector에서 "FinalBossA" 설정용


    private void Awake()
    {
        flash = GetComponent<Flash>();
        knockback = GetComponent<Knockback>();
    }

    private void Start()
    {
        // currentHealth = startingHealth; - 기존 코드 ( Health 체력 변경중 )
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        // 이미 죽었다면 아무것도 처리하지 않습니다.
        if (isDead) return;

        currentHealth -= damage;
        knockback.GetKnockedBack(FindObjectOfType<PlayerController>().transform, knockBackThrust);
        StartCoroutine(flash.FlashRoutine());

        if (currentHealth <= 0)
        {
            // isDead 플래그를 즉시 설정하여 중복 실행을 막고, 죽음 코루틴을 시작합니다.
            isDead = true;
            StartCoroutine(DieRoutine());
        }
    }

    private IEnumerator DieRoutine()
    {
        // 1. 설정된 경험치 추가 (단 한번만 실행 보장)
        PlayerLevel.Instance.AddExp(experienceGiven);

        // 2. 모든 콜라이더를 비활성화하여 추가 상호작용을 막습니다.
        foreach (var col in GetComponentsInChildren<Collider2D>())
        {
            col.enabled = false;
        }

        // 3. 보스 관련 로직 처리
        if (isBoss && bossSpawner != null && !string.IsNullOrEmpty(bossId))
        {
            bossSpawner.OnBossDeath(bossId, transform.position);
        }

        // 4. 죽음 파티클 생성 및 아이템 드랍
        GamePoolManager.Instance.SpawnFromPool("Death VFX", transform.position, Quaternion.identity);
        GetComponent<PickUpSpawner>().DropItems();

        // 5. 플래시 효과가 끝날 때까지 잠시 대기
        yield return new WaitForSeconds(flash.GetRestoreMatTime());
        
        // 6. 게임 오브젝트 파괴
        Destroy(gameObject);
    }
} 