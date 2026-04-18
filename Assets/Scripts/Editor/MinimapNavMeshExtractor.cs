#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// NavMesh 삼각형 메시를 PNG로 래스터라이즈하여 미니맵 배경 이미지를 추출하는 에디터 툴.
/// 씬이 열려있고 NavMesh가 베이크된 상태에서 실행해야 한다.
/// 메뉴: Tools > Minimap > Extract NavMesh Map Image
/// </summary>
public class MinimapNavMeshExtractor : EditorWindow
{
    // ── Inspector 노출 옵션 ──────────────────────────────────────────
    private int    outputResolution = 512;
    private Color  walkableColor    = Color.white;
    private Color  bgColor          = new Color(0f, 0f, 0f, 0f); // 투명 배경
    private float  boundsMargin     = 1f;  // 바운드 여백 (월드 단위)
    private bool   autoCreateData   = true;

    // ── 내부 상태 ───────────────────────────────────────────────────
    private NavMeshTriangulation _triangulation;
    private Vector2              _worldMin;
    private Vector2              _worldMax;
    private bool                 _isAnalyzed;
    private string               _statusMessage = "씬을 열고 [NavMesh 분석] 버튼을 누르세요.";
    private MessageType          _statusType    = MessageType.Info;
    private Texture2D            _previewTex;

    [MenuItem("Tools/Minimap/Extract NavMesh Map Image")]
    public static void ShowWindow()
    {
        var window = GetWindow<MinimapNavMeshExtractor>("Minimap Extractor");
        window.minSize = new Vector2(360f, 520f);
    }

