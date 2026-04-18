// SpriteGridSlicerAndClipper.cs (UI)
// Unity Editor tool — auto-slice 8xN sprite grids and auto-create AnimationClips
// Put under: Assets/Editor/SpriteGridSlicerAndClipper.cs
// Menu: Tools ▶ Sprites ▶ Slice 8xN & Create Clips (Window)

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SpriteGridSlicerAndClipper : EditorWindow
{
    // Direction order (top → bottom rows)
    private static readonly string[] DIR_ROW_ORDER = { "N","NE","E","SE","S","SW","W","NW" };
    private const string GRID_SUFFIX_PATTERN = @"_8x(?<cols>\d+)_grid"; // e.g., Idle_8x12_grid.png

    // UI fields
    private int rows = 8;                 // usually 8
    private int colsOverride = 0;         // 0 = auto, >0 = force

    private int cellWidthPx = 0;          // 0 = auto, >0 = force
    private int cellHeightPx = 0;         // 0 = auto, >0 = force
    private int startX = 0, startY = 0;   // inner offset (pixels) from bottom-left
    private int paddingX = 0, paddingY = 0; // spacing between cells (pixels)

    private SpriteAlignment pivotMode = SpriteAlignment.Custom; // default: custom pivot
    private Vector2 customPivot = new Vector2(0.5f, 0.0f);      // bottom center

    private int clipFPS = 12;                                    // FPS for created clips
    private string loopStatesCsv = "Idle,Walk,Run";              // states that should loop
    private bool createCombinedClip = false;                     // create single combined clip
    private int animationFrameCount = 0;                         // limit animation frames (0 = use all)

    [MenuItem("Tools/Sprites/Slice 8xN & Create Clips (Window)")]
    public static void OpenWindow()
    {
        var w = GetWindow<SpriteGridSlicerAndClipper>("8xN Slicer & Clips");
        w.minSize = new Vector2(420, 380);
        w.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Grid Settings", EditorStyles.boldLabel);
        rows = EditorGUILayout.IntField("Rows", rows);
        colsOverride = EditorGUILayout.IntField(new GUIContent("Cols Override", "열 수를 직접 지정 (0=자동)"), colsOverride);
        EditorGUILayout.Space(4);

        GUILayout.Label("Cell Size (pixels)", EditorStyles.boldLabel);
        cellWidthPx = EditorGUILayout.IntField(new GUIContent("Cell Width", "프레임 가로 픽셀 (0=자동)"), cellWidthPx);
        cellHeightPx = EditorGUILayout.IntField(new GUIContent("Cell Height", "프레임 세로 픽셀 (0=자동)"), cellHeightPx);
        startX = EditorGUILayout.IntField(new GUIContent("Start X", "왼쪽 여백"), startX);
        startY = EditorGUILayout.IntField(new GUIContent("Start Y", "아래 여백"), startY);
        paddingX = EditorGUILayout.IntField(new GUIContent("Padding X", "프레임 사이 가로 간격"), paddingX);
        paddingY = EditorGUILayout.IntField(new GUIContent("Padding Y", "프레임 사이 세로 간격"), paddingY);

        EditorGUILayout.Space(8);
        GUILayout.Label("Pivot", EditorStyles.boldLabel);
        pivotMode = (SpriteAlignment)EditorGUILayout.EnumPopup("Pivot Mode", pivotMode);
        if (pivotMode == SpriteAlignment.Custom)
        {
            customPivot = EditorGUILayout.Vector2Field("Custom Pivot (0..1)", customPivot);
        }

        EditorGUILayout.Space(8);
        GUILayout.Label("Clips", EditorStyles.boldLabel);
        clipFPS = EditorGUILayout.IntField("Clip FPS", clipFPS);
        loopStatesCsv = EditorGUILayout.TextField(new GUIContent("Loop States (CSV)", "LoopTime을 켜줄 상태 이름들, 쉼표로 구분"), loopStatesCsv);
        createCombinedClip = EditorGUILayout.Toggle(new GUIContent("Create Combined Clip", "방향별 개별 클립 외에 모든 프레임을 순차 연결한 통합 클립 생성"), createCombinedClip);
        animationFrameCount = EditorGUILayout.IntField(new GUIContent("Animation Frame Count", "실제 사용할 애니메이션 프레임 수 (0=전체 사용, 빈 칸 제외하고 싶을 때 지정)"), animationFrameCount);

        EditorGUILayout.Space();
        if (GUILayout.Button("Process Selected Textures", GUILayout.Height(34)))
        {
            ProcessSelection();
        }

        EditorGUILayout.HelpBox("팁: 8xN 그리드는 셀 폭/높이를 캡처 사이즈(예: 256x256)로, Padding/Offset=0으로 두면 정확히 잘립니다.\n" + 
                                "Animation Frame Count: 빈 칸으로 인한 캐릭터 사라짐을 방지하려면 실제 프레임 수를 입력하세요 (예: 4x4=16 중 14개만 사용 시 14 입력)", MessageType.Info);
    }

    private void ProcessSelection()
    {
        UnityEngine.Object[] sel = Selection.objects;
        if (sel == null || sel.Length == 0)
        {
            EditorUtility.DisplayDialog("8xN Slicer", "Project 창에서 8xN PNG를 하나 이상 선택하세요.", "OK");
            return;
        }
        int processed = 0;
        for (int i = 0; i < sel.Length; i++)
        {
            string path = AssetDatabase.GetAssetPath(sel[i]);
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                continue;

            if (ProcessGridTexture(path)) processed++;
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("8xN Slicer", "완료: " + processed + " 파일 처리", "OK");
    }

    // ------------ Core per-texture processing ------------
    private bool ProcessGridTexture(string assetPath)
    {
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (tex == null)
        {
            Debug.LogWarning("[Slice] Not a texture: " + assetPath);
            return false;
        }

        // State name guess: part before _8x
        string fileNoExt = Path.GetFileNameWithoutExtension(assetPath);
        string state = fileNoExt;
        int idx = state.IndexOf("_8x", StringComparison.Ordinal);
        if (idx >= 0) state = state.Substring(0, idx);

        // Determine rows/cols/cell size
        int rowsUse = Mathf.Max(1, rows);
        int colsUse = colsOverride;
        if (colsUse <= 0)
        {
            int parsed = ParseColsFromName(fileNoExt);
            if (parsed > 0) colsUse = parsed;
        }

        // If still 0, infer cols using cell width or assuming square
        if (colsUse <= 0)
        {
            if (cellWidthPx > 0)
            {
                colsUse = Mathf.Max(1, (tex.width - startX + paddingX) / Mathf.Max(1, cellWidthPx + paddingX));
            }
            else
            {
                float cellHguess = (tex.height - startY + paddingY) / (float)rowsUse - paddingY; // rough
                colsUse = Mathf.Max(1, Mathf.RoundToInt((tex.width - startX) / Mathf.Max(1f, cellHguess + paddingX)));
            }
        }

        int cw = (cellWidthPx > 0) ? cellWidthPx : Mathf.RoundToInt(((float)tex.width - startX - (colsUse - 1) * paddingX) / colsUse);
        int ch = (cellHeightPx > 0) ? cellHeightPx : Mathf.RoundToInt(((float)tex.height - startY - (rowsUse - 1) * paddingY) / rowsUse);
        cw = Mathf.Max(1, cw); ch = Mathf.Max(1, ch);

        // Configure importer
        TextureImporter ti = (TextureImporter)TextureImporter.GetAtPath(assetPath);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Multiple;
        ti.alphaIsTransparency = true;
        ti.mipmapEnabled = false;

        // Slice into rows x cols grid (top row = N)
        List<SpriteMetaData> metas = new List<SpriteMetaData>(rowsUse * colsUse);
        for (int r = 0; r < rowsUse; r++)
        {
            string dir = (r < DIR_ROW_ORDER.Length) ? DIR_ROW_ORDER[r] : ("Dir" + r);
            for (int c = 0; c < colsUse; c++)
            {
                int x = startX + c * (cw + paddingX);
                int y = startY + (rowsUse - 1 - r) * (ch + paddingY); // bottom-left origin
                Rect rect = new Rect(x, y, cw, ch);

                SpriteMetaData smd = new SpriteMetaData();
                smd.rect = rect;
                smd.name = string.Format("{0}_{1}_{2:0000}", state, dir, c + 1);
                smd.alignment = (int)SpriteAlignment.Custom;
                smd.pivot = (pivotMode == SpriteAlignment.Custom) ? customPivot : GetPivot(pivotMode);
                metas.Add(smd);
            }
        }
        ti.spritesheet = metas.ToArray();
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        // Create clips per row (direction)
        CreateDirectionClips(assetPath, state, colsUse, rowsUse, Mathf.Max(1, clipFPS), ParseLoopStates(loopStatesCsv), createCombinedClip, animationFrameCount);
        return true;
    }

    private static int ParseColsFromName(string name)
    {
        try
        {
            var m = Regex.Match(name, GRID_SUFFIX_PATTERN, RegexOptions.IgnoreCase);
            if (m.Success) return int.Parse(m.Groups["cols"].Value);
        }
        catch { }
        return -1;
    }

    private static Vector2 GetPivot(SpriteAlignment mode)
    {
        switch (mode)
        {
            case SpriteAlignment.Center: return new Vector2(0.5f, 0.5f);
            case SpriteAlignment.TopLeft: return new Vector2(0f, 1f);
            case SpriteAlignment.TopCenter: return new Vector2(0.5f, 1f);
            case SpriteAlignment.TopRight: return new Vector2(1f, 1f);
            case SpriteAlignment.LeftCenter: return new Vector2(0f, 0.5f);
            case SpriteAlignment.RightCenter: return new Vector2(1f, 0.5f);
            case SpriteAlignment.BottomLeft: return new Vector2(0f, 0f);
            case SpriteAlignment.BottomCenter: return new Vector2(0.5f, 0f);
            case SpriteAlignment.BottomRight: return new Vector2(1f, 0f);
            default: return new Vector2(0.5f, 0.5f);
        }
    }

    private static HashSet<string> ParseLoopStates(string csv)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(csv)) return set;
        string[] parts = csv.Split(',');
        for (int i = 0; i < parts.Length; i++)
        {
            string s = parts[i].Trim();
            if (!string.IsNullOrEmpty(s)) set.Add(s);
        }
        return set;
    }

    private static void CreateDirectionClips(string sheetPath, string state, int cols, int rows, int fps, HashSet<string> loopSet, bool createCombinedClip, int maxFrameCount)
    {
        // Load sliced sprites
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(sheetPath);
        List<Sprite> sprites = new List<Sprite>();
        for (int i = 0; i < assets.Length; i++)
        {
            Sprite s = assets[i] as Sprite;
            if (s != null) sprites.Add(s);
        }
        if (sprites.Count == 0)
        {
            Debug.LogWarning("[Clips] No sprites found after slicing: " + sheetPath);
            return;
        }

        // Group by direction using name pattern <state>_<dir>_<####>
        Dictionary<string, List<Sprite>> byDir = new Dictionary<string, List<Sprite>>();
        for (int i = 0; i < sprites.Count; i++)
        {
            string n = sprites[i].name;
            string dir = ExtractDirToken(n);
            if (string.IsNullOrEmpty(dir)) dir = "Dir";
            List<Sprite> list;
            if (!byDir.TryGetValue(dir, out list)) { list = new List<Sprite>(); byDir[dir] = list; }
            list.Add(sprites[i]);
        }
        foreach (var kv in byDir)
        {
            kv.Value.Sort((a,b) => ExtractFrameNumber(a.name).CompareTo(ExtractFrameNumber(b.name)));
        }

        string gridDir = Path.GetDirectoryName(sheetPath).Replace('\\','/');
        string clipsDir = CombineEnsure(gridDir, "Clips");

        if (createCombinedClip)
        {
            // Create combined clip only
            CreateCombinedClip(sprites, sheetPath, state, cols, rows, fps, loopSet, maxFrameCount);
        }
        else
        {
            // Create direction clips only
            foreach (var kv in byDir)
            {
                string dir = kv.Key;
                List<Sprite> frames = kv.Value;
                AnimationClip clip = new AnimationClip();
                clip.frameRate = fps;

                EditorCurveBinding binding = new EditorCurveBinding();
                binding.type = typeof(SpriteRenderer);
                binding.path = string.Empty;
                binding.propertyName = "m_Sprite";

                ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frames.Count];
                for (int i = 0; i < frames.Count; i++)
                {
                    keys[i] = new ObjectReferenceKeyframe { time = i / (float)fps, value = frames[i] };
                }
                AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

                bool loop = (loopSet != null && loopSet.Contains(state));
                SetLoopTime(clip, loop);

                string clipName = string.Format("{0}_{1}.anim", state, dir);
                string clipPath = (gridDir + "/Clips/" + clipName).Replace('\\','/');
                AssetDatabase.CreateAsset(clip, clipPath);
            }
        }
    }

    private static string CombineEnsure(string parent, string child)
    {
        parent = parent.Replace('\\','/');
        string p = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(p)) AssetDatabase.CreateFolder(parent, child);
        return p;
    }

    private static string ExtractDirToken(string spriteName)
    {
        // Split by '_' and get the second-to-last token (direction)
        // Pattern: State_..._Direction_Frame -> get Direction
        string[] parts = spriteName.Split('_');
        if (parts.Length >= 2)
        {
            return parts[parts.Length - 2]; // second-to-last part
        }
        return null;
    }

    private static int ExtractFrameNumber(string spriteName)
    {
        int lastUnderscore = spriteName.LastIndexOf('_');
        if (lastUnderscore >= 0 && lastUnderscore + 1 < spriteName.Length)
        {
            int n; if (int.TryParse(spriteName.Substring(lastUnderscore + 1), out n)) return n;
        }
        return 0;
    }

    private static void SetLoopTime(AnimationClip clip, bool loop)
    {
        SerializedObject so = new SerializedObject(clip);
        SerializedProperty s = so.FindProperty("m_AnimationClipSettings");
        if (s != null)
        {
            SerializedProperty loopTime = s.FindPropertyRelative("m_LoopTime");
            if (loopTime != null) loopTime.boolValue = loop;
            SerializedProperty loopBlend = s.FindPropertyRelative("m_LoopBlend");
            if (loopBlend != null) loopBlend.boolValue = loop;
            so.ApplyModifiedProperties();
        }
    }

    private static void CreateCombinedClip(List<Sprite> sprites, string sheetPath, string state, int cols, int rows, int fps, HashSet<string> loopSet, int maxFrameCount)
    {
        if (sprites.Count == 0) return;
        // Sort sprites in grid order (row-major: top→bottom, left→right)
        List<Sprite> sortedSprites = new List<Sprite>(sprites);
        sortedSprites.Sort((a, b) => {
            // Extract direction and frame number from sprite name pattern: {state}_{dir}_{frame}
            string dirA = ExtractDirToken(a.name);
            string dirB = ExtractDirToken(b.name);
            int frameA = ExtractFrameNumber(a.name);
            int frameB = ExtractFrameNumber(b.name);

            // Get row index from direction
            int rowA = Array.IndexOf(DIR_ROW_ORDER, dirA);
            int rowB = Array.IndexOf(DIR_ROW_ORDER, dirB);
            if (rowA < 0) rowA = 999; // unknown dirs go to end
            if (rowB < 0) rowB = 999;

            // Grid position: (row * cols + col)
            int gridPosA = rowA * cols + (frameA - 1); // frameA is 1-based
            int gridPosB = rowB * cols + (frameB - 1); // frameB is 1-based
            
            return gridPosA.CompareTo(gridPosB);
        });

        // Apply frame count limitation
        if (maxFrameCount > 0 && sortedSprites.Count > maxFrameCount)
        {
            sortedSprites = sortedSprites.GetRange(0, maxFrameCount);
        }

        // Filter out null/empty sprites
        List<Sprite> validSprites = new List<Sprite>();
        for (int i = 0; i < sortedSprites.Count; i++)
        {
            if (sortedSprites[i] != null)
            {
                validSprites.Add(sortedSprites[i]);
            }
        }
        sortedSprites = validSprites;

        // Debug: Log final sprite order
        for (int i = 0; i < Mathf.Min(sortedSprites.Count, 10); i++) // Show first 10
        {
            string dir = ExtractDirToken(sortedSprites[i].name);
            int frame = ExtractFrameNumber(sortedSprites[i].name);
            int row = Array.IndexOf(DIR_ROW_ORDER, dir);
            int gridPos = row * cols + (frame - 1);
        }

        // Create combined animation clip
        AnimationClip clip = new AnimationClip();
        clip.frameRate = fps;

        EditorCurveBinding binding = new EditorCurveBinding();
        binding.type = typeof(SpriteRenderer);
        binding.path = string.Empty;
        binding.propertyName = "m_Sprite";

        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[sortedSprites.Count];
        for (int i = 0; i < sortedSprites.Count; i++)
        {
            keys[i] = new ObjectReferenceKeyframe { time = i / (float)fps, value = sortedSprites[i] };
        }
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        bool loop = (loopSet != null && loopSet.Contains(state));
        SetLoopTime(clip, loop);

        // Save combined clip
        string gridDir = Path.GetDirectoryName(sheetPath).Replace('\\','/');
        string clipName = string.Format("{0}_Sheet.anim", state);
        string clipPath = (gridDir + "/Clips/" + clipName).Replace('\\','/');
        AssetDatabase.CreateAsset(clip, clipPath);
    }
}
