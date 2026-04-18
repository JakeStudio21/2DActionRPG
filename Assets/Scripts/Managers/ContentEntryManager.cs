using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ⚡ 범용 콘텐츠 입장 제한 관리자
/// - 일반 스테이지: 시간 충전형 스태미나 시스템
/// - 일일 던전: 일일 횟수 제한 + BM 티켓 시스템
/// </summary>
public class ContentEntryManager : MonoBehaviour
{
    public static ContentEntryManager Instance { get; private set; }
    
    [Header("⚡ 스태미나 설정")]
    [Tooltip("최대 스태미나")]
    public int maxStamina = 50;
    
    [Tooltip("스태미나 1회복당 소요 시간 (분)")]
    public int staminaRecoveryIntervalMinutes = 5;
    
    [Header("🏰 던전 설정")]
    [Tooltip("일일 던전 기본 입장 횟수")]
    public int maxDailyDungeonEntries = 3;
    
    [Header("🔧 디버그")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // ⚡ Phase D-Revision: AccountData 참조 (계정 공유)
    private AccountData CurrentAccountData
    {
        get
        {
            if (AccountDataManager.Instance != null)
                return AccountDataManager.Instance.GetAccountData();
            return null;
        }
    }
    
    private void Awake()
    {
        // 싱글톤 설정
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    private void Start()
    {
        // 게임 시작 시 오프라인 스태미나 회복 계산
        UpdateOfflineStamina();
    }
    
    // ========================================
    // ⚡ 스태미나 시스템 (일반 스테이지)
    // ========================================
    
    /// <summary>
    /// 오프라인 스태미나 회복 계산 (게임 시작 시 1회 호출)
    /// ⚡ Phase D-Revision: AccountData 사용 (계정 공유)
    /// </summary>
    public void UpdateOfflineStamina()
    {
        if (CurrentAccountData == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("⚠️ [ContentEntry] AccountData가 없어 오프라인 회복을 건너뜁니다.");
            return;
        }
        
        // 이미 MAX인 경우 회복 불필요
        if (CurrentAccountData.currentStamina >= maxStamina)
        {
            return;
        }
        
        // lastStaminaUpdateTime이 비어있으면 초기화
        if (string.IsNullOrEmpty(CurrentAccountData.lastStaminaUpdateTime))
        {
            // 현재 시간으로 초기화 (회복 시작)
            CurrentAccountData.lastStaminaUpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
            
            AccountDataManager.Instance?.Save();
            return;
        }
        
        // 마지막 회복 시간 파싱
        DateTime lastUpdate;
        if (!DateTime.TryParseExact(CurrentAccountData.lastStaminaUpdateTime, "yyyy-MM-dd HH:mm:ss", 
            null, System.Globalization.DateTimeStyles.None, out lastUpdate))
        {
            Debug.LogError($"❌ [ContentEntry] 시간 파싱 실패: {CurrentAccountData.lastStaminaUpdateTime}");
            
            // Fallback: 현재 시간으로 재설정
            CurrentAccountData.lastStaminaUpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            AccountDataManager.Instance?.Save();
            return;
        }
        
        DateTime now = DateTime.Now;
        TimeSpan elapsed = now - lastUpdate;
        int elapsedMinutes = (int)elapsed.TotalMinutes;
        
        // 회복 횟수 계산 (5분당 1)
        int recoveryCount = elapsedMinutes / staminaRecoveryIntervalMinutes;
        
        if (recoveryCount > 0)
        {
            int oldStamina = CurrentAccountData.currentStamina;
            CurrentAccountData.currentStamina = Mathf.Min(CurrentAccountData.currentStamina + recoveryCount, maxStamina);
            
            // ⚡ 핵심: MAX 도달 시 타이머 중지 (lastStaminaUpdateTime 초기화)
            if (CurrentAccountData.currentStamina >= maxStamina)
            {
                CurrentAccountData.lastStaminaUpdateTime = "";
                
            }
            else
            {
                // ⚡ 핵심: 회복한 시간만큼 lastStaminaUpdateTime을 앞당김
                int appliedMinutes = recoveryCount * staminaRecoveryIntervalMinutes;
                DateTime newUpdateTime = lastUpdate.AddMinutes(appliedMinutes);
                CurrentAccountData.lastStaminaUpdateTime = newUpdateTime.ToString("yyyy-MM-dd HH:mm:ss");
                
            }
            
            // 저장
            AccountDataManager.Instance?.Save();
        }
        else
        {
        }
    }
    
    /// <summary>
    /// 스태미나 소비 검증 (일반 스테이지 입장 전)
    /// ⚡ Phase D-Revision: 검증만 수행, 실제 차감은 입장 시점에
    /// </summary>
    public bool CanConsumeStamina(int amount)
    {
        if (CurrentAccountData == null)
        {
            Debug.LogError("❌ [ContentEntry] AccountData가 없습니다!");
            return false;
        }
        
        // 오프라인 회복 먼저 적용
        UpdateOfflineStamina();
        
        bool canConsume = CurrentAccountData.currentStamina >= amount;
        
        
        return canConsume;
    }
    
    /// <summary>
    /// 스태미나 실제 차감 (입장 시점에 호출)
    /// ⚡ Phase D-Revision: AccountData 사용
    /// </summary>
    public void ConsumeStamina(int amount)
    {
        if (CurrentAccountData == null)
        {
            Debug.LogError("❌ [ContentEntry] AccountData가 없습니다!");
            return;
        }
        
        int oldStamina = CurrentAccountData.currentStamina;
        CurrentAccountData.currentStamina = Mathf.Max(0, CurrentAccountData.currentStamina - amount);
        
        // ⚡ 핵심: MAX에서 MAX 미만으로 떨어지는 순간 타이머 시작
        if (oldStamina >= maxStamina && CurrentAccountData.currentStamina < maxStamina)
        {
            CurrentAccountData.lastStaminaUpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
        }
        
        
        // 즉시 저장
        AccountDataManager.Instance?.Save();
    }
    
    /// <summary>
    /// 현재 스태미나 조회 (UI용)
    /// ⚡ Phase D-Revision: AccountData 사용
    /// </summary>
    public int GetCurrentStamina()
    {
        if (CurrentAccountData == null)
            return 0;
        
        // 실시간 회복 반영
        UpdateOfflineStamina();
        
        return CurrentAccountData.currentStamina;
    }
    
    /// <summary>
    /// 최대 스태미나 조회 (UI용)
    /// </summary>
    public int GetMaxStamina()
    {
        return maxStamina;
    }
    
    /// <summary>
    /// 다음 회복까지 남은 시간 (초 단위)
    /// ⚡ Phase D-Revision: AccountData 사용
    /// </summary>
    public int GetSecondsUntilNextRecovery()
    {
        if (CurrentAccountData == null || CurrentAccountData.currentStamina >= maxStamina)
            return 0;
        
        if (string.IsNullOrEmpty(CurrentAccountData.lastStaminaUpdateTime))
            return 0;
        
        DateTime lastUpdate;
        if (!DateTime.TryParseExact(CurrentAccountData.lastStaminaUpdateTime, "yyyy-MM-dd HH:mm:ss",
            null, System.Globalization.DateTimeStyles.None, out lastUpdate))
        {
            return 0;
        }
        
        DateTime now = DateTime.Now;
        TimeSpan elapsed = now - lastUpdate;
        int elapsedMinutes = (int)elapsed.TotalMinutes;
        int remainingMinutes = staminaRecoveryIntervalMinutes - (elapsedMinutes % staminaRecoveryIntervalMinutes);
        
        return remainingMinutes * 60 - ((int)elapsed.TotalSeconds % 60);
    }
    
    // ========================================
    // 🏰 일일 던전 시스템 (카테고리별 제한)
    // ========================================
    
    /// <summary>
    /// 던전 카테고리 입장 가능 여부 검증 (입장 전 체크)
    /// ⚡ Phase D-Revision: categoryId 기반, AccountData 사용
    /// </summary>
    public bool CanEnterDailyDungeon(string categoryId)
    {
        if (CurrentAccountData == null)
        {
            Debug.LogError("❌ [ContentEntry] AccountData가 없습니다!");
            return false;
        }
        
        int remainCount = GetRemainDailyCount(categoryId);
        int tickets = CurrentAccountData.dailyDungeonTickets;
        
        bool canEnter = (remainCount > 0 || tickets > 0);
        
        
        return canEnter;
    }
    
    /// <summary>
    /// 던전 카테고리 입장 실제 차감 (입장 시점에 호출)
    /// ⚡ Phase D-Revision: categoryId 기반, 입장 시 즉시 차감
    /// </summary>
    public void ConsumeDungeonEntry(string categoryId)
    {
        if (CurrentAccountData == null)
        {
            Debug.LogError("❌ [ContentEntry] AccountData가 없습니다!");
            return;
        }
        
        var record = GetOrCreateCategoryRecord(categoryId);
        
        // 날짜 변경 체크
        if (IsDateChanged(record.lastPlayedDate))
        {
            record.dailyPlayCount = 0;
            record.lastPlayedDate = DateTime.Now.ToString("yyyy-MM-dd");
            
        }
        
        // ⚡ 우선순위: 기본 횟수 먼저 소진
        if (record.dailyPlayCount < maxDailyDungeonEntries)
        {
            record.dailyPlayCount++;
            
        }
        else if (CurrentAccountData.dailyDungeonTickets > 0)
        {
            CurrentAccountData.dailyDungeonTickets--;
            
        }
        else
        {
            Debug.LogError($"❌ [ContentEntry] 입장 불가능한 상태에서 차감 시도: {categoryId}");
            return;
        }
        
        // 즉시 저장
        AccountDataManager.Instance?.Save();
    }
    
    /// <summary>
    /// 남은 기본 입장 횟수 조회 (UI용)
    /// ⚡ Phase D-Revision: categoryId 기반
    /// </summary>
    public int GetRemainDailyCount(string categoryId)
    {
        if (CurrentAccountData == null)
            return 0;
        
        var record = GetOrCreateCategoryRecord(categoryId);
        
        // 날짜 변경 체크 (리셋)
        if (IsDateChanged(record.lastPlayedDate))
        {
            record.dailyPlayCount = 0;
            record.lastPlayedDate = DateTime.Now.ToString("yyyy-MM-dd");
        }
        
        int remain = Mathf.Max(0, maxDailyDungeonEntries - record.dailyPlayCount);
        return remain;
    }
    
    /// <summary>
    /// 보유 티켓 수 조회 (UI용)
    /// ⚡ Phase D-Revision: AccountData 사용
    /// </summary>
    public int GetTicketCount()
    {
        if (CurrentAccountData == null)
            return 0;
        
        return CurrentAccountData.dailyDungeonTickets;
    }
    
    /// <summary>
    /// 티켓 추가 (BM 구매용)
    /// ⚡ Phase D-Revision: AccountData 사용
    /// </summary>
    public void AddDungeonTickets(int count)
    {
        if (CurrentAccountData == null)
        {
            Debug.LogError("❌ [ContentEntry] AccountData가 없습니다!");
            return;
        }
        
        CurrentAccountData.dailyDungeonTickets += count;
        
        
        AccountDataManager.Instance?.Save();
    }
    
    // ========================================
    // 🔧 내부 헬퍼 메서드
    // ========================================
    
    /// <summary>
    /// 카테고리 기록 가져오기 (없으면 생성)
    /// ⚡ Phase D-Revision: categoryId 기반, AccountData 사용
    /// </summary>
    private CategoryEntryData GetOrCreateCategoryRecord(string categoryId)
    {
        if (CurrentAccountData == null)
            return null;
        
        // 기존 기록 찾기
        var record = CurrentAccountData.dungeonCategoryEntries.Find(r => r.categoryId == categoryId);
        
        // 없으면 생성
        if (record == null)
        {
            record = new CategoryEntryData(categoryId);
            CurrentAccountData.dungeonCategoryEntries.Add(record);
            
        }
        
        return record;
    }
    
    /// <summary>
    /// 날짜 변경 여부 확인
    /// </summary>
    private bool IsDateChanged(string lastPlayedDate)
    {
        if (string.IsNullOrEmpty(lastPlayedDate))
            return true;
        
        try
        {
            DateTime lastDate = DateTime.ParseExact(lastPlayedDate, "yyyy-MM-dd", null);
            DateTime today = DateTime.Now.Date;
            
            return lastDate.Date != today;
        }
        catch
        {
            // 파싱 실패 시 리셋
            Debug.LogWarning($"⚠️ [ContentEntry] 날짜 파싱 실패: {lastPlayedDate} → 리셋");
            return true;
        }
    }
    
    // ========================================
    // 🔧 유틸리티: 던전 ID → 카테고리 ID 변환
    // ========================================
    
    /// <summary>
    /// 던전 ID로부터 카테고리 ID 추출
    /// </summary>
    public static string GetCategoryIdFromDungeonId(string dungeonId)
    {
        if (string.IsNullOrEmpty(dungeonId))
            return "";
        
        // DG01_SB## 시리즈 → "Daily_Boss_Dungeon" (정령의 가호 던전)
        if (dungeonId.StartsWith("DG01_SB"))
            return "Daily_Boss_Dungeon";
        
        // DG02_WR## 시리즈 → "Weekly_Raid" (주간 레이드)
        if (dungeonId.StartsWith("DG02_WR"))
            return "Weekly_Raid";
        
        // DG03_MF## 시리즈 → "Material_Farm" (재료 파밍)
        if (dungeonId.StartsWith("DG03_MF"))
            return "Material_Farm";
        
        // 기본값 (던전이지만 카테고리 미지정)
        Debug.LogWarning($"⚠️ [ContentEntry] 알 수 없는 던전 ID 형식: {dungeonId}");
        return dungeonId; // Fallback: dungeonId 그대로 사용
    }
    
    /// <summary>
    /// 카테고리 ID → 팝업용 표시 이름 변환
    /// (개별 던전명 대신 카테고리명 표시로 "3회 제한이 카테고리 전체"임을 명확히)
    /// </summary>
    public static string GetCategoryDisplayNameForPopup(string categoryId)
    {
        if (string.IsNullOrEmpty(categoryId))
            return "던전";
        
        switch (categoryId)
        {
            case "Daily_Boss_Dungeon":
                return "정령의 가호 던전";
            case "Weekly_Raid":
                return "주간 레이드";
            case "Material_Farm":
                return "재료 파밍";
            case "Gold_Farm":
                return "골드 파밍";
            case "Exp_Farm":
                return "경험치 파밍";
            default:
                return categoryId;
        }
    }
}
