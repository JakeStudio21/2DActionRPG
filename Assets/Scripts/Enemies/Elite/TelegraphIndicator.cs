using System.Collections;
using UnityEngine;

/// <summary>
/// 텔레그래프 (스킬 경고 표시) 컴포넌트
/// 바닥에 표시되는 반투명 경고 이펙트 관리
/// 자동 페이드 인/아웃 및 크기 조절
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class TelegraphIndicator : MonoBehaviour
{
    [Header("컴포넌트")]
    private SpriteRenderer spriteRenderer;
    
    [Header("설정")]
    private SkillData skillData;
    private float duration;
    private float targetAlpha = 0.5f; // ⭐ Phase 3: skillData 없을 때 사용할 목표 알파값
    
    [Header("⭐ Phase 3: 시전자별 색상")]
    [SerializeField] private Color enemyTelegraphColor = new Color(1f, 0f, 0f, 0.5f); // 빨강
    [SerializeField] private Color playerTelegraphColor = new Color(0f, 0.5f, 1f, 0.5f); // 파랑
    
    [Header("페이드 설정")]
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float fadeOutDuration = 0.2f;
    
    [Header("상태")]
    private bool isInitialized = false;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer == null)
        {
            Debug.LogError("[TelegraphIndicator] SpriteRenderer가 없습니다!");
        }
    }

    /// <summary>
    /// Telegraph 초기화 (보스/엘리트용)
    /// </summary>
    /// <param name="skill">스킬 데이터</param>
    /// <param name="displayDuration">표시 시간</param>
    /// <param name="scaleMultiplier">Phase별 스케일 배율 (기본 1.0)</param>
    /// <param name="casterType">시전자 타입 (기본: Enemy)</param>
    public void Initialize(SkillData skill, float displayDuration, float scaleMultiplier = 1.0f, AOECasterType casterType = AOECasterType.Enemy)
    {
        if (skill == null)
        {
            Debug.LogError("[TelegraphIndicator] SkillData가 null입니다!");
            return;
        }

        Debug.Log($"📍 [TelegraphIndicator] Initialize() 호출:");
        Debug.Log($"   스킬: {skill.SkillName}");
        Debug.Log($"   표시 시간: {displayDuration}초");
        Debug.Log($"   스케일 배율: {scaleMultiplier}x");
        Debug.Log($"   시전자: {casterType}");

        skillData = skill;
        duration = displayDuration;
        isInitialized = true;

        // ⭐ Phase 3: 시전자별 색상 설정
        Color baseColor = (casterType == AOECasterType.Enemy) ? enemyTelegraphColor : playerTelegraphColor;
        this.targetAlpha = skillData.TelegraphColor.a; // ⭐ 목표 알파값 저장
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(
                baseColor.r,
                baseColor.g,
                baseColor.b,
                0f // 초기 알파 0 (페이드 인 시작)
            );
        }

        // ⭐ 크기 설정 (AOE 형태 + Phase별 스케일 적용)
        SetupSize(scaleMultiplier);
        
        // ⭐⭐⭐ Collider 강제 설정 (Trigger 활성화 + Rigidbody 제거)
        ForceColliderToTrigger();
        
        // ⭐⭐⭐ Physics2D 즉시 동기화 (Collider Bounds 업데이트)
        Physics2D.SyncTransforms();
        
        // 📍 Collider 확인 (디버그)
        var collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            Debug.Log($"   Collider: {collider.GetType().Name} (Is Trigger: {collider.isTrigger})");
            Debug.Log($"   Collider Bounds (초기화 직후): Center={collider.bounds.center}, Extents={collider.bounds.extents}");
        }
        else
        {
            Debug.LogWarning($"   Collider: 없음!");
        }

        // 페이드 인 → 대기 → 페이드 아웃 → 파괴
        StartFadeSequence();
    }
    
    /// <summary>
    /// ⭐ Phase 3: Telegraph 초기화 (플레이어용 - 개별 파라미터)
    /// SkillData 타입 제약 없이 사용 가능
    /// </summary>
    public void InitializeForPlayer(
        AOEShapeType shape,
        Vector3 position,
        float radius,
        Vector2 size,
        float angle,
        float displayDuration,
        float scaleMultiplier = 1.0f,
        AOECasterType casterType = AOECasterType.Player)
    {
        Debug.Log($"📍 [TelegraphIndicator] InitializeForPlayer() 호출:");
        Debug.Log($"   Shape: {shape}");
        Debug.Log($"   Position: {position}");
        Debug.Log($"   표시 시간: {displayDuration}초");
        Debug.Log($"   시전자: {casterType}");

        // 기본 설정
        this.skillData = null; // SkillData 없음
        this.duration = displayDuration;
        this.isInitialized = true;
        
        // 위치 설정
        transform.position = position;
        
        // ⭐ 색상 설정
        Color baseColor = (casterType == AOECasterType.Enemy) ? enemyTelegraphColor : playerTelegraphColor;
        this.targetAlpha = baseColor.a; // ⭐ 목표 알파값 저장
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(
                baseColor.r,
                baseColor.g,
                baseColor.b,
                0f // 초기 알파 0 (페이드 인 시작)
            );
        }

        // ⭐ 크기 설정 (개별 파라미터 기반)
        SetupSizeManual(shape, radius, size, angle, scaleMultiplier);
        
        // ⭐ Collider 강제 설정 (Trigger 활성화)
        ForceColliderToTrigger();
        
        // Physics2D 즉시 동기화
        Physics2D.SyncTransforms();
        
        // Collider 확인 (디버그)
        var collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            Debug.Log($"   Collider: {collider.GetType().Name} (Is Trigger: {collider.isTrigger})");
        }

        // 페이드 인 → 대기 → 페이드 아웃 → 파괴
        StartFadeSequence();
    }
    
    /// <summary>
    /// Collider를 강제로 Trigger로 설정 (물리 충돌 방지)
    /// </summary>
    private void ForceColliderToTrigger()
    {
        // 1. Rigidbody2D 제거 (있으면 물리 충돌 발생)
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Debug.LogWarning($"[TelegraphIndicator] Rigidbody2D 발견! 제거합니다.");
            Destroy(rb);
        }
        
        // 2. 모든 Collider2D를 Trigger로 강제 설정
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (var col in colliders)
        {
            if (!col.isTrigger)
            {
                Debug.LogWarning($"[TelegraphIndicator] {col.GetType().Name}이 Trigger가 아닙니다! Is Trigger = true로 변경합니다.");
                col.isTrigger = true;
            }
        }
        
        // 3. 자식 GameObject의 Collider도 확인
        Collider2D[] childColliders = GetComponentsInChildren<Collider2D>();
        foreach (var col in childColliders)
        {
            if (col.gameObject != gameObject) // 자식만
            {
                if (!col.isTrigger)
                {
                    Debug.LogWarning($"[TelegraphIndicator] 자식 {col.gameObject.name}의 {col.GetType().Name}이 Trigger가 아닙니다! Is Trigger = true로 변경합니다.");
                    col.isTrigger = true;
                }
            }
        }
        
        Debug.Log($"✅ [TelegraphIndicator] Collider Trigger 설정 완료!");
    }

    /// <summary>
    /// AOE 형태에 따른 크기 설정 (Phase별 스케일 적용)
    /// </summary>
    /// <param name="scaleMultiplier">Phase별 스케일 배율 (Phase 1: 1.0, Phase 2: 1.5, Phase 3: 2.0)</param>
    private void SetupSize(float scaleMultiplier = 1.0f)
    {
        if (skillData == null)
        {
            Debug.LogError("[TelegraphIndicator] SetupSize: skillData가 null입니다!");
            return;
        }

        Debug.Log($"[TelegraphIndicator] SetupSize: 스킬={skillData.SkillName}, 형태={skillData.AoeShape}, 반경={skillData.AoeRadius}, 스케일={scaleMultiplier}x");

        switch (skillData.AoeShape)
        {
            case AOEShapeType.Circle:
                // ⭐ 원형: 반경을 그대로 사용 (DamageArea와 일치)
                float circleRadius = skillData.AoeRadius * scaleMultiplier;
                transform.localScale = new Vector3(circleRadius, circleRadius, 1f);
                Debug.Log($"[TelegraphIndicator] Circle 크기 설정: 반경={skillData.AoeRadius}, 배율={scaleMultiplier}x, 최종 반경={circleRadius}, Scale={transform.localScale}");
                break;

            case AOEShapeType.Triangle: // Fan (부채꼴)
                // 부채꼴: 반경에 Phase 스케일 적용 (DamageArea와 일치)
                float fanRadius = skillData.AoeRadius * scaleMultiplier;
                transform.localScale = new Vector3(fanRadius, fanRadius, 1f);
                Debug.Log($"[TelegraphIndicator] Fan 크기 설정: 반경={skillData.AoeRadius}, 배율={scaleMultiplier}x, 최종 반경={fanRadius}, Scale={transform.localScale}");
                break;

            case AOEShapeType.Rectangle:
                // 직사각형: 크기 직접 사용 + Phase 스케일 적용
                float rectX = skillData.AoeSize.x * scaleMultiplier;
                float rectY = skillData.AoeSize.y * scaleMultiplier;
                transform.localScale = new Vector3(rectX, rectY, 1f);
                Debug.Log($"[TelegraphIndicator] Rectangle 크기 설정: 원본={skillData.AoeSize}, 배율={scaleMultiplier}x, 최종=({rectX}, {rectY}), Scale={transform.localScale}");
                break;
        }
    }
    
    /// <summary>
    /// ⭐ Phase 3: AOE 형태에 따른 크기 설정 (개별 파라미터 버전)
    /// </summary>
    private void SetupSizeManual(AOEShapeType shape, float radius, Vector2 size, float angle, float scaleMultiplier)
    {
        Debug.Log($"[TelegraphIndicator] SetupSizeManual: 형태={shape}, 반경={radius}, 크기={size}, 스케일={scaleMultiplier}x");

        switch (shape)
        {
            case AOEShapeType.Circle:
                // ⭐ 원형: 반경을 그대로 사용 (DamageArea와 일치)
                float finalRadius = radius * scaleMultiplier;
                transform.localScale = new Vector3(finalRadius, finalRadius, 1f);
                Debug.Log($"[TelegraphIndicator] Circle 크기 설정: 반경={radius}, 배율={scaleMultiplier}x, 최종 반경={finalRadius}");
                break;

            case AOEShapeType.Triangle: // Fan (부채꼴)
                // 부채꼴: 반경에 스케일 적용
                float fanRadius = radius * scaleMultiplier;
                transform.localScale = new Vector3(fanRadius, fanRadius, 1f);
                Debug.Log($"[TelegraphIndicator] Fan 크기 설정: 반경={radius}, 배율={scaleMultiplier}x, 최종 반경={fanRadius}");
                break;

            case AOEShapeType.Rectangle:
                // 직사각형: 크기 직접 사용 + 스케일 적용
                float rectX = size.x * scaleMultiplier;
                float rectY = size.y * scaleMultiplier;
                transform.localScale = new Vector3(rectX, rectY, 1f);
                Debug.Log($"[TelegraphIndicator] Rectangle 크기 설정: 원본={size}, 배율={scaleMultiplier}x, 최종=({rectX}, {rectY})");
                break;
        }
    }

    /// <summary>
    /// 페이드 시퀀스 시작
    /// </summary>
    private void StartFadeSequence()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeSequenceCoroutine());
    }

    /// <summary>
    /// 페이드 인 → 대기 → 페이드 아웃 코루틴
    /// </summary>
    private IEnumerator FadeSequenceCoroutine()
    {
        // ⭐ 목표 알파값 결정: skillData가 있으면 사용, 없으면 targetAlpha 사용
        float fadeTargetAlpha = (skillData != null) ? skillData.TelegraphColor.a : targetAlpha;
        
        // 1. 페이드 인
        yield return StartCoroutine(FadeToAlpha(fadeTargetAlpha, fadeInDuration));

        // 2. 대기 (표시 시간)
        // ⚠️ 주의: 실제로는 EliteSkillController가 RemoveTelegraph()로 수동 제거
        // 하지만 안전장치로 최대 대기 시간 설정 (10초, 충분히 긴 시간)
        float waitTime = Mathf.Max(duration - fadeInDuration - fadeOutDuration, 10f);
        if (waitTime > 0f)
        {
            yield return new WaitForSeconds(waitTime);
        }

        // 3. 페이드 아웃 (자동 제거 시)
        yield return StartCoroutine(FadeToAlpha(0f, fadeOutDuration));

        // 4. 파괴 (자동 제거 시)
        Destroy(gameObject);
    }

    /// <summary>
    /// 알파값 페이드
    /// </summary>
    private IEnumerator FadeToAlpha(float targetAlpha, float fadeDuration)
    {
        if (spriteRenderer == null) yield break;

        Color startColor = spriteRenderer.color;
        float startAlpha = startColor.a;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            spriteRenderer.color = new Color(
                startColor.r,
                startColor.g,
                startColor.b,
                currentAlpha
            );

            yield return null;
        }

        // 최종값 보장
        spriteRenderer.color = new Color(
            startColor.r,
            startColor.g,
            startColor.b,
            targetAlpha
        );
    }

    /// <summary>
    /// 수동 파괴 (Telegraph 조기 제거 시)
    /// </summary>
    public void ForceDestroy()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        Destroy(gameObject);
    }

    #region 디버그

    private void OnDrawGizmos()
    {
        if (!isInitialized || skillData == null) return;

        // Telegraph 범위 시각화
        Gizmos.color = Color.yellow;

        switch (skillData.AoeShape)
        {
            case AOEShapeType.Circle:
                Gizmos.DrawWireSphere(transform.position, skillData.AoeRadius);
                break;

            case AOEShapeType.Rectangle:
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(Vector3.zero, new Vector3(skillData.AoeSize.x, skillData.AoeSize.y, 0.1f));
                Gizmos.matrix = Matrix4x4.identity;
                break;
        }
    }

    #endregion
}


