using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace CueSystem
{
    /// <summary>
    /// 🎭 Cue Profile (도메인별 이벤트 키 → 이펙트 매핑)
    /// Player/Enemy/Stage/UI 각각 별도 프로필 사용
    /// </summary>
    [CreateAssetMenu(fileName = "CueProfile", menuName = "CueSystem/Cue Profile")]
    public class CueProfile : ScriptableObject
    {
        [Header("🎯 프로필 정보")]
        public string profileId;          // 고유 식별자
        public string domain;             // "Player", "Enemy", "Stage", "UI"
        public CueProfile baseProfile;    // 상속할 부모 프로필 (optional)
        
        [Header("🎨 이펙트 카탈로그")]
        public List<VFXCue> vfxCatalog = new List<VFXCue>();
        public List<SFXCue> sfxCatalog = new List<SFXCue>();
        
        [Header("🔑 이벤트 매핑")]
        public List<CueEntry> entries = new List<CueEntry>();
        
        [Header("📝 메타데이터")]
        [TextArea(3, 5)]
        public string description;        // 프로필 설명
        
        // 런타임 캐시 (머지된 결과)
        private Dictionary<string, CueSlot> _mergedCache;
        private Dictionary<string, VFXCue> _vfxLookup;
        private Dictionary<string, SFXCue> _sfxLookup;
        
        /// <summary>
        /// 프로필 초기화 및 캐시 생성
        /// </summary>
        public void Initialize()
        {
            BuildLookupTables();
            BuildMergedCache();
            
            Debug.Log($"🎭 [CueProfile] {profileId} 초기화 완료 - 엔트리: {entries.Count}개, VFX: {vfxCatalog.Count}개, SFX: {sfxCatalog.Count}개");
        }
        
        /// <summary>
        /// 이벤트 키로 Cue 슬롯 조회 (O(1) 캐시 조회)
        /// </summary>
        public CueSlot Resolve(string eventKey)
        {
            if (_mergedCache == null)
            {
                Debug.Log($"🔍 [CueProfile] {profileId} - 캐시가 null, Initialize() 호출");
                Initialize();
            }
            
            // ✅ 디버깅: 캐시 내용 확인
            Debug.Log($"🔍 [CueProfile] {profileId} - 캐시된 키들: [{string.Join(", ", _mergedCache.Keys)}]");
            Debug.Log($"🔍 [CueProfile] {profileId} - 요청 키: '{eventKey}'");
            
            bool found = _mergedCache.TryGetValue(eventKey, out CueSlot slot);
            
            Debug.Log($"🔍 [CueProfile] {profileId} - 키 '{eventKey}' 찾기 결과: {found}");
            
            return slot; // null이면 빈 슬롯
        }
        
        /// <summary>
        /// VFX ID로 VFX Cue 조회
        /// </summary>
        public VFXCue GetVFXCue(string vfxId)
        {
            if (_vfxLookup == null)
                BuildLookupTables();
                
            _vfxLookup.TryGetValue(vfxId, out VFXCue vfx);
            return vfx;
        }
        
        /// <summary>
        /// SFX ID로 SFX Cue 조회
        /// </summary>
        public SFXCue GetSFXCue(string sfxId)
        {
            if (_sfxLookup == null)
                BuildLookupTables();
                
            _sfxLookup.TryGetValue(sfxId, out SFXCue sfx);
            return sfx;
        }
        
        /// <summary>
        /// 캐시 무효화 (프로필 변경 시 호출)
        /// </summary>
        public void InvalidateCache()
        {
            _mergedCache = null;
            _vfxLookup = null;
            _sfxLookup = null;
            
            Debug.Log($"🔄 [CueProfile] {profileId} 캐시 무효화");
        }
        
        #region Private Methods
        
        /// <summary>
        /// VFX/SFX 룩업 테이블 구축
        /// </summary>
        private void BuildLookupTables()
        {
            _vfxLookup = new Dictionary<string, VFXCue>();
            _sfxLookup = new Dictionary<string, SFXCue>();
            
            // 현재 프로필의 카탈로그 추가
            foreach (var vfx in vfxCatalog)
            {
                if (!string.IsNullOrEmpty(vfx.vfxId))
                    _vfxLookup[vfx.vfxId] = vfx;
            }
            
            foreach (var sfx in sfxCatalog)
            {
                if (!string.IsNullOrEmpty(sfx.sfxId))
                    _sfxLookup[sfx.sfxId] = sfx;
            }
            
            // 상속 체인의 카탈로그도 추가
            var profileChain = CollectProfileChain();
            for (int i = 1; i < profileChain.Count; i++) // 현재 프로필 제외
            {
                var profile = profileChain[i];
                foreach (var vfx in profile.vfxCatalog)
                {
                    if (!string.IsNullOrEmpty(vfx.vfxId) && !_vfxLookup.ContainsKey(vfx.vfxId))
                        _vfxLookup[vfx.vfxId] = vfx;
                }
                
                foreach (var sfx in profile.sfxCatalog)
                {
                    if (!string.IsNullOrEmpty(sfx.sfxId) && !_sfxLookup.ContainsKey(sfx.sfxId))
                        _sfxLookup[sfx.sfxId] = sfx;
                }
            }
        }
        
        /// <summary>
        /// 상속 체인을 고려한 머지 캐시 구축 (자식 우선 Replace 방식)
        /// 자식 프로필이 키를 정의하면 부모 정의를 완전히 교체 (additive 아님)
        /// </summary>
        private void BuildMergedCache()
        {
            _mergedCache = new Dictionary<string, CueSlot>();
            
            // 1. 상속 체인 수집: [자식, 부모, 조부모, ...] 순서
            var profileChain = CollectProfileChain();
            
            // 2. 자식 → 부모 순으로 처리. 이미 자식이 정의한 키는 부모가 덮어쓰지 않음 (Replace 의미론)
            foreach (var profile in profileChain)
            {
                foreach (var entry in profile.entries)
                {
                    // 이미 상위 우선순위(자식) 프로필이 이 키를 정의했으면 스킵
                    if (_mergedCache.ContainsKey(entry.eventKey))
                        continue;
                    
                    var slot = new CueSlot();
                    
                    // VFX 구성
                    foreach (var vfxId in entry.vfxIds)
                    {
                        var vfx = GetVFXCue(vfxId);
                        if (vfx != null)
                            slot.vfxCues.Add(vfx);
                    }
                    
                    // SFX 구성
                    foreach (var sfxId in entry.sfxIds)
                    {
                        var sfx = GetSFXCue(sfxId);
                        if (sfx != null)
                            slot.sfxCues.Add(sfx);
                    }
                    
                    // 메타데이터
                    slot.priority = entry.priority;
                    slot.cameraShakePreset = entry.cameraShakePreset;
                    slot.timeStopMs = entry.timeStopMs;
                    
                    _mergedCache[entry.eventKey] = slot;
                }
            }
        }
        
        /// <summary>
        /// 상속 체인 수집 (순환 참조 방지)
        /// </summary>
        private List<CueProfile> CollectProfileChain()
        {
            var chain = new List<CueProfile>();
            var visited = new HashSet<CueProfile>();
            
            var current = this;
            while (current != null && !visited.Contains(current))
            {
                chain.Add(current);
                visited.Add(current);
                current = current.baseProfile;
            }
            
            if (current != null && visited.Contains(current))
            {
                Debug.LogError($"🔴 [CueProfile] 순환 참조 감지: {profileId}");
            }
            
            return chain;
        }
        
        #endregion
        
        #region Editor Validation
        
        private void OnValidate()
        {
            // 에디터에서 변경 시 캐시 무효화
            if (Application.isPlaying)
                InvalidateCache();
        }
        
        #endregion
    }
}
