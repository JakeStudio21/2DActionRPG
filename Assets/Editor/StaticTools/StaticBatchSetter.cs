using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace EditorTools.StaticTools
{
    /// <summary>
    /// 선택된 GameObject와 모든 자식에 대해 Static 플래그를 일괄 설정하는 Editor Tool
    /// 드로우콜 감소를 위한 Static Batching 지원
    /// </summary>
    public static class StaticBatchSetter
    {
        #region Context Menu Items

        /// <summary>
        /// Batching Static만 활성화 (드로우콜 감소용 - 권장)
        /// </summary>
        [MenuItem("GameObject/Static Tools/Set Batching Static (Children)", false, 0)]
        private static void SetBatchingStaticChildren()
        {
            if (!ValidateSelection()) return;

            int count = SetStaticFlagsRecursive(Selection.activeGameObject, StaticEditorFlags.BatchingStatic, true);
            
            if (count > 0)
            {
                Debug.Log($"✅ [StaticTools] {count}개 오브젝트에 Batching Static 적용 완료!");
            }
        }

        /// <summary>
        /// Everything Static 활성화 (완전 정적 오브젝트용)
        /// </summary>
        [MenuItem("GameObject/Static Tools/Set Everything Static (Children)", false, 1)]
        private static void SetEverythingStaticChildren()
        {
            if (!ValidateSelection()) return;

            // 확인 대화상자
            bool confirm = EditorUtility.DisplayDialog(
                "Everything Static 설정",
                $"'{Selection.activeGameObject.name}'과 모든 자식 오브젝트를 Everything Static으로 설정합니다.\n\n" +
                "⚠️ 주의: NavMesh 베이킹과 Lightmap에 영향을 줄 수 있습니다.\n\n" +
                "계속하시겠습니까?",
                "예",
                "취소"
            );

            if (!confirm) return;

            // Everything Static = 모든 플래그 조합
            StaticEditorFlags allFlags = StaticEditorFlags.BatchingStatic |
                                        StaticEditorFlags.NavigationStatic |
                                        StaticEditorFlags.OccluderStatic |
                                        StaticEditorFlags.OccludeeStatic |
                                        StaticEditorFlags.ContributeGI |
                                        StaticEditorFlags.ReflectionProbeStatic;
            
            int count = SetStaticFlagsRecursive(Selection.activeGameObject, allFlags, true);
            
            if (count > 0)
            {
                Debug.Log($"✅ [StaticTools] {count}개 오브젝트에 Everything Static 적용 완료!");
            }
        }

        /// <summary>
        /// Static 플래그 전체 제거
        /// </summary>
        [MenuItem("GameObject/Static Tools/Clear Static (Children)", false, 2)]
        private static void ClearStaticChildren()
        {
            if (!ValidateSelection()) return;

            int count = SetStaticFlagsRecursive(Selection.activeGameObject, 0, false);
            
            if (count > 0)
            {
                Debug.Log($"✅ [StaticTools] {count}개 오브젝트의 Static 플래그 제거 완료!");
            }
        }

        #endregion

        #region Validation

        /// <summary>
        /// Context Menu 항목 활성화 조건 (GameObject 선택됨)
        /// </summary>
        [MenuItem("GameObject/Static Tools/Set Batching Static (Children)", true)]
        [MenuItem("GameObject/Static Tools/Set Everything Static (Children)", true)]
        [MenuItem("GameObject/Static Tools/Clear Static (Children)", true)]
        private static bool ValidateSelectionMenu()
        {
            return Selection.activeGameObject != null;
        }

        /// <summary>
        /// 선택 검증
        /// </summary>
        private static bool ValidateSelection()
        {
            if (Selection.activeGameObject == null)
            {
                EditorUtility.DisplayDialog(
                    "오브젝트 미선택",
                    "Hierarchy에서 GameObject를 선택해주세요.",
                    "확인"
                );
                return false;
            }

            return true;
        }

        #endregion

        #region Core Logic

        /// <summary>
        /// 재귀적으로 모든 자식에 Static 플래그 설정
        /// </summary>
        /// <param name="root">루트 GameObject</param>
        /// <param name="flags">설정할 Static 플래그</param>
        /// <param name="enable">활성화(true) 또는 비활성화(false)</param>
        /// <returns>처리된 오브젝트 개수</returns>
        private static int SetStaticFlagsRecursive(GameObject root, StaticEditorFlags flags, bool enable)
        {
            if (root == null) return 0;

            int count = 0;

            // Progress Bar 표시 준비
            EditorUtility.DisplayProgressBar("Static 플래그 설정 중...", "오브젝트 수집 중...", 0f);

            try
            {
                // 모든 자식 Transform 가져오기 (자기 자신 포함)
                Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true); // includeInactive = true
                
                // Undo 그룹 시작
                Undo.SetCurrentGroupName("Set Static Flags");
                int undoGroup = Undo.GetCurrentGroup();

                for (int i = 0; i < allTransforms.Length; i++)
                {
                    GameObject obj = allTransforms[i].gameObject;

                    // Progress Bar 업데이트
                    float progress = (float)i / allTransforms.Length;
                    EditorUtility.DisplayProgressBar(
                        "Static 플래그 설정 중...",
                        $"{obj.name} ({i + 1}/{allTransforms.Length})",
                        progress
                    );

                    // 예외 케이스 체크
                    if (ShouldSkipObject(obj))
                    {
                        Debug.LogWarning($"⚠️ [StaticTools] {obj.name}: Static 설정을 권장하지 않는 컴포넌트가 있습니다. 스킵됨.");
                        continue;
                    }

                    // Undo 기록
                    Undo.RecordObject(obj, "Set Static Flags");

                    // Static 플래그 설정
                    if (enable)
                    {
                        // 기존 플래그에 추가
                        StaticEditorFlags currentFlags = GameObjectUtility.GetStaticEditorFlags(obj);
                        GameObjectUtility.SetStaticEditorFlags(obj, currentFlags | flags);
                    }
                    else
                    {
                        // 모든 플래그 제거
                        GameObjectUtility.SetStaticEditorFlags(obj, 0);
                    }

                    // Dirty 마킹
                    EditorUtility.SetDirty(obj);

                    count++;
                }

                // Undo 그룹 종료
                Undo.CollapseUndoOperations(undoGroup);

                // Prefab 변경사항 처리
                HandlePrefabModifications(root);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            return count;
        }

        #endregion

        #region Exception Handling

        /// <summary>
        /// Static 설정을 스킵해야 하는 오브젝트 체크
        /// </summary>
        private static bool ShouldSkipObject(GameObject obj)
        {
            // Particle System: 움직이므로 Static 비권장
            if (obj.GetComponent<ParticleSystem>() != null)
                return true;

            // UI Canvas: Static 불필요
            if (obj.GetComponent<Canvas>() != null)
                return true;

            // Animator가 있는 경우: 애니메이션되는 오브젝트
            Animator animator = obj.GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
                return true;

            // Character/Enemy/Player 태그: 움직이는 오브젝트
            if (obj.CompareTag("Player") || obj.CompareTag("Enemy") || obj.CompareTag("Character"))
                return true;

            return false;
        }

        #endregion

        #region Prefab Handling

        /// <summary>
        /// Prefab 변경사항 처리
        /// </summary>
        private static void HandlePrefabModifications(GameObject root)
        {
            // Prefab Instance인지 확인
            if (PrefabUtility.IsPartOfPrefabInstance(root))
            {
                // 변경사항 기록 (Prefab Override)
                PrefabUtility.RecordPrefabInstancePropertyModifications(root);

                // Prefab에 적용할지 확인 (선택사항)
                bool applyToPrefab = EditorUtility.DisplayDialog(
                    "Prefab 변경사항",
                    $"'{root.name}'은 Prefab Instance입니다.\n\n" +
                    "변경사항을 Prefab Asset에 적용하시겠습니까?\n" +
                    "(적용하지 않으면 현재 씬에만 적용됩니다)",
                    "Prefab에 적용",
                    "씬에만 적용"
                );

                if (applyToPrefab)
                {
                    // Prefab Asset에 적용
                    GameObject prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(root);
                    if (prefabAsset != null)
                    {
                        PrefabUtility.ApplyPrefabInstance(root, InteractionMode.UserAction);
                        Debug.Log($"✅ [StaticTools] Prefab Asset에 변경사항 적용 완료: {prefabAsset.name}");
                    }
                }
            }
        }

        #endregion

        #region Utility

        /// <summary>
        /// 현재 Static 플래그 확인 (디버깅용)
        /// </summary>
        [MenuItem("GameObject/Static Tools/Show Current Static Flags", false, 20)]
        private static void ShowCurrentStaticFlags()
        {
            if (!ValidateSelection()) return;

            GameObject obj = Selection.activeGameObject;
            StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(obj);

            string message = $"'{obj.name}' Static 플래그:\n\n";
            
            if (flags == 0)
            {
                message += "❌ Static 플래그 없음";
            }
            else
            {
                if ((flags & StaticEditorFlags.BatchingStatic) != 0)
                    message += "✅ Batching Static\n";
                if ((flags & StaticEditorFlags.NavigationStatic) != 0)
                    message += "✅ Navigation Static\n";
                if ((flags & StaticEditorFlags.OccluderStatic) != 0)
                    message += "✅ Occluder Static\n";
                if ((flags & StaticEditorFlags.OccludeeStatic) != 0)
                    message += "✅ Occludee Static\n";
                if ((flags & StaticEditorFlags.ContributeGI) != 0)
                    message += "✅ Lightmap Static\n";
                if ((flags & StaticEditorFlags.ReflectionProbeStatic) != 0)
                    message += "✅ Reflection Probe Static\n";
            }

            EditorUtility.DisplayDialog("Static 플래그 확인", message, "확인");
        }

        [MenuItem("GameObject/Static Tools/Show Current Static Flags", true)]
        private static bool ValidateShowCurrentStaticFlags()
        {
            return Selection.activeGameObject != null;
        }

        #endregion
    }
}

