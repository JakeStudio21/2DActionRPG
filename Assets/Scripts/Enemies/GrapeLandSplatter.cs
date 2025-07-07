using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrapeLandSplatter : MonoBehaviour
{
    [SerializeField] public int damageAmount = 1; // 발사체가 주는 데미지
    private SpriteFade spriteFade;
    private CapsuleCollider2D capsuleCollider;
    private bool isReallyActive = false; // 실제 사용 여부 플래그

    public void SetDamage(int damage)
    {
        this.damageAmount = damage;
    }

    private void Awake() {
        InitializeComponents();
    }

    private void OnEnable() {
        // ⭐ 핵심 수정: 실제 활성화인지 풀 초기화인지 구분
        InitializeComponents();

        if (capsuleCollider != null)
            capsuleCollider.enabled = true;

        // ⭐ 0.1초 후 실제 활성화 확인 (풀 초기화는 즉시 비활성화됨)
        StartCoroutine(CheckRealActivation());
        
        Invoke("DisableCollider", 0.2f);
    }

    /// <summary>
    /// 실제 활성화인지 확인하는 코루틴
    /// </summary>
    private IEnumerator CheckRealActivation()
    {
        yield return new WaitForSeconds(0.1f);
        
        // 0.1초 후에도 활성화되어 있으면 실제 사용
        if (gameObject.activeInHierarchy)
        {
            isReallyActive = true;
            StartFadeEffect();
        }
    }

    /// <summary>
    /// 실제 사용 시에만 페이드 효과 시작
    /// </summary>
    public void StartFadeEffect()
    {
        if (spriteFade != null && isReallyActive)
        {
            Debug.Log($"[GrapeLandSplatter] SpriteFade 시작: {gameObject.name}");
            StartCoroutine(spriteFade.SlowFadeRoutine());
        }
        else if (spriteFade == null)
        {
            Debug.LogError($"[GrapeLandSplatter] SpriteFade가 null입니다: {gameObject.name}");
        }
    }

    /// <summary>
    /// 컴포넌트 강제 재초기화
    /// </summary>
    private void InitializeComponents()
    {
        if (spriteFade == null)
            spriteFade = GetComponent<SpriteFade>();
        if (capsuleCollider == null)
            capsuleCollider = GetComponent<CapsuleCollider2D>();
        
        if (spriteFade == null)
            Debug.LogError($"[GrapeLandSplatter] SpriteFade 컴포넌트를 찾을 수 없습니다: {gameObject.name}");
        if (capsuleCollider == null)
            Debug.LogError($"[GrapeLandSplatter] CapsuleCollider2D 컴포넌트를 찾을 수 없습니다: {gameObject.name}");
    }

    private void OnDisable()
    {
        // 비활성화될 때 플래그 리셋
        isReallyActive = false;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        PlayerHealth playerHealth = other.gameObject.GetComponent<PlayerHealth>();
        playerHealth?.TakeDamage(damageAmount, transform);
    }

    private void DisableCollider() {
        if (capsuleCollider != null)
            capsuleCollider.enabled = false;
    }
}
