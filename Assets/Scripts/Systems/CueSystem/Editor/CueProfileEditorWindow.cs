using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace CueSystem.Editor
{
    public class CueProfileEditorWindow : EditorWindow
    {
        private string metaCSVPath = "Assets/StreamingAssets/CueData/CueProfile_Meta.csv";
        private string entriesCSVPath = "Assets/StreamingAssets/CueData/CueProfile_Entries.csv";
        
        // 🆕 VFX/SFX CSV 경로 필드 추가
        private string vfxCSVPath = "Assets/StreamingAssets/CueData/VFX_Catalog.csv";
        private string sfxCSVPath = "Assets/StreamingAssets/CueData/SFX_Catalog.csv";
        
        public static void ShowWindow()
        {
            GetWindow<CueProfileEditorWindow>("Cue Profile Importer");
        }
        
        void OnGUI()
        {
            GUILayout.Label("Cue Profile CSV Importer", EditorStyles.boldLabel);
            
            EditorGUILayout.Space();
            
            // 필수 파일들
            GUILayout.Label("Required Files:", EditorStyles.boldLabel);
            
            // Meta CSV
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Meta CSV:", GUILayout.Width(80));
            metaCSVPath = EditorGUILayout.TextField(metaCSVPath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFilePanel("Select Meta CSV", "Assets", "csv");
                if (!string.IsNullOrEmpty(path))
                {
                    metaCSVPath = FileUtil.GetProjectRelativePath(path);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // Entries CSV
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Entries CSV:", GUILayout.Width(80));
            entriesCSVPath = EditorGUILayout.TextField(entriesCSVPath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFilePanel("Select Entries CSV", "Assets", "csv");
                if (!string.IsNullOrEmpty(path))
                {
                    entriesCSVPath = FileUtil.GetProjectRelativePath(path);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // 선택사항 파일들
            GUILayout.Label("Optional Files (for VFX/SFX catalogs):", EditorStyles.boldLabel);
            
            // VFX CSV
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("VFX CSV:", GUILayout.Width(80));
            vfxCSVPath = EditorGUILayout.TextField(vfxCSVPath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFilePanel("Select VFX CSV", "Assets", "csv");
                if (!string.IsNullOrEmpty(path))
                {
                    vfxCSVPath = FileUtil.GetProjectRelativePath(path);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // SFX CSV
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("SFX CSV:", GUILayout.Width(80));
            sfxCSVPath = EditorGUILayout.TextField(sfxCSVPath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFilePanel("Select SFX CSV", "Assets", "csv");
                if (!string.IsNullOrEmpty(path))
                {
                    sfxCSVPath = FileUtil.GetProjectRelativePath(path);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // 버튼들
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Validate CSV"))
            {
                ValidateCSV();
            }
            
            if (GUILayout.Button("Import CSV"))
            {
                ImportCSV();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // 도움말
            EditorGUILayout.HelpBox(
                "필수: Meta CSV와 Entries CSV 파일을 설정하세요.\n" +
                "선택: VFX/SFX CSV 파일이 있으면 완전한 카탈로그가 생성됩니다.\n" +
                "VFX/SFX 파일이 없어도 기본 임포트는 가능합니다.", 
                MessageType.Info
            );
        }
        
        private void ValidateCSV()
        {
            // 기존 ValidateCSV 로직 유지
            if (!File.Exists(metaCSVPath))
            {
                Debug.LogError("🔴 Meta CSV 파일을 찾을 수 없습니다: " + metaCSVPath);
                return;
            }
            
            if (!File.Exists(entriesCSVPath))
            {
                Debug.LogError("🔴 Entries CSV 파일을 찾을 수 없습니다: " + entriesCSVPath);
                return;
            }
            
            // VFX/SFX는 선택사항이므로 경고만
            if (!File.Exists(vfxCSVPath))
            {
                Debug.LogWarning("⚠️ VFX CSV 파일을 찾을 수 없습니다: " + vfxCSVPath);
            }
            
            if (!File.Exists(sfxCSVPath))
            {
                Debug.LogWarning("⚠️ SFX CSV 파일을 찾을 수 없습니다: " + sfxCSVPath);
            }
            
        }
        
        private void ImportCSV()
        {
            if (!File.Exists(metaCSVPath) || !File.Exists(entriesCSVPath))
            {
                Debug.LogError("🔴 필수 CSV 파일이 존재하지 않습니다. 경로를 확인하세요.");
                return;
            }
            
            // VFX/SFX CSV는 선택사항
            string vfxPath = File.Exists(vfxCSVPath) ? vfxCSVPath : null;
            string sfxPath = File.Exists(sfxCSVPath) ? sfxCSVPath : null;
            
            if (vfxPath == null)
                Debug.LogWarning("⚠️ VFX CSV 파일이 없습니다. VFX 카탈로그가 비어있을 수 있습니다.");
            if (sfxPath == null)
                Debug.LogWarning("⚠️ SFX CSV 파일이 없습니다. SFX 카탈로그가 비어있을 수 있습니다.");
            
            var result = CueProfileImporter.ImportFromCSV(metaCSVPath, entriesCSVPath, vfxPath, sfxPath);
            
            
            foreach (var error in result.Errors)
            {
                Debug.LogError($"🔴 {error}");
            }
            
            foreach (var warning in result.Warnings)
            {
                Debug.LogWarning($"⚠️ {warning}");
            }
            
            foreach (var created in result.CreatedAssets)
            {
            }
            
            foreach (var updated in result.UpdatedAssets)
            {
            }
            
            if (result.Success)
            {
                AssetDatabase.Refresh();
            }
        }
    }
}
