using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public int characterIndex = 0; // 현재 선택된 캐릭터 번호
    public int gold = 0;

    // Start is called before the first frame update
    void Start()
    {
        // 게임 시작 시 저장된 골드 불러오기
        gold = SaveManager.Instance.LoadGold(characterIndex);
        
        // EconomyManager와 동기화
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.SetGold(gold);
            Debug.Log($"[PlayerManager] 저장된 골드를 EconomyManager에 동기화: {gold}");
        }
    }

    public void AddGold(int amount)
    {
        gold += amount;
        SaveManager.Instance.SaveGold(characterIndex, gold); // 골드가 바뀔 때마다 저장
        
        // EconomyManager와 동기화
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.SetGold(gold);
            Debug.Log($"[PlayerManager] 골드 추가 후 EconomyManager 동기화: {gold}");
        }
    }

    // EconomyManager의 골드를 PlayerManager로 동기화
    public void SyncGoldFromEconomy()
    {
        if (EconomyManager.Instance != null)
        {
            int economyGold = EconomyManager.Instance.GetCurrentGold();
            if (economyGold != gold)
            {
                gold = economyGold;
                SaveManager.Instance.SaveGold(characterIndex, gold);
                Debug.Log($"[PlayerManager] EconomyManager에서 골드 동기화: {gold}");
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // 주기적으로 EconomyManager와 동기화 (옵션)
        // SyncGoldFromEconomy();
    }
}
