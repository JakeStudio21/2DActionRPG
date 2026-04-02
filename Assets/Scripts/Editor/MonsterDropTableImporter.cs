using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > RewardSystem > Monster Drop Table Importer
///
/// DropTableData.csv 를 읽어 Assets/Resources/DropTables/*.asset 을 업데이트합니다.
/// - 기존 SO 발견 → dropEntries 전체 교체 (GUID 유지)
/// - 기존 SO 없음  → 신규 .asset 생성
/// </summary>
public class MonsterDropTableImporter : EditorWindow
{
    // ─────────────────────────────────────────────────────────────────
    // 상수
    // ─────────────────────────────────────────────────────────────────

    private const string SO_SAVE_PATH    = "Assets/Resources/DropTables/";
    private const string WINDOW_TITLE    = "Monster Drop Table Importer";
    private const int    HEADER_ROW_COUNT = 1; // CSV 첫 번째 줄은 헤더

    // CSV 컬럼 인덱스
    private const int COL_DROP_GROUP_ID    = 0;
    private const int COL_DESCRIPTION      = 1;
    private const int COL_DROP_POLICY      = 2;
    private const int COL_PICK_COUNT       = 3;
    private const int COL_ITEM_ID          = 4;
    private const int COL_WEIGHT           = 5;
    private const int COL_CHANCE           = 6;
    private const int COL_MIN_QTY          = 7;
    private const int COL_MAX_QTY          = 8;
    private const int COL_MIN_STAGE_LEVEL  = 9;
    private const int COL_MAX_STAGE_LEVEL  = 10;
    private const int COL_IS_GUARANTEED    = 11;
    private const int EXPECTED_COL_COUNT   = 12;

    // ─────────────────────────────────────────────────────────────────
    // 내부 데이터 구조
    // ─────────────────────────────────────────────────────────────────

    /// <summary>CSV 한 행에 해당하는 파싱 결과</summary>
    private class CsvRow
    {
        public string dropGroupId;
        public string description;
        public DropPolicy dropPolicy;
        public int    pickCount;
        public string itemId;
        public float  weight;
        public float  chance;
        public int    minQty;
        public int    maxQty;
        public int    minStageLevel;
        public int    maxStageLevel;
        public bool   isGuaranteed;
    }

    /// <summary>미리보기 한 항목</summary>
    private class PreviewEntry
    {
        public string dropGroupId;
        public int    csvEntryCount;
        public bool   soExists;
        public string soPath;
    }

    // ─────────────────────────────────────────────────────────────────
    // UI 상태
    // ─────────────────────────────────────────────────────────────────

    private TextAsset _csvAsset;
    private Vector2   _previewScroll;
    private Vector2   _logScroll;
    private string    _logText = "";

    private List<PreviewEntry>                       _previewEntries = new List<PreviewEntry>();
    private Dictionary<string, List<CsvRow>>         _parsedGroups   = new Dictionary<string, List<CsvRow>>();
    private Dictionary<string, DropTable>            _existingSOs    = new Dictionary<string, DropTable>();

    // ─────────────────────────────────────────────────────────────────
    // 메뉴 등록
    // ─────────────────────────────────────────────────────────────────

