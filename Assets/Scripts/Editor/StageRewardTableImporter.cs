using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using StageSystem;

/// <summary>
/// Tools > RewardSystem > Stage Reward Table Importer
///
/// StageRewardTableData.csv 를 읽어
/// Assets/Resources/Stages/Drops/*_Config.asset 을 업데이트합니다.
///
/// - 기존 SO 발견 → Items 전체 교체, Gold/Exp/GroupType 업데이트 (GUID 유지)
/// - 기존 SO 없음  → {DropGroupID}_Config.asset 신규 생성
/// </summary>
public class StageRewardTableImporter : EditorWindow
{
    // ─────────────────────────────────────────────────────────────────
    // 상수
    // ─────────────────────────────────────────────────────────────────

    private const string SO_SAVE_PATH     = "Assets/Resources/Stages/Drops/";
    private const string WINDOW_TITLE     = "Stage Reward Table Importer";
    private const int    HEADER_ROW_COUNT = 1;

    // CSV 컬럼 인덱스
    private const int COL_DROP_GROUP_ID = 0;
    private const int COL_STAGE_ID      = 1;
    private const int COL_GROUP_TYPE    = 2;
    private const int COL_GOLD          = 3;
    private const int COL_EXP           = 4;
    private const int COL_ITEM_ID       = 5;
    private const int COL_AMOUNT        = 6;
    private const int COL_DROP_RATE     = 7;
    private const int EXPECTED_COL_COUNT = 8;

    // ─────────────────────────────────────────────────────────────────
    // 내부 데이터 구조
    // ─────────────────────────────────────────────────────────────────

    private class CsvRow
    {
        public string        dropGroupId;
        public string        stageId;
        public DropGroupType groupType;
        public int           gold;
        public int           exp;
        public string        itemId;
        public int           amount;
        public float         dropRate;
    }

    private class PreviewEntry
    {
        public string dropGroupId;
        public int    csvItemCount;
        public bool   soExists;
    }

    // ─────────────────────────────────────────────────────────────────
    // UI 상태
    // ─────────────────────────────────────────────────────────────────

    private TextAsset  _csvAsset;
    private Vector2    _previewScroll;
    private Vector2    _logScroll;
    private string     _logText = "";

    private List<PreviewEntry>                    _previewEntries = new List<PreviewEntry>();
    private Dictionary<string, List<CsvRow>>      _parsedGroups   = new Dictionary<string, List<CsvRow>>();
    private Dictionary<string, StageSystem.DropTable> _existingSOs = new Dictionary<string, StageSystem.DropTable>();

    // ─────────────────────────────────────────────────────────────────
    // 메뉴 등록
    // ─────────────────────────────────────────────────────────────────

    [MenuItem("Tools/RewardSystem/Stage Reward Table Importer")]
    public static void OpenWindow()
    {
        var window = GetWindow<StageRewardTableImporter>(WINDOW_TITLE);
        window.minSize = new Vector2(480, 600);
        window.Show();
    }

