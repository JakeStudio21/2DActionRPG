using UnityEngine;
using System.Collections;
using System;
using CueSystem;

/// <summary>
/// 새로운 BaseSkill<T> 기반 스킬 시스템 컨트롤러
/// SRP 원칙에 따라 스킬 관리만 담당
/// </summary>
[System.Serializable]
public class SkillController : MonoBehaviour
{
    [Header("📊 스킬 시스템")]
    public SkillSet skillSet;
    
    // 🆕 Phase 1: 런타임 스킬 참조
    private PlayerSkillManager skillManager;
    
    // ❌ 제거: 사용되지 않는 호환성 필드들
    /*
    [Header("⏰ 호환성 필드 (UI 시스템용)")]
    [Tooltip("스킬1 쿨다운 시간 (UI 시스템 호환성용)")]
    public float cooldownTime = 2f;
    
    [Tooltip("스킬2 쿨다운 시간 (UI 시스템 호환성용)")]
    public float skill2CooldownTime = 3f;
    */
    
    [Header("🔧 디버그")]
    public bool showDebugLogs = true;
    
    [Header("⚙️ Phase 4 설정")]
    [Tooltip("Phase 4 새 시스템 사용 여부 (false면 기존 SkillSet 사용)")]
    public bool usePhase4System = true;
    
    // ⭐ Phase 4: 마지막 공격 방향 저장 (기본공격 패턴과 동일)
    private Vector2 lastAttackDirection = Vector2.right;
    
    // ─── 스킬 실행 상태 ───────────────────────────────────────────────────
    /// <summary>Telegraph 대기 또는 Effect 딜레이가 진행 중인지 여부 (PlayerAnimationController가 참조)</summary>
    public bool IsSkillPendingExecution { get; private set; }
    
    /// <summary>스킬 실행 완전 완료 시 발행 (slotIndex, -1 = 취소)</summary>
    public event Action<int> OnSkillExecutionComplete;
    
    private Coroutine activeSkillCoroutine;
    
    /// <summary>
    /// SkillSet 프로퍼티 (기존 코드 호환성용)
    /// </summary>
    public SkillSet SkillSet 
    { 
        get 
        { 
            if (skillSet == null)
            {
                skillSet = new SkillSet();
                skillSet.showDebugLogs = showDebugLogs;
            }
            return skillSet; 
        } 
    }

    void Awake()
    {
        // 🆕 Phase 1: PlayerSkillManager 참조 초기화
        skillManager = GetComponent<PlayerSkillManager>();
        if (skillManager == null)
        {
            Debug.LogWarning("⚠️ [SkillController] PlayerSkillManager가 없습니다. 기존 SkillSet 사용");
        }
        
        if (skillSet == null)
        {
            skillSet = new SkillSet();
            skillSet.showDebugLogs = showDebugLogs;
            
            if (showDebugLogs)
                Debug.Log("🎯 [SkillController] SkillSet 초기화 완료");
        }
        
        // ⭐ 디버그: 초기 상태 로깅
        if (showDebugLogs)
        {
            Debug.Log("🔍 [SkillController] Awake - 초기 스킬 상태 확인:");
            LogSkillSetInfo();
        }
    }
    
    void Start()
    {
        // ⭐ 디버그: 모든 초기화 후 최종 상태 확인
        if (showDebugLogs)
        {
            Debug.Log("🔍 [SkillController] Start - 최종 스킬 할당 상태 확인:");
            LogSkillSetInfo();
            
            // 스킬 컴포넌트들 직접 확인
            CheckSkillComponents();
        }
        
        // ⭐ 추가: 스킬 할당 검증 강화
        StartCoroutine(VerifySkillAssignmentRoutine());
        
        // ⭐ Phase 4: 마지막 공격 방향 지속적 업데이트 (기본공격과 동일)
        if (usePhase4System)
        {
            StartCoroutine(UpdateLastAttackDirectionCoroutine());
        }
    }
    
    /// <summary>
    /// ⭐ 새로 추가: 스킬 컴포넌트들 직접 확인
    /// </summary>
    [ContextMenu("Check Skill Components")]
    public void CheckSkillComponents()
    {
        Debug.Log("🔍 [SkillController] 스킬 컴포넌트 직접 확인:");
        
        var assasinSkill1 = GetComponent<AssasinSkill1>();
        var assasinSkill2 = GetComponent<AssasinSkill2>();
        var warriorSkill1 = GetComponent<WarriorSkill1>();
        var warriorSkill2 = GetComponent<WarriorSkill2>();
        
        Debug.Log($"   - AssasinSkill1: {(assasinSkill1 != null ? "✅ 존재" : "❌ 없음")}");
        Debug.Log($"   - AssasinSkill2: {(assasinSkill2 != null ? "✅ 존재" : "❌ 없음")}");
        Debug.Log($"   - WarriorSkill1: {(warriorSkill1 != null ? "✅ 존재" : "❌ 없음")}");
        Debug.Log($"   - WarriorSkill2: {(warriorSkill2 != null ? "✅ 존재" : "❌ 없음")}");
    }

