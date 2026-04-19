using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StageSystem
{
    /// <summary>
    /// 스테이지 진행도 디버그 도구
    /// 개발용 치트 기능
    /// </summary>
    public class StageProgressDebugger : MonoBehaviour
    {
        [Header("디버그 설정")]
        public bool enableDebugKeys = true;
        
        [Header("치트 기능 (Context Menu 사용)")]
        // Header를 위한 더미 필드 - 사용하지 않으므로 warning disable
#pragma warning disable CS0414
        [SerializeField] private bool dummyField = false; // Header를 위한 더미 필드
#pragma warning restore CS0414
        
        [ContextMenu("모든 스테이지 해금")]
        public void UnlockAllStages()
        {
            var manager = FindObjectOfType<StageSystem.StageProgressManager>();
            if (manager == null)
            {
                Debug.LogWarning("⚠️ [StageProgressDebugger] StageProgressManager를 찾을 수 없습니다.");
                return;
            }
            
            // 모든 스테이지 설정 로드
            var configs = Resources.LoadAll<StageSystem.StageConfig>("Stages/Configs");
            foreach (var config in configs)
            {
                manager.UnlockStage(config.StageID);
            }
            
        }
        
        [ContextMenu("모든 스테이지 완료")]
        public void CompleteAllStages()
        {
            var manager = FindObjectOfType<StageSystem.StageProgressManager>();
            if (manager == null)
            {
                Debug.LogWarning("⚠️ [StageProgressDebugger] StageProgressManager를 찾을 수 없습니다.");
                return;
            }
            
            var configs = Resources.LoadAll<StageSystem.StageConfig>("Stages/Configs");
            foreach (var config in configs)
            {
                manager.UnlockStage(config.StageID);
                manager.CompleteStage(config.StageID, Random.Range(120, 300)); // 랜덤 클리어 시간
            }
            
        }
        
        [ContextMenu("진행도 초기화")]
        public void ResetAllProgress()
        {
            if (!Application.isEditor)
            {
                Debug.LogWarning("⚠️ [StageProgressDebugger] 에디터에서만 실행 가능합니다.");
                return;
            }
            
            // PlayerDataManager를 통해 빈 진행도로 업데이트
            var emptyProgresses = new List<StageProgress>();
            PlayerDataManager.Instance.UpdateStageProgresses(emptyProgresses);
            
        }
        
        [ContextMenu("STAGE_001만 해금")]
        public void ResetToStage001Only()
        {
            var progresses = new List<StageProgress>
            {
                new StageProgress("STAGE_001", true)
            };
            
            PlayerDataManager.Instance.UpdateStageProgresses(progresses);
            
        }
        
    }
}
