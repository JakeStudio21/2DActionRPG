using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CueSystem
{
    /// <summary>
    /// 🎯 Cue Emitter (정적 헬퍼 - 어디서든 키 발행 가능)
    /// 모든 스크립트에서 this.Emit() 형태로 사용 가능
    /// </summary>
    public static class CueEmitter
    {
        /// <summary>
        /// 🎵 메인 Emit API - 이벤트 키 발행 (정적 메서드)
        /// </summary>
        public static bool Emit(string eventKey, string domain = "Global", CueContext context = default)
        {
            // 🔍 초기화 상태 확인
            if (CuePlayer.Instance == null)
            {
                Debug.LogWarning($"⚠️ [CueEmitter] CuePlayer가 아직 초기화되지 않았습니다. 이벤트: {eventKey}");
                return false;
            }
            
            // 🔍 GamePoolManager 로딩 상태 확인 (BGM/UI는 제외)
            // BGM과 UI는 GamePoolManager와 무관하므로 체크 스킵
            bool isBGMOrUI = domain == "BGM" || domain == "UI" || domain == "Cutscene";
            if (!isBGMOrUI && GamePoolManager.Instance != null && GamePoolManager.Instance.IsLoadingPools)
            {
                Debug.LogWarning($"⚠️ [CueEmitter] GamePoolManager가 아직 풀을 로딩 중입니다. 이벤트: {eventKey}");
                return false;
            }
            
            // 🔍 SoundManager 상태 확인
            if (SoundManager.Instance == null)
            {
                Debug.LogWarning($"⚠️ [CueEmitter] SoundManager가 아직 초기화되지 않았습니다. 이벤트: {eventKey}");
                return false;
            }
            
            
            // 기존 로직 실행
            return CuePlayer.Instance.Play(eventKey, domain, context);
        }
        
        /// <summary>
        /// 🎯 Transform 기반 Emit (위치 자동 설정)
        /// </summary>
        public static bool Emit(this Transform transform, string eventKey, string domain = "Global", float magnitude = 1.0f)
        {
            var context = CueContext.From(transform, magnitude);
            return Emit(eventKey, domain, context);
        }
        
        /// <summary>
        /// 🎯 MonoBehaviour 기반 Emit (편의 메서드)
        /// </summary>
        public static bool Emit(this MonoBehaviour behaviour, string eventKey, string domain = "Global", float magnitude = 1.0f)
        {
            return behaviour.transform.Emit(eventKey, domain, magnitude);
        }
        
        /// <summary>
        /// 🎯 GameObject 기반 Emit (편의 메서드)
        /// </summary>
        public static bool Emit(this GameObject gameObject, string eventKey, string domain = "Global", float magnitude = 1.0f)
        {
            return gameObject.transform.Emit(eventKey, domain, magnitude);
        }
        
        /// <summary>
        /// 🎯 위치 기반 Emit (정적 이벤트용)
        /// </summary>
        public static bool EmitAt(Vector3 position, string eventKey, string domain = "Global", float magnitude = 1.0f)
        {
            var context = CueContext.At(position);
            context.magnitude = magnitude;
            return Emit(eventKey, domain, context);
        }
        
        /// <summary>
        /// 🎯 공격 히트 전용 Emit (표면/크리티컬 자동 감지)
        /// </summary>
        public static bool EmitHit(Transform attacker, Transform target, bool isCritical = false, float damage = 0f)
        {
            // 타겟의 표면 타입 감지
            var surfaceType = DetectSurfaceType(target);
            
            // 컨텍스트 생성
            var context = CueContext.From(target, 1.0f);
            context.isCritical = isCritical;
            context.damage = (int)damage;
            context.surfaceType = surfaceType;
            context.actorType = DetectActorType(target);
            
            // 키 생성: "hit.flesh.normal" 또는 "hit.metal.critical"
            string hitType = isCritical ? "critical" : "normal";
            string eventKey = $"hit.{surfaceType.ToString().ToLower()}.{hitType}";
            
            // 도메인 결정 (타겟 기준)
            string domain = DetermineDomain(target);
            
            return Emit(eventKey, domain, context);
        }
        
        #region Helper Methods
        
        /// <summary>
        /// 표면 타입 자동 감지
        /// </summary>
        private static SurfaceType DetectSurfaceType(Transform target)
        {
            // 태그 기반 감지
            switch (target.tag)
            {
                case "Player": return SurfaceType.Flesh;
                case "Enemy": return SurfaceType.Flesh;
                case "Metal": return SurfaceType.Metal;
                case "Wood": return SurfaceType.Wood;
                case "Stone": return SurfaceType.Stone;
                default: return SurfaceType.Default;
            }
        }
        
        /// <summary>
        /// 액터 타입 자동 감지
        /// </summary>
        private static ActorType DetectActorType(Transform target)
        {
            // 레이어 기반 감지
            int layer = target.gameObject.layer;
            switch (layer)
            {
                case 3: return ActorType.Player;    // Player 레이어
                case 6: return ActorType.Enemy;     // Enemy 레이어
                case 0: return ActorType.Environment; // Default 레이어
                default: return ActorType.Unknown;
            }
        }
        
        /// <summary>
        /// 도메인 자동 결정
        /// </summary>
        private static string DetermineDomain(Transform target)
        {
            var actorType = DetectActorType(target);
            switch (actorType)
            {
                case ActorType.Player: return "Player";
                case ActorType.Enemy: return "Enemy";
                case ActorType.Environment: return "Stage";
                default: return "Global";
            }
        }
        
        #endregion
    }
}
