using UnityEngine;

public class MaterialTestHelper : MonoBehaviour
{
    [ContextMenu("Add Test Materials")]
    public void AddTestMaterials()
    {
        if (!AccountDataManager.IsInitialized())
        {
            Debug.LogWarning("AccountDataManager가 초기화되지 않았습니다.");
            return;
        }
        
        var manager = AccountDataManager.Instance;
        
        // 9가지 재료 추가
        manager.AddMaterial(MaterialType.WeaponFragment, 100);
        manager.AddMaterial(MaterialType.WeaponCrystal, 50);
        manager.AddMaterial(MaterialType.WeaponCore, 20);
        
        manager.AddMaterial(MaterialType.ArmorFragment, 80);
        manager.AddMaterial(MaterialType.ArmorCrystal, 40);
        manager.AddMaterial(MaterialType.ArmorCore, 15);
        
        manager.AddMaterial(MaterialType.AccessoryFragment, 60);
        manager.AddMaterial(MaterialType.AccessoryCrystal, 30);
        manager.AddMaterial(MaterialType.AccessoryCore, 10);
        
        manager.Save();
        
        Debug.Log("✅ 테스트 재료 추가 완료!");
    }
    
    [ContextMenu("Clear All Materials")]
    public void ClearAllMaterials()
    {
        if (!AccountDataManager.IsInitialized())
        {
            Debug.LogWarning("AccountDataManager가 초기화되지 않았습니다.");
            return;
        }
        
        var manager = AccountDataManager.Instance;
        manager.GetAccountData().materials.Clear();
        manager.Save();
        
        Debug.Log("🧹 모든 재료 삭제 완료!");
    }
}