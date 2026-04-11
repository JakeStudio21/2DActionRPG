using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StageSystem
{
    /// <summary>
    /// 스테이지 종료 포털
    /// 플레이어가 접촉하면 스테이지 완료 처리
    /// </summary>
    public class StageExitPortal : MonoBehaviour
    {
        [Header("포털 설정")]
        public bool isActive = false; // 기본적으로 비활성화 (승리 조건 달성 시 활성화)
        public float interactionRadius = 1.5f;
        public LayerMask playerLayerMask = 1 << 3; // Player 레이어
        
        [Header("비주얼")]
        public GameObject portalEffect;
        public Color activeColor = Color.green;
        public Color inactiveColor = Color.gray;
        
        [Header("오디오")]
        public AudioClip portalOpenSound;
        public AudioClip portalEnterSound;
        
        // 이벤트
        public System.Action OnPortalEntered;
        
        private SpriteRenderer spriteRenderer;
        private AudioSource audioSource;
        private bool playerInRange = false;
        
        private void Start()
        {
            InitializePortal();
        }
        
        /// <summary>
        /// 포털 초기화
        /// </summary>
        private void InitializePortal()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            audioSource = GetComponent<AudioSource>();
            
            // 초기 상태 설정
            UpdatePortalVisual();
            
            // 포털 이펙트 비활성화
            if (portalEffect != null)
            {
                portalEffect.SetActive(false);
            }
        }
        
        /// <summary>
        /// 포털 활성화 (승리 조건 달성 시 호출)
        /// </summary>
        public void ActivatePortal()
        {
            if (isActive) return;
            
            isActive = true;
            UpdatePortalVisual();
            
            // 포털 이펙트 활성화
            if (portalEffect != null)
            {
                portalEffect.SetActive(true);
            }
            
            // 포털 열림 사운드 재생
            if (audioSource != null && portalOpenSound != null)
            {
                audioSource.PlayOneShot(portalOpenSound);
            }
            
            Debug.Log("🚪 [StageExitPortal] 포털 활성화됨");
        }
        
        /// <summary>
        /// 포털 비주얼 업데이트
        /// </summary>
        private void UpdatePortalVisual()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = isActive ? activeColor : inactiveColor;
            }
        }
        
        /// <summary>
        /// 플레이어 감지
        /// </summary>
        private void Update()
        {
            if (!isActive) return;
            
            CheckPlayerInRange();
            HandlePlayerInput();
        }
        
        /// <summary>
        /// 플레이어 범위 내 체크
        /// </summary>
        private void CheckPlayerInRange()
        {
            Collider2D playerCollider = Physics2D.OverlapCircle(
                transform.position, 
                interactionRadius, 
                playerLayerMask
            );
            
            bool wasInRange = playerInRange;
            playerInRange = (playerCollider != null);
            
            // 범위 진입/이탈 시 UI 업데이트 등 처리
            if (!wasInRange && playerInRange)
            {
                OnPlayerEnterRange();
            }
            else if (wasInRange && !playerInRange)
            {
                OnPlayerExitRange();
            }
        }
        
        /// <summary>
        /// 플레이어 입력 처리
        /// </summary>
        private void HandlePlayerInput()
        {
            if (!playerInRange) return;

#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Space))
                EnterPortal();
#endif
        }
        
        /// <summary>
        /// 포털 입장 처리
        /// </summary>
        private void EnterPortal()
        {
            if (!isActive) return;
            
            // 포털 입장 사운드 재생
            if (audioSource != null && portalEnterSound != null)
            {
                audioSource.PlayOneShot(portalEnterSound);
            }
            
            OnPortalEntered?.Invoke();
            
            Debug.Log("🏆 [StageExitPortal] 플레이어가 포털에 진입함");
            
            // FSMStageController에 스테이지 완료 알림
            var stageController = FindObjectOfType<FSMStageController>();
            if (stageController != null)
            {
                // stageController.CompleteStage(); // 필요 시 구현
            }
        }
        
        /// <summary>
        /// 플레이어 범위 진입
        /// </summary>
        private void OnPlayerEnterRange()
        {
            Debug.Log("🚪 [StageExitPortal] 플레이어가 포털 근처에 진입");
            // UI 표시 등 처리
        }
        
        /// <summary>
        /// 플레이어 범위 이탈
        /// </summary>
        private void OnPlayerExitRange()
        {
            Debug.Log("🚪 [StageExitPortal] 플레이어가 포털에서 이탈");
            // UI 숨김 등 처리
        }
        
        /// <summary>
        /// 기즈모 그리기
        /// </summary>
        private void OnDrawGizmos()
        {
            // 상호작용 범위 표시
            Gizmos.color = isActive ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
            
            // 포털 중심 표시
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, 0.2f);
        }
        
        /// <summary>
        /// 선택 시 기즈모 표시
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
    }
}