    public void TriggerSkill()
    {
        Debug.Log("🔵 [SkillController] 스킬1 실행 요청");
        
        // ⭐ Phase 4: PlayerSkillManager의 장착된 스킬 사용
        if (usePhase4System && skillManager != null)
        {
            Debug.Log("✅ [SkillController] Phase 4 시스템 사용");
            
            var skillInstance = skillManager.GetEquippedActiveSkill(0); // 슬롯 0
            
            if (skillInstance == null)
            {
                Debug.LogWarning("⚠️ [SkillController] 스킬1 미장착");
                return;
            }
            
            Debug.Log($"📋 [SkillController] 장착된 스킬: {skillInstance.skillData.skillName}");
            Debug.Log($"   - 현재 레벨: {skillInstance.currentLevel}");
            Debug.Log($"   - 쿨다운 남음: {skillInstance.GetCooldownRemaining():F1}초");
            Debug.Log($"   - CanUse: {skillInstance.CanUse()}");
            
            if (skillInstance.CanUse())
            {
                // ⭐ 새로운 스킬 실행 메서드 호출
                ExecuteSkillFromInstance(skillInstance, 0);
                
                if (showDebugLogs)
                    Debug.Log($"⚔️ [SkillController] 스킬1 실행: {skillInstance.skillData.skillName}");
            }
            else
            {
                Debug.LogWarning($"⚠️ [SkillController] 스킬1 쿨다운 ({skillInstance.GetCooldownRemaining():F1}초 남음)");
            }
            return;
        }
        
        // Fallback: 기존 SkillSet 사용 (호환성)
        Debug.LogWarning("🟡 [SkillController] PlayerSkillManager 없음 - 기존 SkillSet 사용");
        var context = new CueContext
        {
            position = transform.position,
            actorType = ActorType.Player,
            magnitude = 1.5f
        };
        
        CueEmitter.Emit("skill.player.skill1", "Player", context);
        
        if (skillSet != null)
        {
            var skill = skillSet.GetSkill(0);
            if (skill != null)
            {
                Debug.Log($"🎯 [SkillController] 스킬1 찾음: {skill.SkillName}, CanUse: {skill.CanUse()}");
            }
            else
            {
                Debug.LogWarning("❌ [SkillController] 스킬1이 SkillSet에 없습니다!");
                return;
            }
            
            bool success = skillSet.ExecuteSkill(0);
            if (!success)
            {
                Debug.LogWarning("🟡 스킬1 실행 실패!");
            }
        }
        else
        {
            Debug.LogError("❌ [SkillController] SkillSet이 null입니다!");
        }
    }

    public void TriggerSkill2()
    {
        Debug.Log("🔵 [SkillController] 스킬2 실행 요청");
        
        // ⭐ Phase 4: PlayerSkillManager의 장착된 스킬 사용
        if (usePhase4System && skillManager != null)
        {
            Debug.Log("✅ [SkillController] Phase 4 시스템 사용");
            
            var skillInstance = skillManager.GetEquippedActiveSkill(1); // 슬롯 1
            
            if (skillInstance == null)
            {
                Debug.LogWarning("⚠️ [SkillController] 스킬2 미장착");
                return;
            }
            
            Debug.Log($"📋 [SkillController] 장착된 스킬: {skillInstance.skillData.skillName}");
            Debug.Log($"   - 현재 레벨: {skillInstance.currentLevel}");
            Debug.Log($"   - 쿨다운 남음: {skillInstance.GetCooldownRemaining():F1}초");
            Debug.Log($"   - CanUse: {skillInstance.CanUse()}");
            
            if (skillInstance.CanUse())
            {
                // ⭐ 새로운 스킬 실행 메서드 호출
                ExecuteSkillFromInstance(skillInstance, 1);
                
                if (showDebugLogs)
                    Debug.Log($"⚔️ [SkillController] 스킬2 실행: {skillInstance.skillData.skillName}");
            }
            else
            {
                Debug.LogWarning($"⚠️ [SkillController] 스킬2 쿨다운 ({skillInstance.GetCooldownRemaining():F1}초 남음)");
            }
            return;
        }
        
        // Fallback: 기존 SkillSet 사용 (호환성)
        Debug.LogWarning("🟡 [SkillController] PlayerSkillManager 없음 - 기존 SkillSet 사용");
        var context = new CueContext
        {
            position = transform.position,
            actorType = ActorType.Player,
            magnitude = 2.0f
        };
        
        CueEmitter.Emit("skill.player.skill2", "Player", context);
        
        if (skillSet != null)
        {
            var skill = skillSet.GetSkill(1);
            if (skill != null)
            {
                Debug.Log($"🎯 [SkillController] 스킬2 찾음: {skill.SkillName}, CanUse: {skill.CanUse()}");
            }
            else
            {
                Debug.LogWarning("❌ [SkillController] 스킬2가 SkillSet에 없습니다!");
                return;
            }
            
            bool success = skillSet.ExecuteSkill(1);
            if (!success)
            {
                Debug.LogWarning("🟡 스킬2 실행 실패!");
            }
        }
        else
        {
            Debug.LogError("❌ [SkillController] SkillSet이 null입니다!");
        }
    }

    public bool TriggerSkill(int slot)
    {
        return skillSet.ExecuteSkill(slot);
    }

