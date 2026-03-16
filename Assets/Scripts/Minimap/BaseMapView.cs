using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MinimapView(전체 미니맵)와 RadarMapView(레이더맵)의 공통 기반 추상 클래스.
///
/// [공통 로직 — 이 클래스에서 구현]
/// - MinimapManager 이벤트 구독/해제
/// - 마커 UI 인스턴스 생성/제거/동기화
/// - 타겟 마커 갱신 및 사각형 Edge Clamping
/// - 플레이어 마커 방향(회전) 갱신
/// - Anti-Popping을 위한 ForceRefresh
///
/// [뷰 특화 로직 — 서브클래스에서 구현 (abstract)]
/// - InitBackground()           : 배경 이미지 크기·위치 초기 설정
/// - UpdateBackground()         : 매 프레임 배경 이미지 갱신
/// - UpdatePlayerMarkerPosition(): 매 프레임 플레이어 마커 위치 갱신
/// - ComputeMarkerPosition()    : 월드 좌표 → 마스크 공간 좌표 변환
/// </summary>
public abstract class BaseMapView : MonoBehaviour
{
    // ── Inspector — 공통 UI 참조 ────────────────────────────────────
    [Header("UI 참조 (공통)")]
    [Tooltip("RectMask2D가 붙어 있는 마스크 RectTransform")]
    [SerializeField] protected RectTransform maskRect;

    [Tooltip("배경 이미지의 RectTransform (크기·위치 조작 대상)")]
    [SerializeField] protected RectTransform bgRect;

    [Tooltip("배경 이미지의 Image 컴포넌트 (스프라이트 교체 대상)")]
    [SerializeField] protected Image bgImage;

    [Tooltip("플레이어 마커 RectTransform (위치 또는 회전 적용)")]
    [SerializeField] protected RectTransform playerMarker;

    [Tooltip("타겟 마커 UI 인스턴스들의 부모 Transform")]
    [SerializeField] protected Transform markerRoot;

    // ── Inspector — 마커 프리팹 ──────────────────────────────────────
    [Header("마커 프리팹")]
    [SerializeField] private GameObject bossMarkerPrefab;
    [SerializeField] private GameObject enemyMarkerPrefab;
    [SerializeField] private GameObject questMarkerPrefab;
    [SerializeField] private GameObject exitMarkerPrefab;

    // ── Inspector — Edge Clamping ────────────────────────────────────
    [Header("Edge Clamping")]
    [Tooltip("마스크 내부 여백(px). 이 값만큼 안쪽에서 마커가 클램프된다.")]
    [SerializeField] protected float edgePadding = 8f;

    // ── 런타임 — 마커 인스턴스 맵 ────────────────────────────────────
    // Value: (RectTransform rt, Image img) 튜플로 UI 인스턴스를 관리
    private readonly Dictionary<MinimapMarker, (RectTransform rt, Image img)> _instances = new();

    // ────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ────────────────────────────────────────────────────────────────

    protected virtual void OnEnable()
    {
        if (MinimapManager.Instance == null) return;

        // 이벤트 구독
        MinimapManager.Instance.OnMarkerRegistered   += HandleMarkerRegistered;
        MinimapManager.Instance.OnMarkerUnregistered += HandleMarkerUnregistered;
        MinimapManager.Instance.OnDataChanged        += HandleDataChanged;

        // bgImage 머티리얼에 FogMask 텍스처 연결
        AssignFogMask();

        // 데이터가 이미 준비된 경우 즉시 초기화
        // 미준비 시 MinimapManager.Start()가 SetData()→OnDataChanged를 발행하면 HandleDataChanged가 처리
        if (MinimapManager.Instance.Data?.IsValid == true)
        {
            InitBackground();
            SyncMarkerInstances();
            ForceRefresh();
        }
    }

    protected virtual void OnDisable()
    {
        if (MinimapManager.Instance == null) return;

        MinimapManager.Instance.OnMarkerRegistered   -= HandleMarkerRegistered;
        MinimapManager.Instance.OnMarkerUnregistered -= HandleMarkerUnregistered;
        MinimapManager.Instance.OnDataChanged        -= HandleDataChanged;
    }

