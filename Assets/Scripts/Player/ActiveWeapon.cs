using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 🎯 [2단계] ActiveWeapon 통합 무기 관리자
/// SRP: 활성 무기의 런타임 데이터와 실행 로직만 담당
/// </summary>
public class ActiveWeapon : MonoBehaviour
{
    [Header("🔗 시스템 연동")]
    private PlayerEquipment playerEquipment;
    private PlayerAnimationController playerAnimationController;
    private Rigidbody2D rb;  // 추가: rb 필드 선언
    
    // ❌ 프로퍼티에는 [Header] 사용 불가
    // [Header("⚔️ 현재 활성 무기 런타임 상태")]
    // public MonoBehaviour CurrentActiveWeapon { get; private set; }
    // public EquipmentData CurrentWeaponData { get; private set; }
    
    // ✅ 올바른 방법 - [Header] 제거
    public MonoBehaviour CurrentActiveWeapon { get; private set; }
    public EquipmentData CurrentWeaponData { get; private set; } // 🆕 현재 무기 데이터 보관
    
    [Header("🎮 무기 방향 제어")]
    public AttackJoystickInput attackJoystickInput; // 인스펙터에서 할당 (폴백 용도)

    
    [Header("📊 디버그")]
    [SerializeField] private bool showDebugLogs = true;

    private void Awake() 
    {
        // 컴포넌트 캐싱
        rb = GetComponent<Rigidbody2D>();
        
        // PlayerEquipment 참조 가져오기 (부모에서 찾기)
        playerEquipment = GetComponent<PlayerEquipment>();
        if (playerEquipment == null)
        {
            playerEquipment = GetComponentInParent<PlayerEquipment>();
            
            if (playerEquipment == null)
            {
                Debug.LogError("🔴 [ActiveWeapon] PlayerEquipment 컴포넌트를 찾을 수 없습니다!");
                return;
            }
        }
        
        // PlayerAnimationController 참조 가져오기
        playerAnimationController = GetComponent<PlayerAnimationController>();
        if (playerAnimationController == null)
        {
            playerAnimationController = GetComponentInParent<PlayerAnimationController>();
            if (playerAnimationController == null)
            {
                playerAnimationController = GetComponentInChildren<PlayerAnimationController>();
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"🔗 [ActiveWeapon] 시스템 연동 상태:");
            Debug.Log($"   - PlayerEquipment: {(playerEquipment != null ? "연결됨" : "없음")}");
            Debug.Log($"   - PlayerAnimationController: {(playerAnimationController != null ? "연결됨" : "없음")}");
        }
    }

    private void Update() 
    {
        // 🎮 무기 방향 업데이트만 담당 (런타임 제어)
        UpdateWeaponDirection();
    }

    #region 무기 교체 및 런타임 상태 관리
    
    /// <summary>
    /// 🔧 무기 교체 요청 (PlayerEquipment 위임 + 런타임 상태 관리)
    /// </summary>
    public void EquipWeapon(EquipmentData weaponData)
    {
        if (weaponData == null)
        {
            Debug.LogError("🔴 [ActiveWeapon] weaponData가 null입니다!");
            return;
        }
        
        if (playerEquipment == null)
        {
            Debug.LogError("🔴 [ActiveWeapon] PlayerEquipment가 없습니다!");
            return;
        }
        
        if (showDebugLogs)
            Debug.Log($"🔧 [ActiveWeapon] 무기 교체 요청: {weaponData.equipmentName}");
        
        // 1. PlayerEquipment에게 물리적 장착 요청 (책임 위임)
        GameObject weaponPrefab = playerEquipment.EquipWeaponPrefab(weaponData);
        
        if (weaponPrefab == null)
        {
            Debug.LogError("🔴 [ActiveWeapon] 무기 프리팹 생성 실패! (호환성 또는 생성 문제)");
            return;
        }
        
        // 2. 런타임 상태 관리 (ActiveWeapon의 핵심 책임)
        MonoBehaviour weaponComponent = weaponPrefab.GetComponent<MonoBehaviour>();
        SetCurrentWeapon(weaponComponent, weaponData);
    }
    
    /// <summary>
    /// ⚔️ 현재 활성 무기 설정 (런타임 상태 관리)
    /// </summary>
    public void SetCurrentWeapon(MonoBehaviour weaponComponent, EquipmentData weaponData)
    {
        if (weaponComponent == null)
        {
            Debug.LogError("🔴 [ActiveWeapon] weaponComponent가 null입니다!");
            return;
        }
        
        // IWeapon 인터페이스 체크
        if (!(weaponComponent is IWeapon))
        {
            Debug.LogError($"🔴 [ActiveWeapon] {weaponComponent.name}이 IWeapon을 구현하지 않습니다!");
            return;
        }
        
        // 런타임 상태 업데이트
        CurrentActiveWeapon = weaponComponent;
        CurrentWeaponData = weaponData;
        
        // 다른 시스템에 무기 변경 알림
        NotifyWeaponChanged(weaponData);
        
        // ⭐ Phase 2: 무기 등급에 따른 콜라이더 크기 자동 조절
        UpdateWeaponColliderForEquipment(weaponComponent);
        
        if (showDebugLogs)
            Debug.Log($"⚔️ [ActiveWeapon] 활성 무기 상태 업데이트: {weaponData.equipmentName}");
    }
    