    public void OnSkillAnimationEvent(int slot)
    {
        Debug.Log($"🎬 [SkillController] OnSkillAnimationEvent 호출 - Slot: {slot}");
        
        // ⭐ Phase 4: PlayerSkillManager 시스템 우선 사용
        if (usePhase4System && skillManager != null)
        {
            Debug.Log($"✅ [SkillController] Phase 4 Animation Event 처리 - Slot {slot}");
            
            var skillInstance = skillManager.GetEquippedActiveSkill(slot);
            if (skillInstance == null)
            {
                Debug.LogWarning($"⚠️ [SkillController] 슬롯 {slot}에 스킬이 장착되지 않음!");
                return;
            }
            
            // ⭐ 직접 발사 로직 실행 (애니메이션 이벤트 타이밍)
            ExecuteSkillFromAnimationEvent(skillInstance, slot);
            return;
        }
        
        // Fallback: 기존 SkillSet 사용
        Debug.LogWarning("🟡 [SkillController] Phase 4 시스템 비활성화 - 기존 SkillSet 사용");
        if (skillSet != null)
        {
            skillSet.OnSkillAnimationEvent(slot);
        }
    }
    
    public void OnSkill1AnimationEvent() => OnSkillAnimationEvent(0);
    public void OnSkill2AnimationEvent() => OnSkillAnimationEvent(1);

    public float GetCooldownRemaining()
    {
        if (skillSet != null)
        {
            return skillSet.GetSkillCooldownRemaining(0);
        }
        return 0f;
    }

    public float GetSkill2CooldownRemaining()
    {
        if (skillSet != null)
        {
            return skillSet.GetSkillCooldownRemaining(1);
        }
        return 0f;
    }

    /// <summary>
    /// ⭐ SRP 준수: 스킬1 쿨다운 시작 (PlayerAnimationController에서 위임받음)
    /// </summary>
    // ❌ 삭제: 잘못된 SRP 구현
    /*
    public void StartSkill1Cooldown()
    {
        var skill1 = SkillSet.GetSkill(0);
        if (skill1 != null)
        {
            // BaseSkill의 StartCooldown() 호출
            skill1.Execute(); // 이미 쿨다운 시작 로직 포함
            
            if (showDebugLogs)
                Debug.Log($"🕐 [SkillController] 스킬1 쿨다운 시작: {skill1.Cooldown}초");
        }
        else
        {
            Debug.LogWarning("🟡 [SkillController] 스킬1을 찾을 수 없어서 쿨다운 시작 불가");
        }
    }
    
    /// <summary>
    /// ⭐ SRP 준수: 스킬2 쿨다운 시작 (PlayerAnimationController에서 위임받음)
    /// </summary>
    public void StartSkill2Cooldown()
    {
        var skill2 = SkillSet.GetSkill(1);
        if (skill2 != null)
        {
            // BaseSkill의 StartCooldown() 호출
            skill2.Execute(); // 이미 쿨다운 시작 로직 포함
            
            if (showDebugLogs)
                Debug.Log($"🕐 [SkillController] 스킬2 쿨다운 시작: {skill2.Cooldown}초");
        }
        else
        {
            Debug.LogWarning("🟡 [SkillController] 스킬2를 찾을 수 없어서 쿨다운 시작 불가");
        }
    }
    */

    // ========================================
    // ⭐ Phase 4: 런타임 스킬 실행 시스템
    // ========================================
    
    /// <summary>
    /// SkillInstance 기반 스킬 실행 (Phase 4)
    /// </summary>
    private void ExecuteSkillFromInstance(SkillInstance skillInstance, int slotIndex)
    {
        Debug.Log($"🎯 [SkillController] ExecuteSkillFromInstance 호출 - Slot: {slotIndex}");
        
        if (skillInstance == null || skillInstance.skillData == null)
        {
            Debug.LogError("❌ [SkillController] SkillInstance 또는 SkillData가 null!");
            return;
        }
        
        // ActiveSkillData로 캐스팅
        if (!(skillInstance.skillData is ActiveSkillData activeData))
        {
            Debug.LogError($"❌ [SkillController] {skillInstance.skillData.skillName}은 액티브 스킬이 아닙니다!");
            return;
        }
        
        Debug.Log($"✅ [SkillController] ActiveSkillData 확인: {activeData.skillName}");
        Debug.Log($"   - isProjectile: {activeData.isProjectile}");
        Debug.Log($"   - projectilePrefab: {(activeData.projectilePrefab != null ? activeData.projectilePrefab.name : "NULL")}");
        Debug.Log($"   - castCueKey: {(string.IsNullOrEmpty(activeData.castCueKey) ? "없음(fallback)" : activeData.castCueKey)}");
        
        // ⭐ 쿨다운 시작
        skillInstance.lastUsedTime = Time.time;
        
        // ⭐ 애니메이션 트리거 먼저 호출 (기존 시스템 호환)
        var animationController = GetComponent<PlayerAnimationController>();
        if (animationController != null)
        {
            if (slotIndex == 0)
                animationController.TriggerSkill1();
            else if (slotIndex == 1)
                animationController.TriggerSkill2();
            
            Debug.Log($"🎬 [SkillController] 애니메이션 트리거 호출: Skill{slotIndex + 1}");
        }
        else
        {
            Debug.LogWarning("⚠️ [SkillController] PlayerAnimationController 없음!");
        }
        
        // ⭐ 0.1초 딜레이 후 실제 발사 (애니메이션과 타이밍 맞추기)
        StartCoroutine(DelayedSkillExecution(activeData, skillInstance, slotIndex, 0.1f));
        
        // castCueKey 미설정 시 generic 키로 fallback (레거시 SO 대응)
        if (string.IsNullOrEmpty(activeData.castCueKey))
        {
            var context = new CueContext
            {
                position = transform.position,
                actorType = ActorType.Player,
                magnitude = 1.5f
            };
            CueEmitter.Emit($"skill.player.skill{slotIndex + 1}", "Player", context);
        }
        
        Debug.Log($"🔥 [SkillController] ExecuteSkillFromInstance 완료");
    }
    
