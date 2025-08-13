using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스테이지 진행도 관리 시스템
/// 보스 처치 상태, 게이트 열림 상태 등을 관리
/// </summary>
public class StageProgressManager : Singleton<StageProgressManager>
{
    [Header("📊 스테이지 진행도")]
    [SerializeField] private Dictionary<string, bool> defeatedBosses = new Dictionary<string, bool>();
    [SerializeField] private Dictionary<string, bool> openedGates = new Dictionary<string, bool>();
    
    private string currentStageId = "";

    #region 🎯 보스 처치 관리

    /// <summary>
    /// 보스 처치 등록
    /// </summary>
    public void RegisterBossDefeat(string bossId)
    {
        if (string.IsNullOrEmpty(bossId)) return;
        
        defeatedBosses[bossId] = true;
        Debug.Log($"[StageProgress] 보스 처치 등록: {bossId}");
        
        // 스테이지 승리 조건 확인
        CheckStageVictoryCondition();
    }

    /// <summary>
    /// 보스 처치 여부 확인
    /// </summary>
    public bool IsBossDefeated(string bossId)
    {
        return defeatedBosses.ContainsKey(bossId) && defeatedBosses[bossId];
    }

    /// <summary>
    /// 현재 스테이지의 모든 보스 처치 여부 확인
    /// ⭐ 보스가 없는 스테이지 처리 개선
    /// </summary>
    public bool AreAllStageeBossesDefeated()
    {
        // 런타임에서 실제 보스 오브젝트들 확인
        EnemyHealth[] allEnemies = FindObjectsOfType<EnemyHealth>();
        
        int totalBossCount = 0;
        int deadBossCount = 0;
        
        foreach (var enemy in allEnemies)
        {
            if (enemy.IsBoss())
            {
                totalBossCount++;
                
                // ⭐ 개선: 사망 애니메이션 중도 죽은 것으로 간주
                if (enemy.isDead) // isDead가 true면 사망으로 처리
                {
                    deadBossCount++;
                    Debug.Log($"[StageProgress] 처치된 보스: {enemy.gameObject.name}");
                }
                else
                {
                    Debug.Log($"[StageProgress] 살아있는 보스: {enemy.gameObject.name}");
                }
            }
        }
        
        // ⭐ 중요: 보스가 없는 스테이지 처리
        if (totalBossCount == 0)
        {
            Debug.Log("[StageProgress] 이 스테이지에는 보스가 없습니다. 일반 스테이지입니다.");
            return false; // 보스가 없으면 승리 조건이 아님
        }
        
        // 모든 보스가 처치되었는지 확인
        bool allDefeated = (deadBossCount == totalBossCount);
        
        if (allDefeated)
        {
            Debug.Log($"[StageProgress] 모든 보스 처치 완료! ({deadBossCount}/{totalBossCount})");
        }
        else
        {
            Debug.Log($"[StageProgress] 보스 처치 현황: {deadBossCount}/{totalBossCount}");
        }
        
        return allDefeated;
    }

    #endregion

    #region 🚪 게이트 관리

    /// <summary>
    /// 게이트 열림 등록
    /// </summary>
    public void RegisterGateOpened(string gateId)
    {
        if (string.IsNullOrEmpty(gateId)) return;
        
        openedGates[gateId] = true;
        Debug.Log($"[StageProgress] 게이트 열림 등록: {gateId}");
    }

    /// <summary>
    /// 게이트 열림 여부 확인
    /// </summary>
    public bool IsGateOpened(string gateId)
    {
        return openedGates.ContainsKey(gateId) && openedGates[gateId];
    }

    #endregion

    #region 🎮 스테이지 관리

    /// <summary>
    /// 새 스테이지 시작 시 초기화
    /// </summary>
    public void StartNewStage(string stageId)
    {
        currentStageId = stageId;
        
        // ⭐ 중요: 스테이지별로 진행도 초기화
        defeatedBosses.Clear();
        openedGates.Clear();
        
        Debug.Log($"[StageProgress] 새 스테이지 시작: {stageId}");
    }

    /// <summary>
    /// 스테이지 승리 조건 확인
    /// </summary>
    private void CheckStageVictoryCondition()
    {
        if (AreAllStageeBossesDefeated())
        {
            Debug.Log($"[StageProgress] 스테이지 {currentStageId} 승리!");
            
            // FSMStageController에 승리 신호 전송
            var stageController = FindObjectOfType<FSMStageController>();
            if (stageController != null)
            {
                // TriggerVictory() 호출 또는 이벤트 발생
                Debug.Log("[StageProgress] FSMStageController에 승리 신호 전송");
            }
        }
    }

    #endregion

    #region 💾 세이브/로드 (향후 확장)

    /// <summary>
    /// 진행도 데이터 저장용 구조체
    /// </summary>
    [System.Serializable]
    public class StageProgressData
    {
        public string stageId;
        public List<string> defeatedBossIds = new List<string>();
        public List<string> openedGateIds = new List<string>();
    }

    /// <summary>
    /// 현재 진행도를 JSON으로 저장
    /// </summary>
    public string SaveProgressToJson()
    {
        StageProgressData data = new StageProgressData();
        data.stageId = currentStageId;
        
        foreach (var boss in defeatedBosses)
        {
            if (boss.Value) data.defeatedBossIds.Add(boss.Key);
        }
        
        foreach (var gate in openedGates)
        {
            if (gate.Value) data.openedGateIds.Add(gate.Key);
        }
        
        return JsonUtility.ToJson(data, true);
    }

    /// <summary>
    /// JSON에서 진행도 로드
    /// </summary>
    public void LoadProgressFromJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        
        StageProgressData data = JsonUtility.FromJson<StageProgressData>(json);
        currentStageId = data.stageId;
        
        defeatedBosses.Clear();
        foreach (string bossId in data.defeatedBossIds)
        {
            defeatedBosses[bossId] = true;
        }
        
        openedGates.Clear();
        foreach (string gateId in data.openedGateIds)
        {
            openedGates[gateId] = true;
        }
        
        Debug.Log($"[StageProgress] 진행도 로드 완료: {currentStageId}");
    }

    #endregion
}
