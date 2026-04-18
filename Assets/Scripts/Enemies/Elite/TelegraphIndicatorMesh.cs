using System.Collections;
using UnityEngine;

/// <summary>
/// 텔레그래프 (스킬 경고 표시) 컴포넌트 - Procedural Mesh 버전
/// Procedural Mesh Generator를 사용하는 이펙트용
/// 자동 페이드 인/아웃 및 크기 조절
/// </summary>
public class TelegraphIndicatorMesh : MonoBehaviour
{
    [Header("설정")]
    private SkillData skillData;
    private float duration;
    
    [Header("페이드 설정")]
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float fadeOutDuration = 0.2f;
    
    [Header("메시 관련")]
    private MeshRenderer meshRenderer;
    private Material meshMaterial;
    
    [Header("파티클 시스템")]
    private ParticleSystem particles;
    
    [Header("상태")]
    private bool isInitialized = false;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        // MeshRenderer 찾기
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = GetComponentInChildren<MeshRenderer>();
        }
        
        if (meshRenderer != null)
        {
            // 머티리얼 복사 (인스턴스화)
            meshMaterial = meshRenderer.material;
        }
        
        // Particle System 찾기 (선택 사항)
        particles = GetComponent<ParticleSystem>();
        if (particles == null)
        {
            particles = GetComponentInChildren<ParticleSystem>();
        }
    }

    /// <summary>
    /// Telegraph 초기화
    /// </summary>
    /// <param name="skill">스킬 데이터</param>
    /// <param name="displayDuration">표시 시간</param>
    /// <param name="scaleMultiplier">Phase별 스케일 배율 (기본 1.0)</param>
    public void Initialize(SkillData skill, float displayDuration, float scaleMultiplier = 1.0f)
    {
        if (skill == null)
        {
            Debug.LogError("[TelegraphIndicatorMesh] SkillData가 null입니다!");
            return;
        }

        skillData = skill;
        duration = displayDuration;
        isInitialized = true;


        // 색상 설정
        if (meshMaterial != null)
        {
            Color startColor = skillData.TelegraphColor;
            startColor.a = 0f; // 초기 알파 0
            meshMaterial.color = startColor;
            
        }
        else
        {
            Debug.LogWarning("[TelegraphIndicatorMesh] MeshRenderer 또는 Material이 없습니다!");
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
        }
        else
        {
            Debug.LogWarning($"   Collider: 없음!");
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
            Debug.LogWarning($"[TelegraphIndicatorMesh] Rigidbody2D 발견! 제거합니다.");
            Destroy(rb);
        }
        
        // 2. 모든 Collider2D를 Trigger로 강제 설정
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (var col in colliders)
        {
            if (!col.isTrigger)
            {
                Debug.LogWarning($"[TelegraphIndicatorMesh] {col.GetType().Name}이 Trigger가 아닙니다! Is Trigger = true로 변경합니다.");
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
                    Debug.LogWarning($"[TelegraphIndicatorMesh] 자식 {col.gameObject.name}의 {col.GetType().Name}이 Trigger가 아닙니다! Is Trigger = true로 변경합니다.");
                    col.isTrigger = true;
                }
            }
        }
        
    }

    /// <summary>
    /// AOE 형태에 따른 크기 설정 (Phase별 스케일 적용)
    /// </summary>
    /// <param name="scaleMultiplier">Phase별 스케일 배율 (Phase 1: 1.0, Phase 2: 1.5, Phase 3: 2.0)</param>
    private void SetupSize(float scaleMultiplier = 1.0f)
    {
        if (skillData == null)
        {
            Debug.LogError("[TelegraphIndicatorMesh] SetupSize: skillData가 null입니다!");
            return;
        }


        switch (skillData.AoeShape)
        {
            case AOEShapeType.Circle:
                // 원형: 반경에 Phase 스케일 적용 (DamageArea와 일치)
                float radius = skillData.AoeRadius * scaleMultiplier;
                transform.localScale = new Vector3(radius, radius, 1f);
                break;

            case AOEShapeType.Triangle: // Fan (부채꼴)
                // 부채꼴: 반경에 Phase 스케일 적용 (DamageArea와 일치)
                float fanRadius = skillData.AoeRadius * scaleMultiplier;
                transform.localScale = new Vector3(fanRadius, fanRadius, 1f);
                break;

            case AOEShapeType.Rectangle:
                // 직사각형: 크기 직접 사용 + Phase 스케일 적용
                float rectX = skillData.AoeSize.x * scaleMultiplier;
                float rectY = skillData.AoeSize.y * scaleMultiplier;
                transform.localScale = new Vector3(rectX, rectY, 1f);
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
        // 1. 페이드 인
        yield return StartCoroutine(FadeToAlpha(skillData.TelegraphColor.a, fadeInDuration));

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
        if (meshMaterial == null) yield break;

        Color startColor = meshMaterial.color;
        float startAlpha = startColor.a;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            meshMaterial.color = new Color(
                startColor.r,
                startColor.g,
                startColor.b,
                currentAlpha
            );

            yield return null;
        }

        // 최종값 보장
        meshMaterial.color = new Color(
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


