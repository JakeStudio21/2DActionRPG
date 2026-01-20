using System.Collections.Generic;
using UnityEngine;

namespace LevelDesign
{
    /// <summary>
    /// 모듈 프리팹의 테마별 매핑 정보를 관리하는 ScriptableObject
    /// 예: Md_Sample_Room_08x12_A → Md_Forest_Room_08x12_A 매핑
    /// </summary>
    [CreateAssetMenu(fileName = "NewThemeMapping", menuName = "Level Design/Module Theme Mapping", order = 1)]
    public class ModuleThemeMapping : ScriptableObject
    {
        [Header("테마 정보")]
        [Tooltip("대상 테마 이름 (예: Forest, Desert, Volcano)")]
        public string themeName = "Forest";

        [Tooltip("원본 테마 이름 (예: Sample)")]
        public string sourceTheme = "Sample";

        [Header("프리팹 매핑")]
        [Tooltip("원본 프리팹 이름 → 대상 프리팹 매핑")]
        public List<ModulePrefabMapping> mappings = new List<ModulePrefabMapping>();

        [Header("네이밍 규칙")]
        [Tooltip("모듈 프리팹 이름 접두사 (예: Md_)")]
        public string modulePrefix = "Md_";

        /// <summary>
        /// 원본 프리팹 이름으로 대상 프리팹을 찾습니다.
        /// </summary>
        public GameObject GetTargetPrefab(string sourcePrefabName)
        {
            foreach (var mapping in mappings)
            {
                if (mapping.sourcePrefabName == sourcePrefabName)
                {
                    return mapping.targetPrefab;
                }
            }

            Debug.LogWarning($"[ModuleThemeMapping] '{sourcePrefabName}'에 대한 매핑을 찾을 수 없습니다. (테마: {themeName})");
            return null;
        }

        /// <summary>
        /// 원본 프리팹으로 대상 프리팹을 찾습니다.
        /// </summary>
        public GameObject GetTargetPrefab(GameObject sourcePrefab)
        {
            if (sourcePrefab == null)
                return null;

            return GetTargetPrefab(sourcePrefab.name);
        }

        /// <summary>
        /// 매핑이 존재하는지 확인합니다.
        /// </summary>
        public bool HasMapping(string sourcePrefabName)
        {
            foreach (var mapping in mappings)
            {
                if (mapping.sourcePrefabName == sourcePrefabName)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 모든 매핑된 원본 프리팹 이름을 반환합니다.
        /// </summary>
        public List<string> GetAllSourcePrefabNames()
        {
            List<string> names = new List<string>();
            foreach (var mapping in mappings)
            {
                if (!string.IsNullOrEmpty(mapping.sourcePrefabName))
                {
                    names.Add(mapping.sourcePrefabName);
                }
            }
            return names;
        }

        /// <summary>
        /// 매핑 개수를 반환합니다.
        /// </summary>
        public int GetMappingCount()
        {
            return mappings.Count;
        }

        /// <summary>
        /// 유효한 매핑 개수를 반환합니다 (targetPrefab이 null이 아닌 것).
        /// </summary>
        public int GetValidMappingCount()
        {
            int count = 0;
            foreach (var mapping in mappings)
            {
                if (mapping.targetPrefab != null)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Inspector에서 매핑 정보를 확인하기 위한 헬퍼 메서드
        /// </summary>
        [ContextMenu("Print Mapping Info")]
        public void PrintMappingInfo()
        {
            Debug.Log($"=== {themeName} Theme Mapping ===");
            Debug.Log($"Source Theme: {sourceTheme}");
            Debug.Log($"Mappings: {GetValidMappingCount()} / {GetMappingCount()}");
            
            foreach (var mapping in mappings)
            {
                string status = mapping.targetPrefab != null ? "✓" : "✗";
                Debug.Log($"{status} {mapping.sourcePrefabName} → {(mapping.targetPrefab ? mapping.targetPrefab.name : "NULL")}");
            }
        }
    }

    /// <summary>
    /// 개별 프리팹 매핑 정보
    /// </summary>
    [System.Serializable]
    public class ModulePrefabMapping
    {
        [Tooltip("원본 프리팹 이름 (예: Md_Sample_Room_08x12_A)")]
        public string sourcePrefabName;

        [Tooltip("대상 프리팹 (예: Md_Forest_Room_08x12_A)")]
        public GameObject targetPrefab;

        public ModulePrefabMapping()
        {
            sourcePrefabName = "";
            targetPrefab = null;
        }

        public ModulePrefabMapping(string sourceName, GameObject target)
        {
            sourcePrefabName = sourceName;
            targetPrefab = target;
        }
    }
}

