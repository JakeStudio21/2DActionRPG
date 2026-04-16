using UnityEngine;
using System.Collections.Generic;

namespace CueSystem
{
    /// <summary>
    /// 이펙트 오브젝트에 적용할 회전 처리 방식
    /// Follow        : context.rotation 그대로 상속 (화살, 파이어볼 등 방향성 이펙트)
    /// Fixed         : 항상 Quaternion.identity — 바닥 장판처럼 방향 무관한 이펙트
    /// HorizontalFlip: X방향만 보고 좌우 반전(Y축 180°) — 메테오·비대칭 AOE처럼
    ///            오른쪽 기준으로 만들어진 이펙트를 왼쪽 공격 시 자연스럽게 뒤집을 때 사용.
    ///            scale.x=-1 대신 Y축 회전을 쓰는 이유: 파티클·자식 오브젝트에 음수 스케일이
    ///            전파되면 충돌체·소팅·파티클 방향이 꼬이는 버그를 방지하기 위함.
    /// FollowFlip    : Follow(회전 상속) + HorizontalFlip(좌방향 Y축 180°) 합산.
    ///            방향을 따라가면서 왼쪽 공격 시에만 추가로 뒤집힘.
    ///            상하 방향(facing.x ≈ 0)은 오른쪽(Flip 없음)으로 처리됨.
    /// </summary>
    public enum VFXRotationMode
    {
        Follow,
        Fixed,
        HorizontalFlip,
        FollowFlip
    }

    /// <summary>
    /// 🎵 VFX Cue 데이터 (단일 비주얼 이펙트)
    /// </summary>
    [System.Serializable]
    public class VFXCue
    {
        [Header("🎯 기본 정보")]
        public string vfxId;              // VFX 고유 ID
        public string poolKey;            // GamePoolManager 풀 키 (보통 프리팹명)
        
        [Header("⚡ 재생 설정")]
        public float duration = -1f;      // 지속시간 (-1: 자동감지)
        public Vector3 offset = Vector3.zero;
        public Vector3 scale = Vector3.one;
        public bool followTarget = false;  // 타겟 따라다니기
        
        [Header("🔄 회전 처리")]
        public VFXRotationMode rotationMode = VFXRotationMode.Follow;
        
        [Header("🎮 게임플레이")]
        public int priority = 50;         // 우선순위 (0~100)
        public float cooldown = 0f;       // 쿨다운 (초)
        
        [Header("📝 메타데이터")]
        public string note;               // 기획 메모
    }
    
    /// <summary>
    /// 🔊 SFX Cue 데이터 (단일 사운드 이펙트)  
    /// </summary>
    [System.Serializable]
    public class SFXCue
    {
        [Header("🎯 기본 정보")]
        public string sfxId;              // SFX 고유 ID
        public AudioClip audioClip;       // 오디오 클립
        
        [Header("🔊 오디오 설정")]
        [Range(0f, 3f)]
        public float volume = 1f;
        [Range(0.1f, 3f)]
        public float pitch = 1f;
        public bool loop = false;         // 루프 재생 (BGM용)
        public bool is3D = false;         // 3D 사운드 여부
        public float maxDistance = 50f;   // 3D 최대 거리
        
        [Header("🎮 게임플레이")]
        public int priority = 50;         // 우선순위 (0~100)
        public float cooldown = 0f;       // 쿨다운 (초)
        
        [Header("📝 메타데이터")]
        public string note;               // 기획 메모
    }
    
    /// <summary>
    /// 🎭 Cue Entry (이벤트 키 → VFX/SFX 매핑)
    /// </summary>
    [System.Serializable]
    public class CueEntry
    {
        [Header("🔑 이벤트 키")]
        public string eventKey;           // "attack.hit", "ui.button.click" 등
        
        [Header("🎨 이펙트 목록")]
        public List<string> vfxIds = new List<string>();
        public List<string> sfxIds = new List<string>();
        
        [Header("🎮 추가 효과")]
        public int priority = 50;         // 우선순위
        public ShakeData shakeData;       // 카메라 진동 설정
        public int timeStopMs = 0;        // 히트스톱 시간 (밀리초)
        
        [Header("📝 메타데이터")]
        public string note;               // 기획 메모
    }
    
    /// <summary>
    /// 🎯 해석된 Cue 슬롯 (런타임 사용)
    /// </summary>
    public class CueSlot
    {
        public List<VFXCue> vfxCues = new List<VFXCue>();
        public List<SFXCue> sfxCues = new List<SFXCue>();
        public int priority = 50;
        public ShakeData shakeData;
        public int timeStopMs = 0;
        
        // shakeData 또는 timeStopMs만 설정된 경우도 "비어있지 않음"으로 처리
        public bool IsEmpty =>
            vfxCues.Count == 0 &&
            sfxCues.Count == 0 &&
            (shakeData == null || !shakeData.useShake) &&
            timeStopMs == 0;
    }
}
