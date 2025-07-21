using UnityEngine;

public class SaveManager : Singleton<SaveManager>
{
    // 캐릭터별 골드 저장
    public void SaveGold(int characterIndex, int gold)
    {
        PlayerPrefs.SetInt($"Gold_{characterIndex}", gold);
        PlayerPrefs.Save();
        Debug.Log($"[{characterIndex}번 캐릭터] 골드 저장: {gold}");
    }

    // 캐릭터별 골드 불러오기
    public int LoadGold(int characterIndex)
    {
        int gold = PlayerPrefs.GetInt($"Gold_{characterIndex}", 0);
        Debug.Log($"[{characterIndex}번 캐릭터] 골드 불러오기: {gold}");
        return gold;
    }

    // ⭐ [Phase C] 범용 클래스 데이터 저장
    public void SaveClassData(int characterIndex, PlayerType classType, BaseClassSaveData data)
    {
        string key = $"Class_{classType}_{characterIndex}";
        string json = data.ToJson();
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();
        
        Debug.Log($"[{characterIndex}번 캐릭터] {classType} 데이터 저장 완료: {data}");
    }
    
    // ⭐ [Phase C] 범용 클래스 데이터 불러오기
    public BaseClassSaveData LoadClassData(int characterIndex, PlayerType classType)
    {
        string key = $"Class_{classType}_{characterIndex}";
        string json = PlayerPrefs.GetString(key, "");
        
        if (string.IsNullOrEmpty(json))
        {
            Debug.Log($"[{characterIndex}번 캐릭터] {classType} 데이터 없음, 기본값 생성");
            var defaultData = new BaseClassSaveData();
            defaultData.Reset(classType);
            return defaultData;
        }
        
        Debug.Log($"[{characterIndex}번 캐릭터] {classType} 데이터 불러오기 완료");
        return BaseClassSaveData.FromJson(json);
    }
    
    // ⭐ [Phase C] 현재 활성 클래스 타입 저장
    public void SaveActiveClass(int characterIndex, PlayerType activeClassType)
    {
        PlayerPrefs.SetInt($"ActiveClass_{characterIndex}", (int)activeClassType);
        PlayerPrefs.Save();
        Debug.Log($"[{characterIndex}번 캐릭터] 활성 클래스 저장: {activeClassType}");
    }
    
    // ⭐ [Phase C] 현재 활성 클래스 타입 불러오기
    public PlayerType LoadActiveClass(int characterIndex)
    {
        int classTypeInt = PlayerPrefs.GetInt($"ActiveClass_{characterIndex}", 0);
        PlayerType classType = (PlayerType)classTypeInt;
        Debug.Log($"[{characterIndex}번 캐릭터] 활성 클래스 불러오기: {classType}");
        return classType;
    }

    // 인벤토리, 레벨 등도 같은 방식으로 추가 가능
} 