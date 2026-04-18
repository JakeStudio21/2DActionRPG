using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 스프라이트 시트의 모든 서브 스프라이트 피봇을 일괄 변경하는 에디터 도구
/// Tools → Sprites → Batch Change Pivot
/// </summary>
public class BatchSpritePivotChanger : EditorWindow
{
    private List<Texture2D> targetTextures = new List<Texture2D>();
    private Vector2 customPivot = new Vector2(0.5f, 0.5f);
    private Vector2 scrollPosition;
    
    [MenuItem("Tools/Sprites/Batch Change Pivot")]
    public static void ShowWindow()
    {
        var window = GetWindow<BatchSpritePivotChanger>("Batch Sprite Pivot");
        window.minSize = new Vector2(400, 350);
        window.Show();
        
        // 창이 열릴 때 선택된 텍스처들 자동 로드
        window.LoadSelectedTextures();
    }

    /// <summary>
    /// Project 창에서 선택된 텍스처들 로드
    /// </summary>
    private void LoadSelectedTextures()
    {
        targetTextures.Clear();
        
        // Selection에서 Texture2D만 필터링
        var textures = Selection.GetFiltered<Texture2D>(SelectionMode.Assets);
        
        foreach (var texture in textures)
        {
            // Multiple Sprite Mode인 것만 추가
            string path = AssetDatabase.GetAssetPath(texture);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            
            if (importer != null && importer.spriteImportMode == SpriteImportMode.Multiple)
            {
                targetTextures.Add(texture);
            }
        }
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // 타이틀
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Batch Sprite Pivot Changer", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Multiple Sprite Mode로 슬라이스된 스프라이트 시트의 모든 서브 스프라이트 피봇을 한번에 변경합니다.\n" +
            "💡 Tip: Project 창에서 여러 스프라이트 시트를 선택한 후 이 도구를 실행하면 자동으로 로드됩니다.",
            MessageType.Info);
        
        GUILayout.Space(10);
        