    // ─────────────────────────────────────────────────────────────────
    // OnGUI
    // ─────────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        DrawHeader();
        DrawCsvField();
        GUILayout.Space(6);
        DrawPreviewSection();
        GUILayout.Space(6);
        DrawActionButtons();
        GUILayout.Space(6);
        DrawLogSection();
    }

    // ─────────────────────────────────────────────────────────────────
    // UI 섹션
    // ─────────────────────────────────────────────────────────────────

    private void DrawHeader()
    {
        EditorGUILayout.Space(8);
        var titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize  = 14,
            alignment = TextAnchor.MiddleCenter,
        };
        EditorGUILayout.LabelField("Stage Reward Table Importer", titleStyle, GUILayout.Height(24));
        EditorGUILayout.LabelField("CSV → StageSystem.DropTable SO 자동 반영 도구", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.Space(4);
        DrawSeparator();
    }

    private void DrawCsvField()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("📄 CSV 파일", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        _csvAsset = (TextAsset)EditorGUILayout.ObjectField(
            "Stage Reward CSV",
            _csvAsset,
            typeof(TextAsset),
            false
        );
        if (EditorGUI.EndChangeCheck())
        {
            _previewEntries.Clear();
            _parsedGroups.Clear();
            _logText = "";
        }
    }

    private void DrawPreviewSection()
    {
        DrawSeparator();
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("🔍 미리보기", EditorStyles.boldLabel);

        if (_previewEntries.Count == 0)
        {
            EditorGUILayout.HelpBox("CSV 파일을 선택한 뒤 [미리보기 갱신]을 클릭하세요.", MessageType.Info);
        }
        else
        {
            _previewScroll = EditorGUILayout.BeginScrollView(_previewScroll, GUILayout.Height(180));

            foreach (var entry in _previewEntries)
            {
                string icon   = entry.soExists ? "✅" : "⚠️";
                string status = entry.soExists
                    ? $"기존 SO 발견 ({entry.csvItemCount}개 아이템으로 덮어쓰기)"
                    : $"SO 없음 → 신규 생성 ({entry.csvItemCount}개 아이템)";

                var style = entry.soExists ? EditorStyles.label : EditorStyles.boldLabel;
                EditorGUILayout.LabelField($"{icon}  {entry.dropGroupId}", status, style);
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField(
                $"총 {_previewEntries.Count}개 그룹 " +
                $"(업데이트 {CountExisting()}개 / 신규 {CountNew()}개)",
                EditorStyles.centeredGreyMiniLabel
            );
        }
    }

    private void DrawActionButtons()
    {
        DrawSeparator();
        EditorGUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();

        GUI.enabled = _csvAsset != null;
        if (GUILayout.Button("미리보기 갱신", GUILayout.Height(30)))
            RefreshPreview();

        GUI.enabled = _csvAsset != null && _previewEntries.Count > 0;
        var executeStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
        if (GUILayout.Button("SO 업데이트 실행", executeStyle, GUILayout.Height(30)))
            ExecuteImport();

        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField(
            $"저장 경로: {SO_SAVE_PATH}",
            EditorStyles.centeredGreyMiniLabel
        );
    }

    private void DrawLogSection()
    {
        DrawSeparator();
        EditorGUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("📋 결과 로그", EditorStyles.boldLabel);
        if (GUILayout.Button("지우기", GUILayout.Width(60)))
            _logText = "";
        EditorGUILayout.EndHorizontal();

        _logScroll = EditorGUILayout.BeginScrollView(
            _logScroll,
            EditorStyles.helpBox,
            GUILayout.ExpandHeight(true)
        );
        EditorGUILayout.LabelField(
            string.IsNullOrEmpty(_logText) ? "—" : _logText,
            EditorStyles.wordWrappedLabel
        );
        EditorGUILayout.EndScrollView();
    }

    private static void DrawSeparator()
    {
        var rect = EditorGUILayout.GetControlRect(false, 1f);
        EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f, 0.5f));
    }

    // ─────────────────────────────────────────────────────────────────
    // 미리보기 갱신
    // ─────────────────────────────────────────────────────────────────

    private void RefreshPreview()
    {
        _previewEntries.Clear();
        _parsedGroups.Clear();
        _logText = "";

        if (!TryParseCsv(out _parsedGroups))
            return;

        LoadExistingSOs();

        foreach (var kv in _parsedGroups)
        {
            string id = kv.Key;
            _existingSOs.TryGetValue(id, out var so);

            _previewEntries.Add(new PreviewEntry
            {
                dropGroupId  = id,
                csvItemCount = kv.Value.Count,
                soExists     = so != null,
            });
        }

        Log($"미리보기 완료: 총 {_previewEntries.Count}개 그룹 파싱됨.");
        Repaint();
    }

    // ─────────────────────────────────────────────────────────────────
    // 임포트 실행
    // ─────────────────────────────────────────────────────────────────

    private void ExecuteImport()
    {
        int existing = CountExisting();
        int newCount = CountNew();

        bool confirmed = EditorUtility.DisplayDialog(
            "SO 업데이트 확인",
            $"총 {_previewEntries.Count}개 그룹을 처리합니다.\n" +
            $"  - 기존 SO 업데이트: {existing}개\n" +
            $"  - 신규 SO 생성:    {newCount}개\n\n" +
            "기존 SO 의 Items 는 CSV 데이터로 완전히 교체됩니다.\n계속하시겠습니까?",
            "실행",
            "취소"
        );

        if (!confirmed) return;

        _logText = "";

        // 저장 폴더 확보
        if (!AssetDatabase.IsValidFolder(SO_SAVE_PATH.TrimEnd('/')))
        {
            string parent = Path.GetDirectoryName(SO_SAVE_PATH.TrimEnd('/')).Replace('\\', '/');
            string folder = Path.GetFileName(SO_SAVE_PATH.TrimEnd('/'));
            AssetDatabase.CreateFolder(parent, folder);
            Log($"📁 폴더 생성: {SO_SAVE_PATH}");
        }

        int successCount = 0;

        foreach (var kv in _parsedGroups)
        {
            string         groupId = kv.Key;
            List<CsvRow>   rows    = kv.Value;
            bool           isNew   = false;

            if (!_existingSOs.TryGetValue(groupId, out StageSystem.DropTable so) || so == null)
            {
                so    = ScriptableObject.CreateInstance<StageSystem.DropTable>();
                isNew = true;
            }

            ApplyGroupToSO(so, groupId, rows);

            if (isNew)
            {
                string assetPath = $"{SO_SAVE_PATH}{groupId}_Config.asset";
                AssetDatabase.CreateAsset(so, assetPath);
                Log($"🆕 신규 생성: {assetPath}  ({rows.Count}개 아이템)");
            }
            else
            {
                EditorUtility.SetDirty(so);
                Log($"✅ 업데이트: {AssetDatabase.GetAssetPath(so)}  ({rows.Count}개 아이템)");
            }

            successCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Log($"\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Log($"완료: {successCount}개 처리됨");
        Log($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        RefreshPreview();
    }

    // ─────────────────────────────────────────────────────────────────
    // CSV 파싱
    // ─────────────────────────────────────────────────────────────────

    private bool TryParseCsv(out Dictionary<string, List<CsvRow>> groups)
    {
        groups = new Dictionary<string, List<CsvRow>>();

        if (_csvAsset == null)
        {
            Log("❌ CSV 파일이 선택되지 않았습니다.");
            return false;
        }

        string[] lines     = _csvAsset.text.Split('\n');
        int      errorCount = 0;

        for (int i = HEADER_ROW_COUNT; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] cols = line.Split(',');

            if (cols.Length < EXPECTED_COL_COUNT)
            {
                Log($"⚠️ {i + 1}행: 컬럼 수 부족 ({cols.Length}/{EXPECTED_COL_COUNT}) → 스킵");
                errorCount++;
                continue;
            }

            if (!TryParseRow(cols, i + 1, out CsvRow row))
            {
                errorCount++;
                continue;
            }

            if (!groups.ContainsKey(row.dropGroupId))
                groups[row.dropGroupId] = new List<CsvRow>();

            groups[row.dropGroupId].Add(row);
        }

        Log($"CSV 파싱 완료: {groups.Count}개 그룹, {errorCount}개 오류 행");
        return groups.Count > 0;
    }

    private bool TryParseRow(string[] cols, int lineNumber, out CsvRow row)
    {
        row = new CsvRow();

        try
        {
            row.dropGroupId = cols[COL_DROP_GROUP_ID].Trim();
            row.stageId     = cols[COL_STAGE_ID].Trim();
            row.itemId      = cols[COL_ITEM_ID].Trim();

            // GroupType enum 파싱
            if (!System.Enum.TryParse(cols[COL_GROUP_TYPE].Trim(), out DropGroupType groupType))
            {
                Log($"⚠️ {lineNumber}행: GroupType 파싱 실패 '{cols[COL_GROUP_TYPE].Trim()}' → STAGE_CLEAR_FIRST 사용");
                groupType = DropGroupType.STAGE_CLEAR_FIRST;
            }
            row.groupType = groupType;

            row.gold     = ParseInt(cols[COL_GOLD],     lineNumber, "Gold",     0);
            row.exp      = ParseInt(cols[COL_EXP],      lineNumber, "Exp",      0);
            row.amount   = ParseInt(cols[COL_AMOUNT],   lineNumber, "Amount",   1);
            row.dropRate = ParseFloat(cols[COL_DROP_RATE], lineNumber, "DropRate", 1f);

            if (string.IsNullOrEmpty(row.dropGroupId))
            {
                Log($"❌ {lineNumber}행: DropGroupID 가 비어 있습니다 → 스킵");
                return false;
            }

            if (string.IsNullOrEmpty(row.itemId))
            {
                Log($"⚠️ {lineNumber}행 ({row.dropGroupId}): ItemID 가 비어 있습니다 → 스킵");
                return false;
            }

            return true;
        }
        catch (System.Exception e)
        {
            Log($"❌ {lineNumber}행 파싱 예외: {e.Message} → 스킵");
            return false;
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // 기존 SO 로드
    // ─────────────────────────────────────────────────────────────────

    private void LoadExistingSOs()
    {
        _existingSOs.Clear();

        // StageSystem.DropTable 타입만 검색
        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { SO_SAVE_PATH });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            // _Config.asset 파일만 대상
            if (!path.EndsWith("_Config.asset")) continue;

            var so = AssetDatabase.LoadAssetAtPath<StageSystem.DropTable>(path);
            if (so == null) continue;

            // StageSystem.DropTable 은 DropGroupID 필드를 가짐 (대문자)
            if (!string.IsNullOrEmpty(so.DropGroupID))
                _existingSOs[so.DropGroupID] = so;
        }

        Log($"기존 SO 로드: {_existingSOs.Count}개");
    }

    // ─────────────────────────────────────────────────────────────────
    // CSV 데이터 → SO 적용
    // ─────────────────────────────────────────────────────────────────

    private void ApplyGroupToSO(StageSystem.DropTable so, string groupId, List<CsvRow> rows)
    {
        if (rows.Count == 0) return;

        CsvRow firstRow = rows[0];

        // 메타 정보 업데이트 (첫 행 기준)
        so.DropGroupID = groupId;
        so.StageID     = firstRow.stageId;
        so.GroupType   = firstRow.groupType;
        so.Gold        = firstRow.gold;
        so.Exp         = firstRow.exp;

        // 아이템 목록 전체 교체
        so.Items.Clear();

        foreach (var row in rows)
        {
            var itemData = new DropItemData(
                row.itemId,
                Mathf.Max(1, row.amount),
                Mathf.Clamp01(row.dropRate)
            );
            so.Items.Add(itemData);
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // 유틸
    // ─────────────────────────────────────────────────────────────────

    private int CountExisting() => _previewEntries.FindAll(e =>  e.soExists).Count;
    private int CountNew()      => _previewEntries.FindAll(e => !e.soExists).Count;

    private void Log(string message)
    {
        _logText += message + "\n";
        Repaint();
    }

    private static int ParseInt(string s, int line, string fieldName, int defaultVal)
    {
        if (int.TryParse(s.Trim(), out int v)) return v;
        Debug.LogWarning($"[StageRewardTableImporter] {line}행 {fieldName} 파싱 실패 '{s}' → 기본값 {defaultVal}");
        return defaultVal;
    }

    private static float ParseFloat(string s, int line, string fieldName, float defaultVal)
    {
        if (float.TryParse(
            s.Trim(),
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out float v)) return v;
        Debug.LogWarning($"[StageRewardTableImporter] {line}행 {fieldName} 파싱 실패 '{s}' → 기본값 {defaultVal}");
        return defaultVal;
    }
}