    /// <summary>
    /// 딜레이 후 스킬 실행 (애니메이션 타이밍 맞추기)
    /// </summary>
    private IEnumerator DelayedSkillExecution(ActiveSkillData activeData, SkillInstance skillInstance, int slotIndex, float delay)
    {
        Debug.Log($"⏰ [SkillController] DelayedSkillExecution 시작 - {delay}초 대기");
        
        yield return new WaitForSeconds(delay);
        
        Debug.Log($"⏰ [SkillController] 딜레이 종료 - 스킬 발동 시작");
        
        // ⭐ PlayerRuntimeStats에서 최종 공격력 가져오기
        var playerStats = GetComponent<PlayerRuntimeStats>();
        if (playerStats == null)
        {
            Debug.LogError("❌ [SkillController] PlayerRuntimeStats를 찾을 수 없습니다!");
            yield break;
        }
        
        // ⭐ 데미지 계산: 플레이어 공격력 × 스킬 데미지 배율
        float damageMultiplier = skillInstance.GetCurrentDamage(); // CSV에서 가져온 % 값 (예: 150)
        int finalDamage = Mathf.RoundToInt(playerStats.FinalAttackDamage * (damageMultiplier / 100f));
        
        Debug.Log($"💥 [SkillController] {activeData.skillName} 데미지 계산:");
        Debug.Log($"   - 플레이어 공격력: {playerStats.FinalAttackDamage:F0}");
        Debug.Log($"   - 스킬 배율: {damageMultiplier}%");
        Debug.Log($"   - 최종 데미지: {finalDamage}");
        Debug.Log($"   - 쿨다운: {skillInstance.GetCurrentCooldown()}초");
        
        // 실행 플래그 설정 후 Telegraph/Effect 코루틴에 위임
        IsSkillPendingExecution = true;

        if (activeData.telegraphPrefab != null && activeData.telegraphDuration > 0f)
        {
            activeSkillCoroutine = StartCoroutine(ExecuteWithTelegraph(activeData, skillInstance, finalDamage, slotIndex));
        }
        else
        {
            activeSkillCoroutine = StartCoroutine(ExecuteSkillEffects(activeData, skillInstance, finalDamage, slotIndex));
        }

        if (showDebugLogs)
            Debug.Log($"✅ [SkillController] DelayedSkillExecution → 코루틴 위임 완료");
    }
    
    /// <summary>
    /// 공격 방향 기준 Telegraph 스폰 위치 계산 (offset 적용)
    /// X: 전방 거리, Y: 측면 거리 (우측 양수)
    /// </summary>
    private Vector3 CalculateTelegraphSpawnPos(Vector3 origin, Vector2 direction, Vector2 offset)
    {
        if (offset == Vector2.zero) return origin;
        Vector2 forward = direction.normalized;
        Vector2 right = new Vector2(-forward.y, forward.x);
        return origin + (Vector3)(forward * offset.x) + (Vector3)(right * offset.y);
    }

    /// <summary>
    /// AOE shape별 Telegraph 회전 계산
    /// Circle: Fixed(identity), Fan/Rectangle: Follow(공격 방향)
    /// </summary>
    private Quaternion CalculateTelegraphRotation(SkillAOEShape shape, Vector2 direction)
    {
        if (shape == SkillAOEShape.Circle)
            return Quaternion.identity;
        
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        return Quaternion.Euler(0f, 0f, angle);
    }
    
    /// <summary>
    /// SkillAOEShape → AOEShapeType 변환 (TelegraphIndicator 호환)
    /// </summary>
    private AOEShapeType ConvertToAOEShapeType(SkillAOEShape shape)
    {
        switch (shape)
        {
            case SkillAOEShape.Fan:       return AOEShapeType.Triangle;
            case SkillAOEShape.Rectangle: return AOEShapeType.Rectangle;
            default:                      return AOEShapeType.Circle;
        }
    }
    
