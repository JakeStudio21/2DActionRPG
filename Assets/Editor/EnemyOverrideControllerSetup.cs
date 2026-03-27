using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Enemy Animator Override Controller에 애니메이션 클립을 자동 연결하는 에디터 툴
/// 메뉴: Tools → Animation → Enemy Override Controllers
/// </summary>
public class EnemyOverrideControllerSetup : EditorWindow
{
    // ─── 입력 필드 ───────────────────────────────────────────
    private AnimatorOverrideController _overrideController;
    private string _animRootFolder = "Assets/Exports/";

    // ─── 미리보기 데이터 ──────────────────────────────────────
    private List<PreviewEntry> _previewEntries = new List<PreviewEntry>();
    private bool _previewGenerated = false;
    private Vector2 _previewScroll;

    // ─── 결과 로그 ────────────────────────────────────────────
    private List<string> _resultLog = new List<string>();
    private bool _resultShown = false;
    private Vector2 _logScroll;

    // ─── 스타일 캐시 ─────────────────────────────────────────
    private GUIStyle _styleHeader;
    private GUIStyle _styleSuccess;
    private GUIStyle _styleWarning;
    private GUIStyle _styleError;
    private bool _stylesInitialized;

    private class PreviewEntry
    {
        public string StateName;
        public string Direction;
        public AnimationClip BaseClip;
        public AnimationClip MonsterClip;
        public bool Found => MonsterClip != null;
    }

    // ─────────────────────────────────────────────────────────
    [MenuItem("Tools/Animation/Enemy Override Controllers")]
    public static void OpenWindow()
    {
        var window = GetWindow<EnemyOverrideControllerSetup>("Override Controller Setup");
        window.minSize = new Vector2(480, 560);
        window.Show();
    }

    // ─── GUI ─────────────────────────────────────────────────
    private void OnGUI()
    {
        InitStyles();

        EditorGUILayout.Space(6);
        GUILayout.Label("Enemy Override Controller Setup", _styleHeader);
        EditorGUILayout.Space(4);

        DrawInputSection();

        EditorGUILayout.Space(8);
        DrawPreviewSection();

        if (_previewGenerated && _previewEntries.Count > 0)
        {
            EditorGUILayout.Space(8);
            DrawApplyButton();
        }

        if (_resultShown)
        {
            EditorGUILayout.Space(8);
            DrawResultLog();
        }
    }

    // ─── 입력 섹션 ────────────────────────────────────────────
    private void DrawInputSection()
    {
        EditorGUILayout.LabelField("설정", EditorStyles.boldLabel);
        using (new EditorGUI.IndentLevelScope(1))
        {
            // Override Controller 필드
            EditorGUI.BeginChangeCheck();
            _overrideController = (AnimatorOverrideController)EditorGUILayout.ObjectField(
                "Override Controller",
                _overrideController,
                typeof(AnimatorOverrideController),
                false
            );
            if (EditorGUI.EndChangeCheck()) ResetState();

            EditorGUILayout.Space(4);

            // 폴더 경로 필드 + 브라우저 버튼
            EditorGUILayout.LabelField("애니메이션 루트 폴더");
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                _animRootFolder = EditorGUILayout.TextField(_animRootFolder);
                if (EditorGUI.EndChangeCheck()) ResetState();

                if (GUILayout.Button("탐색", GUILayout.Width(50)))
                {
                    string selected = EditorUtility.OpenFolderPanel(
                        "애니메이션 루트 폴더 선택",
                        Application.dataPath,
                        ""
                    );
                    if (!string.IsNullOrEmpty(selected))
                    {
                        // 절대경로 → Assets 상대경로로 변환
                        if (selected.StartsWith(Application.dataPath))
                            selected = "Assets" + selected.Substring(Application.dataPath.Length);

                        _animRootFolder = selected.Replace("\\", "/");
                        ResetState();
                        GUI.FocusControl(null);
                    }
                }
            }

            // 폴더 경로 미리보기 (회색 소형 텍스트)
            if (!string.IsNullOrEmpty(_animRootFolder))
            {
                bool folderExists = AssetDatabase.IsValidFolder(_animRootFolder);
                var style = folderExists ? EditorStyles.miniLabel : _styleWarning;
                string hint = folderExists
                    ? $"예상 클립 경로: {_animRootFolder}/Attack/Clips/Attack_N_Sheet_Sheet.anim"
                    : $"⚠ 폴더를 찾을 수 없음: {_animRootFolder}";
                EditorGUILayout.LabelField(hint, style);
            }
        }

