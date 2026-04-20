using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스 메테오 샤워 스킬 패턴
/// 여러 위치에 텔레그래프를 동시 표시 후 시간차로 메테오가 낙하하는 AOE 패턴
/// BossSkillController의 단일 텔레그래프 흐름과 독립적으로 동작
/// </summary>
public class BossMeteorShowerSkill : MonoBehaviour
{
    [Header("⚙️ 참조")]
    [SerializeField] private BaseEnemy baseEnemy;

    [Header("☄️ 메테오 설정")]
    [Tooltip("생성할 메테오 최대 개수")]
    [SerializeField] private int meteorCount = 6;

    [Tooltip("메테오 간 착탄 간격 (초)")]
    [SerializeField] private float meteorDelay = 0.4f;

    [Tooltip("플레이어 근처 배치 반경 (전체의 70%)")]
    [SerializeField] private float playerNearRadius = 7f;

    [Tooltip("아레나 전체 랜덤 배치 반경 (전체의 30%)")]
    [SerializeField] private float arenaRadius = 16f;

    [Tooltip("메테오 간 최소 이격 거리 (겹침 방지)")]
    [SerializeField] private float minSpacingBetweenMeteors = 2.5f;

    [Tooltip("위치 생성 재시도 횟수 (이격 거리 실패 시)")]
    [SerializeField] private int positionRetryCount = 10;

    [Header("🎬 연출 설정")]
    [Tooltip("낙하 연출 지속 시간 (VFX 표시 후 착탄까지)")]
    [SerializeField] private float meteorFallDuration = 0.3f;

    [Tooltip("착탄 직전 텔레그래프 깜빡임 시작 시간 (착탄 전 N초)")]
    [SerializeField] private float warningBlinkDuration = 0.25f;

    [Tooltip("낙하 VFX 프리팹 (하늘에서 내려오는 연출)")]
    [SerializeField] private GameObject meteorFallVFXPrefab;

    [Tooltip("착탄 VFX 프리팹 (폭발 연출)")]
    [SerializeField] private GameObject meteorImpactVFXPrefab;

    // 취소 관리용 캐시
    private readonly List<GameObject> activeTelegraphs = new List<GameObject>();
    private readonly List<Coroutine> activeCoroutines = new List<Coroutine>();
    private readonly List<GameObject> activeVFXs = new List<GameObject>();

    // 위치 캐시 (PreparePositions → ExecuteMeteorShower 간 공유)
    private List<Vector3> cachedMeteorPositions = new List<Vector3>();

    private BossSkillController skillController;

    private void Awake()
    {
        if (baseEnemy == null)
            baseEnemy = GetComponent<BaseEnemy>();

        skillController = GetComponent<BossSkillController>();
    }

    // ───────────────────────────────────────────────────────
    // Public API (BossSkillController에서 호출)
    // ───────────────────────────────────────────────────────

    /// <summary>
    /// 메테오 착탄 위치를 미리 생성하고 캐싱
    /// BossSkillController.OnSkillCastStart()에서 텔레그래프 스폰 전에 호출
    /// </summary>
    public void PreparePositions(Vector3 playerPosition, BossSkillEntry skillEntry)
    {
        cachedMeteorPositions = GenerateMeteorPositions(playerPosition, meteorCount);
    }

