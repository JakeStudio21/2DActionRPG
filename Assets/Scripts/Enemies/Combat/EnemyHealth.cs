using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 몬스터 체력 관리 클래스
/// ⭐ [Phase A] 완전한 데이터 기반 시스템으로 전환
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    [Header("❌ Fallback 제거됨 - 데이터 필수!")]
    [Tooltip("EnemyData + MonsterGrowthProfile 필수 할당!")]
    
    // ❌ 삭제: fallback 필드들 제거
    // [SerializeField] private int fallbackMaxHealth = 100;
    // [SerializeField] private float fallbackKnockBackThrust = 15f;
    // [SerializeField] private int fallbackExperienceGiven = 10;

    private int currentHealth;
    private Knockback knockback;
    private Flash flash;

    // ⭐ 데이터 기반 시스템
    private BaseEnemy baseEnemy;
    
    // ⭐ 사망 관련 상태 플래그들을 명확히 분리
    public bool isDead = false;                    // 데미지 받기 차단용 (즉시 설정)
    private bool isDeathAnimationPlaying = false;  // 사망 애니메이션 재생 중
    private bool deathEventTriggered = false;      // Animation Event 트리거 플래그
    
    public FinalBossSpawner bossSpawner; // Inspector에서 설정 (특수 케이스)

    private void Awake()
    {
        flash = GetComponent<Flash>();
        knockback = GetComponent<Knockback>();
        
        // ⭐ 새 시스템: BaseEnemy 참조 획득
        baseEnemy = GetComponent<BaseEnemy>();
    }

    private void Start()
    {
        // ⭐ 새 시스템: 데이터 기반 체력 계산
        currentHealth = CalculateMaxHealth();
        
        Debug.Log($"[EnemyHealth] {gameObject.name} 체력 초기화: {currentHealth}");
    }

    /// <summary>
    /// ⭐ 데이터 필수 - 최대 체력 계산
    /// </summary>
    private int CalculateMaxHealth()
    {
        if (baseEnemy != null)
        {
            float scaledHealth = baseEnemy.GetScaledMaxHealth();
            Debug.Log($"[EnemyHealth] BaseEnemy 스케일된 체력 사용: {scaledHealth:F1}");
            return Mathf.RoundToInt(scaledHealth);
        }
        
        // ❌ fallback 제거: 에러 처리
        Debug.LogError($"[EnemyHealth] {gameObject.name}: BaseEnemy가 없습니다! 데이터 설정을 확인해주세요.");
        return 1; // 크래시 방지용 최소값
    }

    /// <summary>
    /// ⭐ 데이터 필수 - 넉백 강도 계산
    /// </summary>
    private float CalculateKnockBackThrust()
    {
        if (baseEnemy?.EnemyData != null)
        {
            float knockBack = baseEnemy.EnemyData.KnockBackThrust;
            Debug.Log($"[EnemyHealth] EnemyData 넉백 강도 사용: {knockBack}");
            return knockBack;
        }
        
        // ❌ fallback 제거: 에러 처리
        Debug.LogError($"[EnemyHealth] {gameObject.name}: EnemyData가 없습니다! 넉백 설정을 확인해주세요.");
        return 1f; // 크래시 방지용 최소값
    }

    /// <summary>
    /// ⭐ 데이터 필수 - 경험치 계산
    /// </summary>
    private int CalculateExperienceReward()
    {
        if (baseEnemy?.EnemyData != null && baseEnemy?.GrowthProfile != null)
        {
            int scaledExp = baseEnemy.EnemyData.GetScaledExpReward(baseEnemy.CurrentLevel, baseEnemy.GrowthProfile);
            Debug.Log($"[EnemyHealth] BaseEnemy 스케일된 경험치 사용: {scaledExp}");
            return scaledExp;
        }
        
        // ❌ fallback 제거: 에러 처리
        Debug.LogError($"[EnemyHealth] {gameObject.name}: EnemyData 또는 GrowthProfile이 없습니다! 경험치 설정을 확인해주세요.");
        return 1; // 크래시 방지용 최소값
    }

    /// <summary>
    /// ⭐ 데이터 필수 - 골드 보상 계산
    /// </summary>
    private int CalculateGoldReward()
    {
        if (baseEnemy?.EnemyData != null && baseEnemy?.GrowthProfile != null)
        {
            int scaledGold = baseEnemy.EnemyData.GetScaledGoldReward(baseEnemy.CurrentLevel, baseEnemy.GrowthProfile);
            Debug.Log($"[EnemyHealth] BaseEnemy 스케일된 골드 사용: {scaledGold}");
            return scaledGold;
        }
        
        // ❌ fallback 제거: 에러 처리
        Debug.LogError($"[EnemyHealth] {gameObject.name}: EnemyData 또는 GrowthProfile이 없습니다! 골드 설정을 확인해주세요.");
        return 1; // 크래시 방지용 최소값
    }

    /// <summary>
    /// ⭐ 데이터 필수 - 보스 여부 확인
    /// </summary>
    public bool IsBoss()
    {
        if (baseEnemy?.EnemyData != null)
        {
            return baseEnemy.EnemyData.IsBoss;
        }
        
        Debug.LogError($"[EnemyHealth] {gameObject.name}: EnemyData가 없어서 보스 여부 확인 불가!");
        return false;
    }

    /// <summary>
    /// ⭐ 데이터 필수 - 미니보스 여부 확인
    /// </summary>
    public bool IsMiniBoss()
    {
        if (baseEnemy?.EnemyData != null)
        {
            return baseEnemy.EnemyData.IsMiniBoss;
        }
        
        Debug.LogError($"[EnemyHealth] {gameObject.name}: EnemyData가 없어서 미니보스 여부 확인 불가!");
        return false;
    }

    /// <summary>
    /// ⭐ 데이터 필수 - 보스 ID 확인
    /// </summary>
    public string GetBossId()
    {
        if (baseEnemy?.EnemyData != null)
        {
            return baseEnemy.EnemyData.BossId;
        }
        
        Debug.LogError($"[EnemyHealth] {gameObject.name}: EnemyData가 없어서 보스 ID 확인 불가!");
        return "";
    }

    /// <summary>
    /// ⭐ 데이터 필수 - 사망 이펙트 로드
    /// </summary>
    private GameObject LoadDeathVFX()
    {
        if (baseEnemy?.EnemyData != null)
        {
            string vfxPath = baseEnemy.EnemyData.DeathVFXPrefabPath;
            if (!string.IsNullOrEmpty(vfxPath))
            {
                GameObject vfxPrefab = Resources.Load<GameObject>(vfxPath);
                if (vfxPrefab != null)
                {
                    Debug.Log($"[EnemyHealth] 사망 이펙트 로드 성공: {vfxPath}");
                    return vfxPrefab;
                }
                else
                {
                    Debug.LogError($"[EnemyHealth] {gameObject.name}: 사망 이펙트를 찾을 수 없습니다 - {vfxPath}");
                }
            }
        }
        else
        {
            Debug.LogError($"[EnemyHealth] {gameObject.name}: EnemyData가 없어서 사망 이펙트 로드 불가!");
        }
        
        return null; // 이펙트 없음
    }

    public void TakeDamage(int damage)
    {
        // ⭐ 이미 죽었거나 사망 애니메이션 중이면 데미지 무시
        if (isDead || isDeathAnimationPlaying) 
        {
            Debug.Log($"[EnemyHealth] {gameObject.name}: 이미 죽었거나 사망 중이므로 데미지 무시");
            return;
        }

        currentHealth -= damage;
        
        // ⭐ 데이터 기반 넉백 강도 사용
        float knockBackThrust = CalculateKnockBackThrust();
        knockback.GetKnockedBack(FindObjectOfType<PlayerController>().transform, knockBackThrust);
        StartCoroutine(flash.FlashRoutine());

        // FSM 기반 Hit 상태 전환 (IEnemy 구현 몬스터만)
        IEnemy enemyFSM = GetComponent<IEnemy>();
        if (enemyFSM != null && enemyFSM.FSMController != null)
        {
            // 현재 상태를 저장하고 Hit 상태로 전환
            enemyFSM.FSMController.ChangeState(new EnemyHitState(enemyFSM, null));
        }

        if (currentHealth <= 0)
        {
            // ⭐ 즉시 데미지 차단용 플래그 설정
            isDead = true;
            isDeathAnimationPlaying = true;
            
            Debug.Log($"[EnemyHealth] {gameObject.name}: 사망 상태 진입, 추가 데미지 차단");
            
            // FSM 기반 Die 상태 전환 (IEnemy 구현 몬스터만)
            if (enemyFSM != null && enemyFSM.FSMController != null)
            {
                enemyFSM.FSMController.ChangeState(new EnemyDieState(enemyFSM));
            }
            
            StartCoroutine(DieRoutine());
        }
    }

    private IEnumerator DieRoutine()
    {
        Debug.Log($"[EnemyHealth] {gameObject.name} DieRoutine 실행 시작!");
        
        // ⭐ 플래그 초기화 (이미 TakeDamage에서 설정됨)
        deathEventTriggered = false;

        // ⭐ 사망 애니메이션 재생
        Animator animator = GetComponent<Animator>();
        
        if (animator != null)
        {
            // 사망 애니메이션 트리거
            animator.SetTrigger("Die");
            animator.SetBool("isDead", true);
            
            Debug.Log($"[EnemyHealth] {gameObject.name} 사망 애니메이션 시작");
        }
        else
        {
            Debug.LogWarning($"[EnemyHealth] {gameObject.name}: Animator가 없어서 즉시 처리");
            // Animator가 없으면 즉시 사망 처리
            OnDeathAnimationComplete();
            yield break;
        }

        // ⭐ 즉시 경험치/골드 지급 (타격감 향상)
        int experience = CalculateExperienceReward();
        int goldReward = CalculateGoldReward();
        
        Debug.Log($"[EnemyHealth] {gameObject.name} 처치! 경험치: {experience}, 골드: {goldReward}");
        
        PlayerDataManager.Instance.AddGold(goldReward);
        PlayerDataManager.Instance.AddExp(experience);

        // ⭐ Animation Event를 기다림 (최대 3초 타임아웃)
        float timeout = 3f;
        float elapsedTime = 0f;
        
        while (!deathEventTriggered && elapsedTime < timeout)
        {
            yield return null; // 매 프레임 체크
            elapsedTime += Time.deltaTime;
        }

        if (!deathEventTriggered)
        {
            Debug.LogWarning($"[EnemyHealth] {gameObject.name}: Animation Event 타임아웃, 강제 완료 처리");
            OnDeathAnimationComplete();
        }
    }

    /// <summary>
    /// ⭐ Animation Event에서 호출될 메서드 - 사망 처리 완료
    /// </summary>
    public void OnDeathAnimationComplete()
    {
        if (deathEventTriggered) return; // 중복 실행 방지
        
        deathEventTriggered = true;
        Debug.Log($"[EnemyHealth] {gameObject.name} Animation Event: 사망 처리 완료!");

        // ⭐ 보스 처치 시 StageProgressManager에 등록
        if (IsBoss())
        {
            string bossId = GetBossId();
            Debug.Log($"[EnemyHealth] 보스 처치: {bossId}");
            
            if (StageProgressManager.Instance != null)
            {
                StageProgressManager.Instance.RegisterBossDefeat(bossId);
            }
        }

        // 사망 이펙트 생성
        GameObject deathVFX = LoadDeathVFX();
        if (deathVFX != null)
        {
            Instantiate(deathVFX, transform.position, Quaternion.identity);
            Debug.Log($"[EnemyHealth] {gameObject.name} 사망 이펙트 생성");
        }

        // ⭐ 새 드롭 시스템: DropResolver 사용
        ExecuteNewDropSystem();
        
        // ⭐ 최종 상태 정리
        isDeathAnimationPlaying = false; // 애니메이션 완료
        
        Debug.Log($"[EnemyHealth] {gameObject.name} Animation Event 완료 후 파괴!");
        Destroy(gameObject);
    }

    /// <summary>
    /// ⭐ Animation Event에서 호출될 메서드 - 아이템 드롭 타이밍 (선택적)
    /// </summary>
    public void OnDeathDropItems()
    {
        Debug.Log($"[EnemyHealth] {gameObject.name} Animation Event: 아이템 드롭!");
        ExecuteNewDropSystem();
    }

    /// <summary>
    /// ⭐ Animation Event에서 호출될 메서드 - 사망 이펙트 타이밍 (선택적)
    /// </summary>
    public void OnDeathEffect()
    {
        Debug.Log($"[EnemyHealth] {gameObject.name} Animation Event: 사망 이펙트!");
        
        GameObject deathVFX = LoadDeathVFX();
        if (deathVFX != null)
        {
            Instantiate(deathVFX, transform.position, Quaternion.identity);
        }
    }

    /// <summary>
    /// ⭐ 사망 애니메이션 길이 자동 감지
    /// </summary>
    private float GetDeathAnimationDuration(Animator animator)
    {
        if (animator == null) return 0.5f;

        // 현재 애니메이터 상태 정보 가져오기
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        
        // 사망 애니메이션 클립 이름들 (몬스터별로 다를 수 있음)
        string[] deathAnimationNames = { "Die", "Death", "Dead", "Dying" };
        
        foreach (string animName in deathAnimationNames)
        {
            if (stateInfo.IsName(animName))
            {
                float duration = stateInfo.length;
                Debug.Log($"[EnemyHealth] 사망 애니메이션 '{animName}' 감지, 길이: {duration:F2}초");
                return Mathf.Max(duration, 0.3f); // 최소 0.3초 보장
            }
        }

        // 애니메이션 클립에서 직접 찾기
        RuntimeAnimatorController controller = animator.runtimeAnimatorController;
        if (controller != null)
        {
            foreach (AnimationClip clip in controller.animationClips)
            {
                foreach (string deathName in deathAnimationNames)
                {
                    if (clip.name.ToLower().Contains(deathName.ToLower()))
                    {
                        float duration = clip.length;
                        Debug.Log($"[EnemyHealth] 사망 애니메이션 클립 '{clip.name}' 발견, 길이: {duration:F2}초");
                        return Mathf.Max(duration, 0.3f);
                    }
                }
            }
        }

        // 기본값 반환
        Debug.LogWarning($"[EnemyHealth] {gameObject.name}: 사망 애니메이션을 찾을 수 없어서 기본값 사용 (0.5초)");
        return 0.5f;
    }

    /// <summary>
    /// ⭐ 새 드롭 시스템 실행
    /// </summary>
    private void ExecuteNewDropSystem()
    {
        if (baseEnemy?.EnemyData == null)
        {
            Debug.LogError($"[EnemyHealth] {gameObject.name}: 드롭 시스템 실행 불가 - EnemyData가 없습니다!");
            return;
        }

        string dropGroupId = baseEnemy.EnemyData.DropGroupId;
        int dropRolls = baseEnemy.EnemyData.DropRolls;
        int currentStageLevel = GetCurrentStageLevel();

        Debug.Log($"[EnemyHealth] {gameObject.name} 드롭 시스템 실행:");
        Debug.Log($"  - DropGroupId: {dropGroupId}");
        Debug.Log($"  - DropRolls: {dropRolls}");
        Debug.Log($"  - StageLevel: {currentStageLevel}");

        if (string.IsNullOrEmpty(dropGroupId))
        {
            Debug.LogWarning($"[EnemyHealth] {gameObject.name}: DropGroupId가 설정되지 않았습니다. 아이템 드롭을 건너뜁니다.");
            return;
        }

        // 여러 번 드롭 시도
        List<DropResult> allDropResults = new List<DropResult>();
        
        for (int i = 0; i < dropRolls; i++)
        {
            List<DropResult> dropResults = DropResolver.ResolveDrop(dropGroupId, currentStageLevel);
            allDropResults.AddRange(dropResults);
            
            Debug.Log($"[EnemyHealth] 드롭 시도 #{i + 1}: {dropResults.Count}개 드롭");
        }

        // 드롭 결과 로그
        Debug.Log(DropResolver.GetDropResultsDebugInfo(allDropResults));

        // 실제 아이템 스폰
        SpawnDroppedItems(allDropResults);
    }

    /// <summary>
    /// 드롭된 아이템들을 실제로 스폰
    /// </summary>
    private void SpawnDroppedItems(List<DropResult> dropResults)
    {
        foreach (var result in dropResults)
        {
            for (int i = 0; i < result.quantity; i++)
            {
                SpawnSingleItem(result.itemId, result.rarity);
            }
        }
    }

    /// <summary>
    /// 개별 아이템 스폰
    /// </summary>
    private void SpawnSingleItem(string itemId, ItemRarity rarity)
    {
        // GamePoolManager를 통한 스폰 시도
        GameObject spawnedItem = null;
        
        try
        {
            if (GamePoolManager.Instance != null)
            {
                spawnedItem = GamePoolManager.Instance.SpawnFromPool(itemId, transform.position, Quaternion.identity);
                Debug.Log($"[EnemyHealth] 풀에서 아이템 스폰: {itemId} ({rarity})");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[EnemyHealth] 풀 스폰 실패: {itemId}, 에러: {e.Message}");
        }

        // 풀링 실패 시 리소스에서 직접 로드
        if (spawnedItem == null)
        {
            GameObject prefab = LoadItemPrefab(itemId);
            if (prefab != null)
            {
                spawnedItem = Instantiate(prefab, transform.position, Quaternion.identity);
                Debug.Log($"[EnemyHealth] 리소스에서 아이템 직접 생성: {itemId} ({rarity})");
            }
            else
            {
                Debug.LogError($"[EnemyHealth] 아이템 프리팹을 찾을 수 없습니다: {itemId}");
            }
        }

        // 희귀도별 특수 효과 (선택적)
        if (spawnedItem != null)
        {
            ApplyRarityEffects(spawnedItem, rarity);
        }
    }

    /// <summary>
    /// 아이템 프리팹 로드
    /// </summary>
    private GameObject LoadItemPrefab(string itemId)
    {
        // Resources/Prefabs/Pickup/ 폴더에서 찾기
        return Resources.Load<GameObject>($"Prefabs/Pickup/{itemId}");
    }

    /// <summary>
    /// 희귀도별 특수 효과 적용
    /// </summary>
    private void ApplyRarityEffects(GameObject item, ItemRarity rarity)
    {
        // 희귀도별 파티클 효과, 사운드 등 적용 가능
        switch (rarity)
        {
            case ItemRarity.Rare:
                Debug.Log($"[EnemyHealth] 희귀 아이템 드롭: {item.name}");
                break;
            case ItemRarity.Epic:
                Debug.Log($"[EnemyHealth] 영웅 아이템 드롭: {item.name}");
                break;
            case ItemRarity.Legendary:
                Debug.Log($"[EnemyHealth] 전설 아이템 드롭: {item.name}");
                break;
        }
    }

    /// <summary>
    /// 현재 스테이지 레벨 획득
    /// </summary>
    private int GetCurrentStageLevel()
    {
        // FSMStageController나 GameManager에서 현재 스테이지 레벨 획득
        if (FSMStageController.Instance != null)
        {
            // FSMStageController에서 스테이지별 레벨 매핑
            var currentStage = FSMStageController.Instance.GetCurrentStage();
            return MapStageToLevel(currentStage);
        }
        
        // 몬스터 레벨을 스테이지 레벨로 사용 (fallback)
        return baseEnemy?.CurrentLevel ?? 1;
    }

    /// <summary>
    /// 스테이지를 레벨로 매핑
    /// </summary>
    private int MapStageToLevel(FSMStageController.StageState stage)
    {
        return stage switch
        {
            FSMStageController.StageState.Lobby => 1,
            FSMStageController.StageState.Scene1 => 1,
            FSMStageController.StageState.Scene2 => 2,
            FSMStageController.StageState.Scene3 => 3,
            FSMStageController.StageState.Boss => 5,
            _ => 1
        };
    }
} 