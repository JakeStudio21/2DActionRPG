using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CueSystem; // ⭐ Phase 1-1: 히트 이펙트 Cue 시스템

/// <summary>
/// 🎯 DamageSource - PlayerRuntimeStats 기반 데미지 처리
/// 책임: PlayerRuntimeStats에서 최종 스탯을 가져와 적에게 데미지 적용
/// 개선: 실시간 무기 데미지 반영, 클래스별 특수 효과 통합 관리
/// </summary>
public class DamageSource : MonoBehaviour
{
    [Header("🔗 컴포넌트 참조")]
    [SerializeField] private bool showDebugLogs = true; // ⭐ 벽 차단 테스트를 위해 true로 설정
    
    [Header("🧱 벽 충돌 설정")]
    [SerializeField] private bool checkWallBlocking = true;
    [SerializeField] private LayerMask wallLayer; // Inspector에서 Wall 선택
    
    // 핵심 참조
    private PlayerRuntimeStats playerRuntimeStats;
    private Warrior warrior;
    private Assasin assasin;
    
    private void Start() 
    {
        InitializeReferences();
    }
    
    /// <summary>
    /// 🔗 필수 참조들 초기화
    /// </summary>
    private void InitializeReferences()
    {
        // PlayerRuntimeStats 참조 (최우선)
        playerRuntimeStats = GetComponentInParent<PlayerRuntimeStats>();
        if (playerRuntimeStats == null)
        {
            playerRuntimeStats = FindObjectOfType<PlayerRuntimeStats>();
        }
        
        // 클래스별 컴포넌트 참조 (특수 효과용)
        warrior = FindObjectOfType<Warrior>();
        assasin = FindObjectOfType<Assasin>();
        
        if (showDebugLogs)
        {
            Debug.Log($"🔗 [DamageSource] 참조 초기화:");
            Debug.Log($"   - PlayerRuntimeStats: {(playerRuntimeStats != null ? "연결됨" : "❌ 없음")}");
            Debug.Log($"   - Warrior: {(warrior != null ? "감지됨" : "없음")}");
            Debug.Log($"   - Assasin: {(assasin != null ? "감지됨" : "없음")}");
            
            if (playerRuntimeStats != null)
            {
                Debug.Log($"📊 [DamageSource] 현재 스탯: 공격력 {playerRuntimeStats.FinalAttackDamage:F1}");
            }
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other) 
    {
        EnemyHealth enemyHealth = other.gameObject.GetComponent<EnemyHealth>();
        if (enemyHealth == null) return;
        
        // 🧱 벽 차단 체크
        if (checkWallBlocking && IsBlockedByWall(other.transform))
        {
            if (showDebugLogs)
                Debug.Log($"🚫 [DamageSource] {other.name} - 벽에 막혀서 공격 실패");
            return;
        }
        
        // 🎯 PlayerRuntimeStats에서 실시간 최종 데미지 가져오기
        float baseDamage = GetCurrentBaseDamage();
        float finalDamage = baseDamage;
        
        if (showDebugLogs)
            Debug.Log($"🎯 [DamageSource] 데미지 계산 시작 - 기본 데미지: {baseDamage:F1}");
        
        // 클래스별 특수 효과 적용
        finalDamage = ApplyClassSpecialEffects(finalDamage, baseDamage, other);
        
        // 최종 데미지 적용
        int roundedDamage = Mathf.RoundToInt(finalDamage);
        enemyHealth.TakeDamage(roundedDamage);
        
        // ⭐ Phase 1-1: 히트 이펙트 Cue 발행
        EmitHitEffectCue(other.transform.position);
        
        if (showDebugLogs)
            Debug.Log($"💥 [DamageSource] 최종 데미지: {roundedDamage} → {other.name}");
    }
    
    /// <summary>
    /// 🎯 현재 기본 데미지 가져오기 (PlayerRuntimeStats 우선)
    /// </summary>
    private float GetCurrentBaseDamage()
    {
        if (playerRuntimeStats != null)
        {
            return playerRuntimeStats.FinalAttackDamage;
        }
        
        // Fallback: 기존 방식 (PlayerRuntimeStats가 없을 때만)
        Debug.LogWarning("⚠️ [DamageSource] PlayerRuntimeStats가 없어 Fallback 방식 사용");
        
        var activeWeapon = FindObjectOfType<ActiveWeapon>();
        if (activeWeapon?.CurrentActiveWeapon != null)
        {
            var weaponComponent = activeWeapon.CurrentActiveWeapon as IWeapon;
            if (weaponComponent != null)
            {
                var equipmentData = weaponComponent.GetEquipmentData();
                return equipmentData?.attackDamage ?? 10f;
            }
        }
        
        return 10f; // 최종 기본값
    }
    
    /// <summary>
    /// 🎭 클래스별 특수 효과 적용
    /// </summary>
    private float ApplyClassSpecialEffects(float currentDamage, float baseDamage, Collider2D target)
    {
        float modifiedDamage = currentDamage;
        
        // 🔥 Warrior 특수 효과 (버서커 모드, 반격 등)
        if (warrior != null && warrior.IsActiveClass)
        {
            modifiedDamage = ApplyWarriorEffects(modifiedDamage, baseDamage);
        }
        
        // 🏹 Assasin 특수 효과 (크리티컬, 백어택 등)
        if (assasin != null && assasin.IsActiveClass)
        {
            modifiedDamage = ApplyAssasinEffects(modifiedDamage, baseDamage, target);
        }
        
        return modifiedDamage;
    }
    
    /// <summary>
    /// ⚔️ Warrior 특수 효과 적용
    /// </summary>
    private float ApplyWarriorEffects(float currentDamage, float baseDamage)
    {
        try
        {
            float warriorDamage = warrior.GetModifiedDamage(baseDamage);
            
            if (showDebugLogs)
            {
                Debug.Log($"⚔️ [DamageSource] Warrior 효과 적용: {currentDamage:F1} → {warriorDamage:F1}");
                
                if (warrior.IsInBerserkerMode())
                {
                    Debug.Log($"🔥 [DamageSource] 버서커 모드 활성! 추가 데미지 보너스");
                }
            }
            
            return warriorDamage;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"🟡 [DamageSource] Warrior 효과 적용 실패: {ex.Message}");
            return currentDamage; // 원래 값 반환
        }
    }
    
