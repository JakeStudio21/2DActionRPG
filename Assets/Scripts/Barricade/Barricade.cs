using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.AI; // NavMeshObstacle용
using StageSystem;    // StageManager 접근용

/// <summary>
/// 프리셋 기반 바리케이드 시스템
/// BarricadePreset을 선택하면 모든 설정이 자동 적용됨
/// </summary>
public class Barricade : MonoBehaviour, ITargetable
{
    // ========================================
    // 프리셋 선택 (핵심!)
    // ========================================
    [Header("==== 프리셋 선택 ====")]
    [SerializeField] private BarricadePreset preset;
    
    // ========================================
    // 승리 조건 연동
    // ========================================
    [Header("==== 승리 조건 연동 ====")]
    [Tooltip("true: 이 바리케이드가 파괴되면 StageManager에 알림 (ObjectiveComplete 판정 대상)\n" +
             "false: 일반 장애물, 파괴해도 승리 조건에 영향 없음")]
    [SerializeField] private bool isVictoryTarget = false;
    
    [Tooltip("isVictoryTarget=true일 때 표시할 미션 마커 비주얼 설정\n" +
             "null이면 마커 UI를 표시하지 않음")]
    [SerializeField] private ObjectiveMarkerConfig objectiveMarkerConfig;
    
    // ========================================
    // 런타임 상태
    // ========================================
    [Header("==== 런타임 상태 (읽기 전용) ====")]
    [SerializeField] private int currentHits = 0;
    [SerializeField] private float currentHP = 0;
    [SerializeField] private bool isBroken = false;
    private int currentStageIndex = 0;
    
    // 중복 호출 방지
    private float lastHitTime = -999f;
    private const float HIT_COOLDOWN = 0.1f; // 0.1초 쿨다운
    
    // ========================================
    // 컴포넌트 참조
    // ========================================
    private SpriteRenderer spriteRenderer;
    private BarricadeHPDisplay hpDisplay;
    private PickUpSpawner pickupSpawner;
    private BoxCollider2D boxCollider;
    private NavMeshObstacle navMeshObstacle; // ⭐ Phase 4: 동적 장애물
    private MinimapMarker minimapMarker;
    private ObjectiveMarkerUI objectiveMarkerUI;
    
    // ========================================
    // 초기화
    // ========================================
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        pickupSpawner = GetComponent<PickUpSpawner>();
        hpDisplay = GetComponentInChildren<BarricadeHPDisplay>(); // ⭐ HP 바 참조 초기화
        navMeshObstacle = GetComponent<NavMeshObstacle>(); // ⭐ Phase 4: NavMeshObstacle 참조
        minimapMarker      = GetComponent<MinimapMarker>();
        objectiveMarkerUI  = GetComponentInChildren<ObjectiveMarkerUI>(includeInactive: true);
        
        if (preset == null)
        {
            Debug.LogError($"[Barricade] {gameObject.name}에 프리셋이 할당되지 않았습니다!");
            enabled = false;
            return;
        }
        
