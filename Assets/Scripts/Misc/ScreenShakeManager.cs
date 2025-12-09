using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

/// <summary>
/// 스크린 셰이크 매니저
/// 엘리트/보스 스킬에서 강도별 셰이크 지원
/// </summary>
public class ScreenShakeManager : Singleton<ScreenShakeManager>
{
    private CinemachineImpulseSource source;
    
    [Header("프리셋 강도")]
    [SerializeField] private float weakIntensity = 0.5f;      // 엘리트용
    [SerializeField] private float normalIntensity = 1.0f;    // 기본
    [SerializeField] private float strongIntensity = 2.0f;    // 보스용
    [SerializeField] private float massiveIntensity = 4.0f;   // 보스 필살기용

    protected override void Awake() 
    {
        base.Awake();
        source = GetComponent<CinemachineImpulseSource>();
    }

    /// <summary>
    /// 기본 셰이크 (호환성 유지)
    /// </summary>
    public void ShakeScreen() 
    {
        if (source != null)
        {
            source.GenerateImpulse();
        }
    }
    
    /// <summary>
    /// 강도 지정 셰이크
    /// </summary>
    public void ShakeScreen(float intensity) 
    {
        if (source != null)
        {
            source.GenerateImpulse(intensity);
        }
    }
    
    /// <summary>
    /// 프리셋 셰이크 - 약함 (엘리트)
    /// </summary>
    public void ShakeWeak() 
    {
        ShakeScreen(weakIntensity);
    }
    
    /// <summary>
    /// 프리셋 셰이크 - 보통
    /// </summary>
    public void ShakeNormal() 
    {
        ShakeScreen(normalIntensity);
    }
    
    /// <summary>
    /// 프리셋 셰이크 - 강함 (보스)
    /// </summary>
    public void ShakeStrong() 
    {
        ShakeScreen(strongIntensity);
    }
    
    /// <summary>
    /// 프리셋 셰이크 - 매우 강함 (보스 필살기)
    /// </summary>
    public void ShakeMassive() 
    {
        ShakeScreen(massiveIntensity);
    }
}
