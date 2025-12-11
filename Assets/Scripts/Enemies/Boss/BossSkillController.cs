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
        
        // ⭐ 패턴별 스크립트 호출
        string skillName = currentSkillEntry.skillData.SkillName;
        
        switch (skillName)
        {
            case "Boss_Dash": // 스킬1: 돌진
                if (dashSkill != null)
                {
                    dashSkill.Execute(currentSkillEntry);
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
                    aoeSkill.Execute(currentSkillEntry);
                }
                else
                {
                    Debug.LogError($"[BossSkillController] BossAOESkill 컴포넌트가 없습니다!");
                }
                break;
            
            case "Boss_SpiralFire": // 스킬4: 나선형 멀티샷
                if (multiShotSkill != null)
                {
                    multiShotSkill.Execute(currentSkillEntry);
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
            // 플레이어 방향 계산
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Vector3 direction = Vector3.down;
            
            if (player != null)
            {
                direction = (player.transform.position - transform.position).normalized;
            }
            
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
    
    #region 유틸리티
    
    /// <summary>
    /// AOE 회전 계산
    /// </summary>
    private Quaternion CalculateAOERotation()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Vector3 direction = Vector3.down;
        
        if (player != null)
        {
            direction = (player.transform.position - transform.position).normalized;
        }
        
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        return Quaternion.Euler(0, 0, angle - 90f);
    }
    
    #endregion
}

