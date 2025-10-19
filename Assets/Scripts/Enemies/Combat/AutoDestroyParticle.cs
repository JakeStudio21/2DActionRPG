using UnityEngine;

/// <summary>
/// 파티클 시스템 기반 이펙트 자동 제거 스크립트
/// ⭐ AOE 이펙트 프리팹에 사용
/// </summary>
public class AutoDestroyParticle : MonoBehaviour
{
    [Header("Auto Destroy Settings")]
    [SerializeField] private float lifetime = 2.0f;
    [SerializeField] private bool useParticleDuration = true; // 파티클 Duration 기반
    
    [Header("Pooling Settings")]
    [SerializeField] private bool usePooling = true;
    [SerializeField] private string poolTag = "";
    
    private ParticleSystem[] particleSystems;
    private float maxDuration = 0f;
    
    private void Awake()
    {
        // 모든 파티클 시스템 찾기
        particleSystems = GetComponentsInChildren<ParticleSystem>();
        
        if (useParticleDuration)
        {
            CalculateMaxParticleDuration();
        }
    }
    
    private void OnEnable()
    {
        // 활성화될 때마다 파티클 재생
        PlayAllParticles();
        
        // 자동 제거 타이머 시작
        float destroyTime = useParticleDuration ? maxDuration : lifetime;
        
        if (usePooling)
        {
            Invoke(nameof(ReturnToPool), destroyTime);
        }
        else
        {
            Destroy(gameObject, destroyTime);
        }
    }
    
    private void OnDisable()
    {
        // Invoke 취소
        CancelInvoke();
    }
    
    /// <summary>
    /// 모든 파티클 시스템 재생
    /// </summary>
    private void PlayAllParticles()
    {
        foreach (var ps in particleSystems)
        {
            if (ps != null)
            {
                ps.Stop();
                ps.Clear();
                ps.Play();
            }
        }
    }
    
    /// <summary>
    /// 파티클 시스템의 최대 Duration 계산
    /// </summary>
    private void CalculateMaxParticleDuration()
    {
        maxDuration = 0f;
        
        foreach (var ps in particleSystems)
        {
            if (ps != null)
            {
                float psDuration = ps.main.duration + ps.main.startLifetime.constantMax;
                if (psDuration > maxDuration)
                {
                    maxDuration = psDuration;
                }
            }
        }
        
        // 여유 시간 추가
        maxDuration += 0.5f;
    }
    
    /// <summary>
    /// 풀로 반환
    /// </summary>
    private void ReturnToPool()
    {
        if (GamePoolManager.Instance != null)
        {
            // 풀 태그가 없으면 프리팹 이름 사용
            string tag = string.IsNullOrEmpty(poolTag) ? gameObject.name.Replace("(Clone)", "") : poolTag;
            
            try
            {
                GamePoolManager.Instance.ReturnToPool(tag, gameObject);
                Debug.Log($"[AutoDestroyParticle] {gameObject.name}을(를) 풀로 반환했습니다.");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[AutoDestroyParticle] 풀 반환 실패: {e.Message}, 직접 비활성화합니다.");
                gameObject.SetActive(false);
            }
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 파티클이 모두 재생 완료되었는지 확인
    /// </summary>
    public bool IsFinished()
    {
        foreach (var ps in particleSystems)
        {
            if (ps != null && ps.IsAlive())
            {
                return false;
            }
        }
        return true;
    }
    
    /// <summary>
    /// 파티클 강제 정지
    /// </summary>
    public void StopAllParticles()
    {
        foreach (var ps in particleSystems)
        {
            if (ps != null)
            {
                ps.Stop();
            }
        }
    }
}