        // 스프라이트 시트 선택
        EditorGUILayout.LabelField("1. 스프라이트 시트 목록", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🔄 선택된 텍스처 다시 로드", GUILayout.Height(25)))
        {
            LoadSelectedTextures();
        }
        if (GUILayout.Button("❌ 목록 비우기", GUILayout.Height(25)))
        {
            targetTextures.Clear();
        }
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(5);
        
        // 텍스처 리스트 표시
        ShowTextureList();
        
        GUILayout.Space(10);
        
        // 피봇 입력
        EditorGUILayout.LabelField("2. 새 피봇 설정", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Custom Pivot:", GUILayout.Width(100));
        EditorGUILayout.LabelField("X:", GUILayout.Width(20));
        customPivot.x = EditorGUILayout.FloatField(customPivot.x, GUILayout.Width(60));
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Y:", GUILayout.Width(20));
        customPivot.y = EditorGUILayout.FloatField(customPivot.y, GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();
        
        // 프리셋 버튼들
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("프리셋:", GUILayout.Width(100));
        if (GUILayout.Button("Bottom (0.5, 0)", GUILayout.Width(120)))
            customPivot = new Vector2(0.5f, 0f);
        if (GUILayout.Button("Center (0.5, 0.5)", GUILayout.Width(120)))
            customPivot = new Vector2(0.5f, 0.5f);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(100);
        if (GUILayout.Button("Bottom-Left (0, 0)", GUILayout.Width(120)))
            customPivot = new Vector2(0f, 0f);
        if (GUILayout.Button("Top (0.5, 1)", GUILayout.Width(120)))
            customPivot = new Vector2(0.5f, 1f);
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        
        // 피봇 시각화
        DrawPivotPreview();
        
        GUILayout.Space(10);
        
        // 적용 버튼
        EditorGUILayout.LabelField("3. 일괄 적용", EditorStyles.boldLabel);
        
        GUI.enabled = targetTextures.Count > 0;
        
        if (GUILayout.Button($"✅ {targetTextures.Count}개 스프라이트 시트에 피봇 적용", GUILayout.Height(40)))
        {
            ApplyPivotToAllSprites();
        }
        
        GUI.enabled = true;
        
        GUILayout.Space(10);
        
        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 텍스처 리스트 표시
    /// </summary>
    private void ShowTextureList()
    {
        if (targetTextures.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "선택된 스프라이트 시트가 없습니다.\n\n" +
                "1. Project 창에서 스프라이트 시트들을 선택\n" +
                "2. '선택된 텍스처 다시 로드' 버튼 클릭",
                MessageType.Warning);
            return;
        }
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"📋 선택된 스프라이트 시트: {targetTextures.Count}개", EditorStyles.boldLabel);
        
        GUILayout.Space(5);
        
        int totalSpriteCount = 0;
        
        for (int i = 0; i < targetTextures.Count; i++)
        {
            var texture = targetTextures[i];
            if (texture == null) continue;
            
            string path = AssetDatabase.GetAssetPath(texture);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            
            if (importer == null) continue;
            
            int spriteCount = importer.spritesheet.Length;
            totalSpriteCount += spriteCount;
            
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            
            // 텍스처 아이콘
            GUILayout.Label(EditorGUIUtility.ObjectContent(texture, typeof(Texture2D)).image, 
                GUILayout.Width(40), GUILayout.Height(40));
            
            // 정보
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField($"{i+1}. {texture.name}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"   크기: {texture.width}x{texture.height} | 서브 스프라이트: {spriteCount}개");
            
            if (spriteCount > 0)
            {
                var firstSprite = importer.spritesheet[0];
                EditorGUILayout.LabelField($"   현재 피봇: ({firstSprite.pivot.x:F2}, {firstSprite.pivot.y:F2})");
            }
            EditorGUILayout.EndVertical();
            
            // 삭제 버튼
            if (GUILayout.Button("X", GUILayout.Width(30), GUILayout.Height(40)))
            {
                targetTextures.RemoveAt(i);
                i--;
                continue;
            }
            
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2);
        }
        
        GUILayout.Space(5);
        EditorGUILayout.LabelField($"총 변경될 서브 스프라이트 개수: {totalSpriteCount}개", EditorStyles.boldLabel);
        
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 피봇 시각화 미리보기
    /// </summary>
    private void DrawPivotPreview()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("피봇 미리보기", EditorStyles.boldLabel);
        
        GUILayout.Space(5);
        
        Rect previewRect = GUILayoutUtility.GetRect(200, 200);
        
        // 배경
        EditorGUI.DrawRect(previewRect, new Color(0.2f, 0.2f, 0.2f, 1f));
        
        // 스프라이트 영역 (흰색 사각형)
        Rect spriteRect = new Rect(
            previewRect.x + 50,
            previewRect.y + 50,
            previewRect.width - 100,
            previewRect.height - 100
        );
        EditorGUI.DrawRect(spriteRect, new Color(0.8f, 0.8f, 0.8f, 1f));
        
        // 피봇 위치 (빨간 점)
        Vector2 pivotPos = new Vector2(
            spriteRect.x + spriteRect.width * customPivot.x,
            spriteRect.y + spriteRect.height * (1f - customPivot.y) // Y축 반전
        );
        
        Rect pivotRect = new Rect(pivotPos.x - 5, pivotPos.y - 5, 10, 10);
        EditorGUI.DrawRect(pivotRect, Color.red);
        
        // 십자선
        EditorGUI.DrawRect(new Rect(pivotPos.x - 15, pivotPos.y - 1, 30, 2), Color.red);
        EditorGUI.DrawRect(new Rect(pivotPos.x - 1, pivotPos.y - 15, 2, 30), Color.red);
        
        // 좌표 표시
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.normal.textColor = Color.white;
        labelStyle.alignment = TextAnchor.MiddleCenter;
        
        GUI.Label(
            new Rect(previewRect.x, previewRect.y + previewRect.height + 5, previewRect.width, 20),
            $"Pivot: ({customPivot.x:F2}, {customPivot.y:F2})",
            labelStyle
        );
        
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 모든 스프라이트에 피봇 적용 (여러 텍스처 지원)
    /// </summary>
    private void ApplyPivotToAllSprites()
    {
        if (targetTextures.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "스프라이트 시트를 선택해주세요.", "OK");
            return;
        }
        
        // 총 서브 스프라이트 개수 계산
        int totalSpriteCount = 0;
        foreach (var texture in targetTextures)
        {
            string path = AssetDatabase.GetAssetPath(texture);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.spriteImportMode == SpriteImportMode.Multiple)
            {
                totalSpriteCount += importer.spritesheet.Length;
            }
        }
        
        // 확인 다이얼로그
        bool confirm = EditorUtility.DisplayDialog(
            "피봇 일괄 변경 확인",
            $"📊 변경 대상:\n" +
            $"   - 스프라이트 시트: {targetTextures.Count}개\n" +
            $"   - 총 서브 스프라이트: {totalSpriteCount}개\n\n" +
            $"🎯 새 피봇: ({customPivot.x:F2}, {customPivot.y:F2})\n\n" +
            $"모든 스프라이트의 피봇을 변경하시겠습니까?",
            "변경",
            "취소"
        );
        
        if (!confirm)
            return;
        
        // 진행률 표시
        int processedTextures = 0;
        int processedSprites = 0;
        
        foreach (var texture in targetTextures)
        {
            if (texture == null) continue;
            
            string path = AssetDatabase.GetAssetPath(texture);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            
            if (importer == null) continue;
            
            if (importer.spriteImportMode != SpriteImportMode.Multiple)
            {
                Debug.LogWarning($"[BatchSpritePivotChanger] {texture.name}는 Multiple Sprite Mode가 아니므로 건너뜁니다.");
                continue;
            }
            
            // 피봇 변경
            var spriteSheet = importer.spritesheet;
            int spriteCount = spriteSheet.Length;
            
            for (int i = 0; i < spriteSheet.Length; i++)
            {
                spriteSheet[i].pivot = customPivot;
                spriteSheet[i].alignment = (int)SpriteAlignment.Custom; // Custom Pivot 사용
            }
            
            importer.spritesheet = spriteSheet;
            
            // 변경사항 저장
            EditorUtility.SetDirty(importer);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            
            processedTextures++;
            processedSprites += spriteCount;
        }
        
        // 결과 표시
        EditorUtility.DisplayDialog(
            "✅ 변경 완료",
            $"피봇 변경이 완료되었습니다!\n\n" +
            $"📊 결과:\n" +
            $"   - 처리된 스프라이트 시트: {processedTextures}개\n" +
            $"   - 변경된 서브 스프라이트: {processedSprites}개\n\n" +
            $"🎯 새 피봇: ({customPivot.x:F2}, {customPivot.y:F2})",
            "확인"
        );
    }
}