    /// <summary>
    /// 🏹 Assasin 특수 효과 적용
    /// </summary>
    private float ApplyAssasinEffects(float currentDamage, float baseDamage, Collider2D target)
    {
        try
        {
            // 기본 Assasin 데미지 계산 (크리티컬 포함)
            float assasinDamage = assasin.GetModifiedDamage(baseDamage);
            
            // 백어택 보너스 추가 계산
            float backAttackMultiplier = assasin.GetBackAttackMultiplier(
                transform.position, 
                target.transform.position, 
                target.transform.right // 적의 방향
            );
            
            float finalAssasinDamage = assasinDamage * backAttackMultiplier;
            
            if (showDebugLogs)
            {
                Debug.Log($"🏹 [DamageSource] Assasin 효과 적용:");
                Debug.Log($"   - 기본 → 크리티컬: {baseDamage:F1} → {assasinDamage:F1}");
                Debug.Log($"   - 백어택 배율: x{backAttackMultiplier:F2}");
                Debug.Log($"   - 최종 데미지: {finalAssasinDamage:F1}");
            }
            
            return finalAssasinDamage;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"🟡 [DamageSource] Assasin 효과 적용 실패: {ex.Message}");
            return currentDamage; // 원래 값 반환
        }
    }
    
    /// <summary>
    /// 🔧 디버그용: 현재 데미지 정보 출력
    /// </summary>
    [ContextMenu("Print Current Damage Info")]
    private void PrintCurrentDamageInfo()
    {
        if (playerRuntimeStats != null)
        {
            Debug.Log($"📊 [DamageSource] 현재 데미지 정보:");
            Debug.Log($"   - 최종 공격력: {playerRuntimeStats.FinalAttackDamage:F1}");
            Debug.Log($"   - 크리티컬 확률: {playerRuntimeStats.FinalCriticalChance:P1}");
            Debug.Log($"   - 크리티컬 배율: x{playerRuntimeStats.FinalCriticalDamage:F1}");
        }
        else
        {
            Debug.LogWarning("⚠️ [DamageSource] PlayerRuntimeStats를 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// 🔄 PlayerRuntimeStats 참조 새로고침 (런타임 중 필요시)
    /// </summary>
    public void RefreshReferences()
    {
        InitializeReferences();
    }
    
    #region ⭐ Phase 1-1: 히트 이펙트 시스템
    
    /// <summary>
    /// 💥 히트 이펙트 Cue 발행 (등급별)
    /// </summary>
    private void EmitHitEffectCue(Vector3 hitPosition)
    {
        // 등급별 히트 이벤트 키 생성
        string eventKey = GetHitEffectEventKey();
        float magnitude = GetHitMagnitudeByGrade();
        
        var context = new CueContext
        {
            position = hitPosition,
            rotation = transform.rotation,
            actorType = ActorType.Player,
            magnitude = magnitude,
            surfaceType = SurfaceType.Default
        };
        
        bool cueSuccess = CueEmitter.Emit(eventKey, "Player", context);
        
        if (showDebugLogs)
            Debug.Log($"💥 [DamageSource] Hit Cue 발행: {eventKey} (강도: {magnitude}) → {cueSuccess}");
    }
    
    /// <summary>
    /// 무기 등급에 따른 히트 이벤트 키 생성
    /// </summary>
    private string GetHitEffectEventKey()
    {
        var activeWeapon = FindObjectOfType<ActiveWeapon>();
        if (activeWeapon?.CurrentActiveWeapon == null)
        {
            return "hit.player.normal"; // 기본값
        }
        
        var weaponComponent = activeWeapon.CurrentActiveWeapon as IWeapon;
        if (weaponComponent == null)
        {
            return "hit.player.normal";
        }
        
        var equipmentData = weaponComponent.GetEquipmentData();
        if (equipmentData == null)
        {
            return "hit.player.normal";
        }
        
        // 무기 타입 확인 (근접 vs 원거리)
        string weaponTypeKey = equipmentData.WeaponType == WeaponType.Sword ? "melee" : "ranged";
        
        // 등급별 이벤트 키 매핑
        switch (equipmentData.itemGrade)
        {
            case ItemGrade.S:
                return $"hit.player.{weaponTypeKey}_s";
            case ItemGrade.A:
                return $"hit.player.{weaponTypeKey}_a";
            case ItemGrade.B:
                return $"hit.player.{weaponTypeKey}_b";
            case ItemGrade.C:
                return $"hit.player.{weaponTypeKey}_c";
            case ItemGrade.D:
                return $"hit.player.{weaponTypeKey}_d";
            default:
                return "hit.player.normal"; // fallback
        }
    }
    
    /// <summary>
    /// 등급별 히트 이펙트 강도
    /// </summary>
    private float GetHitMagnitudeByGrade()
    {
        var activeWeapon = FindObjectOfType<ActiveWeapon>();
        if (activeWeapon?.CurrentActiveWeapon == null) return 1.0f;
        
        var weaponComponent = activeWeapon.CurrentActiveWeapon as IWeapon;
        if (weaponComponent == null) return 1.0f;
        
        var equipmentData = weaponComponent.GetEquipmentData();
        if (equipmentData == null) return 1.0f;
        
        switch (equipmentData.itemGrade)
        {
            case ItemGrade.S: return 2.5f;  // S등급: 2.5배 강도
            case ItemGrade.A: return 1.8f;  // A등급: 1.8배
            case ItemGrade.B: return 1.3f;  // B등급: 1.3배
            case ItemGrade.C: return 1.0f;  // C등급: 기본
            case ItemGrade.D: return 0.7f;  // D등급: 0.7배
            default: return 1.0f;
        }
    }
    
    #endregion
    
    #region 🧱 벽 충돌 시스템
    
    /// <summary>
    /// 벽이 중간에 있는지 Raycast로 체크
    /// </summary>
    private bool IsBlockedByWall(Transform target)
    {
        if (wallLayer == 0)
        {
            Debug.LogWarning("🚨 [DamageSource] wallLayer가 설정되지 않았습니다!");
            return false;
        }
        
        Vector2 origin = transform.position;
        Vector2 direction = (target.position - transform.position).normalized;
        float distance = Vector2.Distance(transform.position, target.position);
        
        // 🔍 디버그: Raycast 정보
        if (showDebugLogs)
        {
            Debug.Log($"🔍 [DamageSource] Raycast 체크:");
            Debug.Log($"   - Origin: {origin}");
            Debug.Log($"   - Target: {target.position} ({target.name})");
            Debug.Log($"   - Direction: {direction}");
            Debug.Log($"   - Distance: {distance:F2}");
            Debug.Log($"   - wallLayer: {wallLayer.value}");
        }
        
        // Raycast로 벽 감지
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, wallLayer);
        
        if (hit.collider != null)
        {
            Debug.Log($"🧱 [DamageSource] 벽 감지! {hit.collider.name} (거리: {hit.distance:F2}, Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)})");
            return true;
        }
        else
        {
            if (showDebugLogs)
                Debug.Log($"✅ [DamageSource] 벽 없음 - 공격 가능");
            return false;
        }
    }
    
    #endregion
}