        EditorGUILayout.Space(4);

        // 미리보기 버튼
        using (new EditorGUI.DisabledScope(_overrideController == null || string.IsNullOrEmpty(_animRootFolder)))
        {
            if (GUILayout.Button("미리보기 생성", GUILayout.Height(28)))
                GeneratePreview();
        }
    }

    // ─── 미리보기 섹션 ────────────────────────────────────────
    private void DrawPreviewSection()
    {
        if (!_previewGenerated) return;

        int found = _previewEntries.Count(e => e.Found);
        int missing = _previewEntries.Count(e => !e.Found);

        EditorGUILayout.LabelField(
            $"미리보기  |  연결 가능: {found}개  /  누락: {missing}개  /  전체: {_previewEntries.Count}개",
            EditorStyles.boldLabel
        );

        float rowHeight = 18f;
        float listHeight = Mathf.Min(_previewEntries.Count * rowHeight + 4f, 260f);

        _previewScroll = EditorGUILayout.BeginScrollView(
            _previewScroll,
            GUILayout.Height(listHeight)
        );

        string currentState = null;
        foreach (var entry in _previewEntries)
        {
            // 상태 구분선
            if (entry.StateName != currentState)
            {
                currentState = entry.StateName;
                EditorGUILayout.LabelField($"── {currentState} ──", EditorStyles.centeredGreyMiniLabel);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                // 상태 아이콘
                GUILayout.Label(entry.Found ? "✓" : "✗",
                    entry.Found ? _styleSuccess : _styleError,
                    GUILayout.Width(16));

                // 방향
                GUILayout.Label(entry.Direction, GUILayout.Width(28));

                // 베이스 클립명
                var prevColor = GUI.color;
                GUI.color = Color.gray;
                GUILayout.Label(entry.BaseClip != null ? entry.BaseClip.name : "(null)", GUILayout.Width(160));
                GUI.color = prevColor;

                GUILayout.Label("→", GUILayout.Width(18));

                // 몬스터 클립 (오브젝트 필드 읽기전용)
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(entry.MonsterClip, typeof(AnimationClip), false);
                EditorGUI.EndDisabledGroup();
            }
        }

        EditorGUILayout.EndScrollView();
    }

    // ─── 실행 버튼 ────────────────────────────────────────────
    private void DrawApplyButton()
    {
        int missing = _previewEntries.Count(e => !e.Found);

        if (missing > 0)
        {
            EditorGUILayout.HelpBox(
                $"{missing}개 클립을 찾지 못했습니다. 누락된 클립은 건너뛰고 나머지만 연결됩니다.",
                MessageType.Warning
            );
        }

        var prevBg = GUI.backgroundColor;
        GUI.backgroundColor = missing == 0 ? new Color(0.4f, 0.85f, 0.4f) : new Color(0.95f, 0.75f, 0.3f);

        if (GUILayout.Button(missing == 0 ? "▶  연결 실행  (25개)" : $"▶  연결 실행  ({_previewEntries.Count - missing}/{_previewEntries.Count}개)",
            GUILayout.Height(34)))
        {
            ApplyOverrides();
        }

        GUI.backgroundColor = prevBg;
    }

    // ─── 결과 로그 ────────────────────────────────────────────
    private void DrawResultLog()
    {
        EditorGUILayout.LabelField("결과 로그", EditorStyles.boldLabel);
        _logScroll = EditorGUILayout.BeginScrollView(_logScroll, GUILayout.Height(100));
        foreach (var line in _resultLog)
        {
            bool isErr = line.StartsWith("✗") || line.StartsWith("⚠");
            EditorGUILayout.LabelField(line, isErr ? _styleWarning : _styleSuccess);
        }
        EditorGUILayout.EndScrollView();
    }

    // ─── 미리보기 생성 로직 ───────────────────────────────────
    private void GeneratePreview()
    {
        _previewEntries.Clear();
        _previewGenerated = false;
        _resultShown = false;

        if (_overrideController == null) return;

        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        _overrideController.GetOverrides(overrides);

        if (overrides.Count == 0)
        {
            EditorUtility.DisplayDialog("오류",
                "Base Controller에서 클립을 가져올 수 없습니다.\nOverride Controller에 Base Controller가 지정되어 있는지 확인하세요.",
                "확인");
            return;
        }

        string rootFolder = _animRootFolder.TrimEnd('/');

        foreach (var pair in overrides)
        {
            AnimationClip baseClip = pair.Key;
            if (baseClip == null) continue;

            string basePath = AssetDatabase.GetAssetPath(baseClip);
            string fileName = Path.GetFileNameWithoutExtension(basePath);

            string[] parts = fileName.Split('_');
            if (parts.Length < 2) continue;

            string action    = parts[0];  // Run / Idle / Attack / Hit / Die
            string direction = parts[1];  // N / NE / E / SE / S / ...

            string clipPath = $"{rootFolder}/{action}/Clips/{fileName}.anim";
            var monsterClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);

            _previewEntries.Add(new PreviewEntry
            {
                StateName  = action,
                Direction  = direction,
                BaseClip   = baseClip,
                MonsterClip = monsterClip
            });
        }

        // 상태별 → 방향 순 정렬
        string[] stateOrder = { "Idle", "Run", "Attack", "Hit", "Die" };
        string[] dirOrder   = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

        _previewEntries.Sort((a, b) =>
        {
            int ai = System.Array.IndexOf(stateOrder, a.StateName);
            int bi = System.Array.IndexOf(stateOrder, b.StateName);
            if (ai != bi) return ai.CompareTo(bi);
            return System.Array.IndexOf(dirOrder, a.Direction)
                .CompareTo(System.Array.IndexOf(dirOrder, b.Direction));
        });

        _previewGenerated = true;
        Repaint();
    }

    // ─── 연결 실행 로직 ───────────────────────────────────────
    private void ApplyOverrides()
    {
        _resultLog.Clear();

        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        _overrideController.GetOverrides(overrides);

        string rootFolder = _animRootFolder.TrimEnd('/');
        int matched = 0, missing = 0;

        for (int i = 0; i < overrides.Count; i++)
        {
            AnimationClip baseClip = overrides[i].Key;
            if (baseClip == null) continue;

            string basePath = AssetDatabase.GetAssetPath(baseClip);
            string fileName = Path.GetFileNameWithoutExtension(basePath);
            string[] parts  = fileName.Split('_');
            if (parts.Length < 2) continue;

            string action    = parts[0];
            string direction = parts[1];
            string clipPath  = $"{rootFolder}/{action}/Clips/{fileName}.anim";
            var monsterClip  = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);

            if (monsterClip != null)
            {
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(baseClip, monsterClip);
                _resultLog.Add($"✓  {action,-8} {direction,-3}  →  {monsterClip.name}");
                matched++;
            }
            else
            {
                _resultLog.Add($"✗  {action,-8} {direction,-3}  →  파일 없음: {clipPath}");
                missing++;
            }
        }

        _overrideController.ApplyOverrides(overrides);
        EditorUtility.SetDirty(_overrideController);
        AssetDatabase.SaveAssets();

        _resultLog.Insert(0, $"── 완료: {matched}개 연결  /  {missing}개 누락 ──");
        _resultShown = true;

        Debug.Log($"[Override Setup] {_overrideController.name}: {matched}개 연결, {missing}개 누락");
        Repaint();
    }

    // ─── 유틸 ─────────────────────────────────────────────────
    private void ResetState()
    {
        _previewEntries.Clear();
        _previewGenerated = false;
        _resultShown = false;
        _resultLog.Clear();
    }

    private void InitStyles()
    {
        if (_stylesInitialized) return;

        _styleHeader = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
        };

        _styleSuccess = new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = new Color(0.4f, 0.9f, 0.4f) }
        };

        _styleWarning = new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = new Color(1f, 0.75f, 0.2f) }
        };

        _styleError = new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = new Color(1f, 0.4f, 0.4f) }
        };

        _stylesInitialized = true;
    }
}
