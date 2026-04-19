using UnityEngine;
using System.Collections;

namespace CutsceneSystem
{
    /// <summary>
    /// 플레이어가 특정 영역에 진입하면 컷신 재생 트리거
    /// 튜토리얼 / 챕터 중간 컷신용
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class AreaTrigger : MonoBehaviour
    {
        [Header("=== 컷신 설정 ===")]
        [Tooltip("재생할 컷신 ID")]
        [SerializeField] private string cutsceneId = "";
        
        [Tooltip("재생할 컷신 데이터 (직접 할당 가능)")]
        [SerializeField] private CutsceneData cutsceneData;
        
        [Tooltip("한 번만 재생 (이미 재생한 경우 스킵)")]
        [SerializeField] private bool playOnce = true;
        
        [Tooltip("트리거 활성화 여부")]
        [SerializeField] private bool isActive = true;
        
        [Header("=== 플레이어 인식 ===")]
        [Tooltip("인식할 플레이어 태그")]
        [SerializeField] private string playerTag = "Player";
        
        [Header("=== 타이밍 설정 ===")]
        [Tooltip("진입 후 컷신 재생까지 지연 시간 (초)")]
        [SerializeField] private float delay = 0f;
        
        [Header("=== 시각적 요소 ===")]
        [Tooltip("트리거 완료 후 비활성화할 GameObject (Sprite, Effect 등)")]
        [SerializeField] private GameObject visualElement;
        
        [Tooltip("컷신 재생 후 시각적 요소 비활성화 여부")]
        [SerializeField] private bool hideVisualOnComplete = true;
        
        [Header("=== 디버그 ===")]
        // 재생 여부 추적
        private bool hasPlayed = false;
        private Collider2D triggerCollider;
        private Coroutine delayCoroutine;
        private bool waitingForCutsceneEnd = false;
        
        private void Awake()
        {
            // Collider2D 참조 가져오기
            triggerCollider = GetComponent<Collider2D>();
            
            if (triggerCollider == null)
            {
                Debug.LogError("[AreaTrigger] Collider2D가 없습니다! Collider2D 컴포넌트를 먼저 추가해주세요.");
                return;
            }
            
            // Trigger로 설정 (이미 설정되어 있을 수 있음)
            triggerCollider.isTrigger = true;
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isActive)
                return;
            
            // 플레이어 확인
            if (!IsPlayer(other))
                return;
            
            // 한 번만 재생 체크
            if (playOnce && hasPlayed)
            {
                return;
            }
            
            // 지연 시간이 있으면 코루틴으로 재생
            if (delay > 0f)
            {
                if (delayCoroutine != null)
                    StopCoroutine(delayCoroutine);
                
                delayCoroutine = StartCoroutine(DelayedPlayCutscene());
            }
            else
            {
                // 즉시 재생
                PlayCutscene();
            }
        }
        
        /// <summary>
        /// 플레이어인지 확인
        /// </summary>
        private bool IsPlayer(Collider2D other)
        {
            // 1. 설정된 태그로 확인
            if (!string.IsNullOrEmpty(playerTag) && other.CompareTag(playerTag))
                return true;
            
            // 2. PlayerController 컴포넌트 확인 (Fallback)
            if (other.GetComponent<PlayerController>() != null)
                return true;
            
            return false;
        }
        
        /// <summary>
        /// 지연 후 컷신 재생
        /// </summary>
        private IEnumerator DelayedPlayCutscene()
        {
            yield return new WaitForSeconds(delay);
            
            PlayCutscene();
            delayCoroutine = null;
        }
        
        /// <summary>
        /// 컷신 재생
        /// </summary>
        private void PlayCutscene()
        {
            // CutsceneManager 확인
            if (CutsceneManager.Instance == null)
            {
                Debug.LogError("[AreaTrigger] CutsceneManager를 찾을 수 없습니다!");
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
                Debug.LogWarning("[AreaTrigger] 컷신 ID 또는 CutsceneData가 설정되지 않았습니다!");
                return;
            }
            
            // 재생 완료 표시
            hasPlayed = true;
            
            // 한 번만 재생인 경우 트리거 비활성화
            if (playOnce)
            {
                isActive = false;
            }
            
            // 시각적 요소는 컷신 완료 후 숨김 (이벤트 사용)
            if (hideVisualOnComplete && visualElement != null)
            {
                waitingForCutsceneEnd = true;
                CutsceneManager.Instance.OnCutsceneEnd += OnCutsceneCompleted;
                
            }
        }
        
        /// <summary>
        /// 컷신 완료 콜백
        /// </summary>
        private void OnCutsceneCompleted(string completedCutsceneId)
        {
            if (!waitingForCutsceneEnd)
                return;
            
            // 우리가 재생한 컷신인지 확인
            bool isOurCutscene = false;
            
            if (!string.IsNullOrEmpty(cutsceneId) && completedCutsceneId == cutsceneId)
            {
                isOurCutscene = true;
            }
            else if (cutsceneData != null && completedCutsceneId == cutsceneData.cutsceneId)
            {
                isOurCutscene = true;
            }
            
            if (isOurCutscene && visualElement != null)
            {
                // 시각적 요소 비활성화
                visualElement.SetActive(false);
                
                // 이벤트 구독 해제
                CutsceneManager.Instance.OnCutsceneEnd -= OnCutsceneCompleted;
                waitingForCutsceneEnd = false;
            }
        }
        
        private void OnDestroy()
        {
            // 코루틴 정리
            if (delayCoroutine != null)
            {
                StopCoroutine(delayCoroutine);
                delayCoroutine = null;
            }
            
            // 이벤트 구독 해제
            if (waitingForCutsceneEnd && CutsceneManager.Instance != null)
            {
                CutsceneManager.Instance.OnCutsceneEnd -= OnCutsceneCompleted;
            }
        }
        
        /// <summary>
        /// 트리거 활성화/비활성화
        /// </summary>
        public void SetActive(bool active)
        {
            isActive = active;
        }
        
        /// <summary>
        /// 재생 기록 초기화 (테스트용)
        /// </summary>
        [ContextMenu("재생 기록 초기화")]
        private void ResetPlayed()
        {
            hasPlayed = false;
            isActive = true;
            
            // 시각적 요소 다시 활성화
            if (visualElement != null)
            {
                visualElement.SetActive(true);
            }
            
        }
        
        /// <summary>
        /// 수동으로 컷신 재생 (외부 호출용)
        /// </summary>
        public void TriggerManually()
        {
            if (!isActive)
            {
                Debug.LogWarning("[AreaTrigger] 트리거가 비활성화 상태입니다.");
                return;
            }
            
            if (playOnce && hasPlayed)
            {
                Debug.LogWarning("[AreaTrigger] 이미 재생한 컷신입니다.");
                return;
            }
            
            PlayCutscene();
        }
        
        /// <summary>
        /// Gizmos로 트리거 영역 표시
        /// </summary>
        private void OnDrawGizmos()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col == null)
                return;
            
            Gizmos.color = isActive ? Color.green : Color.gray;
            
            if (col is BoxCollider2D boxCol)
            {
                Gizmos.DrawWireCube(transform.position + (Vector3)boxCol.offset, boxCol.size);
            }
            else if (col is CircleCollider2D circleCol)
            {
                Gizmos.DrawWireSphere(transform.position + (Vector3)circleCol.offset, circleCol.radius);
            }
        }
    }
}
