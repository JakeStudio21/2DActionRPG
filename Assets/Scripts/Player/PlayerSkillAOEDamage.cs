using UnityEngine;
using System.Collections.Generic;
using CueSystem;

/// <summary>
/// 플레이어 스킬 전용 AOE 데미지 처리 컴포넌트
/// SkillAOESpawner가 생성한 AOE GameObject에 자동으로 추가됨
/// 역할: 콜라이더 기반 자동 Enemy 감지 및 데미지 적용 + Hit Cue 발행
/// </summary>
public class PlayerSkillAOEDamage : MonoBehaviour
{
    #region Inspector 설정
    
    [Header("Damage Settings")]
    [SerializeField] private int damageAmount = 0; // 외부에서 SetDamage()로 설정
    [SerializeField] private LayerMask enemyLayerMask = 1 << 6; // Enemy layer (기본값 6)
    [SerializeField] private bool damageOnce = true; // 한 번만 데미지 (기본값 true)
    
    [Header("Cue System")]
    [SerializeField] private string hitCueEventKey = ""; // 예: "skill.warrior.skill1.hit"
    [SerializeField] private bool emitHitCue = true; // Hit Cue 발행 여부
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    #endregion
    
    #region Private Fields
    
    private HashSet<Collider2D> hitEnemies = new HashSet<Collider2D>(); // 중복 데미지 방지
    
    #endregion
    
    #region Unity Lifecycle
    
    private void OnEnable()
    {
        // 풀링 시스템 대응: 활성화될 때마다 초기화
        hitEnemies.Clear();
        
        if (showDebugLogs)
            Debug.Log($"[PlayerSkillAOEDamage] 활성화 - 데미지: {damageAmount}");
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// 데미지 설정
    /// </summary>
    public void SetDamage(int damage)
    {
        damageAmount = damage;
        
        if (showDebugLogs)
            Debug.Log($"[PlayerSkillAOEDamage] 데미지 설정: {damageAmount}");
    }
    
    /// <summary>
    /// Enemy LayerMask 설정
    /// </summary>
    public void SetEnemyLayerMask(LayerMask layerMask)
    {
        enemyLayerMask = layerMask;
        
        if (showDebugLogs)
            Debug.Log($"[PlayerSkillAOEDamage] Enemy LayerMask 설정: {layerMask.value}");
    }
    
    /// <summary>
    /// Hit Cue 이벤트 키 설정
    /// </summary>
    public void SetHitCueEventKey(string eventKey)
    {
        hitCueEventKey = eventKey;
        
        if (showDebugLogs)
            Debug.Log($"[PlayerSkillAOEDamage] Hit Cue 이벤트 키 설정: {hitCueEventKey}");
    }
    
    /// <summary>
    /// Hit Cue 발행 여부 설정
    /// </summary>
    public void SetEmitHitCue(bool emit)
    {
        emitHitCue = emit;
    }
    
    #endregion
    
    #region Collision Detection
    
    /// <summary>
    /// 트리거 진입 시 Enemy 감지 및 데미지 처리
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 한 번만 데미지이고 이미 맞은 적이면 스킵
        if (damageOnce && hitEnemies.Contains(other))
        {
            return;
        }
        
        // Enemy Layer 체크
        if (((1 << other.gameObject.layer) & enemyLayerMask) == 0)
        {
            return; // Enemy가 아니면 스킵
        }
        
        // EnemyHealth 컴포넌트 확인
        var enemyHealth = other.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            // 데미지 적용
            enemyHealth.TakeDamage(damageAmount);
            
            // 중복 데미지 방지용 추가
            hitEnemies.Add(other);
            
            // Hit Cue 발행
            if (emitHitCue && !string.IsNullOrEmpty(hitCueEventKey))
            {
                EmitHitCue(other.transform.position);
            }
            
            if (showDebugLogs)
                Debug.Log($"💥 [PlayerSkillAOEDamage] {other.name}에게 {damageAmount} 데미지!");
        }
    }
    
    #endregion
    
    #region Hit Cue System
    
    /// <summary>
    /// Hit Cue 발행 (몬스터 타격 이펙트)
    /// </summary>
    private void EmitHitCue(Vector3 hitPosition)
    {
        var context = new CueContext
        {
            position = hitPosition,
            rotation = Quaternion.identity,
            actorType = ActorType.Player,
            magnitude = 1.3f,
            isCritical = false,
            surfaceType = SurfaceType.Flesh // 몬스터 타격
        };
        
        bool cueSuccess = CueEmitter.Emit(hitCueEventKey, "Player", context);
        
        if (showDebugLogs)
            Debug.Log($"💥 [PlayerSkillAOEDamage] Hit Cue 발행 ({hitCueEventKey}, 위치: {hitPosition}) → {cueSuccess}");
    }
    
    #endregion
    
    #region Debug
    
    /// <summary>
    /// 디버그 정보 출력
    /// </summary>
    [ContextMenu("Debug PlayerSkillAOEDamage Info")]
    public void DebugInfo()
    {
        string info = $"=== PlayerSkillAOEDamage {gameObject.name} ===\n";
        info += $"Damage Amount: {damageAmount}\n";
        info += $"Enemy LayerMask: {enemyLayerMask.value}\n";
        info += $"Damage Once: {damageOnce}\n";
        info += $"Hit Cue Event Key: {hitCueEventKey}\n";
        info += $"Emit Hit Cue: {emitHitCue}\n";
        info += $"Hit Enemies Count: {hitEnemies.Count}\n";
        
        Debug.Log(info);
    }
    
    #endregion
}

