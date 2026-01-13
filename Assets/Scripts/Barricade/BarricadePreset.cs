using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 바리케이드 프리셋 ScriptableObject
/// 작업자가 프리셋을 선택하면 모든 설정이 자동으로 적용됨
/// </summary>
[CreateAssetMenu(fileName = "BarricadePreset", menuName = "Game/Barricade/Preset")]
public class BarricadePreset : ScriptableObject
{
    // ========================================
    // 프리셋 정보
    // ========================================
    [Header("==== 프리셋 정보 ====")]
    public string presetName = "일반 나무 바리케이드";
    [TextArea(3, 5)]
    public string description = "3타로 파괴되는 기본 바리케이드";
    public BarricadeCategory category = BarricadeCategory.Normal;
    
    // ========================================
    // 1. 파괴 모드 설정
    // ========================================
    [Header("==== 파괴 모드 ====")]
    public BreakMode breakMode = BreakMode.Hits;
    
    [Header("■ Hits 모드 설정")]
    [Tooltip("파괴에 필요한 타수")]
    public int hitsToBreak = 3;
    
    [Header("■ HP 모드 설정")]
    [Tooltip("최대 HP")]
    public int maxHP = 100;
    [Tooltip("데미지 숫자 표시 여부")]
    public bool showDamageNumbers = false;
    
    [Header("■ Condition 모드 설정")]
    [Tooltip("조건 타입 (SpecialKey, QuestComplete 등)")]
    public ConditionType conditionType = ConditionType.SpecialKey;
    [Tooltip("필요한 아이템 ID (예: SPECIAL_KEY)")]
    public string requiredItemID = "SPECIAL_KEY";
    [Tooltip("조건 미충족 시 표시할 메시지")]
    public string conditionMessage = "특별한 열쇠가 필요합니다";
    
    // ========================================
    // 2. 시각적 단계 설정
    // ========================================
    [Header("==== 시각적 단계 (Stages) ====")]
    [Tooltip("None: 변화 없음 / Simple: 3단계 스프라이트 / Advanced: 동적 단계+VFX")]
    public StageMode stageMode = StageMode.Simple;
    [Tooltip("단계별 스프라이트 및 VFX 설정")]
    public VisualStage[] visualStages;
    
    // ========================================
    // 3. 연출 레벨 (핵심!)
    // ========================================
    [Header("==== 연출 레벨 (0~3) ====")]
    [Range(0, 3)]
    [Tooltip("0: 조용히 파괴 / 1: 최소 연출 / 2: 표준 연출 / 3: 풀 연출")]
    public int presentationLevel = 2;
    /*
     * 0: 조용히 파괴 (사운드/VFX 없음) - 반복 덩쿨용
     * 1: 최소 연출 (작은 사운드 + 간단한 VFX)
     * 2: 표준 연출 (흔들림 + 균열 스프라이트)
     * 3: 풀 연출 (카메라 흔들림 + 파편 + 특수 보상 + 메시지)
     */
    
    // ========================================
    // 4. HP 바 설정
    // ========================================
    [Header("==== HP/타수 표시 ====")]
    [Tooltip("HP 바 표시 모드")]
    public HpBarMode hpBarMode = HpBarMode.Off;
    [Tooltip("남은 타수 표시 (true: '3', false: '2/5')")]
    public bool showRemainingHits = true;
    
    // ========================================
    // 5. 사운드/VFX 설정
    // ========================================
    [Header("==== 피격 피드백 ====")]
    [Tooltip("피격 시 사운드")]
    public AudioClip hitSound;
    [Tooltip("피격 시 VFX 프리팹")]
    public GameObject hitVFX;
    [Tooltip("피격 시 오브젝트 흔들림 강도 (0 = 없음)")]
    [Range(0f, 0.5f)]
    public float hitShakeIntensity = 0.05f;
    
    [Header("==== 파괴 피드백 ====")]
    [Tooltip("파괴 시 사운드")]
    public AudioClip destroySound;
    [Tooltip("파괴 시 VFX 프리팹")]
    public GameObject destroyVFX;
    [Tooltip("큰 폭발 이펙트 사용 여부")]
    public bool useBigDestroyFx = false;
    [Tooltip("파괴 시 카메라 흔들림 강도")]
    [Range(0f, 0.5f)]
    public float destroyShakeIntensity = 0.15f;
    
