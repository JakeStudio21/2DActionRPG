using UnityEngine;

namespace StageSystem
{
    /// <summary>
    /// 던전 진행도 데이터 (Phase 1)
    /// StageProgress와 유사한 구조
    /// </summary>
    [System.Serializable]
    public class DungeonProgress
    {
        [Header("기본 정보")]
        public string dungeonId;
        public bool isCompleted;
        public bool isFirstClearRewarded;
        
        [Header("플레이 기록")]
        public int bestClearTime; // 초 단위
        public int clearCount; // 총 클리어 횟수
        public string lastClearDate; // YYYY-MM-DD 형식
        
        /// <summary>
        /// 기본 생성자 (JSON 역직렬화용)
        /// </summary>
        public DungeonProgress()
        {
            dungeonId = "";
            isCompleted = false;
            isFirstClearRewarded = false;
            bestClearTime = int.MaxValue;
            clearCount = 0;
            lastClearDate = "";
        }
        
        /// <summary>
        /// 파라미터 생성자
        /// </summary>
        public DungeonProgress(string dungeonId)
        {
            this.dungeonId = dungeonId;
            this.isCompleted = false;
            this.isFirstClearRewarded = false;
            this.bestClearTime = int.MaxValue;
            this.clearCount = 0;
            this.lastClearDate = "";
        }
        
        /// <summary>
        /// 클리어 기록 업데이트
        /// </summary>
        public void RecordClear(int clearTime, bool isFirstClear = false)
        {
            isCompleted = true;
            clearCount++;
            lastClearDate = System.DateTime.Now.ToString("yyyy-MM-dd");
            
            if (clearTime < bestClearTime)
            {
                bestClearTime = clearTime;
            }
            
            // 🏰 Phase 1: 첫 클리어 보상 지급 여부 기록
            if (isFirstClear)
            {
                isFirstClearRewarded = true;
            }
        }
        
        public override string ToString()
        {
            return $"DungeonProgress({dungeonId}, Clears:{clearCount}, Best:{bestClearTime}s)";
        }
    }
}

