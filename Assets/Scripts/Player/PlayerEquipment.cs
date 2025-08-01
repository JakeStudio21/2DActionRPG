using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

/// <summary>
/// 🔧 플레이어 장비의 물리적 관리 담당
/// 프리팹 생성/파괴, 슬롯 위치 관리, Transform 설정
/// </summary>
public class PlayerEquipment : MonoBehaviour
{
    [Header("🎒 장비 슬롯들")]
    [SerializeField] private Transform weaponSlot;      // 무기 장착 위치
    [SerializeField] private Transform helmetSlot;     // 헬멧 장착 위치  
    [SerializeField] private Transform armorSlot;      // 갑옷 장착 위치
    
    [Header("🎮 현재 장착된 프리팹들")]
    [SerializeField] private GameObject currentWeaponPrefab;
    [SerializeField] private GameObject currentHelmetPrefab;
    [SerializeField] private GameObject currentArmorPrefab;
    
    [Header("📊 디버그")]
    [SerializeField] private bool showDebugLogs = true;
    
    private void Awake()
    {
        // weaponSlot이 설정되지 않았다면 자신을 슬롯으로 사용
        if (weaponSlot == null)
        {
            weaponSlot = transform;
            if (showDebugLogs)
                Debug.Log("🔧 [PlayerEquipment] weaponSlot을 자동으로 설정: " + transform.name);
        }
    }
    
    #region 무기 관리
    
    /// <summary>
    /// 🔧 무기 프리팹 물리적 장착 (호환성 검증 포함)
    /// </summary>
    public GameObject EquipWeaponPrefab(EquipmentData weaponData)
    {
        if (weaponData == null)
        {
            Debug.LogError("🔴 [PlayerEquipment] weaponData가 null입니다!");
            return null;
        }
        
        // ❌ 디버그 로그 제거
        // Debug.Log($"�� [PlayerEquipment] 무기 교체 시도: {weaponData.equipmentName}");
        // Debug.Log($"🔧 [PlayerEquipment] 무기 타입: {weaponData.WeaponType}");
        // Debug.Log($"🔧 [PlayerEquipment] 요구 클래스: {weaponData.usableClass}");
        // Debug.Log("🔍 [PlayerEquipment] 호환성 검사 시작...");
        
        // 1단계: 장착 가능성 검사 (PlayerEquipment의 핵심 책임)
        if (!CanEquipWeapon(weaponData))
        {
            // ⭐ 추가: 클래스 None인 경우 대기 상태로 설정
            if (GetPlayerClass() == PlayerClass.None)
            {
                Debug.Log($"🕐 [PlayerEquipment] 클래스 초기화 대기 중 - 무기 장착 보류: {weaponData.equipmentName}");
                pendingWeaponEquip = weaponData;
                return null; // 현재는 실패하지만 나중에 재시도됨
            }
            
            if (showDebugLogs)
                Debug.LogWarning($"🚫 [PlayerEquipment] {weaponData.equipmentName} 장착 불가!");
            return null;
        }
        
        // ❌ 디버그 로그 제거  
        // Debug.Log($"✅ [PlayerEquipment] 호환성 검사 통과! {weaponData.equipmentName} 장착 진행");
        
        if (weaponData.equipmentPrefab == null)
        {
            Debug.LogError($"🔴 [PlayerEquipment] {weaponData.equipmentName}의 equipmentPrefab이 null입니다!");
            return null;
        }
        
        // 2단계: 기존 무기 프리팹 파괴
        UnequipWeapon();
        
        // 3단계: 새 무기 프리팹 생성
        currentWeaponPrefab = Instantiate(weaponData.equipmentPrefab, weaponSlot);
        
        // 4. 위치/회전 설정
        currentWeaponPrefab.transform.localPosition = Vector3.zero;
        currentWeaponPrefab.transform.localRotation = Quaternion.identity;
        
        // 5. 생성된 무기에 EquipmentData 동적 할당
        SetWeaponData(currentWeaponPrefab, weaponData);
        
        if (showDebugLogs)
            Debug.Log($"🔧 [PlayerEquipment] 무기 프리팹 장착 완료: {weaponData.equipmentName}");
        
        return currentWeaponPrefab;
    }
    
