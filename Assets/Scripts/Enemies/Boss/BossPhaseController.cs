using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스 페이즈 관리 컨트롤러
/// HP 기반 페이즈 전환, 현재 페이즈 추적
/// </summary>
public class BossPhaseController : MonoBehaviour
{
    [Header("⚙️ 참조")]
    [SerializeField] private BaseEnemy baseEnemy;
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private EnemyAnimationController animController;
    
    [Header("📊 페이즈 데이터")]
    [Tooltip("보스의 모든 페이즈 목록 (순서대로)")]
    [SerializeField] private List<BossPhaseData> phases = new List<BossPhaseData>();
    
    [Header("🎯 현재 상태")]
    [SerializeField] private int currentPhaseIndex = 0;
    [SerializeField] private bool isTransitioning = false;
    [SerializeField] private bool isInvincible = false;
    
    [Header("🎮 디버그")]
    
    // 이벤트
    public System.Action<BossPhaseData> OnPhaseChanged;
    public System.Action OnPhaseTransitionStart;
    public System.Action OnPhaseTransitionComplete;
    
    // 프로퍼티
    public BossPhaseData CurrentPhase => currentPhaseIndex < phases.Count ? phases[currentPhaseIndex] : null;
    public int CurrentPhaseIndex => currentPhaseIndex;
    public bool IsTransitioning => isTransitioning;
    public bool IsInvincible => isInvincible;
    public int TotalPhases => phases.Count;
    
    private void Awake()
    {
        // 컴포넌트 자동 참조
        if (baseEnemy == null)
            baseEnemy = GetComponent<BaseEnemy>();
        
        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();
        
        if (animController == null)
            animController = GetComponent<EnemyAnimationController>();
    }
    
    private void Start()
    {
        // 페이즈 검증
        ValidatePhases();
        
        // 첫 페이즈 시작
        if (phases.Count > 0)
        {
            Dbg.Log($"🐲 [BossPhaseController] {gameObject.name}: 페이즈 시스템 시작!");
            Dbg.Log($"🐲 총 페이즈: {phases.Count}");
            Dbg.Log(CurrentPhase.GetDebugInfo());
        }
        else
        {
            Debug.LogError($"[BossPhaseController] {gameObject.name}: 페이즈 데이터가 없습니다!");
        }
    }
    
    private void Update()
    {
        // 전환 중이 아닐 때만 HP 체크
        if (!isTransitioning)
        {
            CheckPhaseTransition();
        }
    }
    
    /// <summary>
    /// 페이즈 전환 체크 (HP 기반)
    /// </summary>
    private void CheckPhaseTransition()
    {
        if (enemyHealth == null || CurrentPhase == null) return;
        
        // 현재 HP 비율 계산 (float 캐스팅으로 정수 나눗셈 방지!)
        float currentHpPercent = (float)enemyHealth.CurrentHealth / enemyHealth.MaxHealth;
        
        // 현재 페이즈 범위를 벗어났는지 확인
        if (!CurrentPhase.IsInHpRange(currentHpPercent))
        {
            // 다음 페이즈 찾기
            int nextPhaseIndex = FindNextPhase(currentHpPercent);
            
            if (nextPhaseIndex != -1 && nextPhaseIndex != currentPhaseIndex)
            {
                Dbg.Log($"🔄 [BossPhaseController] HP {currentHpPercent * 100:F1}% ({enemyHealth.CurrentHealth}/{enemyHealth.MaxHealth}) → 페이즈 전환 감지!");
                Dbg.Log($"   현재: {CurrentPhase.phaseName} → 다음: {phases[nextPhaseIndex].phaseName}");
                
                StartPhaseTransition(nextPhaseIndex);
            }
        }
    }
    
    /// <summary>
    /// 다음 페이즈 인덱스 찾기
    /// </summary>
    private int FindNextPhase(float currentHpPercent)
    {
        for (int i = 0; i < phases.Count; i++)
        {
            if (phases[i].IsInHpRange(currentHpPercent))
            {
                return i;
            }
        }
        
        // 범위에 맞는 페이즈가 없으면 현재 페이즈 유지
        return currentPhaseIndex;
    }
    
    /// <summary>
    /// 페이즈 전환 시작
    /// </summary>
    public void StartPhaseTransition(int nextPhaseIndex)
    {
        if (nextPhaseIndex < 0 || nextPhaseIndex >= phases.Count)
        {
            Debug.LogError($"[BossPhaseController] 잘못된 페이즈 인덱스: {nextPhaseIndex}");
            return;
        }
        
        if (isTransitioning)
        {
            return;
        }
        
        StartCoroutine(PhaseTransitionRoutine(nextPhaseIndex));
    }
    
