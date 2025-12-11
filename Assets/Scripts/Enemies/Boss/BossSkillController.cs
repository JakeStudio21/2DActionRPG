using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스 몬스터 스킬 컨트롤러
/// 페이즈별 스킬 실행, 쿨다운 관리, Cast → Action 단계 제어
/// EliteSkillController를 확장하여 페이즈별 스킬 스케일링 지원
/// </summary>
public class BossSkillController : MonoBehaviour
{
    [Header("⚙️ 참조")]
    [SerializeField] private BaseEnemy baseEnemy;
    [SerializeField] private EnemyAnimationController animController;
    [SerializeField] private BossPhaseController phaseController;
    
    [Header("🎯 현재 스킬 상태")]
    [SerializeField] private BossSkillEntry currentSkillEntry; // 스킬 + 스케일 정보
    [SerializeField] private bool isCasting = false;
    [SerializeField] private bool isActionExecuting = false;
    
    [Header("📍 텔레그래프")]
    private GameObject activeTelegraph;
    
    [Header("📐 위치 조정")]
    [Tooltip("스킬 스폰 위치 - 프리팹 내부 Transform")]
    [SerializeField] private Transform skillSpawnPoint;
    
    [Header("🎮 디버그")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // 프로퍼티
    public bool IsCasting => isCasting;
    public bool IsActionExecuting => isActionExecuting;
    public SkillData CurrentSkill => currentSkillEntry?.skillData;
    
    private void Awake()
    {
        // 컴포넌트 자동 참조
        if (baseEnemy == null)
            baseEnemy = GetComponent<BaseEnemy>();
        
        if (animController == null)
            animController = GetComponent<EnemyAnimationController>();
        
        if (phaseController == null)
            phaseController = GetComponent<BossPhaseController>();
    }
    
    private void Start()
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[BossSkillController] {gameObject.name} 초기화 완료");
        }
    }
    
    /// <summary>
    /// 스킬 캐스팅 시작 (BossSkillEntry로 받음 - 스케일 정보 포함)
    /// </summary>
    public void StartSkillCast(BossSkillEntry skillEntry)
    {
        if (skillEntry == null || skillEntry.skillData == null)
        {
            if (enableDebugLogs)
            {
                Debug.LogError($"[BossSkillController] SkillEntry 또는 SkillData가 null!");
            }
            return;
        }
        
        if (isCasting || isActionExecuting)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning($"[BossSkillController] 이미 스킬 실행 중!");
            }
            return;
        }
        
        currentSkillEntry = skillEntry;
        isCasting = true;
        
        if (enableDebugLogs)
        {
            Debug.Log($"🔮 [BossSkillController] {gameObject.name}: {skillEntry.skillData.SkillName} 캐스팅 시작!");
            Debug.Log($"   스킬 스케일: {skillEntry.skillScaleMultiplier}x");
        }
        
        // 애니메이션 트리거
        if (animController != null)
        {
            animController.TriggerSkillCast();
            
            if (enableDebugLogs)
            {
                Debug.Log($"✅ [BossSkillController] SkillCast 트리거 실행!");
            }
        }
        else
        {
            Debug.LogError($"❌ [BossSkillController] EnemyAnimationController가 없습니다!");
        }
    }
    
    /// <summary>
    /// 스킬 캐스트 시작 (StateMachineBehaviour 콜백)
    /// </summary>
    public void OnSkillCastStart()
    {
        if (currentSkillEntry == null || currentSkillEntry.skillData == null) return;
        
        if (enableDebugLogs)
        {
            Debug.Log($"✨ [BossSkillController] {gameObject.name}: Cast 이펙트 + Telegraph 생성");
        }
        
        // Cast 이펙트 생성
        SpawnCastEffect();
        
        // Telegraph 생성 (경고 표시)
        SpawnTelegraph();
        
        // ⭐ Cast Time 후 자동으로 Action 트리거 (StateMachineBehaviour Exit 대신)
        StartCoroutine(AutoTriggerSkillActionAfterCastTime());
    }
    
    /// <summary>
    /// Cast Time 후 자동으로 SkillAction 트리거
    /// </summary>
    private IEnumerator AutoTriggerSkillActionAfterCastTime()
    {
        if (currentSkillEntry?.skillData == null) yield break;
        
        float castTime = currentSkillEntry.skillData.CastTime;
        
        if (enableDebugLogs)
        {
            Debug.Log($"⏱️ [BossSkillController] {castTime}초 후 Action 트리거 예약...");
        }
        
        yield return new WaitForSeconds(castTime);
        
        // Cast 완료 처리
        OnSkillCastComplete();
    }
    
    /// <summary>
    /// 스킬 캐스트 완료 (StateMachineBehaviour 콜백)
    /// </summary>
    public void OnSkillCastComplete()
    {
        if (currentSkillEntry == null || currentSkillEntry.skillData == null) return;
        
        isCasting = false;
        isActionExecuting = true;
        
        if (enableDebugLogs)
        {
            Debug.Log($"🎯 [BossSkillController] {gameObject.name}: Cast 완료 → Action 단계");
        }
        
        // ⭐ 스킬 이름에 따라 적절한 Action 트리거 호출
        if (animController != null)
        {
            string skillName = currentSkillEntry.skillData.SkillName;
            var animator = animController.GetComponent<Animator>();
            
            if (animator != null)
            {
                // 스킬별 트리거 선택
                switch (skillName)
                {
                    case "Boss_Dash":
                        animator.SetTrigger("Skill1Action");
                        if (enableDebugLogs)
                        {
                            Debug.Log($"✅ [BossSkillController] Skill1Action 트리거 실행!");
                        }
                        break;
                    
                    case "Boss_CircleAOE":
                        animator.SetTrigger("Skill2Action");
                        if (enableDebugLogs)
                        {
                            Debug.Log($"✅ [BossSkillController] Skill2Action 트리거 실행!");
                        }
                        break;
                    
                    case "Boss_FanAOE":
                        animator.SetTrigger("Skill3Action");
                        if (enableDebugLogs)
                        {
                            Debug.Log($"✅ [BossSkillController] Skill3Action 트리거 실행!");
                        }
                        break;
                    
                    case "Boss_SpiralFire":
                        animator.SetTrigger("Skill4Action");
                        if (enableDebugLogs)
                        {
                            Debug.Log($"✅ [BossSkillController] Skill4Action 트리거 실행!");
                        }
                        break;
                    
                    default:
                        // 범용 SkillAction 사용
                        animController.TriggerSkillAction();
                        if (enableDebugLogs)
                        {
                            Debug.LogWarning($"⚠️ [BossSkillController] 알 수 없는 스킬: {skillName}, 범용 SkillAction 사용");
                        }
                        break;
                }
                
                // isSkillCasting = false 설정
                animator.SetBool("isSkillCasting", false);
            }
            else
            {
                Debug.LogError($"❌ [BossSkillController] Animator를 찾을 수 없습니다!");
            }
        }
    }
    
    /// <summary>
    /// 스킬 액션 실행 (StateMachineBehaviour 콜백 - 데미지 판정)
    /// </summary>
    public void ExecuteSkillAction()
    {
        if (currentSkillEntry == null || currentSkillEntry.skillData == null) return;
        
        if (enableDebugLogs)
        {
            Debug.Log($"💥 [BossSkillController] {gameObject.name}: {currentSkillEntry.skillData.SkillName} 실행!");
        }
        
        // ⭐ Telegraph 제거 (데미지 판정 직전)
        RemoveTelegraph();
        
        // 스킬 타입별 실행
        switch (currentSkillEntry.skillData.SkillName)
        {
            case "Boss_Dash": // 스킬1: 돌진
                ExecuteDashSkill();
                break;
            
            default:
                // 기본 AOE 스킬
                ExecuteDefaultAOESkill();
                break;
        }
    }
    
    /// <summary>
    /// 돌진 스킬 실행 (스킬1)
    /// </summary>
    private void ExecuteDashSkill()
    {
        if (enableDebugLogs)
        {
            Debug.Log($"🏃 [BossSkillController] 돌진 스킬 실행!");
        }
        
        // 돌진 코루틴 시작
        StartCoroutine(DashSkillRoutine());
    }
    
    /// <summary>
    /// 돌진 스킬 코루틴
    /// </summary>
    private IEnumerator DashSkillRoutine()
    {
        SkillData skill = currentSkillEntry.skillData;
        float scaleMultiplier = currentSkillEntry.skillScaleMultiplier;
        
        // 플레이어 방향 계산
        Vector3 direction = GetDirectionToPlayer();
        Vector3 startPosition = transform.position;
        
        // 돌진 거리 (스킬 데이터의 AoeRadius 사용)
        float dashDistance = skill.AoeRadius * scaleMultiplier;
        Vector3 targetPosition = startPosition + (direction * dashDistance);
        
        // 돌진 속도 계산 (페이즈별 속도 증가)
        // 페이즈1: 기본, 페이즈2: 1.5배, 페이즈3: 2배
        float baseDashSpeed = 8f;
        float dashSpeed = baseDashSpeed * scaleMultiplier;
        
        if (enableDebugLogs)
        {
            Debug.Log($"🏃 돌진 시작: {startPosition} → {targetPosition}");
            Debug.Log($"   거리: {dashDistance:F1}f, 속도: {dashSpeed:F1}f/s, 스케일: {scaleMultiplier}x");
        }
        
        // 돌진 중 프레임 업데이트
        float elapsedTime = 0f;
        float dashDuration = dashDistance / dashSpeed;
        
        while (elapsedTime < dashDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / dashDuration;
            
            // 선형 이동
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            
            // 돌진 경로에 사각 AOE 데미지 판정 (Phase 2, 3만)
            if (scaleMultiplier > 1.0f) // Phase 2, 3
            {
                CheckDashPathDamage();
            }
            
            yield return null;
        }
        
        // 최종 위치 보정
        transform.position = targetPosition;
        
        if (enableDebugLogs)
        {
            Debug.Log($"🏁 돌진 완료: {transform.position}");
        }
        
        // AOE 이펙트 생성
        SpawnAOEEffect();
    }
    
    /// <summary>
    /// 돌진 경로 데미지 체크 (사각 AOE)
    /// </summary>
    private void CheckDashPathDamage()
    {
        SkillData skill = currentSkillEntry.skillData;
        float scaleMultiplier = currentSkillEntry.skillScaleMultiplier;
        
        // 사각 AOE 크기 (스킬 데이터의 AoeSize 사용)
        Vector2 boxSize = skill.AoeSize * scaleMultiplier;
        
        // 보스 중심에서 사각 충돌 체크
        Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, boxSize, 0f, LayerMask.GetMask("Player"));
        
        if (hits.Length > 0)
        {
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    ApplyDamageToPlayer(hit.gameObject);
                    break; // 플레이어는 한 번만
                }
            }
        }
    }
    
    /// <summary>
    /// 기본 AOE 스킬 실행 (스킬2, 3, 4용)
    /// </summary>
    private void ExecuteDefaultAOESkill()
    {
        if (enableDebugLogs)
        {
            Debug.Log($"💥 [BossSkillController] 기본 AOE 스킬 실행");
        }
        
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
        if (currentSkillEntry == null || currentSkillEntry.skillData == null) return;
        
        isActionExecuting = false;
        
        // ⭐ Animator 파라미터 업데이트
        if (animController != null)
        {
            animController.SetSkillAction(false);
            
            var animator = animController.GetComponent<Animator>();
            if (animator != null)
            {
                animator.ResetTrigger("Attack");
            }
        }
        
        // ⭐ BossAttackBehaviour에 스킬 완료 알림 (전역 쿨다운 시작)
        var bossAttack = GetComponent<BossAttackBehaviour>();
        if (bossAttack != null)
        {
            bossAttack.OnSkillComplete();
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"✅ [BossSkillController] {gameObject.name}: {currentSkillEntry.skillData.SkillName} 완료!");
        }
        
        currentSkillEntry = null;
    }
    
    #region 이펙트 생성
    
    /// <summary>
    /// Cast 이펙트 생성
    /// </summary>
    private void SpawnCastEffect()
    {
        if (currentSkillEntry?.skillData == null || currentSkillEntry.skillData.CastEffect == null) return;
        
        GameObject effect = Instantiate(currentSkillEntry.skillData.CastEffect, transform.position, Quaternion.identity);
        Destroy(effect, currentSkillEntry.skillData.CastTime);
    }
    
    /// <summary>
    /// Telegraph 생성
    /// </summary>
    private void SpawnTelegraph()
    {
        if (currentSkillEntry?.skillData == null || currentSkillEntry.skillData.TelegraphPrefab == null) return;
        
        Vector3 spawnPosition = CalculateTelegraphPosition();
        
        // Telegraph 생성
        activeTelegraph = Instantiate(currentSkillEntry.skillData.TelegraphPrefab, spawnPosition, Quaternion.identity);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[BossSkillController] Telegraph 생성: {activeTelegraph.name} at {spawnPosition}");
        }
        
        // Telegraph 설정
        var indicator = activeTelegraph.GetComponent<TelegraphIndicator>();
        if (indicator != null)
        {
            float duration = currentSkillEntry.skillData.TelegraphDuration;
            indicator.Initialize(currentSkillEntry.skillData, duration);
        }
        else
        {
            var indicatorMesh = activeTelegraph.GetComponent<TelegraphIndicatorMesh>();
            if (indicatorMesh != null)
            {
                float duration = currentSkillEntry.skillData.TelegraphDuration;
                indicatorMesh.Initialize(currentSkillEntry.skillData, duration);
            }
        }
    }
    
    /// <summary>
    /// Telegraph 위치 계산
    /// </summary>
    private Vector3 CalculateTelegraphPosition()
    {
        // 돌진 스킬은 플레이어 방향으로 표시
        if (currentSkillEntry.skillData.SkillName == "Boss_Dash")
        {
            Vector3 direction = GetDirectionToPlayer();
            float distance = currentSkillEntry.skillData.AoeRadius * currentSkillEntry.skillScaleMultiplier;
            return transform.position + (direction * distance * 0.5f); // 중간 지점
        }
        
        // 기본은 보스 중심
        return transform.position;
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
        if (currentSkillEntry?.skillData == null || currentSkillEntry.skillData.AoeEffect == null) return;
        
        Vector3 spawnPosition = transform.position;
        Quaternion rotation = CalculateAOERotation();
        
        GameObject effect = Instantiate(currentSkillEntry.skillData.AoeEffect, spawnPosition, rotation);
        Destroy(effect, 2f);
    }
    
    #endregion
    
    #region AOE 데미지 판정
    
    /// <summary>
    /// AOE 데미지 판정
    /// </summary>
    private void PerformAOEDamage()
    {
        if (currentSkillEntry?.skillData == null) return;
        
        SkillData skill = currentSkillEntry.skillData;
        float scaleMultiplier = currentSkillEntry.skillScaleMultiplier;
        
        Vector3 center = transform.position;
        Vector3 direction = GetDirectionToPlayer();
        
        Collider2D[] hits = null;
        
        switch (skill.AoeShape)
        {
            case AOEShapeType.Circle:
                float radius = skill.AoeRadius * scaleMultiplier;
                hits = Physics2D.OverlapCircleAll(center, radius, LayerMask.GetMask("Player"));
                break;
                
            case AOEShapeType.Triangle: // Fan
                float fanRadius = skill.AoeRadius * scaleMultiplier;
                hits = GetFanHits(center, direction, fanRadius, skill.AoeAngle);
                break;
                
            case AOEShapeType.Rectangle:
                Vector2 size = skill.AoeSize * scaleMultiplier;
                hits = Physics2D.OverlapBoxAll(center, size, GetAngleToPlayer(), LayerMask.GetMask("Player"));
                break;
        }
        
        if (hits == null || hits.Length == 0)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[BossSkillController] AOE 데미지 대상 없음");
            }
            return;
        }
        
        // 플레이어에게 데미지 적용
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                ApplyDamageToPlayer(hit.gameObject);
                break;
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
        
        // 기본 데미지 획득
        int baseDamage = 10;
        
        if (baseEnemy != null)
        {
            var meleeAttack = baseEnemy.GetComponent<MeleeAttack>();
            if (meleeAttack != null && meleeAttack.AttackData != null)
            {
                baseDamage = meleeAttack.GetScaledDamage();
            }
        }
        
        // 스킬 데미지 계산 (기본 데미지 × 스킬 배율 × 페이즈 스케일)
        SkillData skill = currentSkillEntry.skillData;
        float totalMultiplier = skill.DamageMultiplier * currentSkillEntry.skillScaleMultiplier;
        int skillDamage = Mathf.RoundToInt(baseDamage * totalMultiplier);
        
        // 플레이어에게 데미지 적용
        playerHealth.TakeDamage(skillDamage, transform);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[BossSkillController] 플레이어 피격: {skillDamage} 데미지");
            Debug.Log($"   기본: {baseDamage}, 스킬 배율: {skill.DamageMultiplier}x, 페이즈 스케일: {currentSkillEntry.skillScaleMultiplier}x");
        }
        
        // Hit 이펙트
        if (skill.HitEffect != null)
        {
            GameObject hitEffect = Instantiate(skill.HitEffect, player.transform.position, Quaternion.identity);
            Destroy(hitEffect, 2f);
        }
        
        // Screen Shake
        TriggerScreenShake(skill.ShakeIntensity);
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
}