    /// <summary>
    /// 🗑️ 무기 프리팹 제거
    /// </summary>
    public void UnequipWeapon()
    {
        if (currentWeaponPrefab != null)
        {
            if (showDebugLogs)
                Debug.Log($"🗑️ [PlayerEquipment] 기존 무기 제거: {currentWeaponPrefab.name}");
                
            Destroy(currentWeaponPrefab);
            currentWeaponPrefab = null;
        }
    }
    
    /// <summary>
    /// 📊 현재 무기 프리팹 반환
    /// </summary>
    public GameObject GetCurrentWeaponPrefab() => currentWeaponPrefab;
    
    /// <summary>
    /// ✅ 무기 장착 여부 확인
    /// </summary>
    public bool HasWeaponEquipped() => currentWeaponPrefab != null;
    
    #endregion
    
    #region 헬멧/갑옷 관리 (향후 확장용)
    
    public GameObject EquipHelmetPrefab(EquipmentData helmetData)
    {
        // 향후 구현
        return null;
    }
    
    public GameObject EquipArmorPrefab(EquipmentData armorData)
    {
        // 향후 구현  
        return null;
    }
    
    #endregion
    
    #region 프라이빗 메서드
    
    /// <summary>
    /// 🔧 생성된 무기에 EquipmentData 동적 할당
    /// </summary>
    private void SetWeaponData(GameObject weaponPrefab, EquipmentData equipmentData)
    {
        var weaponComponent = weaponPrefab.GetComponent<MonoBehaviour>();
        if (weaponComponent == null)
        {
            Debug.LogError($"🔴 [PlayerEquipment] {weaponPrefab.name}에 무기 컴포넌트가 없습니다!");
            return;
        }
        
        // 🔑 무기 타입별 EquipmentData 할당
        FieldInfo equipmentDataField = null;
        
        if (weaponComponent is Sword)
            equipmentDataField = typeof(Sword).GetField("equipmentData", BindingFlags.NonPublic | BindingFlags.Instance);
        else if (weaponComponent is Bow)
            equipmentDataField = typeof(Bow).GetField("equipmentData", BindingFlags.NonPublic | BindingFlags.Instance);
        else if (weaponComponent is Staff)
            equipmentDataField = typeof(Staff).GetField("equipmentData", BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (equipmentDataField != null)
        {
            equipmentDataField.SetValue(weaponComponent, equipmentData);
            if (showDebugLogs)
                Debug.Log($"✅ [PlayerEquipment] {weaponComponent.GetType().Name}에 EquipmentData 할당: {equipmentData.equipmentName}");
        }
        else
        {
            Debug.LogError($"🔴 [PlayerEquipment] {weaponComponent.GetType().Name}에서 equipmentData 필드를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 🔍 무기 장착 가능성 검사 (PlayerEquipment의 핵심 책임)
    /// </summary>
    private bool CanEquipWeapon(EquipmentData weaponData)
    {
        // 현재 플레이어 클래스 확인
        PlayerClass currentClass = GetPlayerClass();
        
        if (showDebugLogs)
        {
            Debug.Log($"🔍 [PlayerEquipment] 호환성 검사:");
            Debug.Log($"   - 현재 클래스: {currentClass}");
            Debug.Log($"   - 무기 요구 클래스: {weaponData.usableClass}");
            Debug.Log($"   - 무기 타입: {weaponData.WeaponType}");
        }
        
        // ⭐ 간단한 해결: 클래스가 None인 경우 임시 허용
        if (currentClass == PlayerClass.None)
        {
            Debug.LogWarning("🟡 [PlayerEquipment] 활성 클래스를 찾을 수 없지만 임시 허용합니다.");
            return true; // 일단 허용하고 나중에 처리
        }
        
        // 호환성 검사
        bool isCompatible = weaponData.IsCompatibleWith(currentClass);
        
        if (showDebugLogs)
        {
            Debug.Log($"🔍 [PlayerEquipment] IsCompatibleWith() 결과: {isCompatible}");
        }
        
        if (!isCompatible && showDebugLogs)
        {
            Debug.LogWarning($"🚫 [PlayerEquipment] 클래스 호환성 실패: {currentClass}는 {weaponData.usableClass} 전용 {weaponData.WeaponType} 사용 불가");
        }
        
        return isCompatible;
    }

    /// <summary>
    /// 🎯 현재 플레이어 클래스 확인 (PlayerEquipment 내부 유틸리티)
    /// </summary>
    private PlayerClass GetPlayerClass()
    {
        if (showDebugLogs)
            Debug.Log("🎯 [PlayerEquipment] GetPlayerClass() 호출됨");
        
        // 같은 GameObject에서 클래스 컴포넌트 찾기
        var assasin = GetComponent<Assasin>();
        var warrior = GetComponent<Warrior>();
        
        if (showDebugLogs)
        {
            Debug.Log($"🔍 [PlayerEquipment] 컴포넌트 검색 결과:");
            Debug.Log($"   - Assasin 컴포넌트: {(assasin != null ? "발견됨" : "없음")}");
            Debug.Log($"   - Warrior 컴포넌트: {(warrior != null ? "발견됨" : "없음")}");
            
            if (assasin != null)
                Debug.Log($"   - Assasin.IsActiveClass: {assasin.IsActiveClass}");
            if (warrior != null)
                Debug.Log($"   - Warrior.IsActiveClass: {warrior.IsActiveClass}");
        }
        
        // Assasin 확인
        if (assasin != null && assasin.IsActiveClass)
        {
            if (showDebugLogs)
                Debug.Log("✅ [PlayerEquipment] 현재 클래스: Assasin");
            return PlayerClass.Assasin;
        }
        
        // Warrior 확인
        if (warrior != null && warrior.IsActiveClass)
        {
            if (showDebugLogs)
                Debug.Log("✅ [PlayerEquipment] 현재 클래스: Warrior");
            return PlayerClass.Warrior;
        }
        
        // ⭐ 추가: 게임 매니저에서 선택된 클래스 확인 (폴백)
        if (GameManager.Instance?.selectedPlayerData != null)
        {
            var selectedType = GameManager.Instance.selectedPlayerData.selectedPlayerType;
            PlayerClass fallbackClass = selectedType switch
            {
                PlayerType.Warrior => PlayerClass.Warrior,
                PlayerType.Assasin => PlayerClass.Assasin,
                PlayerType.Wizard => PlayerClass.Wizard,
                _ => PlayerClass.None
            };
            
            if (fallbackClass != PlayerClass.None)
            {
                Debug.LogWarning($"🟡 [PlayerEquipment] 활성 클래스를 찾을 수 없어 GameManager 데이터 사용: {fallbackClass}");
                return fallbackClass;
            }
        }
        
        Debug.LogWarning("🟡 [PlayerEquipment] 활성 클래스를 찾을 수 없습니다.");
        return PlayerClass.None;
    }
    
    #endregion

    /// <summary>
    /// 클래스 초기화 완료 알림 받기
    /// </summary>
    public void OnClassInitializationComplete(BaseClassBehaviour classComponent)
    {
        if (showDebugLogs)
            Debug.Log($"📨 [PlayerEquipment] 클래스 초기화 완료 알림 수신: {classComponent.ClassName}");
        
        // 대기 중인 무기 장착이 있다면 재시도
        if (pendingWeaponEquip != null)
        {
            Debug.Log("🔄 [PlayerEquipment] 대기 중인 무기 장착 재시도");
            StartCoroutine(RetryPendingWeaponEquip());
        }
    }

    private EquipmentData pendingWeaponEquip = null;

    /// <summary>
    /// 대기 중인 무기 장착 재시도
    /// </summary>
    private IEnumerator RetryPendingWeaponEquip()
    {
        yield return new WaitForSeconds(0.1f); // 안전 대기
        
        if (pendingWeaponEquip != null)
        {
            var weaponData = pendingWeaponEquip;
            pendingWeaponEquip = null; // 중복 실행 방지
            
            Debug.Log($"🔄 [PlayerEquipment] 대기 무기 재장착 시도: {weaponData.equipmentName}");
            
            // 재시도
            var result = EquipWeaponPrefab(weaponData);
            if (result != null)
            {
                Debug.Log("✅ [PlayerEquipment] 대기 무기 재장착 성공!");
            }
            else
            {
                Debug.LogWarning("🟡 [PlayerEquipment] 대기 무기 재장착도 실패");
            }
        }
    }
}
