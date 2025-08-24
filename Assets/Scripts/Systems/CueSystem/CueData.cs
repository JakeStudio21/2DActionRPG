using UnityEngine;
using System.Collections.Generic;

namespace CueSystem
{
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
        [Range(0f, 1f)]
        public float volume = 1f;
        [Range(0.1f, 3f)]
        public float pitch = 1f;
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
        public string cameraShakePreset;  // 카메라 쉐이크 프리셋
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
        public string cameraShakePreset;
        public int timeStopMs = 0;
        
        public bool IsEmpty => vfxCues.Count == 0 && sfxCues.Count == 0;
    }
}
