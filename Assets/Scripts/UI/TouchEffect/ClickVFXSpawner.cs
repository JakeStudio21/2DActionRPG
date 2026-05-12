using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace UI.TouchEffect
{
    /// <summary>
    /// 클릭/터치 위치에 UI 파티클 이펙트를 생성
    /// 홍보 영상 촬영 시 클릭 위치를 시각적으로 표시하기 위한 용도
    /// 
    /// 위치: LobbyCanvas 하위 빈 오브젝트에 부착
    /// 조건: Canvas Render Mode = Screen Space - Overlay
    /// 입력: New Input System 기반 (PC 마우스 + 모바일 멀티터치 지원)
    /// </summary>
    public class ClickVFXSpawner : MonoBehaviour
    {
        [Header("VFX 설정")]
        [SerializeField] private GameObject clickVFXPrefab;

        [Header("Canvas 참조")]
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private RectTransform vfxParent;

        [Header("활성화 제어")]
        [SerializeField] private bool isActive = true;

        private RectTransform canvasRect;

        private void Awake()
        {
            if (targetCanvas == null)
                targetCanvas = GetComponentInParent<Canvas>();

            if (targetCanvas != null)
                canvasRect = targetCanvas.GetComponent<RectTransform>();

            if (vfxParent == null && targetCanvas != null)
                vfxParent = canvasRect;
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }

        private void Update()
        {
            if (!isActive) return;
            if (clickVFXPrefab == null || canvasRect == null) return;

            // 터치스크린이 연결된 경우(모바일): 터치 입력 처리
            // 멀티터치 지원 — 각 손가락 위치에 개별 이펙트 생성
            if (Touchscreen.current != null)
            {
                foreach (var touch in Touch.activeTouches)
                {
                    if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                        SpawnVFX(touch.screenPosition);
                }
            }
            // 터치스크린 없음(PC): 마우스 클릭 처리
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                SpawnVFX(Mouse.current.position.ReadValue());
            }
        }

        private void SpawnVFX(Vector2 screenPos)
        {
            // Screen Space - Overlay: camera 파라미터는 null
            bool isConverted = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPos,
                null,
                out Vector2 localPoint
            );

            if (!isConverted) return;

            GameObject vfx = Instantiate(clickVFXPrefab, vfxParent);
            vfx.GetComponent<RectTransform>().anchoredPosition = localPoint;

            // 다른 UI 요소 위에 표시
            vfx.transform.SetAsLastSibling();
        }

        /// <summary>
        /// 외부에서 클릭 이펙트 활성/비활성 제어
        /// </summary>
        public void SetActive(bool active)
        {
            isActive = active;
        }
    }
}
