using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flash : MonoBehaviour
{
    [SerializeField] private Material whiteFlashMat;
    
    // ❌ 제거: 설정값 필드
    // [SerializeField] private float restoreDefaultMatTime = .2f;
    
    // ✅ 런타임 설정값 (주입받음)
    private float restoreDefaultMatTime = 0.2f;

    private Material defaultMat;
    private SpriteRenderer spriteRenderer;

    private void Awake() {
        spriteRenderer = GetComponent<SpriteRenderer>();
        defaultMat = spriteRenderer.material;
    }

    /// <summary>
    /// ⭐ SRP: Flash 설정값 주입 (BaseClassBehaviour에서 호출)
    /// </summary>
    public void SetFlashSettings(float flashDuration)
    {
        restoreDefaultMatTime = flashDuration;
        
    }

    public float GetRestoreMatTime() {
        return restoreDefaultMatTime;
    }

    public IEnumerator FlashRoutine() {
        spriteRenderer.material = whiteFlashMat;
        yield return new WaitForSeconds(restoreDefaultMatTime);
        spriteRenderer.material = defaultMat;
    }
}