    /// <summary>
    /// 모든 텔레그래프를 동시에 스폰 (캐스팅 시간 동안 플레이어에게 경고 표시)
    /// </summary>
    public void SpawnAllTelegraphs(BossSkillEntry skillEntry)
    {
        if (skillEntry?.skillData == null) return;
        if (cachedMeteorPositions == null || cachedMeteorPositions.Count == 0)
        {
            Dbg.LogWarning("[BossMeteorShowerSkill] 텔레그래프 스폰 전 PreparePositions() 호출 필요");
            return;
        }

        ClearTelegraphs();

        SkillData skillData = skillEntry.skillData;
        float scaleMultiplier = skillEntry.skillScaleMultiplier;
        float duration = skillData.CastTime + 30f; // 수동 제거 전 자동 파괴 안전장치

        foreach (Vector3 pos in cachedMeteorPositions)
        {
            if (skillData.TelegraphPrefab == null) continue;

            GameObject telegraphGO = Instantiate(skillData.TelegraphPrefab, pos, Quaternion.identity);
            activeTelegraphs.Add(telegraphGO);

            var indicator = telegraphGO.GetComponent<TelegraphIndicator>();
            if (indicator != null)
            {
                indicator.Initialize(skillData, duration, scaleMultiplier);
            }
            else
            {
                var indicatorMesh = telegraphGO.GetComponent<TelegraphIndicatorMesh>();
                if (indicatorMesh != null)
                    indicatorMesh.Initialize(skillData, duration, scaleMultiplier);
            }
        }
    }

    /// <summary>
    /// 메테오 샤워 실행 (시간차 낙하 + DamageArea 판정)
    /// BossSkillController.ExecuteSkillDamage()에서 호출
    /// </summary>
    /// <param name="skillEntry">스킬 엔트리</param>
    /// <param name="onComplete">모든 메테오 완료 콜백</param>
    public void ExecuteMeteorShower(BossSkillEntry skillEntry, System.Action onComplete)
    {
        if (skillEntry?.skillData == null)
        {
            onComplete?.Invoke();
            return;
        }

        if (cachedMeteorPositions == null || cachedMeteorPositions.Count == 0)
        {
            Dbg.LogWarning("[BossMeteorShowerSkill] 실행할 메테오 위치가 없습니다");
            onComplete?.Invoke();
            return;
        }

        Coroutine c = StartCoroutine(MeteorShowerRoutine(skillEntry, onComplete));
        activeCoroutines.Add(c);
    }

    /// <summary>
    /// 스킬 강제 취소 (보스 사망 / ForceCancelSkill 시 호출)
    /// </summary>
    public void Cancel()
    {
        // 실행 중인 코루틴 전부 중단
        foreach (Coroutine c in activeCoroutines)
        {
            if (c != null) StopCoroutine(c);
        }
        activeCoroutines.Clear();

        // 텔레그래프 즉시 제거
        ClearTelegraphs();

        // 낙하/착탄 VFX 즉시 제거
        foreach (GameObject vfx in activeVFXs)
        {
            if (vfx != null) Destroy(vfx);
        }
        activeVFXs.Clear();

        cachedMeteorPositions.Clear();
    }

    // ───────────────────────────────────────────────────────
    // 내부 코루틴
    // ───────────────────────────────────────────────────────

    /// <summary>
    /// 메테오 샤워 전체 시퀀스 코루틴
    /// </summary>
    private IEnumerator MeteorShowerRoutine(BossSkillEntry skillEntry, System.Action onComplete)
    {
        int count = cachedMeteorPositions.Count;

        // 각 메테오를 시간차로 시작 (SingleMeteorCoroutine은 내부적으로 meteorDelay * index 대기)
        List<Coroutine> meteorCoroutines = new List<Coroutine>();
        for (int i = 0; i < count; i++)
        {
            int capturedIndex = i;
            Coroutine mc = StartCoroutine(SingleMeteorCoroutine(skillEntry, capturedIndex));
            meteorCoroutines.Add(mc);
        }

        // 모든 메테오 코루틴 완료 대기
        foreach (Coroutine mc in meteorCoroutines)
        {
            yield return mc;
        }

        // 캐시 정리
        cachedMeteorPositions.Clear();

        // 완료 콜백 (OnSkillActionComplete 연결)
        onComplete?.Invoke();

        // 이 코루틴 자신을 activeCoroutines에서 제거
        // (Cancel()이 호출되지 않은 정상 완료 경로)
        activeCoroutines.RemoveAll(c => c == null);
    }

