using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 사운드 재생 엔진 (BGM 크로스페이드 / SFX 풀링 / UI / AudioMixer 볼륨)
///
/// 역할 분담
///   CuePlayer   → CueProfile 해석 후 SoundManager 호출
///   BGMController → BGM 우선순위 결정 후 SoundManager 호출
///   SoundManager  → 실제 AudioSource 관리 및 재생
/// </summary>
public class SoundManager : Singleton<SoundManager>
{
    // ───────────────────────────────────────────
    //  Inspector
    // ───────────────────────────────────────────

    [Header("BGM (크로스페이드)")]
    [SerializeField] private AudioSource bgmSourceA;
    [SerializeField] private AudioSource bgmSourceB;

    [Header("SFX 풀")]
    [SerializeField] private int sfxPoolSize = 12;

    [Header("UI 사운드")]
    [SerializeField] private AudioSource uiSource;

    [Header("AudioMixer")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioMixerGroup bgmMixerGroup;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] private AudioMixerGroup uiMixerGroup;

    // ───────────────────────────────────────────
    //  AudioMixer 파라미터 이름 (Exposed Parameters)
    // ───────────────────────────────────────────

    private const string PARAM_MASTER = "MasterVolume";
    private const string PARAM_BGM    = "BGMVolume";
    private const string PARAM_SFX    = "SFXVolume";
    private const string PARAM_UI     = "UIVolume";

    // ───────────────────────────────────────────
    //  BGM 상태
    // ───────────────────────────────────────────

    private bool _isAActive = true;
    private Coroutine _bgmFadeCoroutine;

    // ───────────────────────────────────────────
    //  SFX 풀
    // ───────────────────────────────────────────

    private readonly List<AudioSource> _sfxPool = new List<AudioSource>();
    private GameObject _sfxPoolRoot;

