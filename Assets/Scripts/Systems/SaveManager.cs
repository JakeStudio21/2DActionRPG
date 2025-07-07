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

    // 인벤토리, 레벨 등도 같은 방식으로 추가 가능
} 