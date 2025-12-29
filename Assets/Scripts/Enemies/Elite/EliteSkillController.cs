using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 엘리트 몬스터 스킬 컨트롤러
/// 스킬 실행, 쿨다운 관리, Cast → Action 단계 제어
/// StateMachineBehaviour와 연동하여 정확한 타이밍 제어
/// </summary>
public class EliteSkillController : MonoBehaviour
{
    [Header("⚙️ 참조")]
    [SerializeField] private BaseEnemy baseEnemy;
    [SerializeField] private EnemyAnimationController animController;
    
    [Header("🎯 현재 스킬 상태")]
    [SerializeField] private SkillData currentSkill;
    [SerializeField] private bool isCasting = false;
    [SerializeField] private bool isActionExecuting = false;
    
    [Header("⏱️ 쿨다운 관리")]
    private Dictionary<SkillData, float> skillCooldowns = new Dictionary<SkillData, float>();
    
    [Header("📍 텔레그래프")]
    private GameObject activeTelegraph;
    
    [Header("🎯 스킬 타겟 정보 (Phase 2)")]
    private Vector3 cachedOrigin;           // Cast 시작 시점의 Origin (엘리트 중심 위치)
    private Vector3 cachedTargetDirection;  // Cast 시작 시점의 플레이어 방향 (싱크 맞춤용)
    private Vector3 cachedTargetPosition;   // Cast 시작 시점의 플레이어 위치
    
    [Header("📐 위치 조정")]
    [Tooltip("AOE 중심점 오프셋 (Y값 음수로 발쪽 이동)")]
    [SerializeField] private Vector3 aoeOffset = new Vector3(0, -0.5f, 0);
    
    [Tooltip("⭐ 스킬 스폰 위치 (PlantsMonster 방식) - 프리팹 내부 Transform")]
    [SerializeField] private Transform skillSpawnPoint;
    
    [Header("🎮 디버그")]
    [SerializeField] private bool enableDebugLogs = true;

    // 프로퍼티
    public bool IsCasting => isCasting;
    public bool IsActionExecuting => isActionExecuting;
    public SkillData CurrentSkill => currentSkill;

    private void Awake()
    {
        // 컴포넌트 자동 참조
        if (baseEnemy == null)
            baseEnemy = GetComponent<BaseEnemy>();
        
        if (animController == null)
            animController = GetComponent<EnemyAnimationController>();
    }