    /// <summary>
    /// 📢 무기 변경 알림 (런타임 연동)
    /// </summary>
    private void NotifyWeaponChanged(EquipmentData weaponData)
    {
        // PlayerAnimationController 참조가 없으면 씬 전체에서 검색 (초기화 순서 문제 대응)
        if (playerAnimationController == null)
            playerAnimationController = FindObjectOfType<PlayerAnimationController>();
        
        if (playerAnimationController != null)
        {
            playerAnimationController.UpdateWeaponCooldown(weaponData.WeaponCooldown);
            
            if (showDebugLogs)
                Debug.Log($"📢 [ActiveWeapon] PlayerAnimationController에 쿨다운 전달: {weaponData.WeaponCooldown}초");
        }
        else
        {
            Debug.LogWarning("⚠️ [ActiveWeapon] PlayerAnimationController를 찾을 수 없어 쿨다운 전달 실패");
        }
    }
    
    /// <summary>
    /// 🗑️ 무기 제거 (런타임 상태 초기화)
    /// </summary>
    public void WeaponNull() 
    {
        CurrentActiveWeapon = null;
        CurrentWeaponData = null;
        
        if (playerEquipment != null)
        {
            playerEquipment.UnequipWeapon();
        }
        
        if (showDebugLogs)
            Debug.Log("🗑️ [ActiveWeapon] 무기 제거 및 런타임 상태 초기화 완료");
    }
    
    #endregion
    
    #region 런타임 무기 실행 및 제어
    
    /// <summary>
    /// ⚔️ 무기 공격 실행 (런타임 제어)
    /// </summary>
    public void ExecuteWeaponAttack()
    {
        // 🚨 N/S 방향 문제 진단용 로그
        Debug.LogWarning($"🚨🚨🚨 [EXECUTE WEAPON] ExecuteWeaponAttack() 호출됨!");
        Debug.LogWarning($"🚨🚨🚨 [EXECUTE WEAPON] CurrentActiveWeapon: {(CurrentActiveWeapon != null ? CurrentActiveWeapon.name : "NULL")}");
        Debug.LogWarning($"🚨🚨🚨 [EXECUTE WEAPON] CurrentWeaponData: {(CurrentWeaponData != null ? CurrentWeaponData.equipmentName : "NULL")}");
        
        if (CurrentActiveWeapon == null)
        {
            Debug.LogError("🔴🔴🔴 [EXECUTE WEAPON] 활성 무기가 NULL! 공격 중단!");
            return;
        }
        
        var weaponInterface = CurrentActiveWeapon as IWeapon;
        if (weaponInterface == null)
        {
            Debug.LogError($"🔴🔴🔴 [EXECUTE WEAPON] {CurrentActiveWeapon.name}이 IWeapon을 구현하지 않습니다!");
            return;
        }
        
        Debug.LogWarning($"🎯🎯🎯 [EXECUTE WEAPON] {CurrentActiveWeapon.name}.Attack() 호출!");
        weaponInterface.Attack();
        
        Debug.LogWarning("✅✅✅ [EXECUTE WEAPON] 무기 공격 실행 완료!");
    }
    
    /// <summary>
    /// 공격 시작 시 호출. 애니메이션이 완료될 때까지 무기 방향을 고정합니다.
    /// OnAttackComplete()에서 UnlockAttackDirection()으로 해제해야 합니다.
    /// </summary>
    public void LockAttackDirection(Vector2 dir)
    {
        _lockedDir = dir;
        _directionLocked = true;
    }

    /// <summary>
    /// OnAttackComplete() 시점에 호출. 무기 방향 고정을 해제하고 조이스틱 입력으로 복귀합니다.
    /// </summary>
    public void UnlockAttackDirection()
    {
        _directionLocked = false;
    }

    private Vector2 _lockedDir;
    private bool _directionLocked;