    /// <summary>
    /// 발사체 발사 (isProjectile = true)
    /// </summary>
    private void FireProjectile(ActiveSkillData skillData, SkillInstance skillInstance, int damage, int slotIndex)
    {
        // 발사 지점 찾기
        Transform firePoint = FindFirePoint();
        if (firePoint == null)
        {
            Debug.LogError("❌ [SkillController] 발사 지점을 찾을 수 없습니다!");
            return;
        }
        
        // 조이스틱 방향 가져오기
        Vector2 direction = GetAttackDirection();
        
        // 발사체 프리팹 확인
        if (skillData.projectilePrefab == null)
        {
            Debug.LogError($"❌ [SkillController] {skillData.skillName}의 projectilePrefab이 null! Inspector에서 할당하세요.");
            return;
        }
        
        // GamePoolManager 확인
        if (GamePoolManager.Instance == null)
        {
            Debug.LogError("❌ [SkillController] GamePoolManager가 없습니다!");
            return;
        }
        
        // 발사체 개수만큼 발사
        int projectileCount = skillData.projectileCount;
        float spreadAngle = skillData.spreadAngle;
        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float startAngle = baseAngle - (spreadAngle * (projectileCount - 1) / 2f);
        
        int successCount = 0;
        
        for (int i = 0; i < projectileCount; i++)
        {
            float currentAngle = startAngle + (spreadAngle * i);
            Quaternion rotation = Quaternion.AngleAxis(currentAngle, Vector3.forward);
            
            // ⭐ 오브젝트 풀에서 발사체 생성 (풀 이름 = 프리팹 이름)
            string poolName = skillData.projectilePrefab.name;
            var projectile = GamePoolManager.Instance.SpawnFromPool(
                poolName,
                firePoint.position,
                rotation
            );
            
            if (projectile != null)
            {
                successCount++;
                
                // 발사체 속도 설정
                var projectileComponent = projectile.GetComponent<Projectile>();
                if (projectileComponent != null)
                {
                    projectileComponent.UpdateMoveSpeed(skillData.projectileSpeed);
                }
                
                // 발사체 데미지 설정 (DamageSource 컴포넌트 사용)
                var damageSource = projectile.GetComponent<DamageSource>();
                if (damageSource != null)
                {
                    // TODO: DamageSource.SetDamage() 메서드 확인 및 적용
                    if (showDebugLogs)
                        Debug.Log($"🎯 [SkillController] 발사체 데미지 설정: {damage}");
                }
                
                if (showDebugLogs)
                    Debug.Log($"🏹 [SkillController] 발사체 발사 #{i + 1}: 각도 {currentAngle:F1}°, 풀: {poolName}");
            }
            else
            {
                Debug.LogWarning($"⚠️ [SkillController] 발사체 생성 실패 - 풀 '{poolName}'이 등록되지 않았을 수 있습니다!");
            }
        }
        
        if (showDebugLogs)
            Debug.Log($"🏹 [SkillController] 발사 완료: {successCount}/{projectileCount}개 성공, 방향: {direction}");
    }
    
    /// <summary>
    /// 즉발형 AoE 생성 (isProjectile = false) — DamageArea 기반
    /// </summary>
    private void SpawnInstantAOE(ActiveSkillData skillData, SkillInstance skillInstance, int damage, int slotIndex)
    {
        Vector2 direction = GetAttackDirection();
        Transform firePoint = FindFirePoint();
        Vector3 skillPos = firePoint != null ? firePoint.position : transform.position;

        // DamageArea 프리팹 로드 (보스/엘리트와 동일한 경로)
        GameObject damageAreaPrefab = Resources.Load<GameObject>("Prefabs/VFX/DamageArea");
        if (damageAreaPrefab == null)
        {
            Debug.LogError("❌ [SkillController] DamageArea 프리팹을 찾을 수 없습니다! 경로: Resources/Prefabs/VFX/DamageArea");
            return;
        }

        GameObject damageAreaGO = Instantiate(damageAreaPrefab);
        DamageArea damageArea = damageAreaGO.GetComponent<DamageArea>();
        if (damageArea == null)
        {
            Debug.LogError("❌ [SkillController] DamageArea 컴포넌트가 없습니다!");
            Destroy(damageAreaGO);
            return;
        }

        // SkillAOEShape → AOEShapeType 변환 후 플레이어용 초기화
        AOEShapeType shapeType = ConvertToAOEShapeType(skillData.aoeShape);
        damageArea.InitializeForPlayer(
            shape: shapeType,
            origin: skillPos,
            forward: (Vector3)direction,
            radius: skillData.aoeRadius,
            size: skillData.aoeSize,
            angle: skillData.aoeFanAngle,
            playerBaseDamage: damage,
            policy: AOEDamagePolicy.Once
        );

        // 스킬 지속시간 + 여유시간 후 제거
        Destroy(damageAreaGO, skillData.aoeDuration + 0.5f);

        if (showDebugLogs)
            Debug.Log($"💥 [SkillController] DamageArea 생성: {skillData.aoeShape} → {shapeType}, 방향: {direction}, 데미지: {damage}");
    }
    
