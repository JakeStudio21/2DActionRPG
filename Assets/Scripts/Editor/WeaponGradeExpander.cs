using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 🗡️ Weapon Grade Expander - 무기 등급 확장 툴
/// 기존 무기(Sword, Bow)에 부족한 등급(D, SS, EX, TR) 추가
/// 목적: 방어구/장비와 등급 통일 (8개 등급)
/// </summary>
public class WeaponGradeExpander : EditorWindow
{
    [MenuItem("Tools/Equipment Manager/Expand Weapon Grades (D/SS/EX/TR)")]
    public static void ExpandWeaponGrades()
    {
        bool confirm = EditorUtility.DisplayDialog(
            "Expand Weapon Grades",
            "무기 등급을 확장하시겠습니까?\n\n생성될 파일:\n" +
            "- Sword: D, SS, EX, TR (4개)\n" +
            "- Bow: D, SS, EX, TR (4개)\n" +
            "총 8개 파일\n\n" +
            "기존 네이밍 규칙 유지:\n" +
            "ITEM_SWORD_D_Equipment.asset",
            "Generate",
            "Cancel"
        );
        
        if (!confirm) return;
        
        int successCount = 0;
        
        // Sword 등급 확장
        successCount += CreateMissingGrades("SWORD", "Sword", WeaponType.Sword, PlayerClass.Warrior);
        
        // Bow 등급 확장
        successCount += CreateMissingGrades("BOW", "Bow", WeaponType.Bow, PlayerClass.Assasin);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog(
            "Complete",
            $"✅ {successCount}/8개의 무기 등급이 생성되었습니다!\n\n" +
            "경로: Assets/Resources/Equipment/\n\n" +
            "이제 무기도 8개 등급(D~TR)을 갖추었습니다.",
            "OK"
        );
    }
    
    /// <summary>
    /// 특정 무기 타입의 부족한 등급 생성
    /// </summary>
    private static int CreateMissingGrades(string weaponTypeUpper, string weaponTypeName, WeaponType weaponType, PlayerClass usableClass)
    {
        int count = 0;
        string basePath = "Assets/Resources/Equipment/";
        
        // 생성할 등급: D, SS, EX, TR
        ItemGrade[] missingGrades = { ItemGrade.D, ItemGrade.SS, ItemGrade.EX, ItemGrade.TR };
        
        foreach (var grade in missingGrades)
        {
            string fileName = $"ITEM_{weaponTypeUpper}_{grade}_Equipment.asset";
            string fullPath = Path.Combine(basePath, fileName);
            
            // 이미 존재하면 스킵
            if (File.Exists(fullPath))
            {
                Debug.LogWarning($"⚠️ [WeaponGradeExpander] 이미 존재: {fileName}");
                continue;
            }
            
            // 기존 S등급을 템플릿으로 로드
            string templatePath = $"{basePath}ITEM_{weaponTypeUpper}_S_Equipment.asset";
            EquipmentData template = AssetDatabase.LoadAssetAtPath<EquipmentData>(templatePath);
            
            if (template == null)
            {
                Debug.LogError($"❌ [WeaponGradeExpander] 템플릿을 찾을 수 없음: {templatePath}");
                continue;
            }
            
            // 새 EquipmentData 생성
            EquipmentData newWeapon = ScriptableObject.CreateInstance<EquipmentData>();
            
            // 기본 정보 복사 (기존 네이밍 규칙)
            newWeapon.equipmentName = $"{weaponTypeName}_{grade}";
            newWeapon.itemID = $"ITEM_{weaponTypeUpper}_{grade}";
            newWeapon.resourceID = $"RES_WEAPON_{weaponTypeUpper}_{grade}";
            newWeapon.equipmentType = template.equipmentType;
            newWeapon.itemGrade = grade;
            newWeapon.usableClass = usableClass;
            newWeapon.isTradable = true;
            newWeapon.requiredLevel = GetRequiredLevel(grade);
            newWeapon.description = GetWeaponDescription(weaponTypeName, grade);
            
            // 리소스 복사
            newWeapon.icon = template.icon;
            newWeapon.equipmentPrefab = template.equipmentPrefab;
            newWeapon.SetPickupPrefab(template.PickupPrefab);
            
            // 무기 타입 설정
            SetPrivateField(newWeapon, "weaponType", weaponType);
            SetPrivateField(newWeapon, "weaponCooldown", template.WeaponCooldown);
            SetPrivateField(newWeapon, "weaponRange", template.WeaponRange);
            
            // 등급별 스탯 적용
            ApplyWeaponStatsByGrade(newWeapon, grade, weaponType);
            
            // 가격 설정
            newWeapon.buyPrice = GetBuyPrice(grade);
            newWeapon.sellPrice = newWeapon.buyPrice / 2;
            
            // Asset 생성
            AssetDatabase.CreateAsset(newWeapon, fullPath);
            Debug.Log($"✅ [WeaponGradeExpander] 생성 완료: {fileName}");
            count++;
        }
        
        return count;
    }
    