    private void UpdateWeaponDirection()
    {
        if (CurrentActiveWeapon == null) return;

        Vector2 dir;
        if (_directionLocked)
        {
            // 공격 방향 고정 중: OnAttackComplete 때까지 유지
            dir = _lockedDir;
        }
        else
        {
            // 평상시: 조이스틱 방향 사용 (zero일 때 무기 idle 유지)
            dir = attackJoystickInput != null ? attackJoystickInput.GetAttackDirection() : Vector2.zero;
        }
        
        // 플레이어 방향 가져오기
        var playerController = FindObjectOfType<PlayerController>();
        bool facingLeft = playerController != null && playerController.FacingLeft;
        
        // 🗡️ [아이소메트릭] WeaponCollider 회전 시스템
        if (dir.magnitude > 0.1f && playerController != null)
        {
            Transform weaponCollider = playerController.GetWeaponCollider();
            if (weaponCollider != null)
            {
                // 조이스틱 방향을 각도로 변환
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                weaponCollider.rotation = Quaternion.Euler(0, 0, angle);
                
                if (showDebugLogs && Time.frameCount % 60 == 0) // 1초마다 로그
                {
                    Debug.Log($"🗡️ [ActiveWeapon] WeaponCollider 회전: {angle:F1}도 (방향: {dir})");
                }
            }
            else if (showDebugLogs && Time.frameCount % 60 == 0)
            {
                Debug.LogWarning("🟡 [ActiveWeapon] WeaponCollider를 찾을 수 없습니다!");
            }
        }
        
        // 무기에 방향 전달 (기존 로직 유지)
        var weaponInterface = CurrentActiveWeapon as IWeapon;
        if (weaponInterface != null)
        {
            try
            {
                weaponInterface.UpdateDirection(dir, facingLeft);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"🔴 [ActiveWeapon] UpdateDirection 에러: {e.Message}");
            }
        }
    }
    
    #endregion
    
    #region 런타임 무기 정보 제공
    
    /// <summary>
    /// 📊 현재 무기 쿨다운 반환 (런타임 상태 조회)
    /// </summary>
    public float GetCurrentWeaponCooldown() => CurrentWeaponData?.WeaponCooldown ?? 0f;
    
    /// <summary>
    /// 📊 현재 무기 데미지 반환 (런타임 상태 조회)
    /// </summary>
    public float GetCurrentWeaponDamage() => CurrentWeaponData?.attackDamage ?? 0f;
    
    /// <summary>
    /// 📊 현재 무기 타입 반환 (런타임 상태 조회)
    /// </summary>
    public WeaponType GetCurrentWeaponType() => CurrentWeaponData?.WeaponType ?? WeaponType.None;
    
    /// <summary>
    /// 📊 현재 무기 데이터 반환 (런타임 상태 조회)
    /// </summary>
    public EquipmentData GetCurrentWeaponData() => CurrentWeaponData;
    
    /// <summary>
    /// ✅ 활성 무기 보유 여부 (런타임 상태 조회)
    /// </summary>
    public bool HasActiveWeapon() => CurrentActiveWeapon != null && CurrentWeaponData != null;
    
    #endregion
    
    #region ⭐ Phase 2: 등급별 공격 범위 시스템
    
    /// <summary>
    /// 무기 장착 시 콜라이더 크기 자동 조절
    /// </summary>
    private void UpdateWeaponColliderForEquipment(MonoBehaviour weaponComponent)
    {
        // Sword인 경우 콜라이더 크기 조절
        var sword = weaponComponent as Sword;
        if (sword != null)
        {
            // Sword의 UpdateWeaponColliderSize() 메서드 호출
            sword.SendMessage("UpdateWeaponColliderSize", SendMessageOptions.DontRequireReceiver);
            
            if (showDebugLogs)
                Debug.Log($"⚔️ [ActiveWeapon] Sword 콜라이더 크기 자동 조절 요청");
            
            return;
        }
        
        // 원거리 무기(Bow, Staff)는 콜라이더 크기 조절 불필요
        if (showDebugLogs)
            Debug.Log($"🏹 [ActiveWeapon] 원거리 무기는 콜라이더 크기 조절 불필요");
    }
    
    #endregion
    
    #region 레거시 호환성 (임시)
    
    /// <summary>
    /// 🔄 기존 시스템 호환용 (제거 예정)
    /// </summary>
    public void NewWeapon(MonoBehaviour newWeapon) 
    {
        Debug.LogWarning("⚠️ [ActiveWeapon] NewWeapon()은 레거시 메서드입니다. SetCurrentWeapon() 사용을 권장합니다.");
        CurrentActiveWeapon = newWeapon;
        CurrentWeaponData = null; // 데이터 없이 설정됨
    }
    
    /// <summary>
    /// 🔄 기존 시스템 호환용 (제거 예정)
    /// </summary>
    public void PerformAttack()
    {
        Debug.LogWarning("⚠️ [ActiveWeapon] PerformAttack()은 레거시 메서드입니다. ExecuteWeaponAttack() 사용을 권장합니다.");
        ExecuteWeaponAttack();
    }
    
    #endregion
}

