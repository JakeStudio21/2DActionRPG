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
    
    [Header("📐 위치 조정")]
    [Tooltip("AOE 중심점 오프셋 (Y값 음수로 발쪽 이동)")]
    [SerializeField] private Vector3 aoeOffset = new Vector3(0, -0.5f, 0);
    
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
        
        if (enableDebugLogs)
        {
            Debug.Log($"✨ [EliteSkillController] {gameObject.name}: Cast 이펙트 + Telegraph 생성");
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
    /// 스킬 액션 실행 (StateMachineBehaviour 콜백 - 데미지 판정)
    /// </summary>
    public void ExecuteSkillAction()
    {
        if (currentSkill == null) return;
        
        if (enableDebugLogs)
        {
            Debug.Log($"💥 [EliteSkillController] {gameObject.name}: {currentSkill.SkillName} 데미지 판정!");
        }
        
        // ⭐ Telegraph 제거 (데미지 판정 직전)
        RemoveTelegraph();
        
        // AOE 이펙트 생성
        SpawnAOEEffect();
        
        // AOE 데미지 판정
        PerformAOEDamage();
    }

    /// <summary>
    /// 스킬 액션 완료 (StateMachineBehaviour 콜백)
    /// </summary>
    public void OnSkillActionComplete()
    {
        if (currentSkill == null) return;
        
        isActionExecuting = false;
        
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
    /// Telegraph 생성
    /// </summary>
    private void SpawnTelegraph()
    {
        if (currentSkill == null || currentSkill.TelegraphPrefab == null) return;
        
        Vector3 spawnPosition = CalculateAOECenter();
        
        // Telegraph 생성
        activeTelegraph = Instantiate(currentSkill.TelegraphPrefab, spawnPosition, Quaternion.identity);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[EliteSkillController] Telegraph 생성: {activeTelegraph.name} at {spawnPosition}");
        }
        
        // Telegraph 설정 (두 가지 타입 지원)
        // 1. SpriteRenderer 기반 (TelegraphIndicator)
        var indicator = activeTelegraph.GetComponent<TelegraphIndicator>();
        if (indicator != null)
        {
            indicator.Initialize(currentSkill, currentSkill.TelegraphDuration);
            if (enableDebugLogs)
            {
                Debug.Log($"[EliteSkillController] TelegraphIndicator 초기화 완료");
            }
        }
        else
        {
            // 2. Procedural Mesh 기반 (TelegraphIndicatorMesh)
            var indicatorMesh = activeTelegraph.GetComponent<TelegraphIndicatorMesh>();
            if (indicatorMesh != null)
            {
                indicatorMesh.Initialize(currentSkill, currentSkill.TelegraphDuration);
                if (enableDebugLogs)
                {
                    Debug.Log($"[EliteSkillController] TelegraphIndicatorMesh 초기화 완료");
                }
            }
            else
            {
                Debug.LogWarning($"[EliteSkillController] Telegraph에 TelegraphIndicator 또는 TelegraphIndicatorMesh 컴포넌트가 없습니다!");
            }
        }
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
    /// AOE 이펙트 생성
    /// </summary>
    private void SpawnAOEEffect()
    {
        if (currentSkill == null || currentSkill.AoeEffect == null) return;
        
        Vector3 spawnPosition = CalculateAOECenter();
        Quaternion rotation = CalculateAOERotation();
        
        GameObject effect = Instantiate(currentSkill.AoeEffect, spawnPosition, rotation);
        Destroy(effect, 2f);
    }

    #endregion

    #region AOE 데미지 판정

    /// <summary>
    /// AOE 데미지 판정
    /// </summary>
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
    /// 플레이어에게 데미지 적용
    /// </summary>
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
    /// 부채꼴 범위 충돌 감지
    /// </summary>
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

    #region 유틸리티

    /// <summary>
    /// AOE 중심점 계산
    /// </summary>
    private Vector3 CalculateAOECenter()
    {
        Vector3 basePosition = transform.position;
        
        // ⭐ 몬스터별 기본 오프셋 적용 (발 위치 등)
        basePosition += aoeOffset;
        
        // 스킬별 추가 오프셋 적용 (전방향 등)
        if (currentSkill != null && currentSkill.AoeOffset != Vector2.zero)
        {
            Vector3 direction = GetDirectionToPlayer();
            basePosition += direction * currentSkill.AoeOffset.y;
            basePosition += Vector3.Cross(direction, Vector3.forward) * currentSkill.AoeOffset.x;
        }
        
        return basePosition;
    }

    /// <summary>
    /// AOE 회전 계산
    /// </summary>
    private Quaternion CalculateAOERotation()
    {
        Vector3 direction = GetDirectionToPlayer();
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        return Quaternion.Euler(0, 0, angle - 90f);
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