    /// <summary>
    /// ⭐ Phase 4: Animation Event에서 직접 호출
    /// telegraphPrefab이 있으면 코루틴으로 telegraph → 대기 → 발사, 없으면 즉시 발사
    /// </summary>
    private void ExecuteSkillFromAnimationEvent(SkillInstance skillInstance, int slotIndex)
    {
        Debug.Log($"🎯 [SkillController] ExecuteSkillFromAnimationEvent 호출 - Slot: {slotIndex}");
        
        if (!(skillInstance.skillData is ActiveSkillData activeData))
        {
            Debug.LogError($"❌ [SkillController] {skillInstance.skillData.skillName}은 액티브 스킬이 아닙니다!");
            return;
        }
        
        Debug.Log($"✅ [SkillController] ActiveSkillData 확인: {activeData.skillName}");
        Debug.Log($"   - isProjectile: {activeData.isProjectile}");
        Debug.Log($"   - telegraphPrefab: {(activeData.telegraphPrefab != null ? activeData.telegraphPrefab.name : "NULL")}");
        Debug.Log($"   - telegraphDuration: {activeData.telegraphDuration}");
        
        // ⭐ PlayerRuntimeStats에서 최종 공격력 가져오기
        var playerStats = GetComponent<PlayerRuntimeStats>();
        if (playerStats == null)
        {
            Debug.LogError("❌ [SkillController] PlayerRuntimeStats를 찾을 수 없습니다!");
            return;
        }
        
        // ⭐ 데미지 계산
        float damageMultiplier = skillInstance.GetCurrentDamage();
        int finalDamage = Mathf.RoundToInt(playerStats.FinalAttackDamage * (damageMultiplier / 100f));
        
        Debug.Log($"💥 [SkillController] 데미지 계산: {finalDamage}");
        
        // 스킬 실행 시작 — 이동 잠금 연장 플래그
        IsSkillPendingExecution = true;

        if (activeData.telegraphPrefab != null && activeData.telegraphDuration > 0f)
        {
            activeSkillCoroutine = StartCoroutine(ExecuteWithTelegraph(activeData, skillInstance, finalDamage, slotIndex));
        }
        else
        {
            activeSkillCoroutine = StartCoroutine(ExecuteSkillEffects(activeData, skillInstance, finalDamage, slotIndex));
        }
    }
    
    /// <summary>
    /// Telegraph 표시 후 스킬 발사 (코루틴)
    /// ① castEffectDelay → Cast Effect
    /// ② Telegraph 스폰 + telegraphDuration 대기
    /// ③ aoeEffectDelay → AOE Effect + Damage
    /// </summary>
    private IEnumerator ExecuteWithTelegraph(ActiveSkillData activeData, SkillInstance skillInstance, int finalDamage, int slotIndex)
    {
        Vector2 attackDir = GetAttackDirection();
        Transform firePoint = FindFirePoint();
        Vector3 skillPos = firePoint != null ? firePoint.position : transform.position;

        // ① Cast Effect 딜레이
        if (activeData.castEffectDelay > 0f)
            yield return new WaitForSeconds(activeData.castEffectDelay);

        // ② Cast Effect 발동
        EmitCastCue(activeData, attackDir, skillPos);

        // ③ Telegraph 스폰
        Vector3 telegraphPos = CalculateTelegraphSpawnPos(skillPos, attackDir, activeData.telegraphOffset);
        Quaternion telegraphRot = CalculateTelegraphRotation(activeData.aoeShape, attackDir);
        GameObject telegraphObj = Instantiate(activeData.telegraphPrefab, telegraphPos, telegraphRot);

        var telegraph = telegraphObj.GetComponent<TelegraphIndicator>();
        if (telegraph != null)
        {
            telegraph.InitializeForPlayer(
                shape: ConvertToAOEShapeType(activeData.aoeShape),
                position: telegraphPos,
                radius: activeData.aoeRadius,
                size: activeData.aoeSize,
                angle: activeData.aoeFanAngle,
                displayDuration: activeData.telegraphDuration,
                forward: (Vector3)attackDir
            );
        }

        if (showDebugLogs)
            Debug.Log($"📍 [SkillController] Telegraph 표시: {activeData.aoeShape}, {activeData.telegraphDuration}초 대기");

        // ④ Telegraph Duration 대기
        yield return new WaitForSeconds(activeData.telegraphDuration);

        // ⑤ AOE Effect 딜레이
        if (activeData.aoeEffectDelay > 0f)
            yield return new WaitForSeconds(activeData.aoeEffectDelay);

        // ⑥ AOE Effect + Damage 발동
        EmitAOECue(activeData, attackDir, skillPos);

        if (activeData.isProjectile)
            FireProjectile(activeData, skillInstance, finalDamage, slotIndex);
        else
            SpawnInstantAOE(activeData, skillInstance, finalDamage, slotIndex);

        // ⑦ 완료 처리 (이동 해제 이벤트 발행)
        CompleteSkillExecution(slotIndex);
    }

