using UnityEngine;
using Systems;

/// <summary>
/// 아이템 등급별 색상 관리자 (싱글톤)
/// - ItemGradeColorConfig 로드 및 관리
/// - 모든 UI에서 색상 요청 시 중앙 집중식으로 제공
/// </summary>
public class ItemGradeColorManager : MonoBehaviour
{
    #region Singleton
    
    private static ItemGradeColorManager _instance;
    
    public static ItemGradeColorManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // 씬에서 찾기
                _instance = FindObjectOfType<ItemGradeColorManager>();
                
                // 없으면 자동 생성
                if (_instance == null)
                {
                    GameObject go = new GameObject("ItemGradeColorManager");
                    _instance = go.AddComponent<ItemGradeColorManager>();
                    DontDestroyOnLoad(go);
                    
                    Debug.Log("✨ [ItemGradeColorManager] 자동 생성 및 초기화");
                }
            }
            return _instance;
        }
    }
    
    #endregion
    
    #region Fields
    
    [Header("📁 Config")]
    [SerializeField] private ItemGradeColorConfig config;
    
    [Header("📊 디버그")]
    [SerializeField] private bool showDebugLogs = false;
    
    private bool isInitialized = false;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        // 싱글톤 설정
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
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
        
        // Config 자동 로드 (Inspector에서 할당 안 되어있으면)
        if (config == null)
        {
            config = Resources.Load<ItemGradeColorConfig>("Config/ItemGradeColorConfig");
            
            if (config == null)
            {
                Debug.LogError("❌ [ItemGradeColorManager] ItemGradeColorConfig를 찾을 수 없습니다! " +
                              "Resources/Config/ItemGradeColorConfig.asset 파일을 생성해주세요.");
            }
            else
            {
                if (showDebugLogs)
                    Debug.Log("✅ [ItemGradeColorManager] Config 자동 로드 완료");
            }
        }
        
        isInitialized = true;
        
        if (showDebugLogs)
            Debug.Log("🎨 [ItemGradeColorManager] 초기화 완료");
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// 등급별 색상 가져오기
    /// </summary>
    public Color GetGradeColor(ItemGrade grade)
    {
        if (!isInitialized)
            Initialize();
        
        if (config == null)
        {
            Debug.LogWarning($"⚠️ [ItemGradeColorManager] Config가 null입니다. 기본 색상(흰색) 반환: {grade}");
            return Color.white;
        }
        
        Color color = config.GetGradeColor(grade);
        
        if (showDebugLogs)
            Debug.Log($"🎨 [ItemGradeColorManager] {grade} 등급 색상 반환: {color}");
        
        return color;
    }
    
    /// <summary>
    /// 등급 색상이 설정되어 있는지 확인
    /// </summary>
    public bool HasGradeColor(ItemGrade grade)
    {
        if (!isInitialized)
            Initialize();
        
        if (config == null)
            return false;
        
        return config.HasGradeColor(grade);
    }
    
    /// <summary>
    /// Config 재로드 (런타임 변경용)
    /// </summary>
    public void ReloadConfig()
    {
        config = Resources.Load<ItemGradeColorConfig>("Config/ItemGradeColorConfig");
        
        if (config != null)
        {
            Debug.Log("🔄 [ItemGradeColorManager] Config 재로드 완료");
        }
        else
        {
            Debug.LogError("❌ [ItemGradeColorManager] Config 재로드 실패");
        }
    }
    
    /// <summary>
    /// 등급별 색상 설정 (런타임 변경용, 선택사항)
    /// </summary>
    public void SetGradeColor(ItemGrade grade, Color color)
    {
        if (config == null)
        {
            Debug.LogWarning("⚠️ [ItemGradeColorManager] Config가 null입니다. 색상 설정 실패");
            return;
        }
        
        // Config의 gradeColors 배열에서 해당 등급 찾아서 변경
        for (int i = 0; i < config.gradeColors.Length; i++)
        {
            if (config.gradeColors[i].grade == grade)
            {
                config.gradeColors[i].color = color;
                
                if (showDebugLogs)
                    Debug.Log($"🎨 [ItemGradeColorManager] {grade} 등급 색상 변경: {color}");
                
                return;
            }
        }
        
        Debug.LogWarning($"⚠️ [ItemGradeColorManager] {grade} 등급을 찾을 수 없습니다.");
    }
    
    #endregion
    
    #region Debug
    
    /// <summary>
    /// 모든 등급 색상 출력 (디버그용)
    /// </summary>
    [ContextMenu("Print All Grade Colors")]
    public void PrintAllGradeColors()
    {
        if (config == null)
        {
            Debug.LogWarning("⚠️ Config가 null입니다.");
            return;
        }
        
        Debug.Log("═══════════════════════════════════════");
        Debug.Log("🎨 아이템 등급별 색상 목록");
        Debug.Log("═══════════════════════════════════════");
        
        foreach (var entry in config.gradeColors)
        {
            Debug.Log($"{entry.grade}: RGB({entry.color.r:F2}, {entry.color.g:F2}, {entry.color.b:F2})");
        }
        
        Debug.Log("═══════════════════════════════════════");
    }
    
    #endregion
}

