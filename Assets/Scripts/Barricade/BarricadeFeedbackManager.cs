using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 바리케이드 피드백 전역 관리자
/// 피로도 완화: 쿨다운, 연속 재생 제한
/// </summary>
public class BarricadeFeedbackManager : MonoBehaviour
{
    public static BarricadeFeedbackManager Instance { get; private set; }
    
    // ========================================
    // 쿨다운 추적
    // ========================================
    private float lastCameraShakeTime = -999f;
    private float lastBigFxTime = -999f;
    private Dictionary<string, float> soundCooldowns = new Dictionary<string, float>();
    
    // ========================================
    // 연속 재생 제한
    // ========================================
    private int consecutiveFxCount = 0;
    private float consecutiveFxResetTime = 0f;
    private const int MAX_CONSECUTIVE_FX = 3;      // 최대 연속 재생 횟수
    private const float CONSECUTIVE_RESET_TIME = 1f; // 리셋 시간
    
    // ========================================
    // 초기화
    // ========================================
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // ========================================
    // 카메라 흔들림 (쿨다운)
    // ========================================
    public bool CanShakeCamera(float cooldown)
    {
        return Time.time >= lastCameraShakeTime + cooldown;
    }
    
    public void ShakeCamera(float intensity, float duration, float cooldown)
    {
        if (!CanShakeCamera(cooldown)) return;

        lastCameraShakeTime = Time.time;

        if (ScreenShakeManager.Instance != null)
        {
            ScreenShakeManager.Instance.PlayShake(new ShakeData
            {
                useShake  = true,
                intensity = intensity,
                duration  = duration,
                delay     = 0f
            });
        }
    }
    
    // ========================================
    // 큰 이펙트 (쿨다운 + 연속 제한)
    // ========================================
    public bool CanPlayBigFx(float cooldown, bool limitConsecutive)
    {
        // 쿨다운 체크
        if (Time.time < lastBigFxTime + cooldown)
        {
            return false;
        }
        
        // 연속 재생 제한 체크
        if (limitConsecutive)
        {
            // 리셋 시간 경과 시 카운트 초기화
            if (Time.time > consecutiveFxResetTime)
            {
                consecutiveFxCount = 0;
            }
            
            if (consecutiveFxCount >= MAX_CONSECUTIVE_FX)
            {
                return false;
            }
        }
        
        return true;
    }
    
    public void PlayBigFx(GameObject fxPrefab, Vector3 position, float cooldown, bool limitConsecutive)
    {
        if (!CanPlayBigFx(cooldown, limitConsecutive))
        {
            return;
        }
        
        // 쿨다운 업데이트
        lastBigFxTime = Time.time;
        
        // 연속 카운트 증가
        consecutiveFxCount++;
        consecutiveFxResetTime = Time.time + CONSECUTIVE_RESET_TIME;
        
        // 이펙트 생성
        if (fxPrefab != null)
        {
            GameObject fx = Instantiate(fxPrefab, position, Quaternion.identity);
            Destroy(fx, 5f); // 5초 후 자동 파괴
        }
    }
    
    // ========================================
    // 사운드 쿨다운
    // ========================================
    public bool CanPlaySound(string soundID, float cooldown)
    {
        if (!soundCooldowns.ContainsKey(soundID))
        {
            soundCooldowns[soundID] = -999f;
        }
        
        return Time.time >= soundCooldowns[soundID] + cooldown;
    }
    
    public void PlaySound(AudioClip clip, float cooldown)
    {
        if (clip == null) return;
        
        string soundID = clip.name;
        
        if (!CanPlaySound(soundID, cooldown))
        {
            return;
        }
        
        soundCooldowns[soundID] = Time.time;
        AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position);
    }
    
}