    [MenuItem("Tools/RewardSystem/Monster Drop Table Importer")]
    public static void OpenWindow()
    {
        var window = GetWindow<MonsterDropTableImporter>(WINDOW_TITLE);
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
        EditorGUILayout.LabelField("Monster Drop Table Importer", titleStyle, GUILayout.Height(24));
        EditorGUILayout.LabelField("CSV → ScriptableObject 자동 반영 도구", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.Space(4);
        DrawSeparator();
    }

    private void DrawCsvField()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("📄 CSV 파일", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        _csvAsset = (TextAsset)EditorGUILayout.ObjectField(
            "Drop Table CSV",
            _csvAsset,
            typeof(TextAsset),
            false
        );
        // CSV가 교체되면 미리보기 초기화
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
                    ? $"기존 SO 발견 ({entry.csvEntryCount}개 엔트리로 덮어쓰기)"
                    : $"SO 없음 → 신규 생성 ({entry.csvEntryCount}개 엔트리)";

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

        // 1. CSV 파싱
        if (!TryParseCsv(out _parsedGroups))
            return;

        // 2. 기존 SO 로드
        LoadExistingSOs();

        // 3. 미리보기 엔트리 구성
        foreach (var kv in _parsedGroups)
        {
            string id = kv.Key;
            _existingSOs.TryGetValue(id, out DropTable so);

            _previewEntries.Add(new PreviewEntry
            {
                dropGroupId   = id,
                csvEntryCount = kv.Value.Count,
                soExists      = so != null,
                soPath        = so != null ? AssetDatabase.GetAssetPath(so) : "",
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
            "기존 SO 의 dropEntries 는 CSV 데이터로 완전히 교체됩니다.\n계속하시겠습니까?",
            "실행",
            "취소"
        );

        if (!confirmed) return;

        _logText = "";

        // 저장 폴더 확보
        if (!AssetDatabase.IsValidFolder(SO_SAVE_PATH.TrimEnd('/')))
        {
            AssetDatabase.CreateFolder(
                Path.GetDirectoryName(SO_SAVE_PATH.TrimEnd('/')),
                Path.GetFileName(SO_SAVE_PATH.TrimEnd('/'))
            );
            Log($"📁 폴더 생성: {SO_SAVE_PATH}");
        }

        int successCount = 0;
        int failCount    = 0;

        foreach (var kv in _parsedGroups)
        {
            string          groupId = kv.Key;
            List<CsvRow>    rows    = kv.Value;
            bool            isNew   = false;

            // SO 탐색 또는 신규 생성
            if (!_existingSOs.TryGetValue(groupId, out DropTable so) || so == null)
            {
                so    = ScriptableObject.CreateInstance<DropTable>();
                isNew = true;
            }

            // 데이터 주입
            ApplyGroupToSO(so, groupId, rows);

            if (isNew)
            {
                string assetPath = $"{SO_SAVE_PATH}{groupId}.asset";
                AssetDatabase.CreateAsset(so, assetPath);
                Log($"🆕 신규 생성: {assetPath}  ({rows.Count}개 엔트리)");
            }
            else
            {
                EditorUtility.SetDirty(so);
                Log($"✅ 업데이트: {AssetDatabase.GetAssetPath(so)}  ({rows.Count}개 엔트리)");
            }

            successCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Log($"\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Log($"완료: 성공 {successCount}개 / 실패 {failCount}개");
        Log($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        // 미리보기 재갱신
        RefreshPreview();
    }

    // ─────────────────────────────────────────────────────────────────
    // 핵심 로직: CSV 파싱
    // ─────────────────────────────────────────────────────────────────

    private bool TryParseCsv(out Dictionary<string, List<CsvRow>> groups)
    {
        groups = new Dictionary<string, List<CsvRow>>();

        if (_csvAsset == null)
        {
            Log("❌ CSV 파일이 선택되지 않았습니다.");
            return false;
        }

        string[] lines = _csvAsset.text.Split('\n');
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
            row.description = cols[COL_DESCRIPTION].Trim();
            row.itemId      = cols[COL_ITEM_ID].Trim();

            // DropPolicy enum 파싱
            if (!System.Enum.TryParse(cols[COL_DROP_POLICY].Trim(), out DropPolicy policy))
            {
                Log($"⚠️ {lineNumber}행: DropPolicy 파싱 실패 '{cols[COL_DROP_POLICY].Trim()}' → RollEachWithChance 사용");
                policy = DropPolicy.RollEachWithChance;
            }
            row.dropPolicy = policy;

            row.pickCount      = ParseInt(cols[COL_PICK_COUNT],  lineNumber, "PickCount",      1);
            row.weight         = ParseFloat(cols[COL_WEIGHT],    lineNumber, "Weight",         0f);
            row.chance         = ParseFloat(cols[COL_CHANCE],    lineNumber, "Chance",         0f);
            row.minQty         = ParseInt(cols[COL_MIN_QTY],     lineNumber, "MinQty",         1);
            row.maxQty         = ParseInt(cols[COL_MAX_QTY],     lineNumber, "MaxQty",         1);
            row.minStageLevel  = ParseInt(cols[COL_MIN_STAGE_LEVEL], lineNumber, "MinStageLevel", 1);
            row.maxStageLevel  = ParseInt(cols[COL_MAX_STAGE_LEVEL], lineNumber, "MaxStageLevel", 0);
            row.isGuaranteed   = cols[COL_IS_GUARANTEED].Trim().ToUpper() == "TRUE";

            // 기본 유효성 검사
            if (string.IsNullOrEmpty(row.dropGroupId))
            {
                Log($"❌ {lineNumber}행: DropGroupId 가 비어 있습니다 → 스킵");
                return false;
            }

            if (string.IsNullOrEmpty(row.itemId))
            {
                Log($"⚠️ {lineNumber}행 ({row.dropGroupId}): ItemId 가 비어 있습니다 → 스킵");
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
    // 핵심 로직: 기존 SO 로드
    // ─────────────────────────────────────────────────────────────────

    private void LoadExistingSOs()
    {
        _existingSOs.Clear();

        // Items/DropTable 타입(네임스페이스 없음)만 검색
        string[] guids = AssetDatabase.FindAssets("t:DropTable", new[] { SO_SAVE_PATH });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var so = AssetDatabase.LoadAssetAtPath<DropTable>(path);

            if (so == null) continue;

            // StageSystem.DropTable 과 Items DropTable 혼재 시 필터
            // Items/DropTable.cs 는 dropGroupId 필드를 가짐
            if (!string.IsNullOrEmpty(so.dropGroupId))
                _existingSOs[so.dropGroupId] = so;
        }

        Log($"기존 SO 로드: {_existingSOs.Count}개");
    }

    // ─────────────────────────────────────────────────────────────────
    // 핵심 로직: CSV 데이터 → SO 적용
    // ─────────────────────────────────────────────────────────────────

    private void ApplyGroupToSO(DropTable so, string groupId, List<CsvRow> rows)
    {
        if (rows.Count == 0) return;

        CsvRow firstRow = rows[0];

        // SO 메타 정보 업데이트
        so.dropGroupId  = groupId;
        so.description  = firstRow.description;
        so.dropPolicy   = firstRow.dropPolicy;
        so.pickCount    = Mathf.Max(1, firstRow.pickCount);

        // 기존 엔트리 전체 교체
        so.dropEntries.Clear();

        foreach (var row in rows)
        {
            var entry = new DropEntry
            {
                itemId        = row.itemId,
                rarity        = ItemRarity.Common, // CSV에 없음 → 기본값 유지
                weight        = row.weight,
                chance        = Mathf.Clamp01(row.chance),
                minQuantity   = Mathf.Max(1, row.minQty),
                maxQuantity   = Mathf.Max(row.minQty, row.maxQty),
                minStageLevel = Mathf.Max(1, row.minStageLevel),
                maxStageLevel = row.maxStageLevel,
                isGuaranteed  = row.isGuaranteed,
            };

            so.dropEntries.Add(entry);
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // 유틸
    // ─────────────────────────────────────────────────────────────────

    private int CountExisting() =>
        _previewEntries.FindAll(e => e.soExists).Count;

    private int CountNew() =>
        _previewEntries.FindAll(e => !e.soExists).Count;

    private void Log(string message)
    {
        _logText += message + "\n";
        Repaint();
    }

    private static int ParseInt(string s, int line, string fieldName, int defaultVal)
    {
        if (int.TryParse(s.Trim(), out int v)) return v;
        Debug.LogWarning($"[MonsterDropTableImporter] {line}행 {fieldName} 파싱 실패 '{s}' → 기본값 {defaultVal} 사용");
        return defaultVal;
    }

    private static float ParseFloat(string s, int line, string fieldName, float defaultVal)
    {
        if (float.TryParse(s.Trim(),
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out float v)) return v;
        Debug.LogWarning($"[MonsterDropTableImporter] {line}행 {fieldName} 파싱 실패 '{s}' → 기본값 {defaultVal} 사용");
        return defaultVal;
    }
}