    /// <summary>
    /// 활성 상태일 때만 Unity가 호출 (비활성 패널은 자동으로 갱신되지 않음 → 모바일 최적화).
    /// </summary>
    protected virtual void LateUpdate()
    {
        if (MinimapManager.Instance?.Data?.IsValid != true) return;

        UpdateBackground();
        UpdatePlayerMarkerPosition();
        UpdatePlayerMarkerRotation();
        UpdateAllTargetMarkers();
    }

    // ────────────────────────────────────────────────────────────────
    //  Template Methods (서브클래스 구현 필수)
    // ────────────────────────────────────────────────────────────────

    /// <summary>배경 이미지의 크기(sizeDelta)·위치(anchoredPosition) 초기 설정.</summary>
    protected abstract void InitBackground();

    /// <summary>매 프레임 배경 이미지 위치 갱신. 고정형은 빈 구현, 스크롤형은 이동 로직 포함.</summary>
    protected abstract void UpdateBackground();

    /// <summary>매 프레임 플레이어 마커의 localPosition 갱신.</summary>
    protected abstract void UpdatePlayerMarkerPosition();

    /// <summary>월드 좌표를 이 뷰의 마스크 공간(중앙 = (0,0)) 좌표로 변환한다.</summary>
    protected abstract Vector2 ComputeMarkerPosition(Vector3 worldPos);

    // ────────────────────────────────────────────────────────────────
    //  Concrete — 플레이어 마커 회전 (공통)
    // ────────────────────────────────────────────────────────────────

