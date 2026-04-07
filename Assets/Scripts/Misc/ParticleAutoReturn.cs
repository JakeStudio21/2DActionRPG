using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ParticleSystem 완료 시 자동으로 GamePoolManager에 반환하는 컴포넌트
/// SlashAnim 기능도 통합
/// </summary>
public class ParticleAutoReturn : MonoBehaviour, IPoolTagReceiver
{
    private ParticleSystem particleSystem;
    private bool isReturningToPool = false;
    
    [SerializeField] private string poolTag = ""; // SpawnFromPool에서 자동 주입됨 (수동 설정 불필요)
    
    private void Awake()
    {
        particleSystem = GetComponent<ParticleSystem>();
        
        if (particleSystem == null)
        {
            Debug.LogError($"[ParticleAutoReturn] {gameObject.name}에 ParticleSystem이 없습니다!");
        }
    }
    
    public void SetPoolTag(string tag)
    {
        poolTag = tag;
    }
    
    private void OnEnable()
    {
        isReturningToPool = false;
        
        if (particleSystem != null)
        {
            // ParticleSystem이 자동으로 재생되도록 설정 확인
            if (!particleSystem.main.playOnAwake)
            {
                particleSystem.Play();
            }
        }
    }
    
    private void Update()
    {
        // 🔑 SlashAnim의 기능을 흡수: VFX 완료 감지
        if (particleSystem != null && !particleSystem.IsAlive(false) && !isReturningToPool)
        {
            // 🔑 개선: SetActive(false) 대신 GamePoolManager에 반환
            ReturnToPool();
        }
    }
    
    private void OnDisable()
    {
        StopAllCoroutines();
        isReturningToPool = false;
    }
    
    /// <summary>
    /// GamePoolManager에 반환 (Arrow/Health와 동일한 방식)
    /// </summary>
    private void ReturnToPool()
    {
        if (isReturningToPool) return; // 중복 반환 방지
        
        isReturningToPool = true;
        
        // 풀 태그 결정: SpawnFromPool에서 주입된 값 사용, 없으면 이름으로 추론
        string tagToUse = !string.IsNullOrEmpty(poolTag) ? poolTag : DeterminePoolTag();
        
        if (string.IsNullOrEmpty(poolTag))
        {
            Debug.LogWarning($"[ParticleAutoReturn] {gameObject.name}: poolTag가 주입되지 않음. DeterminePoolTag()로 추론: '{tagToUse}'");
        }
        
        if (GamePoolManager.Instance != null)
        {
            // ✅ 올바른 풀 반환
            GamePoolManager.Instance.ReturnToPool(tagToUse, gameObject);
            Debug.Log($"[ParticleAutoReturn] {tagToUse} VFX를 풀에 정상 반환");
        }
        else
        {
            // 백업: 기존 SlashAnim 방식
            Debug.LogWarning("[ParticleAutoReturn] GamePoolManager가 없어 SetActive(false) 사용");
            gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 오브젝트 이름으로부터 풀 태그 추정
    /// </summary>
    private string DeterminePoolTag()
    {
        string objectName = gameObject.name.Replace("(Clone)", "").Trim();
        
        // "_숫자" 패턴 제거 (예: "Projectile VFX_0" → "Projectile VFX")
        int underscoreIndex = objectName.LastIndexOf('_');
        if (underscoreIndex > 0)
        {
            string afterUnderscore = objectName.Substring(underscoreIndex + 1);
            if (int.TryParse(afterUnderscore, out _)) // 숫자면 제거
            {
                objectName = objectName.Substring(0, underscoreIndex);
            }
        }
        
        return objectName;
    }
}
