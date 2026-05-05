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
        KillAll,          // 모든 적 처치
        BossKill,         // 보스 처치 (Boss Clear)
        Survival,         // 제한시간 생존
        ObjectiveComplete // 특정 목표 완수 (objectiveType 필드로 세부 종류 지정)
    }
    
    /// <summary>
    /// Victory = ObjectiveComplete일 때 세부 목표 종류
    /// </summary>
    [System.Serializable]
    public enum ObjectiveType
    {
        None,             // 미설정 (ObjectiveComplete 외 조건에서 사용)
        BarricadeDestroy, // isVictoryTarget=true 바리케이드를 모두 파괴
        ProtectObject,    // 특정 오브젝트가 파괴되지 않도록 보호 (향후 구현)
        ItemCollect,      // 지정 수량의 아이템 수집 (향후 구현)
        ExitReach         // 출구에 도달(접촉)하여 탈출 — 미로/장애물 코스 탈출 스테이지용
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
        TimerExpired,
        Zone1Enter,
        Zone2Enter,
        Zone3Enter
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
    /// 🏰 던전 카테고리 (Phase 1)
    /// </summary>
    [System.Serializable]
    public enum DungeonCategory
    {
        None,               // 카테고리 없음
        DailyBoss,          // 데일리 보스 던전
        WeeklyRaid,         // 주간 레이드
        MaterialFarm,       // 재료 파밍 던전
        GoldFarm,           // 골드 파밍 던전
        ExpFarm             // 경험치 파밍 던전
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
    
    /// <summary>
    /// 🏰 던전 ID 네이밍 규칙 상수 (Phase 1)
    /// </summary>
    public static class DungeonIdConstants
    {
        public const string DUNGEON_PREFIX = "DG";       // DG01, DG02
        public const int DUNGEON_ID_MIN_LENGTH = 4;     // DG01 (최소 4자)
        
        // 던전 카테고리별 Prefix (선택)
        public const string DAILY_BOSS_PREFIX = "DG_DAILY_";     // DG_DAILY_FOREST_BIND
        public const string WEEKLY_RAID_PREFIX = "DG_WEEKLY_";   // DG_WEEKLY_DRAGON
        public const string MATERIAL_PREFIX = "DG_MAT_";         // DG_MAT_CRYSTAL
        public const string GOLD_PREFIX = "DG_GOLD_";            // DG_GOLD_CAVE
        public const string EXP_PREFIX = "DG_EXP_";              // DG_EXP_TOWER
    }
}