    /// <summary>플레이어 이동 방향(FacingDirection)에 따라 마커를 회전시킨다.</summary>
    protected void UpdatePlayerMarkerRotation()
    {
        if (playerMarker == null || MinimapManager.Instance?.PlayerController == null) return;

        Vector2 dir = MinimapManager.Instance.PlayerController.FacingDirection;
        if (dir.sqrMagnitude > 0.01f)
        {
            // angle 0° = 오른쪽, -90° 보정으로 0° = 위쪽(북쪽)
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            playerMarker.localEulerAngles = new Vector3(0f, 0f, angle);
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  Concrete — Anti-Popping (공통)
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 패널이 켜지는 순간 즉시 1회 전체 갱신하여 UI 팝핑 현상을 방지한다.
    /// OnEnable 마지막에 호출되며, 서브클래스에서도 필요 시 호출 가능하다.
    /// </summary>
    protected void ForceRefresh()
    {
        if (MinimapManager.Instance?.Data?.IsValid != true) return;

        UpdateBackground();
        UpdatePlayerMarkerPosition();
        UpdatePlayerMarkerRotation();
        UpdateAllTargetMarkers();
    }

    // ────────────────────────────────────────────────────────────────
    //  Concrete — 타겟 마커 갱신 (공통)
    // ────────────────────────────────────────────────────────────────

    private void UpdateAllTargetMarkers()
    {
        foreach (var kvp in _instances)
        {
            if (kvp.Key == null || kvp.Value.rt == null) continue;

            Vector2 mapPos = ComputeMarkerPosition(kvp.Key.transform.position);
            UpdateSingleMarker(kvp.Key, kvp.Value.rt, kvp.Value.img, mapPos);
        }
    }

    private void UpdateSingleMarker(MinimapMarker source, RectTransform rt, Image img, Vector2 mapPos)
    {
        float halfW = maskRect.rect.width  * 0.5f - edgePadding;
        float halfH = maskRect.rect.height * 0.5f - edgePadding;

        bool outside = Mathf.Abs(mapPos.x) > halfW || Mathf.Abs(mapPos.y) > halfH;

        if (outside)
        {
            source.isOutOfBounds = true;

            // Enemy: 반경 밖이면 마커를 완전히 숨김
            if (source.markerType == MinimapMarkerType.Enemy)
            {
                if (img != null) img.enabled = false;
                return;
            }

            // Boss / Quest / Exit: 테두리에 고정하고 방향 표시 (Edge Clamping 유지)
            float scale = Mathf.Min(
                halfW / Mathf.Max(Mathf.Abs(mapPos.x), 0.001f),
                halfH / Mathf.Max(Mathf.Abs(mapPos.y), 0.001f));

            rt.localPosition    = mapPos * scale;

            float angle = Mathf.Atan2(mapPos.y, mapPos.x) * Mathf.Rad2Deg - 90f;
            rt.localEulerAngles = new Vector3(0f, 0f, angle);

            if (img != null)
            {
                img.enabled = true;
                if (source.arrowSprite != null)
                    img.sprite = source.arrowSprite;
            }
        }
        else
        {
            source.isOutOfBounds = false;

            rt.localPosition    = mapPos;
            rt.localEulerAngles = Vector3.zero;

            if (img != null)
            {
                img.enabled = true;
                if (source.defaultSprite != null)
                    img.sprite = source.defaultSprite;
            }
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  Concrete — 마커 인스턴스 관리 (공통)
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 비활성 상태 중 등록/해제된 마커를 처리하여 _instances를 최신 상태로 동기화.
    /// OnEnable 시 호출된다.
    /// </summary>
    private void SyncMarkerInstances()
    {
        if (MinimapManager.Instance == null) return;

        var currentMarkers = MinimapManager.Instance.Markers;

        // 현재 등록된 마커 중 인스턴스가 없는 것 추가
        foreach (var marker in currentMarkers)
        {
            if (marker != null && !_instances.ContainsKey(marker))
                SpawnMarker(marker);
        }

        // 제거됐거나 null이 된 마커 정리 (패널 비활성 중에 Unregister 이벤트를 놓친 경우)
        var currentSet = new HashSet<MinimapMarker>(currentMarkers);
        var toRemove   = new List<MinimapMarker>();

        foreach (var key in _instances.Keys)
        {
            if (key == null || !currentSet.Contains(key))
                toRemove.Add(key);
        }

        foreach (var key in toRemove)
            RemoveMarker(key);
    }

    private void SpawnMarker(MinimapMarker source)
    {
        if (_instances.ContainsKey(source)) return;

        var prefab = GetPrefabForType(source.markerType);
        if (prefab == null)
        {
            Debug.LogWarning($"[{GetType().Name}] '{source.markerType}' 마커 프리팹이 연결되지 않았습니다.");
            return;
        }

        var go  = Instantiate(prefab, markerRoot);
        var rt  = go.GetComponent<RectTransform>();
        var img = go.GetComponent<Image>();

        if (img != null)
        {
            if (source.defaultSprite != null) img.sprite = source.defaultSprite;
            img.color = source.markerColor;
        }

        _instances[source] = (rt, img);
    }

    private void RemoveMarker(MinimapMarker source)
    {
        if (!_instances.TryGetValue(source, out var inst)) return;
        if (inst.rt != null) Destroy(inst.rt.gameObject);
        _instances.Remove(source);
    }

    // ────────────────────────────────────────────────────────────────
    //  이벤트 핸들러
    // ────────────────────────────────────────────────────────────────

    private void HandleMarkerRegistered(MinimapMarker m)   => SpawnMarker(m);
    private void HandleMarkerUnregistered(MinimapMarker m) => RemoveMarker(m);

    private void HandleDataChanged(MinimapData data)
    {
        AssignFogMask(); // 씬 전환으로 FogTexture가 리셋된 경우 재연결
        InitBackground();
        SyncMarkerInstances();
        ForceRefresh();
    }

    /// <summary>
    /// bgImage 머티리얼의 _FogMask 파라미터에 MinimapManager.FogTexture를 연결한다.
    /// bgImage에는 UI/MapReveal 셰이더를 사용하는 머티리얼이 인스펙터에서 연결되어 있어야 한다.
    /// </summary>
    private void AssignFogMask()
    {
        if (bgImage == null || bgImage.material == null) return;
        var fog = MinimapManager.Instance?.FogTexture;
        if (fog != null) bgImage.material.SetTexture("_FogMask", fog);
    }

    // ────────────────────────────────────────────────────────────────
    //  유틸
    // ────────────────────────────────────────────────────────────────

    private GameObject GetPrefabForType(MinimapMarkerType type) => type switch
    {
        MinimapMarkerType.Boss  => bossMarkerPrefab,
        MinimapMarkerType.Enemy => enemyMarkerPrefab,
        MinimapMarkerType.Quest => questMarkerPrefab,
        MinimapMarkerType.Exit  => exitMarkerPrefab,
        _                       => null
    };

#if UNITY_EDITOR
    protected virtual void OnDrawGizmosSelected()
    {
        var data = MinimapManager.Instance?.Data;
        if (data == null) return;

        Gizmos.color = Color.cyan;
        var center = new Vector3(data.WorldCenter.x, data.WorldCenter.y, 0f);
        var size   = new Vector3(data.WorldSize.x,   data.WorldSize.y,   0f);
        Gizmos.DrawWireCube(center, size);
    }
#endif
}
