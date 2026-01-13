using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 카메라 흔들림 효과
/// </summary>
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }
    
    private Camera mainCamera;
    private Vector3 originalPosition;
    private bool isShaking = false;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        if (mainCamera != null)
        {
            originalPosition = mainCamera.transform.localPosition;
        }
    }
    
    /// <summary>
    /// 카메라 흔들림 시작
    /// </summary>
    /// <param name="intensity">강도 (0.0 ~ 1.0)</param>
    /// <param name="duration">지속 시간 (초)</param>
    public void Shake(float intensity, float duration)
    {
        if (mainCamera == null) return;
        
        if (isShaking)
        {
            StopAllCoroutines();
        }
        
        StartCoroutine(ShakeCoroutine(intensity, duration));
    }
    
    private IEnumerator ShakeCoroutine(float intensity, float duration)
    {
        isShaking = true;
        originalPosition = mainCamera.transform.localPosition;
        
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;
            
            mainCamera.transform.localPosition = originalPosition + new Vector3(x, y, 0);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        mainCamera.transform.localPosition = originalPosition;
        isShaking = false;
    }
}