    // ───────────────────────────────────────────
    //  Unity Lifecycle
    // ───────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        InitializeBGMSources();
        InitializeSFXPool();
        InitializeUISource();
    }

    // ───────────────────────────────────────────
    //  초기화
    // ───────────────────────────────────────────

    private void InitializeBGMSources()
    {
        if (bgmSourceA == null)
            bgmSourceA = CreateAudioSource("BGM_A", bgmMixerGroup);
        else
            bgmSourceA.outputAudioMixerGroup = bgmMixerGroup;

        if (bgmSourceB == null)
            bgmSourceB = CreateAudioSource("BGM_B", bgmMixerGroup);
        else
            bgmSourceB.outputAudioMixerGroup = bgmMixerGroup;

        bgmSourceA.loop        = true;
        bgmSourceA.playOnAwake = false;
        bgmSourceB.loop        = true;
        bgmSourceB.playOnAwake = false;
    }

    private void InitializeSFXPool()
    {
        _sfxPoolRoot = new GameObject("SFX_Pool");
        _sfxPoolRoot.transform.SetParent(transform);

        for (int i = 0; i < sfxPoolSize; i++)
        {
            var source = CreateAudioSource($"SFX_{i}", sfxMixerGroup, _sfxPoolRoot.transform);
            source.playOnAwake = false;
            _sfxPool.Add(source);
        }
    }

    private void InitializeUISource()
    {
        if (uiSource == null)
            uiSource = CreateAudioSource("UI_Source", uiMixerGroup);
        else
            uiSource.outputAudioMixerGroup = uiMixerGroup;

        uiSource.playOnAwake = false;
    }

    private AudioSource CreateAudioSource(string goName, AudioMixerGroup mixerGroup, Transform parent = null)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(parent != null ? parent : transform);
        var source = go.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = mixerGroup;
        return source;
    }

    // ───────────────────────────────────────────
    //  BGM — PlayLoopSFX (CuePlayer에서 호출)
    // ───────────────────────────────────────────

    /// <summary>
    /// BGM 크로스페이드 재생.
    /// fadeTime = 0 이면 즉시 전환.
    /// </summary>
    public void PlayLoopSFX(AudioClip clip, float volume = 1f, float fadeTime = 1f)
    {
        if (clip == null) return;

        // 크로스페이드 진행 중일 수 있으므로 양쪽 소스 모두 확인
        // (CrossFadeRoutine이 실행 중이면 _isAActive가 아직 뒤집히지 않아
        //  incoming 소스가 "비활성"으로 잡힐 수 있음)
        var srcA = bgmSourceA;
        var srcB = bgmSourceB;

        bool playingOnA = srcA != null && srcA.clip == clip && srcA.isPlaying;
        bool playingOnB = srcB != null && srcB.clip == clip && srcB.isPlaying;

        if (playingOnA || playingOnB)
        {
            // 볼륨만 갱신하고 재시작 방지
            var playing = playingOnA ? srcA : srcB;
            playing.volume = volume;
            return;
        }

        if (_bgmFadeCoroutine != null)
            StopCoroutine(_bgmFadeCoroutine);

        if (fadeTime <= 0f)
        {
            SwapBGMImmediate(clip, volume);
            return;
        }

        _bgmFadeCoroutine = StartCoroutine(CrossFadeRoutine(clip, volume, fadeTime));
    }

    /// <summary>
    /// BGM 정지 (페이드 아웃).
    /// </summary>
    public void StopLoopSFX(float fadeTime = 1f)
    {
        if (_bgmFadeCoroutine != null)
            StopCoroutine(_bgmFadeCoroutine);

        _bgmFadeCoroutine = StartCoroutine(FadeOutRoutine(fadeTime));
    }

    /// <summary>
    /// 현재 재생 중인 BGM AudioSource 볼륨 실시간 변경.
    /// SFXCue.volume을 에디터에서 수정할 때 즉시 반영.
    /// </summary>
    public void SetCurrentLoopSFXVolume(float volume)
    {
        AudioSource current = _isAActive ? bgmSourceA : bgmSourceB;
        if (current.isPlaying)
            current.volume = volume;
    }

    private void SwapBGMImmediate(AudioClip clip, float volume)
    {
        AudioSource outgoing = _isAActive ? bgmSourceA : bgmSourceB;
        AudioSource incoming = _isAActive ? bgmSourceB : bgmSourceA;

        outgoing.Stop();
        outgoing.clip = null;

        incoming.clip   = clip;
        incoming.volume = volume;
        incoming.Play();

        _isAActive = !_isAActive;
    }

    private IEnumerator CrossFadeRoutine(AudioClip clip, float targetVolume, float fadeTime)
    {
        AudioSource outgoing = _isAActive ? bgmSourceA : bgmSourceB;
        AudioSource incoming = _isAActive ? bgmSourceB : bgmSourceA;

        incoming.clip   = clip;
        incoming.volume = 0f;
        incoming.Play();

        float elapsed      = 0f;
        float startVolume  = outgoing.volume;

        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeTime);

            incoming.volume = Mathf.Lerp(0f, targetVolume, t);
            outgoing.volume = Mathf.Lerp(startVolume, 0f, t);

            yield return null;
        }

        incoming.volume = targetVolume;
        outgoing.Stop();
        outgoing.clip = null;

        _isAActive = !_isAActive;
        _bgmFadeCoroutine = null;
    }

    private IEnumerator FadeOutRoutine(float fadeTime)
    {
        AudioSource current = _isAActive ? bgmSourceA : bgmSourceB;
        float startVolume   = current.volume;
        float elapsed       = 0f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            current.volume = Mathf.Lerp(startVolume, 0f, Mathf.Clamp01(elapsed / fadeTime));
            yield return null;
        }

        current.Stop();
        current.clip = null;
        _bgmFadeCoroutine = null;
    }

    // ───────────────────────────────────────────
    //  SFX — Pool 기반 재생
    // ───────────────────────────────────────────

    /// <summary>
    /// 2D SFX 재생 (CuePlayer / StatusEffectManager 에서 호출).
    /// </summary>
    public void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;

        var source = GetAvailableSFXSource();
        source.transform.position = Vector3.zero;
        source.spatialBlend       = 0f;
        source.volume             = volume;
        source.pitch              = pitch;
        source.clip               = clip;
        source.Play();
    }

    /// <summary>
    /// 3D SFX 재생 (CuePlayer에서 호출).
    /// </summary>
    public void PlaySFX(AudioClip clip, float volume, float pitch, Vector3 position, float maxDistance = 50f)
    {
        if (clip == null) return;

        var source = GetAvailableSFXSource();
        source.transform.position = position;
        source.spatialBlend       = 1f;
        source.maxDistance        = maxDistance;
        source.rolloffMode        = AudioRolloffMode.Linear;
        source.volume             = volume;
        source.pitch              = pitch;
        source.clip               = clip;
        source.Play();
    }

    private AudioSource GetAvailableSFXSource()
    {
        foreach (var source in _sfxPool)
        {
            if (!source.isPlaying)
                return source;
        }

        // 풀이 가득 찬 경우 — 가장 먼저 추가된 소스 재사용
        _sfxPool[0].Stop();
        return _sfxPool[0];
    }

    // ───────────────────────────────────────────
    //  UI 사운드
    // ───────────────────────────────────────────

    /// <summary>
    /// UI 사운드 재생 (SFX 풀을 소비하지 않음).
    /// </summary>
    public void PlayUI(AudioClip clip, float volume = 1f)
    {
        if (clip == null || uiSource == null) return;
        uiSource.PlayOneShot(clip, volume);
    }

    // ───────────────────────────────────────────
    //  볼륨 제어 (AudioMixer)
    // ───────────────────────────────────────────

    public void SetMasterVolume(float linearValue) => ApplyMixerVolume(PARAM_MASTER, linearValue);
    public void SetBGMVolume(float linearValue)    => ApplyMixerVolume(PARAM_BGM,    linearValue);
    public void SetSFXVolume(float linearValue)    => ApplyMixerVolume(PARAM_SFX,    linearValue);
    public void SetUIVolume(float linearValue)     => ApplyMixerVolume(PARAM_UI,     linearValue);

    /// <summary>
    /// 선형값(0~1) → dB 변환 후 AudioMixer에 적용.
    /// 0 이하는 -80dB(무음) 처리.
    /// </summary>
    private void ApplyMixerVolume(string paramName, float linearValue)
    {
        if (audioMixer == null) return;
        float dB = linearValue > 0.001f ? Mathf.Log10(linearValue) * 20f : -80f;
        audioMixer.SetFloat(paramName, dB);
    }
}
