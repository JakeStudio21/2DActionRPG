using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 미니맵 시스템의 데이터 허브 (Singleton).
///
/// [역할]
/// - 씬별 MinimapData(월드 바운드 + 맵 스프라이트) 관리
/// - 플레이어 Transform / PlayerController 참조 보관
/// - 등록된 MinimapMarker 목록 유지 및 이벤트 발행
///
/// [원칙] UI를 직접 조작하지 않는다. View들이 이 클래스의 데이터를 읽어 렌더링한다.
///
/// [설정]
/// - _data 필드를 Inspector에서 연결하면 수동 지정, null이면 씬 이름으로 자동 로드.
/// - 플레이어가 런타임에 스폰되면 SetPlayer()로 주입할 수 있다.
/// </summary>
[DefaultExecutionOrder(-100)] // View들보다 먼저 Awake/Start 실행 보장
public class MinimapManager : MonoBehaviour
{
    public static MinimapManager Instance { get; private set; }

    // ── Inspector ────────────────────────────────────────────────────
    [Header("데이터 설정")]
    [Tooltip("직접 연결하면 해당 데이터 사용. 비워두면 씬 이름으로 Resources/Minimaps/ 자동 로드.\n" +
             "※ 이 필드는 수동 지정용. 실제 로드된 데이터는 런타임 Data 프로퍼티에 저장됨.")]
    [SerializeField] private MinimapData _data;

    // ── 공개 프로퍼티 ─────────────────────────────────────────────────
    public MinimapData      Data             { get; private set; }
    public Transform        PlayerTransform  { get; private set; }
    public PlayerController PlayerController { get; private set; }

    public IReadOnlyList<MinimapMarker> Markers => _markers;
    private readonly List<MinimapMarker> _markers = new();

    // ── 이벤트 ───────────────────────────────────────────────────────
    public event Action<MinimapMarker> OnMarkerRegistered;
    public event Action<MinimapMarker> OnMarkerUnregistered;
    public event Action<MinimapData>   OnDataChanged;

    // ── Inspector — 안개(Fog of War) 설정 ────────────────────────────
    [Header("안개(Fog of War) 설정")]
    [Tooltip("안개 텍스처 해상도 (정사각형). 512 권장.")]
    [SerializeField] private int fogResolution = 512;

    [Tooltip("플레이어 주변 시야 반경 (월드 단위). 비율 보정으로 항상 정원(circle)을 유지.")]
    [SerializeField] private float fogRevealWorldRadius = 5f;

    [Tooltip("안개 갱신 최소 이동 거리 (월드 단위). 이 값보다 적게 움직이면 갱신 생략.")]
    [SerializeField] private float fogUpdateThreshold = 0.3f;

    // ── 공개 프로퍼티 — 안개 텍스처 (View에서 RawImage.texture로 사용) ─
    public RenderTexture FogTexture { get; private set; }

    // ── 내부 — 안개 렌더링 ───────────────────────────────────────────
    private Texture2D _softBrush;
    private Material  _fogEraserMat;
    private Vector2   _lastRevealPos = Vector2.positiveInfinity;

