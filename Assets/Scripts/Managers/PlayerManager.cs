using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// 플레이어 관련 모든 데이터를 관리하는 통합 매니저
/// EconomyManager 기능을 통합하여 골드 관리 중복 제거
/// </summary>
public class PlayerManager : Singleton<PlayerManager>
{
    [Header("플레이어 데이터")]
    public int characterIndex = 0; // 현재 선택된 캐릭터 번호
    public int currentGold = 0;

    [Header("UI 관리")]
    private TMP_Text goldText;
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
        
        base.OnDestroy();
    }

    private void Start()
    {
        // 게임 시작 시 저장된 골드 불러오기
        LoadPlayerData();
        
        // UI 초기화
        StartCoroutine(InitializeGoldUI());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 새 씬 로드시 goldText 참조 초기화
        goldText = null;
        
        // UI 초기화 (다음 프레임에 실행)
        StartCoroutine(InitializeGoldUI());
    }

    /// <summary>
    /// 플레이어 데이터 로드
    /// </summary>
    private void LoadPlayerData()
    {
        currentGold = SaveManager.Instance.LoadGold(characterIndex);
        Debug.Log($"[PlayerManager] 플레이어 데이터 로드 완료 - 골드: {currentGold}");
    }

    /// <summary>
    /// 골드 UI 초기화
    /// </summary>
    private IEnumerator InitializeGoldUI()
    {
        // UI가 완전히 로드될 때까지 대기
        yield return new WaitForEndOfFrame();
        
        // goldText 찾기 및 현재 골드 값으로 UI 업데이트
        FindGoldText();
        UpdateGoldUI();
    }

    /// <summary>
    /// 골드 텍스트 UI 찾기
    /// </summary>
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

    /// <summary>
    /// 골드 추가 (기존 AddGold + EconomyManager의 UpdateCurrentGold 통합)
    /// </summary>
    public void AddGold(int amount = 1)
    {
        currentGold += amount;
        SavePlayerData();
        UpdateGoldUI();
        
        Debug.Log($"[PlayerManager] 골드 추가: +{amount}, 현재: {currentGold}");
    }

    /// <summary>
    /// 골드 소모
    /// </summary>
    public bool SpendGold(int amount)
    {
        if (currentGold >= amount)
        {
            currentGold -= amount;
            SavePlayerData();
            UpdateGoldUI();
            
            Debug.Log($"[PlayerManager] 골드 소모: -{amount}, 현재: {currentGold}");
            return true;
        }
        else
        {
            Debug.LogWarning($"[PlayerManager] 골드 부족! 필요: {amount}, 보유: {currentGold}");
            return false;
        }
    }

    /// <summary>
    /// 골드 직접 설정 (호환성 유지)
    /// </summary>
    public void SetGold(int gold)
    {
        currentGold = gold;
        SavePlayerData();
        UpdateGoldUI();
        
        Debug.Log($"[PlayerManager] 골드 설정: {currentGold}");
    }

    /// <summary>
    /// 현재 골드 반환
    /// </summary>
    public int GetCurrentGold()
    {
        return currentGold;
    }

    /// <summary>
    /// 골드 UI 업데이트 (EconomyManager 기능 통합)
    /// </summary>
    private void UpdateGoldUI()
    {
        FindGoldText();
        
        if (goldText != null)
        {
            goldText.text = currentGold.ToString("D3");
        }
        else if (Time.frameCount % 300 == 0) // 5초마다 한 번씩만 경고
        {
            Debug.LogWarning("[PlayerManager] goldText UI를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 플레이어 데이터 저장
    /// </summary>
    private void SavePlayerData()
    {
        SaveManager.Instance.SaveGold(characterIndex, currentGold);
    }

    // ===== EconomyManager 호환성 메서드들 =====
    
    /// <summary>
    /// EconomyManager.UpdateCurrentGold() 호환성 메서드
    /// </summary>
    public void UpdateCurrentGold()
    {
        AddGold(1);
    }
}
