using UnityEngine;
using UnityEngine.SceneManagement;
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
        [SerializeField] private bool enableCongestionControl = true;
        
        // 활성 이펙트 추적
        private List<GameObject> _activeVFX = new List<GameObject>();
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
            
            // 씬 전환 이벤트 구독
            SceneManager.sceneLoaded += OnSceneLoaded;
            
        }
        
        protected override void OnDestroy()
        {
            // 씬 전환 이벤트 구독 해제
            SceneManager.sceneLoaded -= OnSceneLoaded;
            
            base.OnDestroy();
        }
        
        /// <summary>
        /// 🧹 씬 전환 시 파괴된 VFX/SFX 참조 정리
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CleanupNullReferences();
            
        }
        
        /// <summary>
        /// 🧹 null 참조 정리 (씬 전환으로 파괴된 오브젝트들)
        /// </summary>
        private void CleanupNullReferences()
        {
            _activeVFX.RemoveAll(obj => obj == null);
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
                    Debug.LogWarning($"⚠️ [CuePlayer] 빈 슬롯: {domain}.{eventKey}");
                return false;
            }
            
            // 2. 쿨다운 체크
            string fullKey = $"{domain}.{eventKey}";
            if (IsOnCooldown(fullKey))
            {
                _droppedByCooldown++;
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

            // 5. 스크린 셰이크 — CueEntry에 설정된 shakeData 적용
            if (slot.shakeData != null && slot.shakeData.useShake)
            {
                if (ScreenShakeManager.Instance != null)
                    ScreenShakeManager.Instance.PlayShake(slot.shakeData);
                else                    Debug.LogWarning("⚠️ [CuePlayer] ScreenShakeManager 인스턴스 없음 — shakeData 무시됨");
            }

            // 6. 쿨다운 설정
            float cooldown = CalculateCooldown(slot);
            SetCooldown(fullKey, cooldown);
            
            _totalPlayed++;
            
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
                        Debug.LogWarning($"🚦 [CuePlayer] VFX 혼잡 제어: {vfxCue.vfxId} (활성: {_activeVFX.Count}/{limit})");
                    return false;
                }
            }
            
            // facingDir 폴백: facingDir가 없는 호출처(SkillController 등)를 위해 rotation에서 유도
            Vector2 facing = context.facingDir.magnitude > 0.1f
                ? context.facingDir
                : (Vector2)(context.rotation * Vector3.right);

            bool facingLeft = facing.x < 0f;

            // offset 위치는 rotationMode와 무관하게 항상 공격 방향 기준으로 계산
            // (rotationMode는 VFX 오브젝트의 시각적 회전만 결정)
            Vector3 spawnPos = context.position + context.rotation * vfxCue.offset;

            // rotationMode — VFX 오브젝트 자체의 회전만 결정
            Quaternion spawnRot;
            switch (vfxCue.rotationMode)
            {
                case VFXRotationMode.Fixed:
                    // 비주얼 회전 완전 고정 (바닥 장판, 원형 AOE 등 방향 무관 이펙트)
                    spawnRot = Quaternion.identity;
                    break;

                case VFXRotationMode.HorizontalFlip:
                    // 왼쪽 공격 시 Y축 180° 반전 (메테오·비대칭 AOE 등 오른쪽 기준 이펙트)
                    // scale.x=-1 대신 Y축 회전을 사용해 파티클·자식 스케일 오염 방지
                    spawnRot = facingLeft ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
                    break;

                case VFXRotationMode.FollowFlip:
                    // 공격 방향 상속 + 왼쪽 공격 시 Y-flip
                    // facing.x < 0 기준: 상하 방향(≈0)은 Flip 없음으로 처리
                    //
                    // [원리] 왼쪽 방향(zAngle > 90°)에서 단순 Y-flip을 적용하면
                    // 반대 방향(5시)으로 발사됨. 올바른 처리는:
                    //   mirroredAngle = 180° - zAngle  (Y축 기준 각도 반사)
                    //   R_y(180°) * R_z(mirroredAngle) → 정확히 원래 방향으로 발사 + 시각 반전
                    if (facingLeft)
                    {
                        float zAngle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
                        spawnRot = Quaternion.Euler(0f, 180f, 0f) * Quaternion.Euler(0f, 0f, 180f - zAngle);
                    }
                    else
                    {
                        spawnRot = context.rotation;
                    }
                    break;

                default: // Follow
                    // 공격 방향 그대로 상속 (화살, 파이어볼 등 방향성 이펙트)
                    spawnRot = context.rotation;
                    break;
            }
            
            // 🔒 내부 전용 스폰 (직참조 차단)
            var vfxObj = GamePoolWrapper.InternalSpawnFromPool(vfxCue.poolKey, spawnPos, spawnRot);
            if (vfxObj == null)
            {
                Debug.LogError($"🔴 [CuePlayer] VFX 스폰 실패: {vfxCue.poolKey}");
                return false;
            }
            
            // 스케일 적용 (context.scale 우선, 직접 할당으로 누적 방지)
            if (context.scale > 0)
            {
                // 동적 스케일 우선 (스킬 레벨 등)
                vfxObj.transform.localScale = vfxCue.scale * context.scale;
            }
            else if (vfxCue.scale != Vector3.one)
            {
                // 정적 스케일 (CueProfile 기본값)
                vfxObj.transform.localScale = vfxCue.scale;
            }

            // 🆕 아이소메트릭 소팅 자동 적용 (EFFECT_LAYER = 2000 사용)
            IsometricSorting.ApplyEffectSorting(vfxObj, spawnPos);

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
        /// 🔊 SFX 재생 (내부 전용) — SoundManager에 위임
        /// </summary>
        private bool PlaySFX(SFXCue sfxCue, CueContext context, string domain)
        {
            if (sfxCue.audioClip == null)
            {
                Debug.LogError($"🔴 [CuePlayer] AudioClip이 없습니다: {sfxCue.sfxId}");
                return false;
            }

            if (SoundManager.Instance == null)
            {
                Debug.LogError("🔴 [CuePlayer] SoundManager가 없습니다!");
                return false;
            }

            if (sfxCue.loop)
            {
                // Loop SFX → SoundManager.PlayLoopSFX (BGM 크로스페이드)
                SoundManager.Instance.PlayLoopSFX(sfxCue.audioClip, sfxCue.volume);
            }
            else if (sfxCue.is3D)
            {
                // 3D SFX → SoundManager Pool (위치 지정)
                SoundManager.Instance.PlaySFX(sfxCue.audioClip, sfxCue.volume, sfxCue.pitch,
                    context.position, sfxCue.maxDistance);
            }
            else if (domain == "UI")
            {
                // UI SFX → UI 전용 채널
                SoundManager.Instance.PlayUI(sfxCue.audioClip, sfxCue.volume);
            }
            else
            {
                // 2D SFX → SoundManager Pool
                SoundManager.Instance.PlaySFX(sfxCue.audioClip, sfxCue.volume, sfxCue.pitch);
            }

            return true;
        }

        /// <summary>
        /// 🛑 현재 BGM 정지 (외부 호출용)
        /// </summary>
        public void StopCurrentBGM()
        {
            SoundManager.Instance?.StopLoopSFX();
        }

        /// <summary>
        /// 🎵 BGM 재생 (외부 호출용) — CueProfile 해석 후 SoundManager에 위임
        /// </summary>
        public bool PlayBGMWithFade(string eventKey, string domain, float fadeTime = 0.5f)
        {
            if (CueRegistry.Instance == null)
            {
                Debug.LogError("🔴 [CuePlayer] CueRegistry가 없습니다!");
                return false;
            }

            var slot = CueRegistry.Instance.Resolve(domain, eventKey);
            if (slot == null || slot.IsEmpty)
            {
                Debug.LogWarning($"⚠️ [CuePlayer] 빈 슬롯: {domain}.{eventKey}");
                return false;
            }

            if (slot.sfxCues.Count == 0)
            {
                Debug.LogWarning($"⚠️ [CuePlayer] SFX Cue 없음: {domain}.{eventKey}");
                return false;
            }

            var sfxCue = slot.sfxCues[0];

            if (SoundManager.Instance == null)
            {
                Debug.LogError("🔴 [CuePlayer] SoundManager가 없습니다!");
                return false;
            }

            SoundManager.Instance.PlayLoopSFX(sfxCue.audioClip, sfxCue.volume, fadeTime);
            Dbg.Log($"🎵 [CuePlayer] BGM 재생 요청 → SoundManager: {sfxCue.sfxId} (fade: {fadeTime}s)");
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
            
        }
        
        /// <summary>
        /// 📊 통계 출력
        /// </summary>
        public void PrintStats()
        {
            float congestionRate = _totalPlayed > 0 ? (float)_droppedByCongestion / _totalPlayed * 100f : 0f;
            float cooldownRate = _totalPlayed > 0 ? (float)_droppedByCooldown / _totalPlayed * 100f : 0f;
            
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
        
        #endregion
    }
}
