using UnityEngine;
using UnityEngine.UI;

namespace UI.Components
{
    /// <summary>
    /// 아이템 아이콘 등급 배경 프레임
    /// - 아이템 등급에 따라 배경 이미지 색상 자동 변경
    /// - 모든 아이템 UI에서 재사용 가능한 범용 컴포넌트
    /// 
    /// 사용법:
    /// 1. ItemIcon_Background GameObject에 이 컴포넌트 추가
    /// 2. backgroundImage 필드에 Image 컴포넌트 연결
    /// 3. SetGrade(ItemGrade) 호출하여 색상 변경
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class ItemIconGradeFrame : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("🖼️ Background Image")]
        [Tooltip("등급 색상을 적용할 배경 이미지 (비어있으면 자동으로 찾음)")]
        [SerializeField] private Image backgroundImage;
        
        [Header("🎨 현재 등급")]
        [Tooltip("현재 표시 중인 등급 (Inspector 미리보기용)")]
        [SerializeField] private ItemGrade currentGrade = ItemGrade.D;
        
        [Header("🔧 옵션")]
        [Tooltip("색상이 설정되지 않았을 때 사용할 기본 색상")]
        [SerializeField] private Color defaultColor = Color.white;
        
        [Header("📊 디버그")]
        [SerializeField] private bool showDebugLogs = false;
        
        #endregion
        
        #region Private Fields
        
        private bool isInitialized = false;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            Initialize();
        }
        
        private void OnEnable()
        {
            // 활성화될 때 현재 등급 색상 다시 적용
            if (isInitialized)
            {
                UpdateColor();
            }
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// 초기화
        /// </summary>
        private void Initialize()
        {
            if (isInitialized)
                return;
            
            // backgroundImage 자동 찾기
            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
                
                if (backgroundImage == null)
                {
                    Debug.LogError($"❌ [ItemIconGradeFrame] Image 컴포넌트를 찾을 수 없습니다: {gameObject.name}");
                }
                else
                {
                    if (showDebugLogs)
                        Debug.Log($"✅ [ItemIconGradeFrame] Image 컴포넌트 자동 찾기 완료: {gameObject.name}");
                }
            }
            
            isInitialized = true;
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// 등급 설정 및 색상 변경
        /// </summary>
        public void SetGrade(ItemGrade grade)
        {
            if (!isInitialized)
                Initialize();
            
            currentGrade = grade;
            UpdateColor();
            
            if (showDebugLogs)
                Debug.Log($"🎨 [ItemIconGradeFrame] 등급 설정: {grade} → {gameObject.name}");
        }
        
        /// <summary>
        /// 등급과 색상을 직접 설정 (오버라이드)
        /// </summary>
        public void SetGradeWithCustomColor(ItemGrade grade, Color customColor)
        {
            if (!isInitialized)
                Initialize();
            
            currentGrade = grade;
            
            if (backgroundImage != null)
            {
                backgroundImage.color = customColor;
                
                if (showDebugLogs)
                    Debug.Log($"🎨 [ItemIconGradeFrame] 커스텀 색상 설정: {grade} → {customColor}");
            }
        }
        
        /// <summary>
        /// 현재 등급 가져오기
        /// </summary>
        public ItemGrade GetCurrentGrade()
        {
            return currentGrade;
        }
        
        /// <summary>
        /// 색상 초기화 (기본 색상으로)
        /// </summary>
        public void ResetColor()
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = defaultColor;
                
                if (showDebugLogs)
                    Debug.Log($"🔄 [ItemIconGradeFrame] 색상 초기화: {defaultColor}");
            }
        }
        
        /// <summary>
        /// 알파값 설정 (어둡게/밝게 처리용)
        /// </summary>
        public void SetAlpha(float alpha)
        {
            if (backgroundImage != null)
            {
                Color color = backgroundImage.color;
                color.a = alpha;
                backgroundImage.color = color;
                
                if (showDebugLogs)
                    Debug.Log($"🎨 [ItemIconGradeFrame] 알파값 설정: {alpha}");
            }
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// 색상 업데이트
        /// </summary>
        private void UpdateColor()
        {
            if (backgroundImage == null)
            {
                if (showDebugLogs)
                    Debug.LogWarning($"⚠️ [ItemIconGradeFrame] backgroundImage가 null입니다: {gameObject.name}");
                return;
            }
            
            // ItemGradeColorManager에서 색상 가져오기
            if (ItemGradeColorManager.Instance == null)
            {
                if (showDebugLogs)
                    Debug.LogWarning($"⚠️ [ItemIconGradeFrame] ItemGradeColorManager.Instance가 null입니다");
                
                // 기본 색상 사용
                backgroundImage.color = defaultColor;
                return;
            }
            
            // 등급별 색상 적용
            Color gradeColor = ItemGradeColorManager.Instance.GetGradeColor(currentGrade);
            
            // 색상이 유효하지 않으면 기본 색상 사용
            if (gradeColor == Color.clear)
            {
                gradeColor = defaultColor;
            }
            
            backgroundImage.color = gradeColor;
            
            if (showDebugLogs)
            {
                Debug.Log($"🎨 [ItemIconGradeFrame] 색상 업데이트: {currentGrade} → {gradeColor} ({gameObject.name})");
            }
        }
        
        #endregion
        
        #region Editor
        
        /// <summary>
        /// Inspector 미리보기 (에디터 전용)
        /// </summary>
        private void OnValidate()
        {
            #if UNITY_EDITOR
            // 플레이 모드에서만 실시간 업데이트
            if (Application.isPlaying && isInitialized)
            {
                UpdateColor();
            }
            
            // 에디터 모드에서는 backgroundImage만 자동 찾기
            if (!Application.isPlaying && backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
            }
            #endif
        }
        
        /// <summary>
        /// Gizmo 표시 (디버그용)
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (backgroundImage != null)
            {
                Gizmos.color = backgroundImage.color;
                Gizmos.DrawWireCube(transform.position, new Vector3(50f, 50f, 0f));
            }
        }
        
        #endregion
    }
}

