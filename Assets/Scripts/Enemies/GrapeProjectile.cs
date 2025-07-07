using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrapeProjectile : MonoBehaviour
{
    [SerializeField] private float duration = 1f;
    [SerializeField] private AnimationCurve animCurve;
    [SerializeField] private float heightY = 3f;
    [SerializeField] private GameObject grapeProjectileShadow;
    [SerializeField] private GameObject splatterPrefab;

    private int damage;
    private Coroutine projectileCoroutine;
    private Coroutine shadowCoroutine;
    private bool isReallyActive = false; // ⭐ 실제 사용 여부 플래그

    public void SetDamage(int damage)
    {
        this.damage = damage;
    }

    /// <summary>
    /// ⭐ 풀 초기화와 실제 사용 구분
    /// </summary>
    private void OnEnable() 
    {
        // ⭐ 0.1초 후 실제 활성화 확인 (풀 초기화는 즉시 비활성화됨)
        StartCoroutine(CheckRealActivation());
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
            StartProjectileMotion();
        }
    }

    /// <summary>
    /// ⭐ 풀로 반환 시 코루틴 중단 및 플래그 리셋
    /// </summary>
    private void OnDisable()
    {
        StopAllProjectileCoroutines();
        isReallyActive = false; // ⭐ 플래그 리셋
    }

    /// <summary>
    /// 발사체 움직임 시작 (실제 사용 시에만 실행)
    /// </summary>
    private void StartProjectileMotion()
    {
        if (!isReallyActive) return; // ⭐ 실제 사용이 아니면 중단
        
        // ⭐ 이전 코루틴 완전 중단
        StopAllProjectileCoroutines();
        
        // ⭐ 실시간 플레이어 위치 획득
        var playerController = FindObjectOfType<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("[GrapeProjectile] PlayerController를 찾을 수 없습니다!");
            gameObject.SetActive(false);
            return;
        }

        Vector3 playerPos = playerController.transform.position;
        Vector3 grapeShadowStartPosition = transform.position + new Vector3(0, -0.3f, 0);

        // 그림자 풀링 생성
        GameObject grapeShadow = GamePoolManager.Instance.SpawnFromPool("GrapeShadow", grapeShadowStartPosition, Quaternion.identity);
        if (grapeShadow == null)
        {
            Debug.LogError("[GrapeProjectile] GrapeShadow 풀링 실패!");
            gameObject.SetActive(false);
            return;
        }

        // ⭐ 코루틴 참조 저장하여 관리
        projectileCoroutine = StartCoroutine(ProjectileCurveRoutine(transform.position, playerPos, grapeShadow));
        shadowCoroutine = StartCoroutine(MoveGrapeShadowRoutine(grapeShadow, grapeShadowStartPosition, playerPos));
        
        Debug.Log($"[GrapeProjectile] 발사체 시작 - 목표: {playerPos}");
    }

    /// <summary>
    /// 모든 발사체 코루틴 중단
    /// </summary>
    private void StopAllProjectileCoroutines()
    {
        if (projectileCoroutine != null)
        {
            StopCoroutine(projectileCoroutine);
            projectileCoroutine = null;
        }
        
        if (shadowCoroutine != null)
        {
            StopCoroutine(shadowCoroutine);
            shadowCoroutine = null;
        }
    }

    private IEnumerator ProjectileCurveRoutine(Vector3 startPosition, Vector3 endPosition, GameObject grapeShadow) 
    {
        float timePassed = 0f;

        while (timePassed < duration)
        {
            timePassed += Time.deltaTime;
            float linearT = timePassed / duration;
            float heightT = animCurve.Evaluate(linearT);
            float height = Mathf.Lerp(0f, heightY, heightT);

            transform.position = Vector2.Lerp(startPosition, endPosition, linearT) + new Vector2(0f, height);

            yield return null;
        }

        // 스플래터 풀링 생성
        GameObject splatter = GamePoolManager.Instance.SpawnFromPool("Grape Projectile Splatter", transform.position, Quaternion.identity);
        if (splatter != null && splatter.TryGetComponent(out GrapeLandSplatter grapeSplatter))
        {
            grapeSplatter.SetDamage(damage);
            grapeSplatter.StartFadeEffect();
        }

        // ⭐ 코루틴 참조 제거
        projectileCoroutine = null;
        
        // 풀로 반환
        gameObject.SetActive(false);
        if (grapeShadow != null) grapeShadow.SetActive(false);
        
        Debug.Log("[GrapeProjectile] 발사체 완료 - 풀로 반환");
    }

    private IEnumerator MoveGrapeShadowRoutine(GameObject grapeShadow, Vector3 startPosition, Vector3 endPosition) 
    {
        float timePassed = 0f;

        while (timePassed < duration) 
        {
            if (grapeShadow == null) yield break; // 그림자가 파괴된 경우 중단
            
            timePassed += Time.deltaTime;
            float linearT = timePassed / duration;
            grapeShadow.transform.position = Vector2.Lerp(startPosition, endPosition, linearT);
            yield return null;
        }
        
        // ⭐ 코루틴 참조 제거
        shadowCoroutine = null;
        
        if (grapeShadow != null) grapeShadow.SetActive(false);
    }
}
