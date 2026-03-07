using System;
using UnityEngine;

namespace StageSystem
{
    /// <summary>
    /// 스테이지 진행도 데이터 모델
    /// JSON 직렬화 지원
    /// </summary>
    [System.Serializable]
    public class StageProgress
    {
        [Header("기본 정보")]
        public string stageId;
        public bool isUnlocked;
        public bool isCompleted;
        public bool isFirstClearRewarded;
        
        [Header("플레이 기록")]
        public int bestClearTime; // 초 단위
        public int clearCount;
        public string lastPlayedTime; // DateTime을 string으로 저장
        
        /// <summary>
        /// 기본 생성자 (JSON 역직렬화용)
        /// </summary>
        public StageProgress()
        {
            stageId = "";
            isUnlocked = false;
            isCompleted = false;
            isFirstClearRewarded = false;
            bestClearTime = int.MaxValue;
            clearCount = 0;
            lastPlayedTime = DateTime.MinValue.ToString("o");
        }
        
        /// <summary>
        /// 초기화 생성자
        /// </summary>
        public StageProgress(string stageId, bool isUnlocked = false)
        {
            this.stageId = stageId;
            this.isUnlocked = isUnlocked;
            this.isCompleted = false;
            this.isFirstClearRewarded = false;
            this.bestClearTime = int.MaxValue;
            this.clearCount = 0;
            this.lastPlayedTime = DateTime.MinValue.ToString("o");
        }
        
        /// <summary>
        /// 스테이지 완료 처리
        /// </summary>
        public void CompleteStage(int clearTime, bool isFirstClear = false)
        {
            isCompleted = true;
            clearCount++;
            lastPlayedTime = DateTime.Now.ToString("o");
            
            // 최고 기록 갱신
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
        
        /// <summary>
        /// 첫 클리어 보상 수령
        /// </summary>
        public void ClaimFirstClearReward()
        {
            if (isCompleted && !isFirstClearRewarded)
            {
                isFirstClearRewarded = true;
            }
        }
        
        /// <summary>
        /// 첫 클리어 보상 수령 가능 여부
        /// </summary>
        public bool CanClaimFirstClearReward()
        {
            return isCompleted && !isFirstClearRewarded;
        }
        
        /// <summary>
        /// 마지막 플레이 시간 DateTime으로 변환
        /// </summary>
        public DateTime GetLastPlayedDateTime()
        {
            if (DateTime.TryParse(lastPlayedTime, out DateTime result))
                return result;
            return DateTime.MinValue;
        }
        
        /// <summary>
        /// 최고 기록 시간을 포맷된 문자열로 반환
        /// </summary>
        public string GetBestClearTimeString()
        {
            if (bestClearTime == int.MaxValue)
                return "기록 없음";
            
            int minutes = bestClearTime / 60;
            int seconds = bestClearTime % 60;
            return $"{minutes:D2}:{seconds:D2}";
        }
        
        /// <summary>
        /// 디버그용 정보 출력
        /// </summary>
        public override string ToString()
        {
            return $"[{stageId}] 해금:{isUnlocked}, 완료:{isCompleted}, " +
                   $"보상:{isFirstClearRewarded}, 클리어:{clearCount}회, " +
                   $"최고기록:{GetBestClearTimeString()}";
        }
    }
}
