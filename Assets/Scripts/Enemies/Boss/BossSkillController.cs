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
    
    [Header("🎯 스킬 패턴")]
    [SerializeField] private BossDashSkill dashSkill;
    [SerializeField] private BossAOESkill aoeSkill;
    [SerializeField] private BossMultiShotSkill multiShotSkill;
    
    [Header("🎯 현재 스킬 상태")]
    [SerializeField] private BossSkillEntry currentSkillEntry; // 스킬 + 스케일 정보
    [SerializeField] private bool isCasting = false;
    [SerializeField] private bool isActionExecuting = false;
    
    [Header("📍 텔레그래프")]
    private GameObject activeTelegraph;
    
    [Header("🎯 스킬 타겟 정보")]
    private Vector3 cachedTargetDirection; // Cast 시작 시점의 플레이어 방향 (싱크 맞춤용)
    private Vector3 cachedTargetPosition;  // Cast 시작 시점의 플레이어 위치
    
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
        
        // ⭐ 스킬 패턴 스크립트 자동 참조
        if (dashSkill == null)
            dashSkill = GetComponent<BossDashSkill>();
        
        if (aoeSkill == null)
            aoeSkill = GetComponent<BossAOESkill>();
        
        if (multiShotSkill == null)
            multiShotSkill = GetComponent<BossMultiShotSkill>();
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
        
        // Cast 시작 시점의 플레이어 방향/위치 저장 (Telegraph와 실제 스킬 싱크 맞춤)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            cachedTargetPosition = player.transform.position;
            cachedTargetDirection = (cachedTargetPosition - transform.position).normalized;
        }
        else
        {
            cachedTargetDirection = Vector3.down; // fallback
            cachedTargetPosition = transform.position + Vector3.down * 5f;
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
        
        // 스킬 이름에 따라 적절한 Action 트리거 호출
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
                        break;
                    
                    case "Boss_CircleAOE":
                        animator.SetTrigger("Skill2Action");
                        break;
                    
                    case "Boss_FanAOE":
                        animator.SetTrigger("Skill3Action");
                        break;
                    
                    case "Boss_SpiralFire":
                        animator.SetTrigger("Skill4Action");
                        break;
                    
                    default:
                        // 범용 SkillAction 사용
                        animController.TriggerSkillAction();
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
        
        // 패턴별 스크립트 호출
        string skillName = currentSkillEntry.skillData.SkillName;
        
        switch (skillName)
        {
            case "Boss_Dash": // 스킬1: 돌진
                RemoveTelegraph();
                
                if (dashSkill != null)
                {
                    dashSkill.Execute(currentSkillEntry, cachedTargetDirection);
                }
                else
                {
                    Debug.LogError($"[BossSkillController] BossDashSkill 컴포넌트가 없습니다!");
                }
                break;
            
            case "Boss_CircleAOE": // 스킬2: 원형 AOE
            case "Boss_FanAOE":    // 스킬3: 부채꼴 AOE
                if (aoeSkill != null)
                {
                    aoeSkill.Execute(currentSkillEntry, cachedTargetDirection);
                    RemoveTelegraph();
                }
                else
                {
                    Debug.LogError($"[BossSkillController] BossAOESkill 컴포넌트가 없습니다!");
                }
                break;
            
            case "Boss_SpiralFire": // 스킬4: 나선형 멀티샷
                RemoveTelegraph();
                
                if (multiShotSkill != null)
                {
                    multiShotSkill.Execute(currentSkillEntry, cachedTargetDirection);
                }
                else
                {
                    Debug.LogError($"[BossSkillController] BossMultiShotSkill 컴포넌트가 없습니다!");
                }
                break;
            
            default:
                Debug.LogWarning($"[BossSkillController] 알 수 없는 스킬: {skillName}");
                break;
        }
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
        
        // BossAttackBehaviour에 스킬 완료 알림 (전역 쿨다운 시작)
        var bossAttack = GetComponent<BossAttackBehaviour>();
        if (bossAttack != null)
        {
            bossAttack.OnSkillComplete();
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
        Quaternion rotation = CalculateTelegraphRotation();
        
        // ⭐ 이전 Telegraph가 있으면 제거
        if (activeTelegraph != null)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning($"⚠️ [BossSkillController] 이전 Telegraph 남아있음! 제거합니다: ID={activeTelegraph.GetInstanceID()}");
            }
            Destroy(activeTelegraph);
            activeTelegraph = null;
        }
        
        // Telegraph 생성 (위치 + 회전 적용)
        activeTelegraph = Instantiate(currentSkillEntry.skillData.TelegraphPrefab, spawnPosition, rotation);
        
        // Telegraph 설정 (Phase별 스케일 적용 + Center Mode 적용)
        var indicator = activeTelegraph.GetComponent<TelegraphIndicator>();
        if (indicator != null)
        {
            float duration = currentSkillEntry.skillData.TelegraphDuration;
            float scaleMultiplier = currentSkillEntry.skillScaleMultiplier;
            
            indicator.Initialize(currentSkillEntry.skillData, duration, scaleMultiplier);
            
            // Center Mode에 따라 Telegraph 위치 조정
            AdjustTelegraphPositionForCenterMode(activeTelegraph, currentSkillEntry.skillData, scaleMultiplier);
        }
        else
        {
            var indicatorMesh = activeTelegraph.GetComponent<TelegraphIndicatorMesh>();
            if (indicatorMesh != null)
            {
                float duration = currentSkillEntry.skillData.TelegraphDuration;
                float scaleMultiplier = currentSkillEntry.skillScaleMultiplier;
                
                indicatorMesh.Initialize(currentSkillEntry.skillData, duration, scaleMultiplier);
                
                // Center Mode에 따라 Telegraph 위치 조정
                AdjustTelegraphPositionForCenterMode(activeTelegraph, currentSkillEntry.skillData, scaleMultiplier);
            }
        }
    }
    
    /// <summary>
    /// Telegraph 위치 계산 (Origin 기준, Center Mode는 AdjustTelegraphPositionForCenterMode에서 처리)
    /// </summary>
    private Vector3 CalculateTelegraphPosition()
    {
        // Origin 위치 반환 (보스 중심)
        // ForwardAnchored 모드는 AdjustTelegraphPositionForCenterMode에서 조정
        return CalculateTelegraphOriginPosition();
    }
    
    /// <summary>
    /// Center Mode에 따라 Telegraph 위치 조정
    /// </summary>
    private void AdjustTelegraphPositionForCenterMode(GameObject telegraph, SkillData skillData, float scaleMultiplier)
    {
        if (telegraph == null || skillData == null) return;
        
        // ForwardAnchored 모드일 때만 위치 조정
        if (skillData.AoeCenterMode == AOECenterMode.ForwardAnchored)
        {
            // Center Offset 계산 (DamageArea와 동일한 로직)
            float centerOffset = skillData.AoeCenterOffset;
            if (centerOffset <= 0f)
            {
                // Fallback: AoeOffset 사용
                Vector2 aoeOffset = skillData.AoeOffset;
                centerOffset = aoeOffset.magnitude;
            }
            
            // 오프셋이 여전히 0이면 경고 후 종료
            if (centerOffset <= 0f)
            {
                Debug.LogWarning($"[BossSkillController] ⚠️ ForwardAnchored 모드인데 Center Offset이 0입니다! SkillData에서 AoeCenterOffset을 설정해주세요.");
                return;
            }
            
            // 스케일 적용된 오프셋 (DamageArea와 동일)
            float finalOffset = centerOffset * scaleMultiplier;
            
            // Forward 방향으로 위치 이동
            Vector3 forward = cachedTargetDirection.normalized;
            
            // Origin 위치를 기준으로 오프셋 적용 (보스 중심)
            Vector3 originPosition = CalculateTelegraphOriginPosition();
            Vector3 newPosition = originPosition + forward * finalOffset;
            
            telegraph.transform.position = newPosition;
        }
    }
    
    /// <summary>
    /// Telegraph Origin 위치 계산 (보스 중심 위치)
    /// </summary>
    private Vector3 CalculateTelegraphOriginPosition()
    {
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
    
    #region 유틸리티
    
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
    /// AOE 회전 계산
    /// </summary>
    private Quaternion CalculateAOERotation()
    {
        Vector3 direction = GetDirectionToPlayer();
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        return Quaternion.Euler(0, 0, angle - 90f);
    }
    
    /// <summary>
    /// Telegraph 회전 계산 (플레이어 방향)
    /// </summary>
    private Quaternion CalculateTelegraphRotation()
    {
        // ⭐ Cast 시작 시점에 저장한 방향 사용 (Telegraph와 실제 스킬 싱크 맞춤)
        Vector3 direction = cachedTargetDirection;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // 스킬별 회전 보정
        string skillName = currentSkillEntry?.skillData?.SkillName;
        
        switch (skillName)
        {
            case "Boss_Dash":
                // 돌진: 진행 방향 (스프라이트가 위쪽 향하는 경우 -90)
                return Quaternion.Euler(0, 0, angle);
            
            case "Boss_CircleAOE":
                // 원형: 회전 불필요 (원은 모든 방향 동일)
                return Quaternion.identity;
            
            case "Boss_FanAOE":
                // 부채꼴: 진행 방향
                return Quaternion.Euler(0, 0, angle);
            
            case "Boss_SpiralFire":
                // 나선형: 회전 불필요 (360도 발사)
                return Quaternion.identity;
            
            default:
                // 기본: 플레이어 방향
                return Quaternion.Euler(0, 0, angle);
        }
    }
    
    /// <summary>
    /// 아이소메트릭 거리 보정 계산 (EliteSkillController와 동일)
    /// </summary>
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
        return correctedDistance;
    }
    
    #endregion
}

