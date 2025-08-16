using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StageSystem
{
    /// <summary>
    /// 스테이지 시스템 공통 데이터 타입 정의
    /// CSV 데이터와 일치하도록 구성
    /// </summary>
    
    [System.Serializable]
    public enum BackgroundType
    {
        FIELD,
        DUNGEON,
        BOSS_ROOM
    }
    
    [System.Serializable]
    public enum VictoryCondition
    {
        KillAll,        // 모든 적 처치
        BossKill,       // 보스 처치 (Boss Clear)
        Survival,       // 제한시간 생존
        ObjectiveComplete // 특정 목표 완수
    }
    
    [System.Serializable]
    public enum WaveStartCondition
    {
        AutoAfterDelay, // 자동 시작 (딜레이 후)
        OnClearPrev,    // 이전 웨이브 완료 후
        OnTrigger       // 특정 트리거 (보스 게이트 등)
    }
    
    [System.Serializable]
    public enum WaveTriggerId
    {
        None,
        BossGateOpened,
        PlayerReachedPoint,
        TimerExpired
    }
    
    [System.Serializable]
    public enum SpawnType
    {
        Point,          // 정확한 위치
        Area,           // 반경 내 랜덤
        Circle,         // 원형 배치
        Rectangle       // 직사각형 배치
    }
    
    [System.Serializable]
    public enum DropGroupType
    {
        STAGE_CLEAR_FIRST,  // 첫 클리어 보상
        STAGE_CLEAR_REPEAT, // 반복 클리어 보상
        MONSTER_DROP,       // 몬스터 드롭
        TREASURE_BOX        // 보물상자
    }
    
    /// <summary>
    /// 스테이지 ID 네이밍 규칙 상수
    /// </summary>
    public static class StageIdConstants
    {
        public const string STAGE_PREFIX = "STAGE_";
        public const string WAVE_PREFIX = "_WAVE_";
        public const string GROUP_PREFIX = "_GROUP_";
        public const string DROP_PREFIX = "DROP_";
        
        public const int STAGE_ID_LENGTH = 3;    // 001, 002, 003
        public const int WAVE_ID_LENGTH = 2;     // 01, 02, 03
        public const int GROUP_ID_LENGTH = 2;    // 01, 02, 03
    }
}