    /// <summary>
    /// 단일 메테오 처리 코루틴
    /// </summary>
    private IEnumerator SingleMeteorCoroutine(BossSkillEntry skillEntry, int index)
    {
        // 시간차 대기
        yield return new WaitForSeconds(meteorDelay * index);

        // 이미 Cancel됐으면 중단
        if (cachedMeteorPositions == null || index >= cachedMeteorPositions.Count)
            yield break;

        Vector3 targetPos = cachedMeteorPositions[index];

        // 텔레그래프 깜빡임 시작 (착탄 경고)
        if (index < activeTelegraphs.Count && activeTelegraphs[index] != null)
        {
            Coroutine blinkC = StartCoroutine(BlinkTelegraph(activeTelegraphs[index]));
            activeCoroutines.Add(blinkC);
        }

        // 낙하 VFX 스폰 (하늘에서 떨어지는 연출)
        if (meteorFallVFXPrefab != null)
        {
            Vector3 fallStartPos = targetPos + new Vector3(0f, 3f, 0f);
            GameObject fallVFX = Instantiate(meteorFallVFXPrefab, fallStartPos, Quaternion.identity);
            activeVFXs.Add(fallVFX);
            Destroy(fallVFX, meteorFallDuration + 0.5f);
        }

        // 낙하 연출 대기
        yield return new WaitForSeconds(meteorFallDuration);

        // 텔레그래프 제거
        if (index < activeTelegraphs.Count && activeTelegraphs[index] != null)
        {
            Destroy(activeTelegraphs[index]);
            activeTelegraphs[index] = null;
        }

        // 착탄 카메라 진동
        ScreenShakeManager.Instance?.PlayShake(skillEntry.skillData.ImpactShake);

        // 착탄 VFX 스폰
        if (meteorImpactVFXPrefab != null)
        {
            GameObject impactVFX = Instantiate(meteorImpactVFXPrefab, targetPos, Quaternion.identity);
            activeVFXs.Add(impactVFX);
            Destroy(impactVFX, 2f);
        }

        // DamageArea 생성 (실제 피해 판정)
        SpawnMeteorDamageArea(skillEntry, targetPos);
    }

    /// <summary>
    /// 텔레그래프 깜빡임 코루틴 (착탄 직전 경고)
    /// </summary>
    private IEnumerator BlinkTelegraph(GameObject telegraphGO)
    {
        if (telegraphGO == null) yield break;

        SpriteRenderer sr = telegraphGO.GetComponent<SpriteRenderer>();
        if (sr == null) yield break;

        float elapsed = 0f;
        float blinkInterval = 0.06f;
        Color originalColor = sr.color;
        Color warningColor = new Color(1f, 0.3f, 0f, originalColor.a); // 주황빛으로 변경

        while (elapsed < warningBlinkDuration && telegraphGO != null)
        {
            sr.color = warningColor;
            yield return new WaitForSeconds(blinkInterval);

            if (telegraphGO == null) yield break;

            sr.color = originalColor;
            yield return new WaitForSeconds(blinkInterval);

            elapsed += blinkInterval * 2f;
        }

        if (telegraphGO != null && sr != null)
            sr.color = originalColor;
    }

    // ───────────────────────────────────────────────────────
    // 위치 생성
    // ───────────────────────────────────────────────────────

    /// <summary>
    /// 메테오 착탄 위치 목록 생성 (C안: 70% 플레이어 근처 + 30% 전체 랜덤)
    /// </summary>
    private List<Vector3> GenerateMeteorPositions(Vector3 playerPos, int count)
    {
        List<Vector3> positions = new List<Vector3>();
        int nearCount = Mathf.Max(1, Mathf.RoundToInt(count * 0.7f));
        int farCount = count - nearCount;

        // 70%: 플레이어 근처
        for (int i = 0; i < nearCount; i++)
        {
            Vector3 pos = GetValidPosition(playerPos, playerNearRadius, positions);
            positions.Add(pos);
        }

        // 30%: 아레나 전체 랜덤 (보스 중심 기준)
        for (int i = 0; i < farCount; i++)
        {
            Vector3 pos = GetValidPosition(transform.position, arenaRadius, positions);
            positions.Add(pos);
        }

        return positions;
    }