    private void OnGUI()
    {
        GUILayout.Label("NavMesh → 미니맵 이미지 추출기", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        // ── 씬 정보 ──────────────────────────────────────────────────
        EditorGUILayout.LabelField("현재 씬", SceneManager.GetActiveScene().name, EditorStyles.helpBox);
        EditorGUILayout.Space(6);

        // ── 옵션 ─────────────────────────────────────────────────────
        GUILayout.Label("출력 설정", EditorStyles.boldLabel);
        outputResolution = EditorGUILayout.IntSlider("해상도", outputResolution, 128, 2048);
        walkableColor    = EditorGUILayout.ColorField("Walkable 색상", walkableColor);
        bgColor          = EditorGUILayout.ColorField("배경 색상 (투명 권장)", bgColor);
        boundsMargin     = EditorGUILayout.FloatField("바운드 여백 (월드 단위)", boundsMargin);
        autoCreateData   = EditorGUILayout.Toggle("MinimapData 에셋 자동 생성", autoCreateData);
        EditorGUILayout.Space(8);

        // ── Step 1: NavMesh 분석 ──────────────────────────────────────
        if (GUILayout.Button("① NavMesh 분석", GUILayout.Height(32)))
            AnalyzeNavMesh();

        if (_isAnalyzed)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("삼각형 수",  $"{_triangulation.indices.Length / 3:N0}");
            EditorGUILayout.LabelField("World Min", _worldMin.ToString("F2"));
            EditorGUILayout.LabelField("World Max", _worldMax.ToString("F2"));
            EditorGUILayout.LabelField("World Size",
                $"{(_worldMax - _worldMin).x:F1} × {(_worldMax - _worldMin).y:F1}");
            EditorGUILayout.Space(8);

            // ── Step 2: 추출 ──────────────────────────────────────────
            if (GUILayout.Button("② PNG 추출 및 저장", GUILayout.Height(32)))
                ExtractAndSave();
        }

        // ── 상태 메시지 ───────────────────────────────────────────────
        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox(_statusMessage, _statusType);

        // ── 미리보기 ─────────────────────────────────────────────────
        if (_previewTex != null)
        {
            EditorGUILayout.Space(4);
            GUILayout.Label("미리보기 (256px)", EditorStyles.boldLabel);
            float size = Mathf.Min(position.width - 20f, 256f);
            GUILayout.Label(GUIContent.none, GUILayout.Width(size), GUILayout.Height(size));
            var rect = GUILayoutUtility.GetLastRect();
            EditorGUI.DrawPreviewTexture(rect, _previewTex);
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  NavMesh 분석
    // ────────────────────────────────────────────────────────────────
    private void AnalyzeNavMesh()
    {
        _triangulation = NavMesh.CalculateTriangulation();

        if (_triangulation.vertices.Length == 0)
        {
            _statusMessage = "NavMesh를 찾을 수 없습니다. 씬에서 NavMesh를 베이크하세요.";
            _statusType    = MessageType.Error;
            _isAnalyzed    = false;
            return;
        }

        // NavMeshPlus 2D: vertex.x/y = 2D 월드 좌표, vertex.z ≈ 0
        _worldMin = new Vector2(float.MaxValue, float.MaxValue);
        _worldMax = new Vector2(float.MinValue, float.MinValue);

        foreach (var v in _triangulation.vertices)
        {
            if (v.x < _worldMin.x) _worldMin.x = v.x;
            if (v.y < _worldMin.y) _worldMin.y = v.y;
            if (v.x > _worldMax.x) _worldMax.x = v.x;
            if (v.y > _worldMax.y) _worldMax.y = v.y;
        }

        // 여백 적용
        _worldMin -= Vector2.one * boundsMargin;
        _worldMax += Vector2.one * boundsMargin;

        // 긴 축 기준으로 정사각형으로 확장 (세로 긴 맵/가로 긴 맵 모두 대응)
        // → PNG와 worldMin/worldMax가 항상 정사각형 기준 → 미니맵 마스크와 1:1 매핑
        Vector2 worldCenter = (_worldMin + _worldMax) * 0.5f;
        float   halfSquare  = Mathf.Max(_worldMax.x - _worldMin.x,
                                        _worldMax.y - _worldMin.y) * 0.5f;
        _worldMin = worldCenter - Vector2.one * halfSquare;
        _worldMax = worldCenter + Vector2.one * halfSquare;

        _isAnalyzed    = true;
        _statusMessage = $"분석 완료. 삼각형 {_triangulation.indices.Length / 3:N0}개 발견.";
        _statusType    = MessageType.Info;
        Repaint();
    }

    // ────────────────────────────────────────────────────────────────
    //  PNG 추출 및 저장
    // ────────────────────────────────────────────────────────────────
    private void ExtractAndSave()
    {
        int w = outputResolution;
        int h = outputResolution;

        // 픽셀 배열 초기화 (배경색으로 채움)
        Color32[] pixels = new Color32[w * h];
        Color32 bg32   = bgColor;
        Color32 walk32 = walkableColor;
        for (int i = 0; i < pixels.Length; i++) pixels[i] = bg32;

        // 삼각형 래스터라이즈 (scanline fill)
        int[] indices  = _triangulation.indices;
        Vector3[] verts = _triangulation.vertices;

        for (int i = 0; i < indices.Length; i += 3)
        {
            Vector2 p0 = WorldToPixel(verts[indices[i    ]], w, h);
            Vector2 p1 = WorldToPixel(verts[indices[i + 1]], w, h);
            Vector2 p2 = WorldToPixel(verts[indices[i + 2]], w, h);
            FillTriangle(pixels, w, h, p0, p1, p2, walk32);
        }

        // Texture2D 생성 및 미리보기 저장
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels32(pixels);
        tex.Apply();

        _previewTex = tex;

        // PNG 저장 (Resources/Minimaps 경로 사용 → Resources.Load로 런타임 접근 가능)
        string sceneName = SceneManager.GetActiveScene().name;
        string savePath  = $"Assets/Resources/Minimaps/{sceneName}_minimap.png";

        Directory.CreateDirectory(Path.GetDirectoryName(Application.dataPath + "/" + savePath.Substring("Assets/".Length)));
        File.WriteAllBytes(Application.dataPath + "/" + savePath.Substring("Assets/".Length), tex.EncodeToPNG());
        AssetDatabase.Refresh();

        // 임포트 설정: Sprite 타입으로 설정
        TextureImporter importer = AssetImporter.GetAtPath(savePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType         = TextureImporterType.Sprite;
            importer.spriteImportMode    = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled       = false;
            importer.filterMode          = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        // MinimapData 에셋 자동 생성/갱신
        if (autoCreateData)
            CreateOrUpdateMinimapData(sceneName, savePath);

        _statusMessage = $"저장 완료: {savePath}";
        _statusType    = MessageType.Info;
        Repaint();
    }

    // ────────────────────────────────────────────────────────────────
    //  MinimapData SO 생성 또는 갱신
    // ────────────────────────────────────────────────────────────────
    private void CreateOrUpdateMinimapData(string sceneName, string spritePath)
    {
        string dataPath = $"Assets/Resources/Minimaps/{sceneName}_MinimapData.asset";

        var data = AssetDatabase.LoadAssetAtPath<MinimapData>(dataPath);
        if (data == null)
        {
            data = CreateInstance<MinimapData>();
            AssetDatabase.CreateAsset(data, dataPath);
        }

        data.worldMin   = _worldMin;
        data.worldMax   = _worldMax;
        data.mapSprite  = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
    }

    // ────────────────────────────────────────────────────────────────
    //  좌표 변환 유틸
    // ────────────────────────────────────────────────────────────────

    /// <summary>월드 좌표 → 텍스처 픽셀 좌표 (Y축 플립 포함)</summary>
    private Vector2 WorldToPixel(Vector3 worldPos, int w, int h)
    {
        float nx = Mathf.InverseLerp(_worldMin.x, _worldMax.x, worldPos.x);
        float ny = Mathf.InverseLerp(_worldMin.y, _worldMax.y, worldPos.y);
        return new Vector2(nx * (w - 1), ny * (h - 1));
    }

    // ────────────────────────────────────────────────────────────────
    //  Scanline 삼각형 채우기 (CPU 래스터라이즈)
    // ────────────────────────────────────────────────────────────────
    private void FillTriangle(Color32[] pixels, int w, int h,
                               Vector2 a, Vector2 b, Vector2 c, Color32 color)
    {
        // Y 기준 오름차순 정렬
        if (a.y > b.y) Swap(ref a, ref b);
        if (a.y > c.y) Swap(ref a, ref c);
        if (b.y > c.y) Swap(ref b, ref c);

        int y0 = Mathf.Max(0, Mathf.FloorToInt(a.y));
        int y2 = Mathf.Min(h - 1, Mathf.CeilToInt(c.y));

        for (int y = y0; y <= y2; y++)
        {
            float t = (c.y - a.y) > 0.001f ? (y - a.y) / (c.y - a.y) : 0f;
            float xLeft  = Mathf.Lerp(a.x, c.x, t);
            float xRight = xLeft;

            if (y < b.y)
            {
                float t2 = (b.y - a.y) > 0.001f ? (y - a.y) / (b.y - a.y) : 0f;
                xRight = Mathf.Lerp(a.x, b.x, t2);
            }
            else
            {
                float t2 = (c.y - b.y) > 0.001f ? (y - b.y) / (c.y - b.y) : 0f;
                xRight = Mathf.Lerp(b.x, c.x, t2);
            }

            if (xLeft > xRight) { float tmp = xLeft; xLeft = xRight; xRight = tmp; }

            int x0c = Mathf.Max(0, Mathf.FloorToInt(xLeft));
            int x1c = Mathf.Min(w - 1, Mathf.CeilToInt(xRight));

            for (int x = x0c; x <= x1c; x++)
                pixels[y * w + x] = color;
        }
    }

    private static void Swap(ref Vector2 a, ref Vector2 b)
    {
        Vector2 tmp = a; a = b; b = tmp;
    }
}
#endif