    /// <summary>
    /// Telegraph 없이 Effect 딜레이만 적용하는 스킬 실행 코루틴
    /// ① castEffectDelay → Cast Effect
    /// ② aoeEffectDelay  → AOE Effect + Damage
    /// </summary>
    private IEnumerator ExecuteSkillEffects(ActiveSkillData activeData, SkillInstance skillInstance, int finalDamage, int slotIndex)
    {
        Vector2 attackDir = GetAttackDirection();
        Transform firePoint = FindFirePoint();
        Vector3 skillPos = firePoint != null ? firePoint.position : transform.position;

        // ① Cast Effect 딜레이
        if (activeData.castEffectDelay > 0f)
            yield return new WaitForSeconds(activeData.castEffectDelay);

        // ② Cast Effect 발동
        EmitCastCue(activeData, attackDir, skillPos);

        // ③ AOE Effect 딜레이
        if (activeData.aoeEffectDelay > 0f)
            yield return new WaitForSeconds(activeData.aoeEffectDelay);

        // ④ AOE Effect + Damage 발동
        EmitAOECue(activeData, attackDir, skillPos);

        if (activeData.isProjectile)
        {
            if (showDebugLogs) Debug.Log("🏹 [SkillController] 발사체 모드 진입");
            FireProjectile(activeData, skillInstance, finalDamage, slotIndex);
        }
        else
        {
            if (showDebugLogs) Debug.Log("💥 [SkillController] 즉발 AoE 모드 진입");
            SpawnInstantAOE(activeData, skillInstance, finalDamage, slotIndex);
        }

        // ⑤ 완료 처리 (이동 해제 이벤트 발행)
        CompleteSkillExecution(slotIndex);
    }

    /// <summary>
    /// 스킬 실행 완료 — 플래그 해제 + 이동 해제 이벤트 발행
    /// </summary>
    private void CompleteSkillExecution(int slotIndex)
    {
        IsSkillPendingExecution = false;
        activeSkillCoroutine = null;
        OnSkillExecutionComplete?.Invoke(slotIndex);

        if (showDebugLogs)
            Debug.Log($"✅ [SkillController] 스킬 실행 완료 — 슬롯 {slotIndex}, 이동 해제 이벤트 발행");
    }

    /// <summary>
    /// 스킬 실행 강제 취소 (피격/사망 시 PlayerAnimationController가 호출)
    /// 진행 중인 코루틴을 중단하고 이동 해제 이벤트를 강제 발행
    /// </summary>
    public void CancelSkillExecution()
    {
        if (!IsSkillPendingExecution) return;

        if (activeSkillCoroutine != null)
        {
            StopCoroutine(activeSkillCoroutine);
            activeSkillCoroutine = null;
        }

        IsSkillPendingExecution = false;
        OnSkillExecutionComplete?.Invoke(-1); // -1 = 취소

        if (showDebugLogs)
            Debug.Log("⚠️ [SkillController] 스킬 실행 취소 — 이동 해제 이벤트 강제 발행");
    }
    
    /// <summary>
    /// 발사 지점 찾기
    /// </summary>
    private Transform FindFirePoint()
    {
        // 무기의 FirePoint 찾기
        var activeWeapon = FindObjectOfType<ActiveWeapon>();
        if (activeWeapon != null)
        {
            Debug.Log($"🔍 [SkillController] ActiveWeapon 찾음: {activeWeapon.gameObject.name}");
            
            var weaponParent = activeWeapon.transform.Find("WeaponParent");
            if (weaponParent != null)
            {
                Debug.Log($"🔍 [SkillController] WeaponParent 찾음");
                
                var firePoint = weaponParent.Find("FirePoint");
                if (firePoint != null)
                {
                    Debug.Log($"✅ [SkillController] FirePoint 찾음: {firePoint.position}, Rotation: {firePoint.rotation.eulerAngles}");
                    return firePoint;
                }
                else
                {
                    Debug.LogWarning("⚠️ [SkillController] FirePoint 자식 없음 - WeaponParent 사용");
                    return weaponParent;
                }
            }
            else
            {
                Debug.LogWarning("⚠️ [SkillController] WeaponParent 없음 - ActiveWeapon 사용");
                return activeWeapon.transform;
            }
        }
        
        Debug.LogWarning("⚠️ [SkillController] ActiveWeapon 없음 - 플레이어 위치 사용");
        // Fallback: 플레이어 위치
        return transform;
    }
    
    /// <summary>
    /// Cast Effect VFX 발행 (castCueKey)
    /// </summary>
    private void EmitCastCue(ActiveSkillData skillData, Vector2 direction, Vector3 emitPos)
    {
        if (string.IsNullOrEmpty(skillData.castCueKey)) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        var ctx = new CueContext
        {
            position = emitPos,
            rotation = Quaternion.Euler(0f, 0f, angle),
            facingDir = direction,
            actorType = ActorType.Player,
            magnitude = 1.0f
        };
        CueEmitter.Emit(skillData.castCueKey, "Player", ctx);

        if (showDebugLogs)
            Debug.Log($"✨ [SkillController] Cast Cue: {skillData.castCueKey} ({skillData.skillType})");
    }

    /// <summary>
    /// AOE Effect VFX 발행 (aoeCueKey) — WaveClear 스킬에서만 사용
    /// </summary>
    private void EmitAOECue(ActiveSkillData skillData, Vector2 direction, Vector3 emitPos)
    {
        if (skillData.skillType != ActiveSkillType.WaveClear) return;
        if (string.IsNullOrEmpty(skillData.aoeCueKey)) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        var ctx = new CueContext
        {
            position = emitPos,
            rotation = Quaternion.Euler(0f, 0f, angle),
            facingDir = direction,
            actorType = ActorType.Player,
            magnitude = 2.0f
        };
        CueEmitter.Emit(skillData.aoeCueKey, "Player", ctx);

        if (showDebugLogs)
            Debug.Log($"✨ [SkillController] AOE Cue: {skillData.aoeCueKey}");
    }