        InitializeFromPreset();
        InitializeNavMeshObstacle(); // ⭐ Phase 4: NavMesh 장애물 초기화
    }
    
    private void Start()
    {
        // isVictoryTarget 여부에 따라 MinimapMarker 활성 제어
        // true  → 미니맵/레이더에 미션 목표로 표시 (OnEnable에서 자동 등록)
        // false → 등록하지 않음 (일반 장애물은 맵에 표시하지 않음)
        if (minimapMarker != null)
            minimapMarker.enabled = isVictoryTarget;

        // 미션 목표 오브젝트이고 Config가 연결되어 있으면 마커 UI 초기화
        if (isVictoryTarget && objectiveMarkerUI != null && objectiveMarkerConfig != null)
        {
            objectiveMarkerUI.Initialize(objectiveMarkerConfig);
            UpdateObjectiveMarkerProgress();
        }
    }
    
    private void InitializeFromPreset()
    {
        // HP 바 참조 확인
        if (hpDisplay == null)
        {
            Dbg.LogWarning($"[Barricade] {gameObject.name}에 BarricadeHPDisplay를 찾을 수 없습니다!");
        }
        
        // Break 모드에 따른 초기화
        switch (preset.breakMode)
        {
            case BreakMode.Hits:
                currentHits = 0;
                break;
            case BreakMode.HP:
                currentHP = preset.maxHP;
                break;
            case BreakMode.Condition:
                // Condition 모드는 별도 처리
                break;
        }
        
        // 시각 초기화
        UpdateVisualStage();
        
        // HP 바 초기화
        InitializeHPDisplay();
    }
    
    private void InitializeHPDisplay()
    {
        if (hpDisplay != null)
        {
            hpDisplay.Initialize(preset);
            UpdateHPDisplay();
        }
    }
    
    /// <summary>
    /// ⭐ Phase 4: NavMeshObstacle 초기화
    /// Carve를 활성화하여 NavMesh를 동적으로 차단
    /// </summary>
    private void InitializeNavMeshObstacle()
    {
        if (navMeshObstacle == null) return;
        
        // Carve 활성화 (NavMesh에 구멍 생성)
        navMeshObstacle.carving = true;
        
        // 2D 환경에 맞게 설정
        navMeshObstacle.carveOnlyStationary = true; // 정적 장애물로 설정
        
        // ⭐ Shape은 Inspector에서 설정한 대로 유지 (Box, Capsule 등)
        // navMeshObstacle.shape는 건드리지 않음!
        
        // Collider 크기에 맞춰 자동 설정
        AutoSizeNavMeshObstacle();
    }
    
    /// <summary>
    /// ⭐ Collider 크기에 맞춰 NavMeshObstacle 크기 자동 설정
    /// </summary>
    private void AutoSizeNavMeshObstacle()
    {
        if (navMeshObstacle == null) return;
        
        // BoxCollider2D 우선 시도
        BoxCollider2D boxCol = GetComponent<BoxCollider2D>();
        if (boxCol != null)
        {
            // 2D Collider 크기를 NavMeshObstacle 크기로 변환
            navMeshObstacle.size = new Vector3(boxCol.size.x, 1f, boxCol.size.y);
            navMeshObstacle.center = new Vector3(boxCol.offset.x, 0.5f, boxCol.offset.y);
            return;
        }
        
        // PolygonCollider2D 지원 (Bounds 기반)
        PolygonCollider2D polyCol = GetComponent<PolygonCollider2D>();
        if (polyCol != null)
        {
            // Polygon의 Bounds를 Box/Capsule로 근사
            Bounds bounds = polyCol.bounds;
            Vector3 localCenter = transform.InverseTransformPoint(bounds.center);
            
            navMeshObstacle.size = new Vector3(bounds.size.x, 1f, bounds.size.y);
            navMeshObstacle.center = new Vector3(localCenter.x, 0.5f, localCenter.y);
            return;
        }
        
        // CircleCollider2D 지원
        CircleCollider2D circleCol = GetComponent<CircleCollider2D>();
        if (circleCol != null)
        {
            float diameter = circleCol.radius * 2f;
            navMeshObstacle.size = new Vector3(diameter, 1f, diameter);
            navMeshObstacle.center = new Vector3(circleCol.offset.x, 0.5f, circleCol.offset.y);
            return;
        }
        
        // Collider 없으면 기본 크기
        navMeshObstacle.size = new Vector3(1f, 1f, 1f);
        navMeshObstacle.center = new Vector3(0f, 0.5f, 0f);
    }
    
    // ========================================
    // 피격 처리
    // ========================================
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isBroken) return;
        
        // 중복 호출 방지 (0.1초 쿨다운)
        if (Time.time < lastHitTime + HIT_COOLDOWN)
        {
            return;
        }
        
        // DamageSource 또는 Projectile 체크
        bool isDamageSource = other.GetComponent<DamageSource>() != null;
        bool isProjectile = other.GetComponent<Projectile>() != null;
        
        if (!isDamageSource && !isProjectile)
        {
            return;
        }
        
        lastHitTime = Time.time;
        TakeDamage(other);
    }
    
    // ========================================
    // 피격 처리
    // ========================================
    private void TakeDamage(Collider2D attacker)
    {
        if (isBroken) return;
        
        switch (preset.breakMode)
        {
            case BreakMode.Hits:
                ProcessHitMode();
                break;
            case BreakMode.HP:
                ProcessHPMode(attacker);
                break;
            case BreakMode.Condition:
                ProcessConditionMode();
                break;
        }
    }
    
    // ========================================
    // Hits 모드
    // ========================================
    private void ProcessHitMode()
    {
        currentHits++;
        
        // 피격 피드백
        PlayHitFeedback();
        
        // 시각 업데이트
        UpdateVisualStage();
        
        // HP 바 업데이트
        UpdateHPDisplay();
        
        // 미션 마커 진행도 업데이트
        UpdateObjectiveMarkerProgress();
        
        // 파괴 체크
        if (currentHits >= preset.hitsToBreak)
        {
            Break();
        }
    }
    
    // ========================================
    // HP 모드
    // ========================================
    private void ProcessHPMode(Collider2D attacker)
    {
        // 데미지 계산
        float damage = CalculateDamage(attacker);
        currentHP -= damage;
        
        if (currentHP < 0) currentHP = 0;
        
        // 데미지 숫자 표시 (preset 설정에 따라)
        if (preset.showDamageNumbers)
        {
            ShowDamageNumber(damage);
        }
        
        PlayHitFeedback();
        UpdateVisualStage();
        UpdateHPDisplay();
        
        // 미션 마커 진행도 업데이트
        UpdateObjectiveMarkerProgress();
        
        if (currentHP <= 0)
        {
            Break();
        }
    }
    
    /// <summary>
    /// 데미지 계산
    /// 바리케이드는 플레이어 공격력과 무관하게 고정 데미지 적용
    /// </summary>
    private float CalculateDamage(Collider2D attacker)
    {
        // 모든 플레이어 공격(근접/원거리)에 대해 고정 데미지
        // DamageSource나 Projectile이 확인되면 데미지 적용
        
        bool isPlayerAttack = attacker.GetComponent<DamageSource>() != null || 
                              attacker.GetComponent<Projectile>() != null;
        
        if (isPlayerAttack)
        {
            // 바리케이드 고정 데미지 (프리셋에서 조정 가능하도록 개선 가능)
            return 10f;
        }
        
        // 플레이어 공격이 아니면 0 (데미지 없음)
        return 0f;
    }
    
    // ========================================
    // Condition 모드
    // ========================================
    private void ProcessConditionMode()
    {
        bool conditionMet = CheckCondition();
        
        if (conditionMet)
        {
            Break();
        }
        else
        {
            // 조건 미충족 메시지
            ShowConditionMessage();
            PlayFailFeedback();
        }
    }
    
    private bool CheckCondition()
    {
        switch (preset.conditionType)
        {
            case ConditionType.SpecialKey:
                // 특수 열쇠 보유 체크
                return CheckHasSpecialKey(preset.requiredItemID);
                
            case ConditionType.QuestComplete:
                // 퀘스트 시스템 체크 (미구현)
                return false;
                
            case ConditionType.AllEnemiesDead:
                // 모든 적 처치 체크
                return FindObjectsOfType<MonoBehaviour>().Length == 0;
                
            case ConditionType.TimeElapsed:
                // 타이머 체크 (미구현)
                return false;
                
            default:
                return false;
        }
    }
    
    /// <summary>
    /// 특수 열쇠 보유 체크 (인벤토리 검색)
    /// </summary>
    private bool CheckHasSpecialKey(string itemID)
    {
        if (PlayerDataManager.Instance == null)
        {
            return false;
        }
        
        // 인벤토리에서 itemID와 일치하는 아이템 찾기
        var inventoryItems = PlayerDataManager.Instance.InventoryItems;
        if (inventoryItems != null)
        {
            foreach (var item in inventoryItems)
            {
                if (item != null && item.itemID == itemID)
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    private void ShowConditionMessage()
    {
        // TODO: UI 메시지 시스템 연동
    }
    
    private void PlayFailFeedback()
    {
        // 조건 미충족 시 간단한 피드백
        if (preset.presentationLevel >= 1)
        {
            // 실패 사운드 재생 (있으면)
        }
    }
    
    // ========================================
    // 피드백 시스템 (PresentationLevel 기반)
    // ========================================
    private void PlayHitFeedback()
    {
        int level = preset.presentationLevel;
        
        if (level >= 1)
        {
            // Level 1: 최소 사운드 + VFX
            if (preset.hitSound != null)
            {
                PlaySound(preset.hitSound);
            }
            
            if (preset.hitVFX != null)
            {
                SpawnVFX(preset.hitVFX, transform.position);
            }
        }
        
        if (level >= 2)
        {
            // Level 2: 오브젝트 흔들림
            if (preset.hitShakeIntensity > 0)
            {
                StartCoroutine(ShakeObject(preset.hitShakeIntensity, 0.1f));
            }
        }
        
        if (level >= 3)
        {
            // Level 3: 카메라 흔들림 (쿨다운 체크)
            if (preset.hitShakeIntensity > 0)
            {
                ShakeCameraWithCooldown(preset.hitShakeIntensity, 0.1f);
            }
        }
    }
    
    private void PlayDestroyFeedback()
    {
        int level = preset.presentationLevel;
        
        if (level >= 1)
        {
            // Level 1: 최소 사운드 + VFX
            if (preset.destroySound != null)
            {
                PlaySound(preset.destroySound);
            }
            
            if (preset.destroyVFX != null)
            {
                SpawnVFX(preset.destroyVFX, transform.position);
            }
        }
        
        if (level >= 2)
        {
            // Level 2: 파편 효과
            if (preset.spawnDebris)
            {
                SpawnDebris(preset.debrisCount);
            }
        }
        
        if (level >= 3)
        {
            // Level 3: 큰 이펙트 (쿨다운 체크)
            if (preset.useBigDestroyFx && preset.destroyVFX != null)
            {
                SpawnBigFxWithCooldown();
            }
            
            if (preset.destroyShakeIntensity > 0)
            {
                ShakeCameraWithCooldown(preset.destroyShakeIntensity, 0.3f);
            }
        }
    }
    
    // ========================================
    // 헬퍼 메서드
    // ========================================
    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        
        if (BarricadeFeedbackManager.Instance != null)
        {
            BarricadeFeedbackManager.Instance.PlaySound(clip, 0.1f);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clip, transform.position);
        }
    }
    
    private void SpawnVFX(GameObject vfxPrefab, Vector3 position)
    {
        if (vfxPrefab == null) return;
        
        GameObject vfx = Instantiate(vfxPrefab, position, Quaternion.identity);
        Destroy(vfx, 3f); // 3초 후 자동 파괴
    }
    
    private IEnumerator ShakeObject(float intensity, float duration)
    {
        Vector3 originalPos = transform.localPosition;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;
            
            transform.localPosition = originalPos + new Vector3(x, y, 0);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.localPosition = originalPos;
    }
    
    private void ShakeCameraWithCooldown(float intensity, float duration)
    {
        if (BarricadeFeedbackManager.Instance != null)
        {
            BarricadeFeedbackManager.Instance.ShakeCamera(
                intensity, 
                duration, 
                preset.cameraShakeCooldown
            );
        }
    }
    
    private void SpawnBigFxWithCooldown()
    {
        if (BarricadeFeedbackManager.Instance != null && preset.destroyVFX != null)
        {
            BarricadeFeedbackManager.Instance.PlayBigFx(
                preset.destroyVFX,
                transform.position,
                preset.bigFxCooldown,
                preset.limitConsecutivePlayback
            );
        }
    }
    
    private void SpawnDebris(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 randomOffset = Random.insideUnitCircle * 0.5f;
            Vector3 spawnPos = transform.position + randomOffset;
            
            GameObject debris;
            if (preset.debrisPrefab != null)
            {
                debris = Instantiate(preset.debrisPrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                // 기본 파편 (간단한 사각형)
                debris = GameObject.CreatePrimitive(PrimitiveType.Quad);
                debris.transform.position = spawnPos;
                debris.transform.localScale = Vector3.one * 0.2f;
                
                // 색상 설정
                var renderer = debris.GetComponent<Renderer>();
                if (renderer != null && spriteRenderer != null)
                {
                    renderer.material.color = spriteRenderer.color;
                }
            }
            
            // Rigidbody2D 추가 (날아가는 효과)
            var rb = debris.AddComponent<Rigidbody2D>();
            Vector2 randomForce = Random.insideUnitCircle * Random.Range(2f, 5f);
            rb.AddForce(randomForce, ForceMode2D.Impulse);
            rb.AddTorque(Random.Range(-5f, 5f), ForceMode2D.Impulse);
            
            // 자동 파괴
            Destroy(debris, 2f);
        }
    }
    
    private void ShowDamageNumber(float damage)
    {
        // TODO: 데미지 숫자 UI 시스템 연동
    }
    
    private void ShowMessage(string message)
    {
        // TODO: 메시지 UI 시스템 연동
    }
    
    private void UnlockArea()
    {
        // TODO: AreaUnlockManager 연동
    }
    
    // ========================================
    // 시각 업데이트
    // ========================================
    private void UpdateVisualStage()
    {
        if (preset.stageMode == StageMode.None) return;
        if (preset.visualStages == null || preset.visualStages.Length == 0) return;
        
        float progress = GetBreakProgress(); // 0.0 ~ 1.0
        
        // 현재 진행도에 맞는 단계 찾기
        for (int i = preset.visualStages.Length - 1; i >= 0; i--)
        {
            if (progress <= preset.visualStages[i].hpThreshold)
            {
                if (currentStageIndex != i)
                {
                    ChangeStage(i);
                }
                break;
            }
        }
    }
    
    private float GetBreakProgress()
    {
        switch (preset.breakMode)
        {
            case BreakMode.Hits:
                return 1f - ((float)currentHits / preset.hitsToBreak);
                
            case BreakMode.HP:
                return currentHP / preset.maxHP;
                
            default:
                return 1f;
        }
    }
    
    private void ChangeStage(int newStageIndex)
    {
        currentStageIndex = newStageIndex;
        var stage = preset.visualStages[newStageIndex];
        
        // 스프라이트 변경
        if (stage.sprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = stage.sprite;
        }
        
        // 단계 VFX (Advanced 모드만)
        if (preset.stageMode == StageMode.Advanced && stage.stageVFX != null)
        {
            SpawnVFX(stage.stageVFX, transform.position);
        }
    }
    
    // ========================================
    // HP 바 업데이트
    // ========================================
    private void UpdateHPDisplay()
    {
        if (hpDisplay == null) return;
        
        switch (preset.breakMode)
        {
            case BreakMode.Hits:
                hpDisplay.UpdateDisplay(currentHits, preset.hitsToBreak);
                break;
                
            case BreakMode.HP:
                hpDisplay.UpdateDisplay((int)currentHP, preset.maxHP);
                break;
        }
    }
    
    /// <summary>
    /// 미션 마커 UI 진행도 업데이트 (isVictoryTarget=true + objectiveMarkerUI 존재 시)
    /// BreakMode에 따라 남은 비율(0~1)을 계산해 SetProgress()에 전달
    /// </summary>
    private void UpdateObjectiveMarkerProgress()
    {
        if (objectiveMarkerUI == null || !isVictoryTarget) return;
        
        float progress = preset.breakMode switch
        {
            BreakMode.Hits => 1f - ((float)currentHits / Mathf.Max(preset.hitsToBreak, 1)),
            BreakMode.HP   => currentHP / Mathf.Max(preset.maxHP, 1f),
            _              => 1f
        };
        
        objectiveMarkerUI.SetProgress(progress);
    }
    
    // ========================================
    // 보상 드롭
    // ========================================
    private void DropRewards()
    {
        switch (preset.rewardMode)
        {
            case RewardMode.None:
                // 보상 없음
                break;
                
            case RewardMode.Normal:
                // 일반 PickUpSpawner 사용
                if (pickupSpawner != null)
                {
                    pickupSpawner.DropItems();
                }
                break;
                
            case RewardMode.Special:
                // 특수 보상 테이블 사용
                DropSpecialRewards();
                break;
        }
    }
    
    private void DropSpecialRewards()
    {
        if (preset.rewardTable == null)
        {
            return;
        }
        
        var table = preset.rewardTable;
        
        // 확정 골드
        if (table.guaranteedGold > 0)
        {
            // TODO: 골드 드롭 로직
        }
        
        // 확정 장비
        if (table.guaranteedEquipment != null)
        {
            // TODO: 장비 드롭 로직
        }
        
        // 랜덤 보상
        if (table.randomRewards != null && table.randomRewards.Length > 0)
        {
            for (int i = 0; i < table.randomRewardCount; i++)
            {
                DropRandomReward(table.randomRewards);
            }
        }
    }
    
    private void DropRandomReward(BarricadeRewardTable.RewardEntry[] rewards)
    {
        foreach (var entry in rewards)
        {
            if (Random.Range(0f, 100f) <= entry.dropChance)
            {
                int amount = Random.Range(entry.minAmount, entry.maxAmount + 1);
                // TODO: 실제 드롭 로직
                return;
            }
        }
    }
    
    // ========================================
    // 파괴 처리
    // ========================================
    private void Break()
    {
        if (isBroken) return;
        isBroken = true;
        
        // 즉시 충돌 방지: Collider 모두 비활성화
        var colliders = GetComponents<Collider2D>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }
        
        // ⭐ Phase 4: NavMeshObstacle 제거 (NavMesh 자동 복구)
        if (navMeshObstacle != null)
        {
            navMeshObstacle.enabled = false; // 즉시 NavMesh에서 제거됨
        }
        
        // ⭐ Phase 4: 자식 NavMeshObstacle도 모두 비활성화 (수동 Multiple Obstacles 지원)
        NavMeshObstacle[] childObstacles = GetComponentsInChildren<NavMeshObstacle>();
        foreach (var obstacle in childObstacles)
        {
            if (obstacle != null && obstacle != navMeshObstacle) // 부모 Obstacle 제외
            {
                obstacle.enabled = false;
            }
        }
        
        // 즉시 시각적으로 숨기기: 모든 Renderer 비활성화
        var spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in spriteRenderers)
        {
            sr.enabled = false;
        }
        
        // Canvas Renderer도 비활성화 (UI 요소)
        var canvasRenderers = GetComponentsInChildren<CanvasRenderer>();
        foreach (var cr in canvasRenderers)
        {
            cr.gameObject.SetActive(false);
        }
        
        // HP 바 숨기기
        if (hpDisplay != null)
        {
            hpDisplay.gameObject.SetActive(false);
        }
        
        // 파괴 피드백 (VFX/사운드는 계속 재생됨)
        PlayDestroyFeedback();
        
        // 보상 드롭
        DropRewards();
        
        // 메시지 표시
        if (preset.showMessageOnBreak)
        {
            ShowMessage(preset.breakMessage);
        }
        
        // 구역 해금
        if (preset.unlockAreaOnDestroy)
        {
            UnlockArea();
        }
        
        // ⭐ 모든 피드백 완료 후 GameObject 비활성화 (2초 후)
        StartCoroutine(DestroyAfterDelay(2f));
        
        // 파괴 시 미니맵 마커 제거 (OnDisable에서 MinimapManager 자동 Unregister)
        if (minimapMarker != null)
            minimapMarker.enabled = false;
        
        // 미션 마커 완료 연출 (스케일 업 → 페이드 아웃)
        if (objectiveMarkerUI != null)
            objectiveMarkerUI.ShowComplete();
        
        // 승리 조건 대상이면 StageManager에 파괴 통지
        if (isVictoryTarget)
            StageManager.Instance?.NotifyBarricadeDestroyed(this);
    }
    
    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }
    
    // ========================================
    // 공개 속성 (StageManager 등 외부 참조용)
    // ========================================
    public bool IsVictoryTarget => isVictoryTarget;
    public bool IsBroken => isBroken;

    // ========================================
    // ITargetable 구현 (오토타게팅 시스템)
    // ========================================
    private TargetOutlineEffect _outlineEffect;

    bool ITargetable.IsAlive() => !isBroken;

    EnemyRank ITargetable.GetRank() =>
        isVictoryTarget ? EnemyRank.MissionObject : EnemyRank.Obstacle;

    Transform ITargetable.GetTransform() => transform;

    void ITargetable.ActivateLockOn()
    {
        if (_outlineEffect == null) _outlineEffect = GetComponentInChildren<TargetOutlineEffect>();
        _outlineEffect?.Activate();
    }

    void ITargetable.DeactivateLockOn()
    {
        _outlineEffect?.Deactivate();
    }
}

