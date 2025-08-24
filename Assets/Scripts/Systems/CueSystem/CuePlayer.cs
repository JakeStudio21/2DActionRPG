using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace CueSystem
{
    /// <summary>
    /// 🎵 Cue Player (실제 VFX/SFX 재생 엔진)
    /// 직참조 차단 + 혼잡 제어 + 쿨다운 시스템
    /// </summary>
    public class CuePlayer : Singleton<CuePlayer>
    {
        [Header("🚦 혼잡 제어 설정")]
        [SerializeField] private int maxVFX = 30;
        [SerializeField] private int maxSFX = 20;
        [SerializeField] private int maxUI = 8;
        [SerializeField] private float defaultCooldown = 0.05f; // 50ms
        
        [Header("🔧 디버그 설정")]
        [SerializeField] private bool showDebugLogs = true;
        [SerializeField] private bool enableCongestionControl = true;
        
        // 활성 이펙트 추적
        private List<GameObject> _activeVFX = new List<GameObject>();
        private List<AudioSource> _activeSFX = new List<AudioSource>();
        private List<string> _activeUI = new List<string>();
        
        // 쿨다운 추적
        private Dictionary<string, float> _cooldowns = new Dictionary<string, float>();
        
        // 통계
        private int _totalPlayed = 0;
        private int _droppedByCongestion = 0;
        private int _droppedByCooldown = 0;
        
        protected override void Awake()
        {
            base.Awake();
            
            if (showDebugLogs)
                Debug.Log("🎵 [CuePlayer] 초기화 완료 - 직참조 차단 활성화");
        }
        
        private void Update()
        {
            // 주기적으로 만료된 쿨다운 정리 (5초마다)
            if (Time.time % 5f < Time.deltaTime)
            {
                CleanupExpiredCooldowns();
            }
        }
        
        /// <summary>
        /// 🎯 메인 재생 API - 이벤트 키로 VFX/SFX 재생
        /// </summary>
        public bool Play(string eventKey, string domain, CueContext context = default)
        {
            if (CueRegistry.Instance == null)
            {
                Debug.LogError("🔴 [CuePlayer] CueRegistry가 없습니다!");
                return false;
            }
            
            // 1. 키 해석
            var slot = CueRegistry.Instance.Resolve(domain, eventKey, context);
            if (slot == null || slot.IsEmpty)
            {
                if (showDebugLogs)
                    Debug.LogWarning($"⚠️ [CuePlayer] 빈 슬롯: {domain}.{eventKey}");
                return false;
            }
            
            // 2. 쿨다운 체크
            string fullKey = $"{domain}.{eventKey}";
            if (IsOnCooldown(fullKey))
            {
                _droppedByCooldown++;
                if (showDebugLogs)
                    Debug.Log($"⏱️ [CuePlayer] 쿨다운 중: {fullKey}");
                return false;
            }
            
            // 3. VFX 재생
            bool vfxSuccess = true;
            foreach (var vfxCue in slot.vfxCues)
            {
                if (!PlayVFX(vfxCue, context, domain))
                    vfxSuccess = false;
            }
            
            // 4. SFX 재생
            bool sfxSuccess = true;
            foreach (var sfxCue in slot.sfxCues)
            {
                if (!PlaySFX(sfxCue, context, domain))
                    sfxSuccess = false;
            }
            
            // 5. 쿨다운 설정
            float cooldown = CalculateCooldown(slot);
            SetCooldown(fullKey, cooldown);
            
            _totalPlayed++;
            
            if (showDebugLogs && (vfxSuccess || sfxSuccess))
                Debug.Log($"🎵 [CuePlayer] 재생: {fullKey} (VFX: {slot.vfxCues.Count}, SFX: {slot.sfxCues.Count})");
            
            return vfxSuccess || sfxSuccess;
        }
        
        /// <summary>
        /// 🎨 VFX 재생 (내부 전용)
        /// </summary>
        private bool PlayVFX(VFXCue vfxCue, CueContext context, string domain)
        {
            // 혼잡 제어
            if (enableCongestionControl)
            {
                int limit = domain == "UI" ? maxUI : maxVFX;
                if (_activeVFX.Count >= limit)
                {
                    _droppedByCongestion++;
                    if (showDebugLogs)
                        Debug.LogWarning($"🚦 [CuePlayer] VFX 혼잡 제어: {vfxCue.vfxId} (활성: {_activeVFX.Count}/{limit})");
                    return false;
                }
            }
            
            // 위치 계산
            Vector3 spawnPos = context.position + context.rotation * vfxCue.offset;
            Quaternion spawnRot = context.rotation;
            
            // 🔒 내부 전용 스폰 (직참조 차단)
            var vfxObj = GamePoolWrapper.InternalSpawnFromPool(vfxCue.poolKey, spawnPos, spawnRot);
            if (vfxObj == null)
            {
                Debug.LogError($"🔴 [CuePlayer] VFX 스폰 실패: {vfxCue.poolKey}");
                return false;
            }
            
            // 스케일 적용
            if (vfxCue.scale != Vector3.one)
                vfxObj.transform.localScale = Vector3.Scale(vfxObj.transform.localScale, vfxCue.scale);
            
            // 따라다니기
            if (vfxCue.followTarget && context.follow != null)
            {
                vfxObj.transform.SetParent(context.follow);
            }
            
            // 활성 목록에 추가
            _activeVFX.Add(vfxObj);
            
            // 자동 정리 (duration이 설정된 경우)
            if (vfxCue.duration > 0)
            {
                StartCoroutine(CleanupVFXAfterDelay(vfxObj, vfxCue.duration));
            }
            else
            {
                // ParticleAutoReturn이 있으면 자동 처리됨
                StartCoroutine(CleanupVFXWhenInactive(vfxObj));
            }
            
            return true;
        }
        
        /// <summary>
        /// 🔊 SFX 재생 (내부 전용)
        /// </summary>
        private bool PlaySFX(SFXCue sfxCue, CueContext context, string domain)
        {
            // 혼잡 제어
            if (enableCongestionControl)
            {
                int limit = domain == "UI" ? maxUI : maxSFX;
                if (_activeSFX.Count >= limit)
                {
                    _droppedByCongestion++;
                    if (showDebugLogs)
                        Debug.LogWarning($"🚦 [CuePlayer] SFX 혼잡 제어: {sfxCue.sfxId} (활성: {_activeSFX.Count}/{limit})");
                    return false;
                }
            }
            
            if (sfxCue.audioClip == null)
            {
                Debug.LogError($"🔴 [CuePlayer] AudioClip이 없습니다: {sfxCue.sfxId}");
                return false;
            }
            
            // SoundManager 연동
            if (SoundManager.Instance != null)
            {
                // 3D 사운드 처리
                if (sfxCue.is3D)
                {
                    return Play3DSFX(sfxCue, context);
                }
                else
                {
                    // 2D 사운드 (기존 SoundManager 사용)
                    SoundManager.Instance.PlaySFX(sfxCue.audioClip, sfxCue.volume, sfxCue.pitch);
                    return true;
                }
            }
            
            Debug.LogError("🔴 [CuePlayer] SoundManager가 없습니다!");
            return false;
        }
        
        /// <summary>
        /// 🔊 3D SFX 재생 처리
        /// </summary>
        private bool Play3DSFX(SFXCue sfxCue, CueContext context)
        {
            // 3D AudioSource 생성 (임시)
            var tempObj = new GameObject($"3D_SFX_{sfxCue.sfxId}");
            tempObj.transform.position = context.position;
            
            var audioSource = tempObj.AddComponent<AudioSource>();
            audioSource.clip = sfxCue.audioClip;
            audioSource.volume = sfxCue.volume;
            audioSource.pitch = sfxCue.pitch;
            audioSource.spatialBlend = 1.0f; // 3D
            audioSource.maxDistance = sfxCue.maxDistance;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.Play();
            
            _activeSFX.Add(audioSource);
            
            // 자동 정리
            StartCoroutine(CleanupSFXAfterPlay(audioSource, tempObj));
            
            return true;
        }
        
        /// <summary>
        /// ⏱️ 쿨다운 체크
        /// </summary>
        private bool IsOnCooldown(string key)
        {
            if (!_cooldowns.ContainsKey(key))
                return false;
                
            return Time.time < _cooldowns[key];
        }
        
        /// <summary>
        /// ⏱️ 쿨다운 설정
        /// </summary>
        private void SetCooldown(string key, float duration)
        {
            if (duration > 0)
                _cooldowns[key] = Time.time + duration;
        }
        
        /// <summary>
        /// ⏱️ 쿨다운 계산 (VFX/SFX 중 최대값)
        /// </summary>
        private float CalculateCooldown(CueSlot slot)
        {
            float maxCooldown = defaultCooldown;
            
            foreach (var vfxCue in slot.vfxCues)
            {
                maxCooldown = Mathf.Max(maxCooldown, vfxCue.cooldown);
            }
            
            foreach (var sfxCue in slot.sfxCues)
            {
                maxCooldown = Mathf.Max(maxCooldown, sfxCue.cooldown);
            }
            
            return maxCooldown;
        }
        
        /// <summary>
        /// 🧹 만료된 쿨다운 정리
        /// </summary>
        private void CleanupExpiredCooldowns()
        {
            var expiredKeys = new List<string>();
            float currentTime = Time.time;
            
            foreach (var kvp in _cooldowns)
            {
                if (currentTime >= kvp.Value)
                    expiredKeys.Add(kvp.Key);
            }
            
            foreach (var key in expiredKeys)
            {
                _cooldowns.Remove(key);
            }
            
            if (expiredKeys.Count > 0 && showDebugLogs)
                Debug.Log($"🧹 [CuePlayer] 만료된 쿨다운 {expiredKeys.Count}개 정리");
        }
        
        /// <summary>
        /// 📊 통계 출력
        /// </summary>
        public void PrintStats()
        {
            float congestionRate = _totalPlayed > 0 ? (float)_droppedByCongestion / _totalPlayed * 100f : 0f;
            float cooldownRate = _totalPlayed > 0 ? (float)_droppedByCooldown / _totalPlayed * 100f : 0f;
            
            Debug.Log($"📊 [CuePlayer] 재생 통계:");
            Debug.Log($"   총 재생 요청: {_totalPlayed}회");
            Debug.Log($"   혼잡 제어 드랍: {_droppedByCongestion}회 ({congestionRate:F1}%)");
            Debug.Log($"   쿨다운 드랍: {_droppedByCooldown}회 ({cooldownRate:F1}%)");
            Debug.Log($"   현재 활성 VFX: {_activeVFX.Count}개 (상한: {maxVFX})");
            Debug.Log($"   현재 활성 SFX: {_activeSFX.Count}개 (상한: {maxSFX})");
            Debug.Log($"   활성 쿨다운: {_cooldowns.Count}개");
        }
        
        /// <summary>
        /// 🔧 설정 업데이트 (런타임 조정용)
        /// </summary>
        public void UpdateSettings(int newMaxVFX, int newMaxSFX, int newMaxUI, float newDefaultCooldown)
        {
            maxVFX = newMaxVFX;
            maxSFX = newMaxSFX;
            maxUI = newMaxUI;
            defaultCooldown = newDefaultCooldown;
            
            if (showDebugLogs)
                Debug.Log($"🔧 [CuePlayer] 설정 업데이트: VFX({maxVFX}), SFX({maxSFX}), UI({maxUI}), 쿨다운({defaultCooldown}s)");
        }
        
        #region Cleanup Coroutines
        
        /// <summary>
        /// VFX 지정 시간 후 정리
        /// </summary>
        private IEnumerator CleanupVFXAfterDelay(GameObject vfxObj, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (vfxObj != null)
            {
                _activeVFX.Remove(vfxObj);
                
                // ParticleAutoReturn이 있으면 자동 처리, 없으면 수동 반환
                var autoReturn = vfxObj.GetComponent<ParticleAutoReturn>();
                if (autoReturn == null)
                {
                    vfxObj.SetActive(false);
                }
            }
        }
        
        /// <summary>
        /// VFX 비활성화 시 정리 (ParticleAutoReturn 연동)
        /// </summary>
        private IEnumerator CleanupVFXWhenInactive(GameObject vfxObj)
        {
            // 최대 30초 대기 (무한 대기 방지)
            float timeout = 30f;
            float elapsed = 0f;
            
            while (vfxObj != null && vfxObj.activeInHierarchy && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;
            }
            
            if (vfxObj != null)
            {
                _activeVFX.Remove(vfxObj);
                
                if (elapsed >= timeout)
                {
                    Debug.LogWarning($"⚠️ [CuePlayer] VFX 타임아웃: {vfxObj.name}");
                    vfxObj.SetActive(false);
                }
            }
        }
        
        /// <summary>
        /// SFX 재생 완료 후 정리
        /// </summary>
        private IEnumerator CleanupSFXAfterPlay(AudioSource audioSource, GameObject tempObj)
        {
            // 재생 완료까지 대기
            yield return new WaitWhile(() => audioSource != null && audioSource.isPlaying);
            
            if (audioSource != null)
                _activeSFX.Remove(audioSource);
                
            if (tempObj != null)
                Destroy(tempObj);
        }
        
        #endregion
    }
}