    private void Start()
    {
        // 스킬 쿨다운 초기화
        if (baseEnemy?.EnemyData?.SkillDataList != null)
        {
            foreach (var skill in baseEnemy.EnemyData.SkillDataList)
            {
                if (skill != null)
                {
                    skillCooldowns[skill] = 0f; // 처음엔 사용 가능
                    
                    // ⭐ 스킬 정보 로그 출력
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[EliteSkillController] 스킬 등록: {skill.SkillName}");
                        Debug.Log($"[EliteSkillController] {skill.GetDebugInfo(10)}");
                    }
                }
            }
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[EliteSkillController] {gameObject.name} 초기화 완료 (스킬 개수: {skillCooldowns.Count})");
        }
    }

    private void Update()
    {
        // 쿨다운 타이머 업데이트
        UpdateCooldowns();
    }

    /// <summary>
    /// 쿨다운 타이머 업데이트
    /// </summary>
    private void UpdateCooldowns()
    {
        List<SkillData> skills = new List<SkillData>(skillCooldowns.Keys);
        
        foreach (var skill in skills)
        {
            if (skillCooldowns[skill] > 0f)
            {
                skillCooldowns[skill] -= Time.deltaTime;
                
                if (skillCooldowns[skill] <= 0f)
                {
                    skillCooldowns[skill] = 0f;
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[EliteSkillController] {gameObject.name}: {skill.SkillName} 쿨다운 완료!");
                    }
                }
            }
        }
    }

    /// <summary>
    /// 스킬 사용 가능 여부 확인
    /// </summary>
    public bool CanUseSkill(SkillData skill)
    {
        if (skill == null) 
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[EliteSkillController] CanUseSkill: skill이 null!");
            return false;
        }
        
        if (isCasting || isActionExecuting) 
        {
            if (enableDebugLogs)
                Debug.Log($"[EliteSkillController] CanUseSkill: 이미 스킬 실행 중 (isCasting={isCasting}, isAction={isActionExecuting})");
            return false;
        }
        
        // 쿨다운 체크
        if (skillCooldowns.ContainsKey(skill))
        {
            bool canUse = skillCooldowns[skill] <= 0f;
            if (enableDebugLogs)
            {
                if (!canUse)
                    Debug.Log($"[EliteSkillController] CanUseSkill: {skill.SkillName} 쿨다운 중 (남은 시간: {skillCooldowns[skill]:F1}초)");
                else
                    Debug.Log($"[EliteSkillController] CanUseSkill: {skill.SkillName} 사용 가능! ✅");
            }
            return canUse;
        }
        
        if (enableDebugLogs)
            Debug.LogWarning($"[EliteSkillController] CanUseSkill: {skill.SkillName}가 쿨다운 딕셔너리에 없음!");
        return true;
    }

    /// <summary>
    /// 스킬 범위 내인지 확인
    /// </summary>
    public bool IsInSkillRange(SkillData skill, float distanceToPlayer)
    {
        if (skill == null) return false;
        return skill.IsInRange(distanceToPlayer);
    }

    /// <summary>
    /// 스킬 캐스팅 시작
    /// </summary>
    public void StartSkillCast(SkillData skill)
    {
        if (!CanUseSkill(skill))
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning($"[EliteSkillController] {gameObject.name}: {skill.SkillName} 사용 불가!");
            }
            return;
        }

        currentSkill = skill;
        isCasting = true;
        
        if (enableDebugLogs)
        {
            Debug.Log($"🔮 [EliteSkillController] {gameObject.name}: {skill.SkillName} 캐스팅 시작!");
        }
        
        // 애니메이션 트리거
        if (animController != null)
        {
            animController.TriggerSkillCast();
        }
    }

    /// <summary>
    /// 스킬 캐스트 시작 (StateMachineBehaviour 콜백)
    /// </summary>
    public void OnSkillCastStart()
    {
        if (currentSkill == null) return;
        
        // ⭐ Phase 2: Cast 시작 시점의 Origin과 Forward 저장 (Telegraph와 DamageArea 싱크 맞춤)
        // ⭐ 수정: 보스와 동일하게 피봇 중심점 사용 (aoeOffset 제거)
        cachedOrigin = transform.position;  // 엘리트 피봇 중심점
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            cachedTargetPosition = player.transform.position;
            Vector3 elitePos = transform.position;
            Vector3 toPlayer = cachedTargetPosition - elitePos;
            cachedTargetDirection = toPlayer.normalized;
            
            if (enableDebugLogs)
            {
                Debug.Log($"🎯 [EliteSkillController] Forward 방향 계산:");
                Debug.Log($"   - Elite 위치: {elitePos}");
                Debug.Log($"   - Player 위치: {cachedTargetPosition}");
                Debug.Log($"   - 방향 벡터 (정규화 전): {toPlayer}");
                Debug.Log($"   - 방향 벡터 (정규화 후): {cachedTargetDirection}");
                
                // 2D 각도 계산 (X-Y 평면 기준)
                float angle2D = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
                Debug.Log($"   - 2D 각도: {angle2D}° (0°=우측, 90°=상단, 180°=좌측, -90°=하단)");
            }
        }
        else
        {
            cachedTargetDirection = Vector3.down; // fallback
            cachedTargetPosition = transform.position + Vector3.down * 5f;
            
            if (enableDebugLogs)
            {
                Debug.LogWarning($"⚠️ [EliteSkillController] Player를 찾을 수 없음! Fallback: Vector3.down");
            }
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"✨ [EliteSkillController] {gameObject.name}: Cast 이펙트 + Telegraph 생성");
            Debug.Log($"   - Cached Origin: {cachedOrigin}");
            Debug.Log($"   - Cached Forward: {cachedTargetDirection}");
        }
        
        // Cast 이펙트 생성
        SpawnCastEffect();
        
        // Telegraph 생성 (경고 표시)
        SpawnTelegraph();
    }

    /// <summary>
    /// 스킬 캐스트 완료 (StateMachineBehaviour 콜백)
    /// </summary>
    public void OnSkillCastComplete()
    {
        if (currentSkill == null) return;
        
        isCasting = false;
        isActionExecuting = true;
        
        if (enableDebugLogs)
        {
            Debug.Log($"🎯 [EliteSkillController] {gameObject.name}: Cast 완료 → Action 단계");
        }
        
        // ⚠️ Telegraph는 여기서 제거하지 않음! (Action 실행 직전에 제거)
        // RemoveTelegraph(); ← 주석 처리
        
        // Action 애니메이션 트리거
        if (animController != null)
        {
            animController.TriggerSkillAction();
        }
    }

    /// <summary>
    /// ⚠️ [DEPRECATED - Phase 4] VFX만 실행
    /// Phase 4부터 VFX는 SpawnDamageArea()에서 자동으로 생성됨
    /// </summary>
    [System.Obsolete("Phase 4: VFX는 SpawnDamageArea()에서 자동으로 생성됨. ExecuteSkillDamageOnly() 단독 사용 권장")]
    public void ExecuteSkillVFXOnly()
    {
        if (currentSkill == null) return;
        
        if (enableDebugLogs)
        {
            Debug.LogWarning($"⚠️ [EliteSkillController] ExecuteSkillVFXOnly()는 Deprecated! VFX는 자동으로 생성됩니다.");
        }
        
        // Phase 4: VFX는 SpawnDamageArea()에서 자동 생성되므로 여기서는 아무것도 하지 않음
    }
    
    /// <summary>
    /// ⭐ Phase 2: Damage만 실행 (EliteSkillActionStateBehaviour에서 호출)
    /// </summary>
    public void ExecuteSkillDamageOnly()
    {
        if (currentSkill == null) return;
        
        if (enableDebugLogs)
        {
            Debug.Log($"💥 [EliteSkillController] {gameObject.name}: {currentSkill.SkillName} 데미지 판정!");
        }
        
        // ⭐ Telegraph 제거 (데미지 판정 직전)
        RemoveTelegraph();
        
        // ⭐ Phase 2: DamageArea를 사용한 데미지 판정
        SpawnDamageArea();
    }
    
    /// <summary>
    /// ⭐ Phase 4: 스킬 액션 실행 (VFX는 자동 생성)
    /// </summary>
    public void ExecuteSkillAction()
    {
        if (currentSkill == null) return;
        
        if (enableDebugLogs)
        {
            Debug.Log($"💥 [EliteSkillController] {gameObject.name}: {currentSkill.SkillName} 액션 실행");
        }
        
        // ⭐ Phase 4: Damage만 호출 (VFX는 SpawnDamageArea()에서 자동 생성)
        ExecuteSkillDamageOnly();
    }

    /// <summary>
    /// 스킬 액션 완료 (StateMachineBehaviour 콜백)
    /// </summary>
    public void OnSkillActionComplete()
    {
        if (currentSkill == null) return;
        
        isActionExecuting = false;
        
        // ⭐⭐⭐ 핵심 수정: Animator 파라미터 업데이트!
        if (animController != null)
        {
            animController.SetSkillAction(false);
            
            // ⭐⭐ 추가: Attack 트리거 리셋 (혹시 남아있을 수 있음)
            var animator = animController.GetComponent<Animator>();
            if (animator != null)
            {
                animator.ResetTrigger("Attack");
                
                if (enableDebugLogs)
                {
                    Debug.Log($"🧹 [EliteSkillController] {gameObject.name}: Attack 트리거 리셋!");
                }
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"🎬 [EliteSkillController] {gameObject.name}: isSkillAction = false 설정!");
            }
        }
        else
        {
            Debug.LogError($"❌ [EliteSkillController] {gameObject.name}: EnemyAnimationController가 없습니다!");
        }
        
        // ⭐⭐⭐ EliteAttackBehaviour에 스킬 완료 알림 (전역 쿨다운 시작)
        var eliteAttack = GetComponent<EliteAttackBehaviour>();
        if (eliteAttack != null)
        {
            eliteAttack.OnSkillComplete();
        }
        
        // 쿨다운 시작
        if (skillCooldowns.ContainsKey(currentSkill))
        {
            skillCooldowns[currentSkill] = currentSkill.Cooldown;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"✅ [EliteSkillController] {gameObject.name}: {currentSkill.SkillName} 완료! (쿨다운: {currentSkill.Cooldown}초)");
        }
        
        currentSkill = null;
    }

    #region 이펙트 생성

    /// <summary>
    /// Cast 이펙트 생성
    /// </summary>
    private void SpawnCastEffect()
    {
        if (currentSkill == null || currentSkill.CastEffect == null) return;
        
        GameObject effect = Instantiate(currentSkill.CastEffect, transform.position, Quaternion.identity);
        Destroy(effect, currentSkill.CastTime);
    }

    /// <summary>
    /// ⭐ Phase 3: Telegraph 생성 (보스 방식 적용 - Origin 기준 + Center Mode 조정)
    /// </summary>
    private void SpawnTelegraph()
    {
        if (currentSkill == null || currentSkill.TelegraphPrefab == null) return;
        
        // ⭐ Phase 3: Origin 위치에 생성 (보스 방식과 동일)
        Vector3 spawnPosition = cachedOrigin;
        
        // ⭐ Phase 6: 회전 적용 (보스와 동일하게 cachedTargetDirection 기반)
        Quaternion rotation = CalculateTelegraphRotation();
        
        // Telegraph 생성 (위치 + 회전 적용)
        activeTelegraph = Instantiate(currentSkill.TelegraphPrefab, spawnPosition, rotation);
        
        if (enableDebugLogs)
        {
            Debug.Log($"📍 [EliteSkillController] Telegraph 생성: {activeTelegraph.name}");
            Debug.Log($"   - Origin: {cachedOrigin}");
            Debug.Log($"   - Rotation: {rotation.eulerAngles}");
            Debug.Log($"   - Center Mode: {currentSkill.AoeCenterMode}");
            Debug.Log($"   - Center Offset: {currentSkill.AoeCenterOffset}");
        }
        
        // Telegraph 설정 (두 가지 타입 지원)
        float scaleMultiplier = 1.0f; // 엘리트는 기본값 1.0
        
        // 1. SpriteRenderer 기반 (TelegraphIndicator)
        var indicator = activeTelegraph.GetComponent<TelegraphIndicator>();
        if (indicator != null)
        {
            indicator.Initialize(currentSkill, currentSkill.TelegraphDuration, scaleMultiplier);
            
            // ⭐ Phase 3: Center Mode에 따라 Telegraph 위치 조정 (DamageArea와 동일)
            AdjustTelegraphPositionForCenterMode(activeTelegraph, scaleMultiplier);
            
            if (enableDebugLogs)
            {
                Debug.Log($"✅ [EliteSkillController] TelegraphIndicator 초기화 완료 (최종 위치: {activeTelegraph.transform.position})");
            }
        }
        else
        {
            // 2. Procedural Mesh 기반 (TelegraphIndicatorMesh)
            var indicatorMesh = activeTelegraph.GetComponent<TelegraphIndicatorMesh>();
            if (indicatorMesh != null)
            {
                indicatorMesh.Initialize(currentSkill, currentSkill.TelegraphDuration, scaleMultiplier);
                
                // ⭐ Phase 3: Center Mode에 따라 Telegraph 위치 조정 (DamageArea와 동일)
                AdjustTelegraphPositionForCenterMode(activeTelegraph, scaleMultiplier);
                
                if (enableDebugLogs)
                {
                    Debug.Log($"✅ [EliteSkillController] TelegraphIndicatorMesh 초기화 완료 (최종 위치: {activeTelegraph.transform.position})");
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ [EliteSkillController] Telegraph에 TelegraphIndicator 또는 TelegraphIndicatorMesh 컴포넌트가 없습니다!");
            }
        }
    }
    
    /// <summary>
    /// ⭐ Phase 3: Center Mode에 따라 Telegraph 위치 조정 (보스 방식과 동일)
    /// DamageArea.CalculateCenter()와 동일한 로직 적용
    /// </summary>
    private void AdjustTelegraphPositionForCenterMode(GameObject telegraph, float scaleMultiplier)
    {
        if (telegraph == null || currentSkill == null) return;
        
        // ForwardAnchored 모드일 때만 위치 조정
        if (currentSkill.AoeCenterMode == AOECenterMode.ForwardAnchored)
        {
            // Center Offset 계산 (DamageArea와 동일한 로직)
            float centerOffset = currentSkill.AoeCenterOffset;
            
            // Fallback: AoeOffset 사용 (호환성 유지)
            if (centerOffset <= 0f)
            {
                Vector2 aoeOffset = currentSkill.AoeOffset;
                centerOffset = aoeOffset.magnitude;
            }
            
            // 오프셋이 여전히 0이면 경고 후 종료
            if (centerOffset <= 0f)
            {
                if (enableDebugLogs)
                {
                    Debug.LogWarning($"⚠️ [EliteSkillController] ForwardAnchored 모드인데 Center Offset이 0입니다! SkillData에서 AoeCenterOffset을 설정해주세요.");
                }
                return;
            }
            
            // 스케일 적용된 오프셋 (DamageArea와 동일)
            float finalOffset = centerOffset * scaleMultiplier;
            
            // Forward 방향으로 위치 이동
            Vector3 forward = cachedTargetDirection.normalized;
            
            // Origin 위치를 기준으로 오프셋 적용
            Vector3 newPosition = cachedOrigin + forward * finalOffset;
            
            telegraph.transform.position = newPosition;
            
            if (enableDebugLogs)
            {
                Debug.Log($"🎯 [EliteSkillController] ForwardAnchored 모드 - Telegraph 위치 조정:");
                Debug.Log($"   - Origin: {cachedOrigin}");
                Debug.Log($"   - Center Offset: {centerOffset}");
                Debug.Log($"   - Scale Multiplier: {scaleMultiplier}");
                Debug.Log($"   - Final Offset: {finalOffset}");
                Debug.Log($"   - Forward: {forward}");
                Debug.Log($"   - New Position: {newPosition}");
            }
        }
        // Centered 모드일 때는 Origin 위치 그대로 유지 (조정 불필요)
    }

    /// <summary>
    /// Telegraph 제거
    /// </summary>
    private void RemoveTelegraph()
    {
        if (activeTelegraph != null)
        {
            Destroy(activeTelegraph);
            activeTelegraph = null;
        }
    }

    /// <summary>
    /// ⭐ Phase 4: AOE 이펙트 생성 (DamageArea Center 기준)
    /// </summary>
    private void SpawnAOEEffectAtCenter(Vector3 center)
    {
        if (currentSkill == null || currentSkill.AoeEffect == null) return;
        
        // ⭐ 저장된 cachedTargetDirection 사용 (Telegraph와 동일한 방향)
        Quaternion rotation = CalculateTelegraphRotation();
        
        if (enableDebugLogs)
        {
            Debug.Log($"🎨 [EliteSkillController] AOE 이펙트 생성: {currentSkill.AoeEffect.name}");
            Debug.Log($"   - Center: {center}");
            Debug.Log($"   - Rotation: {rotation.eulerAngles}");
        }
        
        GameObject effect = Instantiate(currentSkill.AoeEffect, center, rotation);
        Destroy(effect, 2f);
    }
    
    /// <summary>
    /// ⚠️ [DEPRECATED - Phase 4] AOE 이펙트 생성 (Origin 기준)
    /// SpawnAOEEffectAtCenter() 사용 권장
    /// </summary>
    [System.Obsolete("Phase 4: SpawnAOEEffectAtCenter(Vector3) 사용 권장. DamageArea.GetCalculatedCenter()와 동기화 필요")]
    private void SpawnAOEEffect()
    {
        if (currentSkill == null || currentSkill.AoeEffect == null) return;
        
        // ⭐ Phase 2: 저장된 Origin 사용 (CalculateAOECenter 제거)
        Vector3 spawnPosition = cachedOrigin;
        Quaternion rotation = CalculateAOERotation();
        
        if (enableDebugLogs)
        {
            Debug.Log($"🎨 [EliteSkillController] AOE 이펙트 생성 (Obsolete): {currentSkill.AoeEffect.name} at {spawnPosition}");
        }
        
        GameObject effect = Instantiate(currentSkill.AoeEffect, spawnPosition, rotation);
        Destroy(effect, 2f);
    }

    #endregion

    #region AOE 데미지 판정 (Phase 2: DamageArea 사용)
    
    /// <summary>
    /// ⭐ Phase 2: DamageArea를 사용한 데미지 판정
    /// </summary>
    private void SpawnDamageArea()
    {
        if (currentSkill == null)
        {
            Debug.LogError("[EliteSkillController] currentSkill이 null!");
            return;
        }
        
        // DamageArea 프리팹 로드
        GameObject damageAreaPrefab = Resources.Load<GameObject>("Prefabs/VFX/DamageArea");
        if (damageAreaPrefab == null)
        {
            Debug.LogError("[EliteSkillController] DamageArea 프리팹을 찾을 수 없습니다! 경로: Prefabs/VFX/DamageArea");
            return;
        }
        
        // DamageArea 인스턴스 생성
        GameObject damageAreaGO = Instantiate(damageAreaPrefab);
        DamageArea damageArea = damageAreaGO.GetComponent<DamageArea>();
        
        if (damageArea == null)
        {
            Debug.LogError("[EliteSkillController] DamageArea 컴포넌트가 없습니다!");
            Destroy(damageAreaGO);
            return;
        }
        
        // ⭐ Phase 4: 엘리트용 Initialize 호출 (정책 명시)
        damageArea.Initialize(
            skillData: currentSkill,                   // SkillData
            origin: cachedOrigin,                      // Origin (Cast 시작 시점 저장됨)
            forward: cachedTargetDirection,            // Forward (Cast 시작 시점 저장됨)
            enemy: baseEnemy,                          // BaseEnemy
            scaleMultiplier: 1.0f,                     // scaleMultiplier (엘리트는 기본 1.0)
            policy: AOEDamagePolicy.Once               // ⭐ 명시적 정책 (Once/Window/Tick 선택 가능)
        );
        
        if (enableDebugLogs)
        {
            Debug.Log($"✅ [EliteSkillController] DamageArea 생성 완료:");
            Debug.Log($"   - Skill: {currentSkill.SkillName}");
            Debug.Log($"   - Origin: {cachedOrigin}");
            Debug.Log($"   - Forward: {cachedTargetDirection}");
        }
        
        // ⭐ Phase 4: DamageArea의 Left Pivot 보정 위치를 사용하여 VFX 생성
        Vector3 effectPosition = damageArea.GetEffectSpawnPositionForLeftPivot();
        SpawnAOEEffectAtCenter(effectPosition);
        
        if (enableDebugLogs)
        {
            Debug.Log($"🎨 [EliteSkillController] VFX 이펙트 생성 완료:");
            Debug.Log($"   - Position: {effectPosition}");
            Debug.Log($"   - DamageArea와 VFX 위치 동기화 완료! (Left Pivot 보정 적용)");
        }
        
        // 데미지 판정 실행
        damageArea.PerformDamage();
        
        // ⭐ 1초 후 제거 (Gizmos 확인용)
        Destroy(damageAreaGO, 1.0f);
    }
    
    #region ⚠️ Phase 2: 아래 메서드들은 제거 예정 (DamageArea로 대체됨)
    
    /// <summary>
    /// ⚠️ [DEPRECATED - Phase 2] AOE 데미지 판정 (DamageArea로 대체됨)
    /// 하위 호환성을 위해 유지되지만 사용하지 않음
    /// </summary>
    [System.Obsolete("Phase 2: DamageArea로 대체됨. SpawnDamageArea() 사용 권장")]
    private void PerformAOEDamage()
    {
        if (currentSkill == null) return;
        
        Vector3 center = CalculateAOECenter();
        Vector3 direction = GetDirectionToPlayer();
        
        Collider2D[] hits = null;
        
        switch (currentSkill.AoeShape)
        {
            case AOEShapeType.Circle:
                hits = Physics2D.OverlapCircleAll(center, currentSkill.AoeRadius, LayerMask.GetMask("Player"));
                break;
                
            case AOEShapeType.Triangle: // Fan
                hits = GetFanHits(center, direction, currentSkill.AoeRadius, currentSkill.AoeAngle);
                break;
                
            case AOEShapeType.Rectangle:
                hits = Physics2D.OverlapBoxAll(center, currentSkill.AoeSize, GetAngleToPlayer(), LayerMask.GetMask("Player"));
                break;
        }
        
        if (hits == null || hits.Length == 0)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[EliteSkillController] AOE 데미지 대상 없음");
            }
            return;
        }
        
        // 플레이어에게 데미지 적용
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                ApplyDamageToPlayer(hit.gameObject);
                break; // 플레이어는 한 번만
            }
        }
    }

    /// <summary>
    /// ⚠️ [DEPRECATED - Phase 2] 플레이어에게 데미지 적용 (DamageArea로 대체됨)
    /// </summary>
    [System.Obsolete("Phase 2: DamageArea로 대체됨")]
    private void ApplyDamageToPlayer(GameObject player)
    {
        if (player == null) return;
        
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth == null) return;
        
        // 데미지 계산 (평타 데미지 × 스킬 배율)
        // MeleeAttack 컴포넌트에서 기본 데미지 가져오기
        int baseDamage = 10; // 기본값 (fallback)
        
        if (baseEnemy != null)
        {
            // 1순위: MeleeAttack 컴포넌트에서 데미지 가져오기
            var meleeAttack = baseEnemy.GetComponent<MeleeAttack>();
            if (meleeAttack != null && meleeAttack.AttackData != null)
            {
                baseDamage = meleeAttack.GetScaledDamage();
            }
            // 2순위: RangedAttack 컴포넌트에서 데미지 가져오기
            else
            {
                var rangedAttack = baseEnemy.GetComponent<RangedAttack>();
                if (rangedAttack != null && rangedAttack.AttackData != null)
                {
                    baseDamage = rangedAttack.GetScaledDamage();
                }
            }
            
            // 로그 출력
            if (enableDebugLogs)
            {
                Debug.Log($"[EliteSkillController] 기본 데미지 획득: {baseDamage} (from {(meleeAttack != null ? "MeleeAttack" : "RangedAttack")})");
            }
        }
        
        // 스킬 데미지 계산 (기본 데미지 × 스킬 배율)
        int skillDamage = currentSkill.GetScaledDamage(baseDamage);
        
        // 플레이어에게 데미지 적용
        playerHealth.TakeDamage(skillDamage, transform);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[EliteSkillController] 플레이어 피격: {skillDamage} 데미지 (기본: {baseDamage}, 배율: {currentSkill.DamageMultiplier}x)");
        }
        
        // Hit 이펙트
        if (currentSkill.HitEffect != null)
        {
            GameObject hitEffect = Instantiate(currentSkill.HitEffect, player.transform.position, Quaternion.identity);
            Destroy(hitEffect, 2f);
        }
        
        // Screen Shake
        TriggerScreenShake(currentSkill.ShakeIntensity);
    }

    /// <summary>
    /// ⚠️ [DEPRECATED - Phase 2] 부채꼴 범위 충돌 감지 (DamageArea로 대체됨)
    /// </summary>
    [System.Obsolete("Phase 2: DamageArea로 대체됨")]
    private Collider2D[] GetFanHits(Vector3 center, Vector3 direction, float radius, float angle)
    {
        Collider2D[] allHits = Physics2D.OverlapCircleAll(center, radius, LayerMask.GetMask("Player"));
        List<Collider2D> fanHits = new List<Collider2D>();
        
        foreach (var hit in allHits)
        {
            Vector3 toTarget = (hit.transform.position - center).normalized;
            float dotProduct = Vector3.Dot(direction, toTarget);
            float angleToTarget = Mathf.Acos(dotProduct) * Mathf.Rad2Deg;
            
            if (angleToTarget <= angle / 2f)
            {
                fanHits.Add(hit);
            }
        }
        
        return fanHits.ToArray();
    }

    /// <summary>
    /// Screen Shake 트리거
    /// </summary>
    private void TriggerScreenShake(float intensity)
    {
        if (ScreenShakeManager.Instance != null)
        {
            ScreenShakeManager.Instance.ShakeScreen(intensity);
        }
    }

    #endregion
    
    #endregion

    #region 유틸리티

    /// <summary>
    /// ⭐ Phase 2: 스킬 Origin 위치 계산 (간소화)
    /// Center 계산은 DamageArea에 위임
    /// </summary>
    [System.Obsolete("Phase 2: cachedOrigin 사용 권장")]
    private Vector3 GetSkillOrigin()
    {
        // ⭐ 수정: 보스와 동일하게 피봇 중심점 반환 (aoeOffset 제거)
        return transform.position;
    }
    
    /// <summary>
    /// ⚠️ [DEPRECATED - Phase 2] AOE 중심점 계산 (DamageArea로 대체됨)
    /// Telegraph 생성에서만 사용 (호환성 유지)
    /// </summary>
    [System.Obsolete("Phase 2: GetSkillOrigin() 사용 권장. Center 계산은 DamageArea에 위임")]
    private Vector3 CalculateAOECenter()
    {
        // ⭐ Phase 2: 간소화된 로직 (Origin만 반환)
        // Center 계산은 DamageArea가 SkillData 기반으로 처리
        return GetSkillOrigin();
    }
    
    /// <summary>
    /// ⚠️ [DEPRECATED - Phase 2] 아이소메트릭 뷰 거리 보정 계산 (제거 예정)
    /// DamageArea가 내부적으로 처리하므로 더 이상 필요 없음
    /// </summary>
    [System.Obsolete("Phase 2: DamageArea가 내부적으로 처리")]
    private float GetIsometricDistanceCorrection(Vector3 direction, float baseDistance)
    {
        // 방향을 각도로 변환 (0° = E, 90° = N, 180° = W, 270° = S)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;
        
        // 방향별 보정 계수
        float correctionFactor = 1.0f;
        
        // 8방향 판별 (22.5도 간격)
        if (angle >= 337.5f || angle < 22.5f)
        {
            // E (0°) - 좌우 방향
            correctionFactor = 1.0f;
        }
        else if (angle >= 22.5f && angle < 67.5f)
        {
            // NE (45°) - 대각선
            correctionFactor = 0.85f;
        }
        else if (angle >= 67.5f && angle < 112.5f)
        {
            // N (90°) - 상하 방향
            correctionFactor = 0.7f;
        }
        else if (angle >= 112.5f && angle < 157.5f)
        {
            // NW (135°) - 대각선
            correctionFactor = 0.85f;
        }
        else if (angle >= 157.5f && angle < 202.5f)
        {
            // W (180°) - 좌우 방향
            correctionFactor = 1.0f;
        }
        else if (angle >= 202.5f && angle < 247.5f)
        {
            // SW (225°) - 대각선
            correctionFactor = 0.85f;
        }
        else if (angle >= 247.5f && angle < 292.5f)
        {
            // S (270°) - 상하 방향
            correctionFactor = 0.7f;
        }
        else if (angle >= 292.5f && angle < 337.5f)
        {
            // SE (315°) - 대각선
            correctionFactor = 0.85f;
        }
        
        float correctedDistance = baseDistance * correctionFactor;
        
        if (enableDebugLogs)
        {
            string directionName = GetDirectionName(angle);
            Debug.Log($"🎯 [거리 보정] 방향: {directionName} ({angle:F1}°), 계수: {correctionFactor:F2}, 원본: {baseDistance:F2} → 보정: {correctedDistance:F2}");
        }
        
        return correctedDistance;
    }
    
    /// <summary>
    /// 각도를 방향 이름으로 변환 (디버그용)
    /// </summary>
    private string GetDirectionName(float angle)
    {
        if (angle >= 337.5f || angle < 22.5f) return "E (우측)";
        if (angle >= 22.5f && angle < 67.5f) return "NE (우상단)";
        if (angle >= 67.5f && angle < 112.5f) return "N (위쪽)";
        if (angle >= 112.5f && angle < 157.5f) return "NW (좌상단)";
        if (angle >= 157.5f && angle < 202.5f) return "W (좌측)";
        if (angle >= 202.5f && angle < 247.5f) return "SW (좌하단)";
        if (angle >= 247.5f && angle < 292.5f) return "S (아래쪽)";
        if (angle >= 292.5f && angle < 337.5f) return "SE (우하단)";
        return "Unknown";
    }

    /// <summary>
    /// AOE 회전 계산
    /// </summary>
    private Quaternion CalculateAOERotation()
    {
        Vector3 direction = GetDirectionToPlayer();
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        // ⭐ 이펙트 프리팹이 정상 방향이므로 보정 제거
        return Quaternion.Euler(0, 0, angle);
    }
    
    /// <summary>
    /// ⭐ Telegraph 회전 계산 (플레이어 방향) - 보스와 동일
    /// Cast 시작 시점에 저장한 cachedTargetDirection 사용
    /// </summary>
    private Quaternion CalculateTelegraphRotation()
    {
        // Cast 시작 시점에 저장한 방향 사용 (Telegraph와 실제 스킬 싱크 맞춤)
        Vector3 direction = cachedTargetDirection;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // AOE 형태별 회전 처리
        switch (currentSkill.AoeShape)
        {
            case AOEShapeType.Circle:
                // ⭐ 원형도 플레이어 방향 적용 (메테오 등 방향성 이펙트 지원)
                return Quaternion.Euler(0, 0, angle);
            
            case AOEShapeType.Triangle:
            case AOEShapeType.Rectangle:
                // 삼각형(Fan)/직사각형: 플레이어 방향
                return Quaternion.Euler(0, 0, angle);
            
            default:
                return Quaternion.Euler(0, 0, angle);
        }
    }

    /// <summary>
    /// 플레이어 방향 벡터
    /// </summary>
    private Vector3 GetDirectionToPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            return (player.transform.position - transform.position).normalized;
        }
        return Vector3.down;
    }

    /// <summary>
    /// 플레이어 각도
    /// </summary>
    private float GetAngleToPlayer()
    {
        Vector3 direction = GetDirectionToPlayer();
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    #endregion

    #region 디버그

    [ContextMenu("Debug Skill Cooldowns")]
    private void DebugSkillCooldowns()
    {
        string info = "=== Skill Cooldowns ===\n";
        foreach (var kvp in skillCooldowns)
        {
            info += $"{kvp.Key.SkillName}: {kvp.Value:F1}초\n";
        }
        Debug.Log(info);
    }

    #endregion
}