    // ────────────────────────────────────────────────────────────────
    //  초기화
    // ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        InitFogSystem();
    }

    private void LateUpdate()
    {
        UpdateFog();
    }

    private void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        MinimapData initialData;
        if (_data != null)
        {
            // Inspector에 직접 연결된 경우
            initialData = _data;
            Debug.Log($"[MinimapManager] Inspector에서 MinimapData 사용: {_data.name}");
        }
        else
        {
            // 씬 이름으로 Resources/Minimaps/ 자동 로드
            string resourcePath = $"Minimaps/{sceneName}_MinimapData";
            initialData = Resources.Load<MinimapData>(resourcePath);

            if (initialData != null)
                Debug.Log($"[MinimapManager] 자동 로드 성공: Resources/{resourcePath}");
            else
                Debug.LogWarning($"[MinimapManager] 자동 로드 실패. 경로: Assets/Resources/{resourcePath}.asset 에 파일이 있는지 확인하세요.\n" +
                                 $"또는 Inspector의 Data 필드에 직접 연결하세요.");
        }

        SetData(initialData);
        FindAndSetPlayer();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        if (FogTexture != null)
        {
            FogTexture.Release();
            FogTexture = null;
        }
        if (_fogEraserMat != null) Destroy(_fogEraserMat);
        if (_softBrush    != null) Destroy(_softBrush);
    }

    // ────────────────────────────────────────────────────────────────
    //  씬 전환 처리
    // ────────────────────────────────────────────────────────────────

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Additive 로드는 메인 씬 갱신 불필요
        if (mode == LoadSceneMode.Additive) return;

        var newData = Resources.Load<MinimapData>($"Minimaps/{scene.name}_MinimapData");
        if (newData != null) SetData(newData);

        FindAndSetPlayer();
    }

    // ────────────────────────────────────────────────────────────────
    //  공개 API
    // ────────────────────────────────────────────────────────────────

    /// <summary>MinimapMarker가 OnEnable 시 자동 호출한다.</summary>
    public void Register(MinimapMarker marker)
    {
        if (marker == null || _markers.Contains(marker)) return;
        _markers.Add(marker);
        OnMarkerRegistered?.Invoke(marker);
    }

    /// <summary>MinimapMarker가 OnDisable 시 자동 호출한다.</summary>
    public void Unregister(MinimapMarker marker)
    {
        if (!_markers.Remove(marker)) return;
        OnMarkerUnregistered?.Invoke(marker);
    }

    /// <summary>
    /// MinimapData를 교체하고 OnDataChanged를 발행한다.
    /// View들이 이 이벤트를 받아 배경 이미지와 바운드를 재설정한다.
    /// </summary>
    public void SetData(MinimapData data)
    {
        if (data == null)
        {
            Debug.LogWarning($"[MinimapManager] MinimapData 없음. 씬: {SceneManager.GetActiveScene().name}" +
                             "\nAssets/Resources/Minimaps/ 경로에 에셋이 있는지 확인하세요.");
            return;
        }
        Data = data;
        ResetFog();                    // 씬/데이터 교체 시 안개 초기화
        OnDataChanged?.Invoke(data);
    }

    /// <summary>
    /// 플레이어가 런타임에 스폰되는 씬에서 PlayerSpawner 등이 직접 주입할 때 사용한다.
    /// </summary>
    public void SetPlayer(Transform playerTransform, PlayerController playerController)
    {
        PlayerTransform  = playerTransform;
        PlayerController = playerController;
    }

    // ────────────────────────────────────────────────────────────────
    //  안개(Fog of War) 시스템
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// FogTexture, 소프트 브러시, 지우개 머티리얼을 초기화한다.
    /// Awake에서 1회 호출.
    /// </summary>
    private void InitFogSystem()
    {
        // RenderTexture 생성
        FogTexture = new RenderTexture(fogResolution, fogResolution, 0, RenderTextureFormat.ARGB32);
        FogTexture.filterMode = FilterMode.Bilinear;
        FogTexture.wrapMode   = TextureWrapMode.Clamp;
        FogTexture.Create();

        // 소프트 원형 브러시 텍스처 코드 생성 (에셋 파일 불필요)
        _softBrush = BuildSoftBrush(128);

        // FogEraser 셰이더 기반 머티리얼 생성
        var shader = Shader.Find("Minimap/FogEraser");
        if (shader == null)
        {
            Debug.LogError("[MinimapManager] 'Minimap/FogEraser' 셰이더를 찾을 수 없습니다.\n" +
                           "Assets/Shaders/FogEraser.shader 파일이 있는지 확인하세요.");
            return;
        }
        _fogEraserMat             = new Material(shader);
        _fogEraserMat.mainTexture = _softBrush;

        ResetFog();
    }

    /// <summary>
    /// FogTexture 전체를 불투명 검정(RGBA 0,0,0,1)으로 초기화한다.
    /// SetData() 및 씬 전환 시 자동 호출.
    /// </summary>
    private void ResetFog()
    {
        if (FogTexture == null) return;

        var prev = RenderTexture.active;
        RenderTexture.active = FogTexture;
        GL.Clear(true, true, new Color(0f, 0f, 0f, 1f));
        RenderTexture.active = prev;

        _lastRevealPos = Vector2.positiveInfinity; // 첫 이동 시 즉시 갱신 보장
    }

    /// <summary>
    /// 플레이어 이동 거리가 fogUpdateThreshold 이상일 때만 안개를 걷어낸다 (성능 최적화).
    /// 브러시 UV 크기에 월드 Aspect Ratio 역산을 적용하여 항상 정원(circle)을 유지한다.
    /// </summary>
    private void UpdateFog()
    {
        if (FogTexture == null || _fogEraserMat == null) return;
        if (Data == null || !Data.IsValid)              return;
        if (PlayerTransform == null)                    return;

        Vector2 playerWorld = PlayerTransform.position;

        // 최소 이동 거리 미만이면 갱신 생략
        if (Vector2.SqrMagnitude(playerWorld - _lastRevealPos) <
            fogUpdateThreshold * fogUpdateThreshold) return;

        _lastRevealPos = playerWorld;

        // 월드 → UV 좌표 (0~1)
        float uvX = Mathf.InverseLerp(Data.worldMin.x, Data.worldMax.x, playerWorld.x);
        float uvY = Mathf.InverseLerp(Data.worldMin.y, Data.worldMax.y, playerWorld.y);

        // ★ Aspect Ratio 비율 보정 ★
        //   FogTexture는 정사각형(fogResolution × fogResolution)이지만
        //   월드가 직사각형이면 단순 UV 매핑 시 원이 타원으로 왜곡됨.
        //   revealUvX/Y를 월드 각 축의 크기에 반비례하여 계산하면
        //   어떤 비율의 월드에서도 브러시가 정원으로 유지된다.
        float revealUvX = fogRevealWorldRadius / Mathf.Max(Data.WorldSize.x, 0.001f);
        float revealUvY = fogRevealWorldRadius / Mathf.Max(Data.WorldSize.y, 0.001f);

        var prev = RenderTexture.active;
        RenderTexture.active = FogTexture;

        GL.PushMatrix();
        GL.LoadOrtho(); // 0~1 직교 투영. Unity가 플랫폼 Y축 차이를 내부 처리하므로 추가 반전 불필요.

        _fogEraserMat.SetPass(0);

        GL.Begin(GL.QUADS);
        GL.TexCoord2(0f, 0f); GL.Vertex3(uvX - revealUvX, uvY - revealUvY, 0f);
        GL.TexCoord2(1f, 0f); GL.Vertex3(uvX + revealUvX, uvY - revealUvY, 0f);
        GL.TexCoord2(1f, 1f); GL.Vertex3(uvX + revealUvX, uvY + revealUvY, 0f);
        GL.TexCoord2(0f, 1f); GL.Vertex3(uvX - revealUvX, uvY + revealUvY, 0f);
        GL.End();

        GL.PopMatrix();

        RenderTexture.active = prev;
    }

    /// <summary>
    /// 소프트 원형 그라디언트 브러시 텍스처를 코드로 생성한다.
    /// 중심 alpha=1, 가장자리 alpha=0 (SmoothStep 감쇠).
    /// </summary>
    private static Texture2D BuildSoftBrush(int size)
    {
        var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode   = TextureWrapMode.Clamp;

        var pixels = new Color32[size * size];
        float half = size * 0.5f;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx   = (x - half) / half; // -1 ~ 1
            float dy   = (y - half) / half; // -1 ~ 1
            float dist = Mathf.Sqrt(dx * dx + dy * dy); // 0(중심) ~ √2(모서리)

            // dist=0 → alpha=1, dist=1 → alpha=0, 그 이상 → 0
            float alpha = Mathf.SmoothStep(1f, 0f, dist);
            byte  a     = (byte)(Mathf.Clamp01(alpha) * 255f);

            pixels[y * size + x] = new Color32(255, 255, 255, a);
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    // ────────────────────────────────────────────────────────────────
    //  내부 유틸
    // ────────────────────────────────────────────────────────────────

    private void FindAndSetPlayer()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            PlayerTransform  = playerObj.transform;
            PlayerController = playerObj.GetComponent<PlayerController>();
        }
        else
        {
            Debug.LogWarning("[MinimapManager] 'Player' 태그 오브젝트를 찾을 수 없습니다. " +
                             "플레이어 스폰 후 SetPlayer()를 호출하거나 태그를 확인하세요.");
        }
    }
}