    /// <summary>
    /// 등급별 무기 스탯 적용
    /// </summary>
    private static void ApplyWeaponStatsByGrade(EquipmentData weapon, ItemGrade grade, WeaponType weaponType)
    {
        // 공격력
        weapon.attackDamage = GetWeaponDamage(grade);
        
        // 공격 속도 (기본값)
        weapon.attackSpeed = 1.0f;
        
        // 공격 범위 (기본값)
        weapon.attackRange = 1.0f;
        
        // 크리티컬
        weapon.criticalChance = GetCriticalChance(grade);
        weapon.criticalDamage = GetCriticalDamage(grade);
        
        // Bow는 발사체 ID 설정
        if (weaponType == WeaponType.Bow)
        {
            weapon.projectileId = "Arrow"; // 기존 발사체 사용
        }
    }
    
    /// <summary>
    /// 등급별 공격력
    /// </summary>
    private static int GetWeaponDamage(ItemGrade grade)
    {
        switch (grade)
        {
            case ItemGrade.TR: return 250;  // 초월
            case ItemGrade.EX: return 200;  // 고대
            case ItemGrade.SS: return 150;  // 신화
            case ItemGrade.S: return 100;   // 전설
            case ItemGrade.A: return 80;
            case ItemGrade.B: return 60;
            case ItemGrade.C: return 40;
            case ItemGrade.D: return 20;
            default: return 10;
        }
    }
    
    /// <summary>
    /// 등급별 크리티컬 확률
    /// </summary>
    private static float GetCriticalChance(ItemGrade grade)
    {
        switch (grade)
        {
            case ItemGrade.TR: return 0.50f;  // 50%
            case ItemGrade.EX: return 0.40f;  // 40%
            case ItemGrade.SS: return 0.30f;  // 30%
            case ItemGrade.S: return 0.25f;   // 25%
            case ItemGrade.A: return 0.20f;
            case ItemGrade.B: return 0.15f;
            case ItemGrade.C: return 0.10f;
            case ItemGrade.D: return 0.05f;
            default: return 0.0f;
        }
    }
    
    /// <summary>
    /// 등급별 크리티컬 데미지 배율
    /// </summary>
    private static float GetCriticalDamage(ItemGrade grade)
    {
        switch (grade)
        {
            case ItemGrade.TR: return 3.5f;  // 350%
            case ItemGrade.EX: return 3.2f;  // 320%
            case ItemGrade.SS: return 2.8f;  // 280%
            case ItemGrade.S: return 2.5f;   // 250%
            case ItemGrade.A: return 2.3f;
            case ItemGrade.B: return 2.1f;
            case ItemGrade.C: return 1.9f;
            case ItemGrade.D: return 1.7f;
            default: return 1.5f;
        }
    }
    
    /// <summary>
    /// 등급별 필요 레벨
    /// </summary>
    private static int GetRequiredLevel(ItemGrade grade)
    {
        switch (grade)
        {
            case ItemGrade.TR: return 100;
            case ItemGrade.EX: return 80;
            case ItemGrade.SS: return 60;
            case ItemGrade.S: return 50;
            case ItemGrade.A: return 40;
            case ItemGrade.B: return 30;
            case ItemGrade.C: return 20;
            case ItemGrade.D: return 10;
            default: return 1;
        }
    }
    
    /// <summary>
    /// 등급별 가격
    /// </summary>
    private static int GetBuyPrice(ItemGrade grade)
    {
        switch (grade)
        {
            case ItemGrade.TR: return 2000000;  // 200만
            case ItemGrade.EX: return 1000000;  // 100만
            case ItemGrade.SS: return 200000;   // 20만
            case ItemGrade.S: return 20000;     // 2만
            case ItemGrade.A: return 10000;
            case ItemGrade.B: return 4000;
            case ItemGrade.C: return 2000;
            case ItemGrade.D: return 1000;
            default: return 500;
        }
    }
    
    /// <summary>
    /// 무기 설명 생성
    /// </summary>
    private static string GetWeaponDescription(string weaponName, ItemGrade grade)
    {
        string gradeKorean = GetGradeKorean(grade);
        string weaponKorean = weaponName == "Sword" ? "전쟁검" : "전쟁활";
        
        return $"{gradeKorean}의 {weaponKorean}";
    }
    
    /// <summary>
    /// 등급 한글명
    /// </summary>
    private static string GetGradeKorean(ItemGrade grade)
    {
        switch (grade)
        {
            case ItemGrade.TR: return "초월";
            case ItemGrade.EX: return "고대";
            case ItemGrade.SS: return "신화";
            case ItemGrade.S: return "전설";
            case ItemGrade.A: return "영웅";
            case ItemGrade.B: return "희귀";
            case ItemGrade.C: return "고급";
            case ItemGrade.D: return "일반";
            default: return "기본";
        }
    }
    
    /// <summary>
    /// Reflection으로 private 필드 설정
    /// </summary>
    private static void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(obj, value);
        }
        else
        {
            Debug.LogWarning($"⚠️ [WeaponGradeExpander] 필드를 찾을 수 없음: {fieldName}");
        }
    }
}

