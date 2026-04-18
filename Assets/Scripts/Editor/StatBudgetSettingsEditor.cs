using UnityEditor;
using UnityEngine;

/// <summary>
/// StatBudgetSettings용 커스텀 Inspector
/// CSV 파싱 버튼 제공
/// </summary>
[CustomEditor(typeof(StatBudgetSettings))]
public class StatBudgetSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 기본 Inspector 그리기
        DrawDefaultInspector();

        GUILayout.Space(20);
        
        StatBudgetSettings settings = (StatBudgetSettings)target;

        // 📥 CSV 파싱 버튼
        if (GUILayout.Button("📥 Parse CSV Data", GUILayout.Height(40)))
        {
            settings.ParseAllCSVData();
            EditorUtility.SetDirty(settings); // 변경사항 저장
            AssetDatabase.SaveAssets();
        }

        GUILayout.Space(10);

        // 도움말
        EditorGUILayout.HelpBox(
            "1. 위의 4개 CSV 파일을 할당하세요.\n" +
            "2. 'Parse CSV Data' 버튼을 클릭하여 데이터를 파싱하세요.\n" +
            "3. 파싱된 데이터는 Dictionary에 저장됩니다.",
            MessageType.Info
        );
    }
}