    /// <summary>
    /// 조이스틱 공격 방향 가져오기 (마지막 방향 기억 기능 포함)
    /// WarriorSkill1/2와 동일한 패턴 사용
    /// </summary>
    private Vector2 GetAttackDirection()
    {
        var activeWeapon = FindObjectOfType<ActiveWeapon>();
        if (activeWeapon != null && activeWeapon.attackJoystickInput != null)
        {
            Vector2 joystickDir = activeWeapon.attackJoystickInput.GetAttackDirection();
            
            // ⭐ 조이스틱 입력이 있을 때만 마지막 방향 업데이트
            if (joystickDir.magnitude > 0.1f)
            {
                lastAttackDirection = joystickDir.normalized;
                
                if (showDebugLogs)
                    Debug.Log($"🎮 [SkillController] 조이스틱 방향 저장: {lastAttackDirection}");
                
                return lastAttackDirection;
            }
        }
        
        // ⭐ Fallback: 마지막 저장된 방향 사용
        if (showDebugLogs)
            Debug.Log($"🎯 [SkillController] 마지막 저장 방향 사용: {lastAttackDirection} (조이스틱 입력 없음)");
        
        return lastAttackDirection;
    }
    
    /// <summary>
    /// 백그라운드에서 마지막 공격 방향 지속적 업데이트 (기본공격과 동일한 방식)
    /// </summary>
    private IEnumerator UpdateLastAttackDirectionCoroutine()
    {
        while (true)
        {
            // ActiveWeapon에서 현재 조이스틱 방향 체크
            var activeWeapon = FindObjectOfType<ActiveWeapon>();
            if (activeWeapon != null && activeWeapon.attackJoystickInput != null)
            {
                Vector2 joystickDir = activeWeapon.attackJoystickInput.GetAttackDirection();
                
                // 조이스틱 입력이 있으면 마지막 방향 업데이트
                if (joystickDir.magnitude > 0.1f)
                {
                    lastAttackDirection = joystickDir.normalized;
                }
            }
            
            // 60FPS로 업데이트 (기본공격과 동일한 빈도)
            yield return new WaitForSeconds(1f / 60f);
        }
    }
    
    [ContextMenu("Log SkillSet Info")]
    public void LogSkillSetInfo()
    {
        if (skillSet != null)
        {
            skillSet.LogAllSkills();
        }
        else
        {
            Debug.LogWarning("❌ [SkillController] SkillSet이 null이어서 로그를 출력할 수 없습니다!");
        }
    }

    /// <summary>
    /// 스킬 할당 검증 코루틴 (약간의 딜레이 후 체크)
    /// </summary>
    private IEnumerator VerifySkillAssignmentRoutine()
    {
        yield return new WaitForSeconds(0.5f); // 다른 시스템 초기화 대기
        
        Debug.Log("🔍 [SkillController] 스킬 할당 검증 시작");
        
        if (skillSet == null)
        {
            Debug.LogError("❌ [SkillController] SkillSet이 null입니다!");
            yield break;
        }
        
        Debug.Log($"🔍 [SkillController] 현재 스킬 개수: {skillSet.SkillCount}");
        
        for (int i = 0; i < skillSet.SkillCount; i++)
        {
            var skill = skillSet.GetSkill(i);
            if (skill != null)
            {
                Debug.Log($"   ✅ 슬롯 {i}: {skill.SkillName} ({skill.GetType().Name})");
            }
            else
            {
                Debug.LogWarning($"   ❌ 슬롯 {i}: null");
            }
        }
        
        // 수동 재할당 시도
        if (skillSet.SkillCount == 0)
        {
            Debug.LogWarning("🟡 [SkillController] 스킬이 하나도 없습니다. 수동 재할당 시도...");
            TryManualSkillAssignment();
        }
    }

    /// <summary>
    /// 수동 스킬 할당 시도
    /// </summary>
    private void TryManualSkillAssignment()
    {
        var assasinSkill1 = GetComponent<AssasinSkill1>();
        var assasinSkill2 = GetComponent<AssasinSkill2>();
        var warriorSkill1 = GetComponent<WarriorSkill1>();
        var warriorSkill2 = GetComponent<WarriorSkill2>();
        
        int slot = 0;
        
        if (assasinSkill1 != null)
        {
            skillSet.SetSkill(slot++, assasinSkill1);
            Debug.Log($"🔧 [SkillController] 수동 할당: AssasinSkill1 → 슬롯 {slot-1}");
        }
        
        if (assasinSkill2 != null)
        {
            skillSet.SetSkill(slot++, assasinSkill2);
            Debug.Log($"🔧 [SkillController] 수동 할당: AssasinSkill2 → 슬롯 {slot-1}");
        }
        
        if (warriorSkill1 != null)
        {
            skillSet.SetSkill(slot++, warriorSkill1);
            Debug.Log($"🔧 [SkillController] 수동 할당: WarriorSkill1 → 슬롯 {slot-1}");
        }
        
        if (warriorSkill2 != null)
        {
            skillSet.SetSkill(slot++, warriorSkill2);
            Debug.Log($"🔧 [SkillController] 수동 할당: WarriorSkill2 → 슬롯 {slot-1}");
        }
    }
} 