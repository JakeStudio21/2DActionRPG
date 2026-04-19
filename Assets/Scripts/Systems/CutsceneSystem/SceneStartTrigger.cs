using UnityEngine;
using UnityEngine.SceneManagement;

namespace CutsceneSystem
{
    /// <summary>
    /// 씬 시작 시 컷신 자동 재생 트리거
    /// 인트로/챕터 시작 컷신용
    /// </summary>
    public class SceneStartTrigger : MonoBehaviour
    {
        [Header("=== 컷신 설정 ===")]
        [Tooltip("재생할 컷신 ID")]
        [SerializeField] private string cutsceneId = "";
        
        [Tooltip("재생할 컷신 데이터 (직접 할당 가능)")]
        [SerializeField] private CutsceneData cutsceneData;
        
        [Tooltip("지연 시간 (초) - 씬 로드 후 몇 초 후 재생")]
        [SerializeField] private float delay = 0.5f;
        
        [Tooltip("한 번만 재생 (이미 재생한 경우 스킵)")]
        [SerializeField] private bool playOnce = true;
        
        [Header("=== 디버그 ===")]
        // 재생 여부 추적
        private static System.Collections.Generic.HashSet<string> playedCutscenes = new System.Collections.Generic.HashSet<string>();
        private bool hasPlayed = false;
        
        private void Start()
        {
            // 씬 로드 완료 후 컷신 재생
            if (delay > 0)
            {
                Invoke(nameof(PlayCutscene), delay);
            }
            else
            {
                PlayCutscene();
            }
        }
        
        /// <summary>
        /// 컷신 재생
        /// </summary>
        private void PlayCutscene()
        {
            // 한 번만 재생 체크
            if (playOnce)
            {
                string key = GetCutsceneKey();
                if (playedCutscenes.Contains(key))
                {
                    return;
                }
                
                playedCutscenes.Add(key);
            }
            
            // CutsceneManager 확인
            if (CutsceneManager.Instance == null)
            {
                Debug.LogError("[SceneStartTrigger] CutsceneManager를 찾을 수 없습니다!");
                return;
            }
            
            // 컷신 재생
            if (cutsceneData != null)
            {
                // 직접 할당된 데이터 사용
                CutsceneManager.Instance.PlayCutscene(cutsceneData);
                
            }
            else if (!string.IsNullOrEmpty(cutsceneId))
            {
                // ID로 재생
                CutsceneManager.Instance.PlayCutscene(cutsceneId);
                
            }
            else
            {
                Debug.LogWarning("[SceneStartTrigger] 컷신 ID 또는 CutsceneData가 설정되지 않았습니다!");
            }
        }
        
        /// <summary>
        /// 컷신 키 생성 (씬 이름 + 컷신 ID)
        /// </summary>
        private string GetCutsceneKey()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            string id = cutsceneData != null ? cutsceneData.cutsceneId : cutsceneId;
            return $"{sceneName}_{id}";
        }
        
        /// <summary>
        /// 재생 기록 초기화 (테스트용)
        /// </summary>
        [ContextMenu("재생 기록 초기화")]
        private void ClearPlayedRecords()
        {
            playedCutscenes.Clear();
            hasPlayed = false;
        }
    }
}
