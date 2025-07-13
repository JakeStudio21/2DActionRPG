using UnityEngine;

/// <summary>
/// Ghost 몬스터 설정 컴포넌트
/// ⭐ 모듈식 시스템 적용: EnemyAI + MultiShotRangedAttack 조합 사용
/// 기존 독립적인 공격 시스템을 제거하고 표준 모듈식 아키텍처로 전환
/// </summary>
public class Ghost : MonoBehaviour
{
    [Header("Ghost Settings")]
    [Tooltip("Ghost 전용 설정값들 (MultiShotRangedAttack에서 사용)")]
    [SerializeField] private bool showDebugInfo = true;
    
    [Header("Integration Status")]
    [SerializeField] private bool isModularSystemActive = false;
    
    private EnemyAI enemyAI;
    private MultiShotRangedAttack attackModule;
    
    private void Awake()
    {
        // 컴포넌트 참조 설정
        enemyAI = GetComponent<EnemyAI>();
        attackModule = GetComponent<MultiShotRangedAttack>();
    }
    
    private void Start()
    {
        // 모듈식 시스템 통합 확인
        CheckModularSystemIntegration();
        
        if (showDebugInfo)
        {
            Debug.Log($"[Ghost] {gameObject.name} - 모듈식 시스템으로 초기화 완료");
            Debug.Log($"[Ghost] EnemyAI: {(enemyAI != null ? "✓" : "✗")}");
            Debug.Log($"[Ghost] MultiShotRangedAttack: {(attackModule != null ? "✓" : "✗")}");
        }
    }
    
    /// <summary>
    /// 모듈식 시스템 통합 상태 확인
    /// </summary>
    private void CheckModularSystemIntegration()
    {
        bool hasEnemyAI = enemyAI != null;
        bool hasAttackModule = attackModule != null;
        
        isModularSystemActive = hasEnemyAI && hasAttackModule;
        
        if (!isModularSystemActive)
        {
            Debug.LogError($"[Ghost] {gameObject.name} - 모듈식 시스템 설정 불완전!");
            if (!hasEnemyAI) Debug.LogError("- EnemyAI 컴포넌트 누락");
            if (!hasAttackModule) Debug.LogError("- MultiShotRangedAttack 컴포넌트 누락");
        }
        else
        {
            Debug.Log($"[Ghost] {gameObject.name} - 모듈식 시스템 완벽 통합 ✓");
        }
    }
    
    /// <summary>
    /// Inspector에서 모듈식 시스템 상태를 시각적으로 확인
    /// </summary>
    private void OnValidate()
    {
        // 런타임이 아닐 때는 컴포넌트 참조만 확인
        if (!Application.isPlaying)
        {
            enemyAI = GetComponent<EnemyAI>();
            attackModule = GetComponent<MultiShotRangedAttack>();
            isModularSystemActive = enemyAI != null && attackModule != null;
        }
    }
    
    /// <summary>
    /// 디버그용 Gizmo - Ghost 특화 시각화
    /// </summary>
    private void OnDrawGizmos()
    {
        // Ghost 타입 표시
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.8f);
        
        // 모듈식 시스템 상태 표시
        if (isModularSystemActive)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 1.5f, Vector3.one * 0.3f);
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 1.5f, Vector3.one * 0.3f);
        }
        
        #if UNITY_EDITOR
        // 몬스터 타입 정보 표시
        if (Application.isPlaying)
        {
            string status = isModularSystemActive ? "모듈식 ✓" : "설정 오류 ✗";
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2, $"Ghost ({status})");
        }
        #endif
    }
} 