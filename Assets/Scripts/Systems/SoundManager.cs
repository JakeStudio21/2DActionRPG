using UnityEngine;

public class SoundManager : Singleton<SoundManager>
{
    public AudioSource bgmSource;
    public AudioSource sfxSource;
    
    [Header("🎵 CueSystem 연동")]
    public bool enableCueSystemIntegration = true;

    public void PlayBGM(AudioClip clip)
    {
        if (bgmSource != null)
        {
            bgmSource.clip = clip;
            bgmSource.loop = true;
            bgmSource.Play();
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }
    
    /// <summary>
    /// 🎵 CueSystem용 SFX 재생 (볼륨/피치 조절)
    /// </summary>
    public void PlaySFX(AudioClip clip, float volume, float pitch)
    {
        if (sfxSource != null)
        {
            // 임시로 볼륨/피치 조절하여 재생
            float originalVolume = sfxSource.volume;
            float originalPitch = sfxSource.pitch;
            
            sfxSource.volume = volume;
            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(clip);
            
            // 다음 프레임에 원래 설정 복원
            StartCoroutine(RestoreAudioSettings(originalVolume, originalPitch));
        }
    }
    
    /// <summary>
    /// 오디오 설정 복원 (다음 프레임)
    /// </summary>
    private System.Collections.IEnumerator RestoreAudioSettings(float originalVolume, float originalPitch)
    {
        yield return null; // 한 프레임 대기
        
        if (sfxSource != null)
        {
            sfxSource.volume = originalVolume;
            sfxSource.pitch = originalPitch;
        }
    }

    public void SetBGMVolume(float volume)
    {
        if (bgmSource != null)
            bgmSource.volume = volume;
    }

    public void SetSFXVolume(float volume)
    {
        if (sfxSource != null)
            sfxSource.volume = volume;
    }
} 