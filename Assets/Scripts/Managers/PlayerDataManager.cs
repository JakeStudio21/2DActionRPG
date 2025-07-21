using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System;

/// <summary>
/// ⭐ [Phase 1] 모든 플레이어 데이터를 통합 관리하는 매니저
/// 골드, 레벨, 경험치, 인벤토리, 장비 등을 하나의 시스템에서 관리
/// 기존 PlayerManager + PlayerLevel + 미래 확장성을 위한 통합 솔루션
/// </summary>
public class PlayerDataManager : Singleton<PlayerDataManager>
{
    [Header("🎮 플레이어 기본 정보")]
    public int characterIndex = 0; // 현재 선택된 캐릭터 번호
    public string playerName = "Player"; // 플레이어 이름
    
    [Header("💰 재화 관리")]
    [SerializeField] private int currentGold = 0;
    
    [Header("📈 레벨 & 경험치")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int currentExp = 0;
    [SerializeField] private int expToNextLevel = 100;
    
    [Header("🔧 UI 관리")]
    private TMP_Text goldText;
    private const string COIN_AMOUNT_TEXT = "Gold Amount Text";
    
    [Header("📊 디버그")]
    [SerializeField] private bool showDebugLogs = true;
    
    // 이벤트 시스템
    public event Action<int> OnGoldChanged;
    public event Action<int> OnLevelChanged;
    public event Action<int, int> OnExpChanged; // (currentExp, expToNextLevel)
    
    // 접근자 프로퍼티
    public int CurrentGold => currentGold;
    public int CurrentLevel => currentLevel;
    public int CurrentExp => currentExp;
    public int ExpToNextLevel => expToNextLevel;
    
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
        // 게임 시작 시 저장된 데이터 불러오기
        LoadAllPlayerData();
        
        // UI 초기화
        StartCoroutine(InitializeUI());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 새 씬 로드시 UI 참조 초기화
        goldText = null;
        
        // UI 초기화 (다음 프레임에 실행)
        StartCoroutine(InitializeUI());
    }

    #region 💰 골드 관리 시스템
    
    /// <summary>
    /// 골드 추가
    /// </summary>
    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        
        currentGold += amount;
        SavePlayerData();
        UpdateGoldUI();
        OnGoldChanged?.Invoke(currentGold);
        
