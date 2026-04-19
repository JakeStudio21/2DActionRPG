using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CueSystem
{
    /// <summary>
    /// 🎬 Animation Event Relay (애니메이션 이벤트 → Cue 발행 중계)
    /// Animation Event에서 문자열로 호출하면 CueEmitter로 전달
    /// </summary>
    public class AnimationEventRelay : MonoBehaviour
    {
        [Header("🎭 도메인 설정")]
        [SerializeField] private string defaultDomain = "Global";
        [SerializeField] private bool autoDetectDomain = true;
        
        [Header("🔧 디버그 설정")]
        // 도메인 캐시 (성능 최적화)
        private string _cachedDomain;
        
        private void Awake()
        {
            // 도메인 미리 계산
            if (autoDetectDomain)
            {
                _cachedDomain = DetermineDomainFromGameObject();
            }
            else
            {
                _cachedDomain = defaultDomain;
            }
            
        }
        
        /// <summary>
        /// 🎵 Animation Event 메서드 - 기본 키 발행
        /// Animation Event에서 "EmitCue:attack.hit" 형태로 호출
        /// </summary>
        public void EmitCue(string eventKey)
        {
            if (string.IsNullOrEmpty(eventKey))
            {
                Debug.LogWarning("⚠️ [AnimationEventRelay] 빈 이벤트 키");
                return;
            }
            
            var context = CueContext.From(transform);
            bool success = CueEmitter.Emit(eventKey, _cachedDomain, context);
            
        }
        
        /// <summary>
        /// 🎵 Animation Event 메서드 - 강도 지정 키 발행
        /// Animation Event에서 "EmitCueWithMagnitude:attack.hit:1.5" 형태로 호출
        /// </summary>
        public void EmitCueWithMagnitude(string eventKeyAndMagnitude)
        {
            if (string.IsNullOrEmpty(eventKeyAndMagnitude))
            {
                Debug.LogWarning("⚠️ [AnimationEventRelay] 빈 이벤트 키");
                return;
            }
            
            // "attack.hit:1.5" 파싱
            string[] parts = eventKeyAndMagnitude.Split(':');
            string eventKey = parts[0];
            float magnitude = parts.Length > 1 && float.TryParse(parts[1], out float mag) ? mag : 1.0f;
            
            var context = CueContext.From(transform, magnitude);
            bool success = CueEmitter.Emit(eventKey, _cachedDomain, context);
            
        }
        
        /// <summary>
        /// 🎵 Animation Event 메서드 - 도메인 지정 키 발행
        /// Animation Event에서 "EmitCueWithDomain:attack.hit:Enemy" 형태로 호출
        /// </summary>
        public void EmitCueWithDomain(string eventKeyAndDomain)
        {
            if (string.IsNullOrEmpty(eventKeyAndDomain))
            {
                Debug.LogWarning("⚠️ [AnimationEventRelay] 빈 이벤트 키");
                return;
            }
            
            // "attack.hit:Enemy" 파싱
            string[] parts = eventKeyAndDomain.Split(':');
            string eventKey = parts[0];
            string domain = parts.Length > 1 ? parts[1] : _cachedDomain;
            
            var context = CueContext.From(transform);
            bool success = CueEmitter.Emit(eventKey, domain, context);
            
        }
        
        /// <summary>
        /// 🎵 레거시 호환 - 기존 Animation Event 메서드들
        /// 기존 코드와의 호환성을 위해 유지
        /// </summary>
        public void AttackHit()
        {
            EmitCue("attack.hit");
        }
        
        public void SpawnProjectileAnimEvent()
        {
            EmitCue("attack.projectile");
        }
        
        public void FootStep()
        {
            EmitCue("movement.footstep");
        }
        
        public void JumpLand()
        {
            EmitCue("movement.land");
        }
        
        /// <summary>
        /// GameObject로부터 도메인 자동 감지
        /// </summary>
        private string DetermineDomainFromGameObject()
        {
            // 레이어 기반 감지
            int layer = gameObject.layer;
            switch (layer)
            {
                case 3: return "Player";    // Player 레이어
                case 6: return "Enemy";     // Enemy 레이어
                case 0: return "Stage";     // Default 레이어 (환경)
                default:
                    // 태그 기반 백업 감지
                    switch (tag)
                    {
                        case "Player": return "Player";
                        case "Enemy": return "Enemy";
                        case "UI": return "UI";
                        default: return "Global";
                    }
            }
        }
        
        /// <summary>
        /// 런타임 도메인 변경 (필요시)
        /// </summary>
        public void SetDomain(string newDomain)
        {
            _cachedDomain = newDomain;
        }
    }
}
