using UnityEngine;

/// <summary>
/// 분열형 SimpleMob
/// - 사망 시 자식 몬스터를 주변에 스폰하고 풀로 반환
/// - canSplit = false이면 일반 사망 (자식 미스폰)
///
/// [프리팹 구성 가이드]
/// - 권장: 원본 SimpleMobSplitter 프리팹 (childPoolTag = 자식 프리팹 풀 태그)
///         자식 SimpleMob 프리팹         (SimpleMobSplitter 미포함 → 자동으로 분열 없음)
/// - 대안: 같은 프리팹 사용 시 canSplit 플래그로 제어
///         (OnEnable에서 true로 리셋 → WaveSpawner 스폰 = 원본, 자식 스폰 = false 세팅)
/// </summary>
public class SimpleMobSplitter : SimpleMob
{
    [Header("분열 설정")]
    [Tooltip("true: 사망 시 분열 / false: 일반 사망\n" +
             "자식 스폰 시 자동으로 false가 세팅되어 무한 분열 방지")]
    [SerializeField] private bool canSplit = true;

    // ─────────────────────────────────────────────
    // 풀 재사용 시 초기화 (OnEnable 오버라이드)
    // ─────────────────────────────────────────────

    protected override void OnEnable()
    {
        base.OnEnable();
        // 풀에서 꺼낼 때마다 분열 가능 상태로 리셋
        // → WaveSpawner가 스폰한 인스턴스는 항상 "원본"으로 동작
        canSplit = true;
    }

    // ─────────────────────────────────────────────
    // 외부에서 분열 가능 여부 설정 (자식 스폰 직후 호출)
    // ─────────────────────────────────────────────

    /// <summary>
    /// 분열 가능 여부 설정.
    /// 부모 Die()에서 자식 스폰 후 즉시 false로 설정해 무한 분열 방지.
    /// </summary>
    public void SetCanSplit(bool value)
    {
        canSplit = value;
    }

    // ─────────────────────────────────────────────
    // 사망 처리
    // ─────────────────────────────────────────────

    protected override void Die()
    {
        if (isDead) return;

        // canSplit = true 이고 mobData 유효할 때만 분열
        if (canSplit
            && mobData != null
            && !string.IsNullOrEmpty(mobData.childPoolTag)
            && GamePoolManager.Instance != null)
        {
            for (int i = 0; i < mobData.childCount; i++)
            {
                Vector2 offset   = Random.insideUnitCircle * mobData.childSpawnRadius;
                Vector3 spawnPos = transform.position + new Vector3(offset.x, offset.y, 0f);

                GameObject child = GamePoolManager.Instance.SpawnFromPool(
                    mobData.childPoolTag, spawnPos, Quaternion.identity);

                if (child == null)
                {
                    if (enableDebugLogs)
                        Debug.LogWarning($"[SimpleMobSplitter] 자식 스폰 실패 — 풀 태그 '{mobData.childPoolTag}'");
                    continue;
                }

                // 같은 프리팹(SimpleMobSplitter)을 자식으로 사용하는 경우:
                // 자식은 분열 불가능하게 설정 → 무한 분열 방지
                SimpleMobSplitter childSplitter = child.GetComponent<SimpleMobSplitter>();
                if (childSplitter != null)
                    childSplitter.SetCanSplit(false);
            }

            if (enableDebugLogs)
                Debug.Log($"[SimpleMobSplitter] {gameObject.name} 분열 — {mobData.childCount}마리 스폰");
        }
        else if (!canSplit && enableDebugLogs)
        {
            Debug.Log($"[SimpleMobSplitter] {gameObject.name} 분열 스킵 (canSplit = false)");
        }

        // 기반 클래스에 나머지 사망 처리 위임 (애니메이션 + 풀 반환)
        base.Die();
    }
}