    /// <summary>
    /// 최소 이격 거리를 만족하는 유효한 위치 생성
    /// 재시도 횟수 초과 시 마지막으로 생성된 위치 반환
    /// </summary>
    private Vector3 GetValidPosition(Vector3 center, float radius, List<Vector3> existingPositions)
    {
        Vector3 candidate = GetRandomPositionInRadius(center, radius);

        for (int attempt = 0; attempt < positionRetryCount; attempt++)
        {
            if (!IsTooClose(existingPositions, candidate, minSpacingBetweenMeteors))
                return candidate;

            candidate = GetRandomPositionInRadius(center, radius);
        }

        // 재시도 초과: 마지막 후보 그대로 사용 (완벽한 배치보다 진행 우선)
        return candidate;
    }

    /// <summary>
    /// 원형 범위 내 랜덤 위치 (균일 분포)
    /// </summary>
    private Vector3 GetRandomPositionInRadius(Vector3 center, float radius)
    {
        // 균일 분포를 위해 sqrt 보정
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float dist = Mathf.Sqrt(Random.Range(0f, 1f)) * radius;

        return new Vector3(
            center.x + Mathf.Cos(angle) * dist,
            center.y + Mathf.Sin(angle) * dist,
            center.z
        );
    }

    /// <summary>
    /// 기존 위치 목록 중 하나라도 최소 이격 거리 미만이면 true
    /// </summary>
    private bool IsTooClose(List<Vector3> positions, Vector3 candidate, float minDistance)
    {
        foreach (Vector3 pos in positions)
        {
            if (Vector3.Distance(pos, candidate) < minDistance)
                return true;
        }
        return false;
    }

    // ───────────────────────────────────────────────────────
    // DamageArea 생성
    // ───────────────────────────────────────────────────────

    /// <summary>
    /// 지정 위치에 메테오 DamageArea 생성
    /// </summary>
    private void SpawnMeteorDamageArea(BossSkillEntry skillEntry, Vector3 worldPosition)
    {
        if (skillEntry?.skillData == null) return;

        GameObject damageAreaPrefab = Resources.Load<GameObject>("Prefabs/VFX/DamageArea");
        if (damageAreaPrefab == null)
        {
            Debug.LogError("[BossMeteorShowerSkill] DamageArea 프리팹을 찾을 수 없습니다! (Prefabs/VFX/DamageArea)");
            return;
        }

        GameObject damageAreaGO = Instantiate(damageAreaPrefab);
        DamageArea damageArea = damageAreaGO.GetComponent<DamageArea>();

        if (damageArea == null)
        {
            Debug.LogError("[BossMeteorShowerSkill] DamageArea 컴포넌트 없음");
            Destroy(damageAreaGO);
            return;
        }

        damageArea.Initialize(
            skillData: skillEntry.skillData,
            skillEntry: skillEntry,
            origin: worldPosition,
            forward: Vector3.down, // 메테오는 방향성 없음 (원형 AOE)
            enemy: baseEnemy,
            policy: skillEntry.skillData.AoeDamagePolicy
        );

        // AOE 이펙트 (SkillData.AoeEffect 있으면 착탄 위치에 생성)
        if (skillEntry.skillData.AoeEffect != null)
        {
            GameObject aoeEffect = Instantiate(skillEntry.skillData.AoeEffect, worldPosition, Quaternion.identity);
            activeVFXs.Add(aoeEffect);
            Destroy(aoeEffect, 2f);
        }

        float destroyDelay = skillEntry.skillData.AoeDuration + 0.5f;
        Destroy(damageAreaGO, destroyDelay);
    }

    // ───────────────────────────────────────────────────────
    // 내부 정리 유틸리티
    // ───────────────────────────────────────────────────────

    private void ClearTelegraphs()
    {
        foreach (GameObject t in activeTelegraphs)
        {
            if (t != null) Destroy(t);
        }
        activeTelegraphs.Clear();
    }
}