        if (showDebugLogs)
            Debug.Log($"💰 [PlayerData] 골드 추가: +{amount}, 현재: {currentGold}");
    }
    
    /// <summary>
    /// 골드 소모
    /// </summary>
    public bool SpendGold(int amount)
    {
        if (amount <= 0) return false;
        
        if (currentGold >= amount)
        {
            currentGold -= amount;
            SavePlayerData();
            UpdateGoldUI();
            OnGoldChanged?.Invoke(currentGold);
            
            if (showDebugLogs)
                Debug.Log($"💰 [PlayerData] 골드 소모: -{amount}, 현재: {currentGold}");
            return true;
        }
        else
        {
            if (showDebugLogs)
                Debug.LogWarning($"💰 [PlayerData] 골드 부족! 필요: {amount}, 보유: {currentGold}");
            return false;
        }
    }
    
    /// <summary>
    /// 골드 직접 설정 (호환성 유지)
    /// </summary>
    public void SetGold(int gold)
    {
        currentGold = Mathf.Max(0, gold);
        SavePlayerData();
        UpdateGoldUI();
        OnGoldChanged?.Invoke(currentGold);
        
        if (showDebugLogs)
            Debug.Log($"💰 [PlayerData] 골드 설정: {currentGold}");
    }
    
    #endregion
    
    #region 📈 레벨 & 경험치 관리 시스템
    
    /// <summary>
    /// 경험치 추가 및 레벨업 체크
    /// </summary>
    public void AddExp(int expAmount)
    {
        if (expAmount <= 0) return;
        
        currentExp += expAmount;
        OnExpChanged?.Invoke(currentExp, expToNextLevel);
        
        if (showDebugLogs)
            Debug.Log($"📈 [PlayerData] 경험치 {expAmount} 획득! 현재: {currentExp}/{expToNextLevel}");

        // 레벨업 체크 (여러 레벨업 가능)
        while (currentExp >= expToNextLevel)
        {
            LevelUp();
        }
        
        SavePlayerData();
    }
    
    /// <summary>
    /// 레벨업 처리
    /// </summary>
    private void LevelUp()
    {
        currentExp -= expToNextLevel;
        currentLevel++;
        
        // 다음 레벨 필요 경험치 계산 (1.2배씩 증가)
        expToNextLevel = Mathf.RoundToInt(expToNextLevel * 1.2f);
        
        // 이벤트 발생
        OnLevelChanged?.Invoke(currentLevel);
        OnExpChanged?.Invoke(currentExp, expToNextLevel);
        
        if (showDebugLogs)
            Debug.Log($"🆙 [PlayerData] 레벨 업! Lv.{currentLevel} (다음 레벨까지: {expToNextLevel - currentExp})");
        
        // 클래스별 레벨업 보너스 적용
        ApplyLevelUpBonus();
    }
    
    /// <summary>
    /// 레벨 직접 설정 (치트/테스트용)
    /// </summary>
    public void SetLevel(int level, int exp = 0)
    {
        currentLevel = Mathf.Max(1, level);
        currentExp = Mathf.Max(0, exp);
        
        // 레벨에 맞는 필요 경험치 계산
        expToNextLevel = CalculateExpForLevel(currentLevel + 1);
        
        OnLevelChanged?.Invoke(currentLevel);
        OnExpChanged?.Invoke(currentExp, expToNextLevel);
        SavePlayerData();
        
        if (showDebugLogs)
            Debug.Log($"📈 [PlayerData] 레벨 설정: Lv.{currentLevel}, EXP: {currentExp}/{expToNextLevel}");
    }
    
    /// <summary>
    /// 특정 레벨에 필요한 총 경험치 계산
    /// </summary>
    private int CalculateExpForLevel(int targetLevel)
    {
        int baseExp = 100;
        for (int i = 2; i <= targetLevel; i++)
        {
            baseExp = Mathf.RoundToInt(baseExp * 1.2f);
        }
        return baseExp;
    }
    
    /// <summary>
    /// 레벨업 시 클래스별 보너스 적용
    /// </summary>
    private void ApplyLevelUpBonus()
    {
        // 활성 클래스들에게 레벨업 알림
        var activeClasses = FindObjectsOfType<BaseClassBehaviour>();
        foreach (var classComp in activeClasses)
        {
            if (classComp.IsActiveClass)
            {
                classComp.OnLevelUp(currentLevel);
            }
        }
    }
    
    #endregion
    
    #region 💾 저장/로드 시스템
    
    /// <summary>
    /// 모든 플레이어 데이터 저장
    /// </summary>
    public void SavePlayerData()
    {
        if (SaveManager.Instance == null) return;
        
                 var saveData = new PlayerSaveData
         {
             characterIndex = this.characterIndex,
             playerName = this.playerName,
             lastPlayTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
             
             gold = this.currentGold,
             level = this.currentLevel,
             exp = this.currentExp,
             expToNextLevel = this.expToNextLevel
         };
        
        string json = saveData.ToJson();
        string key = $"PlayerData_{characterIndex}";
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();
        
        if (showDebugLogs)
            Debug.Log($"💾 [PlayerData] 데이터 저장 완료: {saveData}");
    }
    
    /// <summary>
    /// 모든 플레이어 데이터 로드
    /// </summary>
    public void LoadAllPlayerData()
    {
        if (SaveManager.Instance == null) return;
        
        string key = $"PlayerData_{characterIndex}";
        string json = PlayerPrefs.GetString(key, "");
        
        if (string.IsNullOrEmpty(json))
        {
            if (showDebugLogs)
                Debug.Log($"📁 [PlayerData] 저장 데이터 없음. 기본값 사용.");
            return;
        }
        
        var saveData = PlayerSaveData.FromJson(json);
        if (saveData != null)
        {
            this.currentGold = saveData.gold;
            this.currentLevel = saveData.level;
            this.currentExp = saveData.exp;
            this.expToNextLevel = saveData.expToNextLevel;
            this.playerName = saveData.playerName;
            
            // 이벤트 발생 (UI 업데이트)
            OnGoldChanged?.Invoke(currentGold);
            OnLevelChanged?.Invoke(currentLevel);
            OnExpChanged?.Invoke(currentExp, expToNextLevel);
            
            if (showDebugLogs)
                Debug.Log($"📁 [PlayerData] 데이터 로드 완료: {saveData}");
        }
    }
    
    #endregion
    
    #region 🎨 UI 관리 시스템
    
    /// <summary>
    /// UI 초기화
    /// </summary>
    private IEnumerator InitializeUI()
    {
        // UI가 완전히 로드될 때까지 대기
        yield return new WaitForEndOfFrame();
        
        // UI 찾기 및 업데이트
        FindUIElements();
        UpdateAllUI();
    }
    
    /// <summary>
    /// UI 요소 찾기
    /// </summary>
    private void FindUIElements()
    {
        // 골드 텍스트 찾기
        if (goldText == null)
        {
            var goldTextObject = GameObject.Find(COIN_AMOUNT_TEXT);
            if (goldTextObject != null)
            {
                goldText = goldTextObject.GetComponent<TMP_Text>();
            }
        }
        
        // 레벨 텍스트는 LevelUI에서 자동 관리되므로 여기서는 찾지 않음
    }
    
    /// <summary>
    /// 골드 UI 업데이트
    /// </summary>
    private void UpdateGoldUI()
    {
        FindUIElements();
        
        if (goldText != null)
        {
            goldText.text = currentGold.ToString("D3");
        }
    }
    
    /// <summary>
    /// 모든 UI 업데이트
    /// </summary>
    private void UpdateAllUI()
    {
        UpdateGoldUI();
        // 레벨 UI는 이벤트로 자동 업데이트됨
    }
    
    #endregion
    
    #region 🔧 호환성 메서드 (기존 시스템 연동)
    
    /// <summary>
    /// EconomyManager.UpdateCurrentGold() 호환성 메서드
    /// </summary>
    public void UpdateCurrentGold()
    {
        AddGold(1);
    }
    
    /// <summary>
    /// PlayerManager.GetCurrentGold() 호환성 메서드
    /// </summary>
    public int GetCurrentGold()
    {
        return currentGold;
    }
    
    #endregion
    
    #region 📊 디버그 메서드
    
    /// <summary>
    /// 현재 플레이어 상태 출력
    /// </summary>
    public void PrintPlayerStatus()
    {
        Debug.Log($"🎮 [PlayerData] 플레이어 상태:");
        Debug.Log($"   - 캐릭터: {playerName} (#{characterIndex})");
        Debug.Log($"   - 레벨: {currentLevel} ({currentExp}/{expToNextLevel})");
        Debug.Log($"   - 골드: {currentGold}");
    }
    
    /// <summary>
    /// 치트: 골드/경험치 추가 (테스트용)
    /// </summary>
    [ContextMenu("치트: 골드 +100")]
    public void CheatAddGold() => AddGold(100);
    
    [ContextMenu("치트: 경험치 +50")]
    public void CheatAddExp() => AddExp(50);
    
    #endregion
}

/// <summary>
/// ⭐ [Phase 1] 플레이어 저장 데이터 구조
/// 향후 Phase 2에서 모듈식으로 확장 예정
/// </summary>
[System.Serializable]
public class PlayerSaveData
{
    [Header("기본 정보")]
    public int characterIndex;
    public string playerName = "Player";
    public string lastPlayTime; // DateTime을 string으로 저장
    
    [Header("진행 데이터")]
    public int gold;
    public int level;
    public int exp;
    public int expToNextLevel;
    
    // 향후 확장 예정
    // public List<ItemSaveData> inventory;
    // public Dictionary<string, ItemSaveData> equipment;
    
    /// <summary>
    /// JSON 문자열로 변환
    /// </summary>
    public string ToJson()
    {
        return JsonUtility.ToJson(this, true);
    }
    
    /// <summary>
    /// JSON 문자열에서 복원
    /// </summary>
    public static PlayerSaveData FromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            return null;
            
        return JsonUtility.FromJson<PlayerSaveData>(json);
    }
    
    /// <summary>
    /// 디버깅용 문자열 표현
    /// </summary>
    public override string ToString()
    {
        return $"PlayerData[{playerName}] Lv.{level} Gold:{gold} EXP:{exp}/{expToNextLevel}";
    }
} 