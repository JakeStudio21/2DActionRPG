using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class EconomyManager : Singleton<EconomyManager>
{
    private TMP_Text goldText;
    private int currentGold = 0;

    const string COIN_AMOUNT_TEXT = "Gold Amount Text";

    protected override void Awake()
    {
        base.Awake();
        
        // 씬 로드 이벤트 구독
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    protected override void OnDestroy()
    {
        // 이벤트 구독 해제
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        base.OnDestroy();  // 부모 클래스 메서드 호출
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 새 씬 로드시 goldText 참조 초기화
        goldText = null;
        
        // UI 초기화 (다음 프레임에 실행)
        StartCoroutine(InitializeGoldUI());
    }

    private IEnumerator InitializeGoldUI()
    {
        // UI가 완전히 로드될 때까지 대기
        yield return new WaitForEndOfFrame();
        
        // goldText 찾기 및 현재 골드 값으로 UI 업데이트
        FindGoldText();
        if (goldText != null)
        {
            goldText.text = currentGold.ToString("D3");
            Dbg.Log($"[EconomyManager] 씬 로드시 골드 UI 초기화: {currentGold}");
        }
    }

    private void FindGoldText()
    {
        if (goldText == null)
        {
            var goldTextObject = GameObject.Find(COIN_AMOUNT_TEXT);
            if (goldTextObject != null)
            {
                goldText = goldTextObject.GetComponent<TMP_Text>();
            }
        }
    }

    public void UpdateCurrentGold() 
    {
        currentGold += 1;

        FindGoldText();
        
        if (goldText != null)
        {
            goldText.text = currentGold.ToString("D3");
        }
        else
        {
            Debug.LogWarning("[EconomyManager] goldText를 찾을 수 없습니다!");
        }
    }

    // 골드 값 직접 설정 (SaveManager 연동용)
    public void SetGold(int gold)
    {
        currentGold = gold;
        FindGoldText();
        
        if (goldText != null)
        {
            goldText.text = currentGold.ToString("D3");
        }
    }

    // 현재 골드 값 반환
    public int GetCurrentGold()
    {
        return currentGold;
    }
}
