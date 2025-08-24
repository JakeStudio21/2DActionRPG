using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace CueSystem
{
    /// <summary>
    /// 🎯 Cue Registry (전역 키→프로필 매핑 관리자)
    /// 도메인별 프로필 등록 및 머지-캐시 기반 O(1) 조회
    /// </summary>
    public class CueRegistry : Singleton<CueRegistry>
    {
        [Header("🎭 도메인별 프로필")]
        public CueProfile playerProfile;
        public CueProfile enemyProfile;
        public CueProfile stageProfile;
        public CueProfile uiProfile;
        public CueProfile globalDefaults;  // 글로벌 기본값
        
        [Header("🔧 디버그 설정")]
        public bool showDebugLogs = true;
        public MissingKeyPolicy missingKeyPolicy = MissingKeyPolicy.WarnAndGlobalDefault;
        
        // 런타임 프로필 딕셔너리
        private Dictionary<string, CueProfile> _profileRegistry = new Dictionary<string, CueProfile>();
        
        // 통계
        private Dictionary<string, int> _resolveStats = new Dictionary<string, int>();
        private int _totalResolves = 0;
        private int _cacheHits = 0;
        private int _cacheMisses = 0;
        
        protected override void Awake()
        {
            base.Awake();
            InitializeRegistry();
        }
        
        /// <summary>
        /// 레지스트리 초기화
        /// </summary>
        private void InitializeRegistry()
        {
            // 기본 프로필 등록
            RegisterProfile("Player", playerProfile);
            RegisterProfile("Enemy", enemyProfile);
            RegisterProfile("Stage", stageProfile);
            RegisterProfile("UI", uiProfile);
            RegisterProfile("Global", globalDefaults);
            
            if (showDebugLogs)
                Debug.Log($"🎯 [CueRegistry] 초기화 완료 - 등록된 프로필: {_profileRegistry.Count}개");
        }
        
        /// <summary>
        /// 도메인별 프로필 등록
        /// </summary>
        public void RegisterProfile(string domain, CueProfile profile)
        {
            if (profile == null)
            {
                if (showDebugLogs)
                    Debug.LogWarning($"⚠️ [CueRegistry] {domain} 프로필이 null입니다");
                return;
            }
            
            _profileRegistry[domain] = profile;
            profile.Initialize(); // 머지-캐시 생성
            
            if (showDebugLogs)
                Debug.Log($"📝 [CueRegistry] {domain} 프로필 등록: {profile.profileId}");
        }
        
        /// <summary>
        /// 이벤트 키 해석 (O(1) 캐시 조회)
        /// </summary>
        public CueSlot Resolve(string domain, string eventKey, CueContext context = default)
        {
            _totalResolves++;
            
            // ✅ 디버깅: 등록된 도메인들 확인
            Debug.Log($"🔍 [CueRegistry] 해석 요청 - 도메인: '{domain}', 키: '{eventKey}'");
            Debug.Log($"🔍 [CueRegistry] 등록된 도메인들: [{string.Join(", ", _profileRegistry.Keys)}]");
            
            // 1. 도메인 프로필 조회
            if (!_profileRegistry.TryGetValue(domain, out CueProfile profile))
            {
                Debug.LogError($"🔴 [CueRegistry] 도메인 '{domain}' 프로필을 찾을 수 없습니다!");
                HandleMissingProfile(domain, eventKey);
                return null;
            }
            
            Debug.Log($"🔍 [CueRegistry] 도메인 '{domain}' 프로필 발견: {profile.name}");
            
            // 2. 프로필에서 키 해석
            var slot = profile.Resolve(eventKey);
            if (slot != null && !slot.IsEmpty)
            {
                _cacheHits++;
                RecordResolveStats($"{domain}.{eventKey}");
                return slot;
            }
            
            // 3. 글로벌 기본값 시도
            if (missingKeyPolicy == MissingKeyPolicy.WarnAndGlobalDefault && globalDefaults != null)
            {
                slot = globalDefaults.Resolve(eventKey);
                if (slot != null && !slot.IsEmpty)
                {
                    if (showDebugLogs)
                        Debug.LogWarning($"⚠️ [CueRegistry] {domain}.{eventKey} 누락 → 글로벌 기본값 사용");
                    
                    _cacheHits++;
                    return slot;
                }
            }
            
            // 4. 완전 실패
            _cacheMisses++;
            HandleMissingKey(domain, eventKey);
            return null;
        }
        
        /// <summary>
        /// 캐시 무효화 (특정 도메인)
        /// </summary>
        public void InvalidateCache(string domain)
        {
            if (_profileRegistry.TryGetValue(domain, out CueProfile profile))
            {
                profile.InvalidateCache();
                if (showDebugLogs)
                    Debug.Log($"🔄 [CueRegistry] {domain} 캐시 무효화");
            }
        }
        
        /// <summary>
        /// 전체 캐시 무효화
        /// </summary>
        public void InvalidateAllCaches()
        {
            foreach (var profile in _profileRegistry.Values)
            {
                profile?.InvalidateCache();
            }
            
            if (showDebugLogs)
                Debug.Log($"🔄 [CueRegistry] 전체 캐시 무효화");
        }
        
        /// <summary>
        /// 성능 통계 출력
        /// </summary>
        public void PrintStats()
        {
            float hitRate = _totalResolves > 0 ? (float)_cacheHits / _totalResolves * 100f : 0f;
            
            Debug.Log($"📊 [CueRegistry] 성능 통계:");
            Debug.Log($"   총 해석: {_totalResolves}회");
            Debug.Log($"   캐시 히트: {_cacheHits}회 ({hitRate:F1}%)");
            Debug.Log($"   캐시 미스: {_cacheMisses}회");
            
            Debug.Log($"📈 [CueRegistry] 인기 키 TOP 5:");
            var sortedStats = new List<KeyValuePair<string, int>>(_resolveStats);
            sortedStats.Sort((a, b) => b.Value.CompareTo(a.Value));
            
            for (int i = 0; i < Mathf.Min(5, sortedStats.Count); i++)
            {
                var kvp = sortedStats[i];
                Debug.Log($"   {i + 1}. {kvp.Key}: {kvp.Value}회");
            }
        }
        
        #region Private Methods
        
        private void HandleMissingProfile(string domain, string eventKey)
        {
            switch (missingKeyPolicy)
            {
                case MissingKeyPolicy.WarnAndSkip:
                    Debug.LogWarning($"⚠️ [CueRegistry] 도메인 '{domain}' 프로필이 없습니다: {eventKey}");
                    break;
                case MissingKeyPolicy.Error:
                    Debug.LogError($"🔴 [CueRegistry] 도메인 '{domain}' 프로필이 없습니다: {eventKey}");
                    break;
            }
        }
        
        private void HandleMissingKey(string domain, string eventKey)
        {
            switch (missingKeyPolicy)
            {
                case MissingKeyPolicy.WarnAndSkip:
                case MissingKeyPolicy.WarnAndGlobalDefault:
                    Debug.LogWarning($"⚠️ [CueRegistry] 키를 찾을 수 없습니다: {domain}.{eventKey}");
                    break;
                case MissingKeyPolicy.Error:
                    Debug.LogError($"🔴 [CueRegistry] 키를 찾을 수 없습니다: {domain}.{eventKey}");
                    break;
            }
        }
        
        private void RecordResolveStats(string fullKey)
        {
            if (!_resolveStats.ContainsKey(fullKey))
                _resolveStats[fullKey] = 0;
            _resolveStats[fullKey]++;
        }
        
        #endregion
    }
    
    /// <summary>
    /// 누락 키 처리 정책
    /// </summary>
    public enum MissingKeyPolicy
    {
        WarnAndSkip = 0,           // 경고 후 스킵
        WarnAndGlobalDefault = 1,  // 경고 후 글로벌 기본값 사용
        Error = 2                  // 에러 발생
    }
}
