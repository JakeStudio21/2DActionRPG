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
        
        // ❌ 디버그 로그 제거
        // Debug.Log($"🎯 [PlayerEquipment] 현재 클래스: {currentClass}");
        // Debug.Log($"🛡️ [PlayerEquipment] 무기 요구 클래스: {weaponData.usableClass}");
        // Debug.Log($"⚔️ [PlayerEquipment] 무기 타입: {weaponData.WeaponType}");
        
        if (currentClass == PlayerClass.None)
        {
            if (showDebugLogs)
                Debug.LogWarning("🟡 [PlayerEquipment] 플레이어 클래스를 확인할 수 없습니다.");
            return false;
        }
        
        // 호환성 검사 (EquipmentData의 책임 활용)
        bool isCompatible = weaponData.IsCompatibleWith(currentClass);
        
        // ❌ 디버그 로그 제거
        // Debug.Log($"🔍 [PlayerEquipment] IsCompatibleWith() 결과: {isCompatible}");
        
        if (!isCompatible && showDebugLogs)
        {
            Debug.LogWarning($"�� [PlayerEquipment] 클래스 호환성 실패: {currentClass}는 {weaponData.usableClass} 전용 {weaponData.WeaponType} 사용 불가");
        }
        
        return isCompatible;
    }

    /// <summary>
    /// 🎯 현재 플레이어 클래스 확인 (PlayerEquipment 내부 유틸리티)
    /// </summary>
    private PlayerClass GetPlayerClass()
    {
        // ❌ 디버그 로그 제거
        // Debug.Log("🎯 [PlayerEquipment] GetPlayerClass() 호출됨");
        
        // 같은 GameObject에서 클래스 컴포넌트 찾기
        var assasin = GetComponent<Assasin>();
        // Debug.Log($"🎯 [PlayerEquipment] Assasin 컴포넌트: {(assasin != null ? "발견됨" : "없음")}");
        
        if (assasin != null)
        {
            // Debug.Log($"🎯 [PlayerEquipment] Assasin.IsActiveClass: {assasin.IsActiveClass}");
            if (assasin.IsActiveClass)
            {
                // Debug.Log($"�� [PlayerEquipment] 현재 클래스: Assasin");
                return PlayerClass.Assasin;
            }
        }
            
        var warrior = GetComponent<Warrior>();
        // Debug.Log($"🎯 [PlayerEquipment] Warrior 컴포넌트: {(warrior != null ? "발견됨" : "없음")}");
        
        if (warrior != null)
        {
            // Debug.Log($"🎯 [PlayerEquipment] Warrior.IsActiveClass: {warrior.IsActiveClass}");
            if (warrior.IsActiveClass)
            {
                // Debug.Log("✅ [PlayerEquipment] 현재 클래스: Warrior");
                return PlayerClass.Warrior;
            }
        }
        
        // Wizard 추가 시
        // var wizard = GetComponent<Wizard>();
        // if (wizard != null && wizard.IsActiveClass)
        //     return PlayerClass.Wizard;
        
        // Debug.LogWarning("🟡 [PlayerEquipment] 활성 클래스를 찾을 수 없습니다.");
        return PlayerClass.None;
    }
    
    #endregion
}