    [Header("==== 피로도 완화 설정 ====")]
    [Tooltip("카메라 흔들림 쿨다운 (초)")]
    public float cameraShakeCooldown = 0.5f;
    [Tooltip("큰 이펙트 쿨다운 (초)")]
    public float bigFxCooldown = 2f;
    [Tooltip("연속 재생 제한 활성화")]
    public bool limitConsecutivePlayback = true;
    
    // ========================================
    // 6. 보상 설정
    // ========================================
    [Header("==== 보상 모드 ====")]
    [Tooltip("보상 모드 (None: 없음 / Normal: 일반 드롭 / Special: 특수 보상)")]
    public RewardMode rewardMode = RewardMode.Normal;
    [Tooltip("특수 보상 테이블 (RewardMode.Special 시 사용)")]
    public BarricadeRewardTable rewardTable;
    
    // ========================================
    // 7. 메시지/UI
    // ========================================
    [Header("==== 메시지 ====")]
    [Tooltip("파괴 시 메시지 표시 여부")]
    public bool showMessageOnBreak = false;
    [Tooltip("표시할 메시지")]
    public string breakMessage = "길이 열렸습니다!";
    [Tooltip("메시지 표시 시간 (초)")]
    public float messageDuration = 2f;
    
    // ========================================
    // 8. 스테이지 연동
    // ========================================
    [Header("==== 스테이지 연동 ====")]
    [Tooltip("파괴 시 구역 해금 여부")]
    public bool unlockAreaOnDestroy = false;
    [Tooltip("해금할 구역 ID")]
    public string unlockAreaID;
    
    // ========================================
    // 9. 파편 효과 설정
    // ========================================
    [Header("==== 파편 효과 ====")]
    [Tooltip("파편 생성 여부")]
    public bool spawnDebris = false;
    [Tooltip("파편 개수")]
    [Range(3, 20)]
    public int debrisCount = 5;
    [Tooltip("파편 프리팹 (null이면 기본 사각형)")]
    public GameObject debrisPrefab;
}

// ========================================
// Enum 정의
// ========================================

/// <summary>
/// 바리케이드 카테고리
/// </summary>
public enum BarricadeCategory
{
    Normal,         // 일반 (반복 배치용)
    Important,      // 중요 (키 오브젝트)
    Boss,           // 보스룸 입구
    Secret          // 비밀 구역
}

/// <summary>
/// 파괴 모드
/// </summary>
public enum BreakMode
{
    Hits,           // 타수 기반 (반복 덩쿨 추천)
    HP,             // HP 기반 (보스 바리케이드)
    Condition       // 조건 기반 (열쇠 필요 등)
}

/// <summary>
/// 시각적 단계 모드
/// </summary>
public enum StageMode
{
    None,           // 변화 없음
    Simple,         // 3단계 스프라이트만 (옵션2)
    Advanced        // 동적 단계 + VFX + 애니메이션 (옵션3)
}

/// <summary>
/// HP 바 표시 모드
/// </summary>
public enum HpBarMode
{
    Off,            // 표시 안 함
    Small,          // 숫자만 (3/5)
    Full,           // 완전한 바
    OnlyImportant   // Important 카테고리만
}

/// <summary>
/// 보상 모드
/// </summary>
public enum RewardMode
{
    None,           // 보상 없음
    Normal,         // 일반 드롭 (PickUpSpawner)
    Special         // 특수 보상 (RewardTable)
}

/// <summary>
/// 조건 타입
/// </summary>
public enum ConditionType
{
    SpecialKey,     // 특수 열쇠
    QuestComplete,  // 퀘스트 완료
    AllEnemiesDead, // 모든 적 처치
    TimeElapsed     // 시간 경과
}

/// <summary>
/// 시각적 단계 정의
/// </summary>
[System.Serializable]
public class VisualStage
{
    [Tooltip("HP/타수 비율 (1.0 = 100%, 0.0 = 0%)")]
    [Range(0f, 1f)]
    public float hpThreshold = 1.0f;
    
    [Tooltip("이 단계의 스프라이트")]
    public Sprite sprite;
    
    [Tooltip("단계 진입 시 재생할 VFX (선택)")]
    public GameObject stageVFX;
    
    [Tooltip("단계 설명 (디버깅용)")]
    public string stageName = "정상";
}