    /// <summary>
    /// 페이즈 전환 코루틴
    /// </summary>
    private IEnumerator PhaseTransitionRoutine(int nextPhaseIndex)
    {
        isTransitioning = true;
        BossPhaseData nextPhase = phases[nextPhaseIndex];
        
        Dbg.Log($"🔄 [BossPhaseController] 페이즈 전환 시작: {CurrentPhase.phaseName} → {nextPhase.phaseName}");
        
        // 전환 시작 이벤트
        OnPhaseTransitionStart?.Invoke();
        
        // 무적 모드 활성화 (공격데미지 없음, 방어만)
        if (nextPhase.hasTransitionAnimation)
        {
            isInvincible = true;
            
            // 전환 애니메이션 트리거 (있다면)
            if (animController != null)
            {
                // PhaseTransition 트리거 (Animation Controller에 추가 필요)
                var animator = animController.GetComponent<Animator>();
                if (animator != null && animator.parameters != null)
                {
                    foreach (var param in animator.parameters)
                    {
                        if (param.name == "PhaseTransition")
                        {
                            animator.SetTrigger("PhaseTransition");
                            break;
                        }
                    }
                }
            }
            
            // 전환 이펙트 생성
            if (nextPhase.transitionEffect != null)
            {
                GameObject effect = Instantiate(nextPhase.transitionEffect, transform.position, Quaternion.identity);
                Destroy(effect, 3f);
            }
            
            // 전환 사운드 재생
            if (nextPhase.transitionSound != null)
            {
                AudioSource audioSource = GetComponent<AudioSource>();
                if (audioSource != null)
                {
                    audioSource.PlayOneShot(nextPhase.transitionSound);
                }
            }
            
            // 무적 시간 대기
            yield return new WaitForSeconds(nextPhase.transitionInvincibleTime);
            
            isInvincible = false;
        }
        
        // 페이즈 변경
        currentPhaseIndex = nextPhaseIndex;
        
        // 쿨다운 리셋 (옵션)
        if (nextPhase.resetCooldownsOnTransition)
        {
            ResetAllCooldowns();
        }
        
        Dbg.Log($"✅ [BossPhaseController] 페이즈 전환 완료: {nextPhase.phaseName}");
        Dbg.Log(nextPhase.GetDebugInfo());
        
        // 전환 완료 이벤트
        OnPhaseChanged?.Invoke(nextPhase);
        OnPhaseTransitionComplete?.Invoke();
        
        isTransitioning = false;
    }
    
    /// <summary>
    /// 모든 쿨다운 리셋 (BossAttackBehaviour에 알림)
    /// </summary>
    private void ResetAllCooldowns()
    {
        var bossAttack = GetComponent<BossAttackBehaviour>();
        if (bossAttack != null)
        {
            bossAttack.ResetAllCooldowns();
            
        }
    }
    
    /// <summary>
    /// 페이즈 데이터 검증
    /// </summary>
    private void ValidatePhases()
    {
        if (phases.Count == 0)
        {
            Debug.LogError($"[BossPhaseController] {gameObject.name}: 페이즈 데이터가 없습니다!");
            return;
        }
        
        // HP 범위가 겹치는지 검증
        for (int i = 0; i < phases.Count; i++)
        {
            for (int j = i + 1; j < phases.Count; j++)
            {
                if (PhasesOverlap(phases[i], phases[j]))
                {
                    Debug.LogWarning($"[BossPhaseController] 페이즈 HP 범위 겹침: {phases[i].phaseName} vs {phases[j].phaseName}");
                }
            }
        }
    }
    
    /// <summary>
    /// 두 페이즈의 HP 범위가 겹치는지 확인
    /// </summary>
    private bool PhasesOverlap(BossPhaseData phase1, BossPhaseData phase2)
    {
        return !(phase1.hpThresholdMax < phase2.hpThresholdMin || phase2.hpThresholdMax < phase1.hpThresholdMin);
    }
    
    /// <summary>
    /// 강제 페이즈 변경 (테스트용)
    /// </summary>
    [ContextMenu("Force Next Phase")]
    public void ForceNextPhase()
    {
        if (currentPhaseIndex < phases.Count - 1)
        {
            StartPhaseTransition(currentPhaseIndex + 1);
        }
        else
        {
        }
    }
    
    /// <summary>
    /// 디버그 정보 출력
    /// </summary>
    [ContextMenu("Debug Phase Info")]
    public void DebugPhaseInfo()
    {
        string info = $"=== Boss Phase Controller ===\n";
        info += $"Current Phase: {currentPhaseIndex + 1}/{phases.Count}\n";
        info += $"Is Transitioning: {isTransitioning}\n";
        info += $"Is Invincible: {isInvincible}\n\n";
        
        if (enemyHealth != null)
        {
            float hpPercent = enemyHealth.CurrentHealth / enemyHealth.MaxHealth;
            info += $"Current HP: {enemyHealth.CurrentHealth}/{enemyHealth.MaxHealth} ({hpPercent * 100:F1}%)\n\n";
        }
        
        if (CurrentPhase != null)
        {
            info += CurrentPhase.GetDebugInfo();
        }
        
    }
}


