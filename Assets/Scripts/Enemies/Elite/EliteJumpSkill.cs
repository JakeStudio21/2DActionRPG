using System.Collections;
using UnityEngine;

/// <summary>
/// 엘리트 몬스터 점프 낙하 스킬
/// 포물선 이동으로 타겟 위치까지 이동 후 착지 시 DamageArea 생성
///
/// 사용법:
///   1. Elite 프리팹에 컴포넌트 추가
///   2. EliteSkillController가 SkillType.Jump 분기에서 Execute() 호출
///   3. 착지 시 onLanded 콜백 → EliteSkillController.OnJumpLanded() → SpawnDamageArea()
/// </summary>
public class EliteJumpSkill : MonoBehaviour
{
    [Header("🦘 점프 설정")]
    [Tooltip("최고점 Y 높이 (포물선 정점, 유닛)")]
    [SerializeField] private float jumpHeight = 2.5f;

    [Tooltip("전체 체공 시간 (초). 0이면 SkillData.ActionDuration 사용")]
    [Min(0f)]
    [SerializeField] private float jumpDuration = 0.5f;

    [Header("💥 착지 연출")]
    [Tooltip("착지 시 카메라 진동 (ShakeData 재사용)\nuseShake = false 이면 진동 없음")]
    [SerializeField] private ShakeData landingShakeData;

    // VFX는 EliteSkillController.SpawnDamageArea() → SpawnAOEEffectAtCenter()에서 일괄 처리
    // (Telegraph 피봇과 동기화되므로 별도 override 없음)

    // 내부 참조
    private BaseEnemy baseEnemy;
    private Coroutine jumpCoroutine;

    private void Awake()
    {
        baseEnemy = GetComponent<BaseEnemy>();
    }

    // ─────────────────────────────────────────
    // 공개 API
    // ─────────────────────────────────────────

    /// <summary>
    /// 점프 실행 (EliteSkillController에서 호출)
    /// </summary>
    /// <param name="skill">스킬 데이터 (duration, VFX fallback 참조)</param>
    /// <param name="targetPosition">착지 목표 위치 (cachedTargetPosition)</param>
    /// <param name="enemy">BaseEnemy 참조 (null이면 this에서 사용)</param>
    /// <param name="onLanded">착지 완료 콜백 → DamageArea 생성</param>
    public void Execute(SkillData skill, Vector3 targetPosition, BaseEnemy enemy, System.Action onLanded)
    {
        if (skill == null)
        {
            Debug.LogError("[EliteJumpSkill] SkillData가 null!");
            onLanded?.Invoke();
            return;
        }

        if (enemy != null) baseEnemy = enemy;

        if (jumpCoroutine != null)
        {
            StopCoroutine(jumpCoroutine);
            jumpCoroutine = null;
        }

        jumpCoroutine = StartCoroutine(JumpRoutine(skill, targetPosition, onLanded));
    }

    /// <summary>
    /// 점프 강제 중단 (스태거·사망 등)
    /// </summary>
    public void Cancel()
    {
        if (jumpCoroutine != null)
        {
            StopCoroutine(jumpCoroutine);
            jumpCoroutine = null;
        }

        ResumeNavMesh();
    }

    // ─────────────────────────────────────────
    // 내부 코루틴
    // ─────────────────────────────────────────

    private IEnumerator JumpRoutine(SkillData skill, Vector3 targetPosition, System.Action onLanded)
    {
        Vector3 startPos = transform.position;

        // Z축 고정 (2D 공간)
        Vector3 landPos = new Vector3(targetPosition.x, targetPosition.y, startPos.z);

        // 체공 시간 결정
        float duration = jumpDuration > 0f ? jumpDuration : skill.ActionDuration;

        // ① NavMesh 이동 중단 (공중 이동 중 경로 재계산 방지)
        PauseNavMesh();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // 수평 위치: 선형 보간
            Vector3 horizontalPos = Vector3.Lerp(startPos, landPos, t);

            // 수직 위치: sin 포물선 (t=0.5 에서 최고점)
            float heightOffset = jumpHeight * Mathf.Sin(t * Mathf.PI);

            transform.position = new Vector3(horizontalPos.x, horizontalPos.y + heightOffset, horizontalPos.z);

            yield return null;
        }

        // ② 착지 위치 확정
        transform.position = landPos;

        // ③ NavMesh 위치 동기화 & 재개
        ResumeNavMesh();

        // ④ 착지 카메라 진동
        ScreenShakeManager.Instance?.PlayShake(landingShakeData);

        // ⑤ 착지 VFX: skill 파라미터(코루틴 캡처, 항상 유효)로 직접 생성
        // SpawnAOEEffectAtCenter는 currentSkill에 의존하므로 점프 착지 타이밍에는 신뢰 불가
        if (skill.AoeEffect != null)
        {
            GameObject vfxObj = Instantiate(skill.AoeEffect, landPos, Quaternion.identity);
            Destroy(vfxObj, 2f);
        }

        // ⑥ 착지 콜백 → EliteSkillController.SpawnDamageArea()가 DamageArea 생성
        onLanded?.Invoke();

        jumpCoroutine = null;
    }

    // ─────────────────────────────────────────
    // NavMesh 유틸
    // ─────────────────────────────────────────

    private void PauseNavMesh()
    {
        if (baseEnemy == null || !baseEnemy.IsUsingNavMesh) return;
        baseEnemy.Agent.ResetPath();
        baseEnemy.Agent.isStopped = true;
    }

    private void ResumeNavMesh()
    {
        if (baseEnemy == null || !baseEnemy.IsUsingNavMesh) return;

        if (baseEnemy.Agent.enabled && baseEnemy.Agent.isOnNavMesh)
            baseEnemy.Agent.Warp(transform.position);

        baseEnemy.Agent.isStopped = false;
    }
}
