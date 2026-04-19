using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CueSystem
{
    /// <summary>
    /// 💥 Hit Event Relay (히트 이벤트 → 표면/크리티컬 자동 감지 Cue 발행)
    /// 데미지 처리 시점에서 자동으로 적절한 히트 이펙트 재생
    /// </summary>
    public class HitEventRelay : MonoBehaviour
    {
        [Header("🎯 히트 감지 설정")]
        [SerializeField] private bool enableAutoHitDetection = true;
        [SerializeField] private float criticalThreshold = 1.5f; // 크리티컬 판정 배율
        
        /// <summary>
        /// 💥 데미지 히트 이벤트 (외부에서 호출)
        /// </summary>
        public void OnDamageReceived(float damage, Transform attacker = null, bool forceCritical = false)
        {
            if (!enableAutoHitDetection)
                return;
                
            // 크리티컬 판정
            bool isCritical = forceCritical || IsCriticalHit(damage);
            
            // 히트 이벤트 발행
            bool success = CueEmitter.EmitHit(attacker, transform, isCritical, damage);
            
            {
                string hitType = isCritical ? "크리티컬" : "일반";
                string attackerName = attacker != null ? attacker.name : "Unknown";
            }
        }
        
        /// <summary>
        /// 💥 표면 타입 지정 히트 이벤트
        /// </summary>
        public void OnDamageReceivedWithSurface(float damage, SurfaceType surfaceType, bool isCritical = false)
        {
            if (!enableAutoHitDetection)
                return;
                
            // 컨텍스트 생성
            var context = CueContext.From(transform);
            context.isCritical = isCritical;
            context.damage = (int)damage;
            context.surfaceType = surfaceType;
            
            // 키 생성
            string hitType = isCritical ? "critical" : "normal";
            string eventKey = $"hit.{surfaceType.ToString().ToLower()}.{hitType}";
            
            // 도메인 결정
            string domain = DetermineDomain();
            
            bool success = CueEmitter.Emit(eventKey, domain, context);
            
            {
                string critText = isCritical ? "크리티컬" : "일반";
            }
        }
        
        /// <summary>
        /// 🔥 특수 이펙트 히트 (폭발, 마법 등)
        /// </summary>
        public void OnSpecialHit(string effectType, float magnitude = 1.0f)
        {
            if (!enableAutoHitDetection)
                return;
                
            string eventKey = $"hit.special.{effectType}";
            var context = CueContext.From(transform, magnitude);
            
            string domain = DetermineDomain();
            bool success = CueEmitter.Emit(eventKey, domain, context);
            
        }
        
        /// <summary>
        /// 🛡️ 블록/회피 이벤트
        /// </summary>
        public void OnBlock(float blockedDamage)
        {
            string eventKey = "defense.block";
            var context = CueContext.From(transform);
            context.damage = (int)blockedDamage;
            
            string domain = DetermineDomain();
            bool success = CueEmitter.Emit(eventKey, domain, context);
            
        }
        
        public void OnDodge()
        {
            string eventKey = "defense.dodge";
            var context = CueContext.From(transform);
            
            string domain = DetermineDomain();
            bool success = CueEmitter.Emit(eventKey, domain, context);
            
        }
        
        #region Helper Methods
        
        /// <summary>
        /// 크리티컬 히트 판정
        /// </summary>
        private bool IsCriticalHit(float damage)
        {
            // 기본 데미지 대비 배율로 판정 (추후 더 정교한 로직 가능)
            // 예: 평균 데미지의 1.5배 이상이면 크리티컬
            return damage >= (GetBaseDamage() * criticalThreshold);
        }
        
        /// <summary>
        /// 기본 데미지 추정 (간단한 구현)
        /// </summary>
        private float GetBaseDamage()
        {
            // PlayerHealth나 EnemyHealth에서 기본 데미지 정보 가져오기
            // 현재는 고정값 사용
            return 10f;
        }
        
        /// <summary>
        /// 도메인 결정
        /// </summary>
        private string DetermineDomain()
        {
            int layer = gameObject.layer;
            switch (layer)
            {
                case 3: return "Player";
                case 6: return "Enemy";
                default: return "Global";
            }
        }
        
        #endregion
        
        /// <summary>
        /// 설정 업데이트 (런타임)
        /// </summary>
        public void UpdateSettings(bool enableDetection, float newCriticalThreshold)
        {
            enableAutoHitDetection = enableDetection;
            criticalThreshold = newCriticalThreshold;
            
        }
    }
}
