using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tutorial 씬 전용 게임 관리자
/// - GameManager 대체 (Tutorial에서만 사용)
/// - DontDestroyOnLoad 없음 (씬 종료 시 자동 파괴)
/// - TutorialPlayerData 관리
/// - Tutorial 플로우 제어
/// </summary>
public class TutorialManager : MonoBehaviour
{
    private static TutorialManager instance;
    public static TutorialManager Instance => instance;
    
    [Header("Tutorial 설정")]
    [SerializeField] private PlayerType defaultPlayerClass = PlayerType.Assasin;
    
    [Header("기본 무기 데이터")]
    [SerializeField] private EquipmentData bowEquipment;   // Assasin용
    [SerializeField] private EquipmentData swordEquipment; // Warrior용
    [SerializeField] private EquipmentData staffEquipment; // Wizard용
    
    [Header("기본 스킬 데이터 (Tutorial용)")]
    [SerializeField] private ActiveSkillData defaultSkill1; // 슬롯 0 — 예: SKILL_MULTISHOT
    [SerializeField] private ActiveSkillData defaultSkill2; // 슬롯 1 — 예: SKILL_FOCUSE_STRIKE
    
    // Tutorial 플레이어 데이터
    private TutorialPlayerData tutorialPlayerData;
    public TutorialPlayerData TutorialPlayerData => tutorialPlayerData;
    
    // Tutorial 상태
    private bool isTutorialCompleted = false;
    public bool IsTutorialCompleted => isTutorialCompleted;
    
    void Awake()
    {
        // Singleton 설정 (DontDestroyOnLoad 없음!)
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        
        // 🎯 중요: DontDestroyOnLoad 사용 안 함!
        // Tutorial 씬 종료 시 자동으로 파괴됨
        
    }
    
    void Start()
    {
        InitializeTutorial();
    }
    
    /// <summary>
    /// Tutorial 초기화
    /// </summary>
    private void InitializeTutorial()
    {
        // Tutorial 플레이어 데이터 생성
        tutorialPlayerData = new TutorialPlayerData(defaultPlayerClass);
        
        // 기본 무기 할당
        EquipmentData defaultWeapon = GetWeaponForClass(defaultPlayerClass);
        if (defaultWeapon != null)
        {
            tutorialPlayerData.SetDefaultWeapon(defaultWeapon);
            
        }
        else
        {
            Debug.LogError($"[TutorialManager] {defaultPlayerClass} 클래스의 기본 무기를 찾을 수 없습니다!");
        }
        
        // 기본 스킬 할당
        tutorialPlayerData.tutorialSkill1 = defaultSkill1;
        tutorialPlayerData.tutorialSkill2 = defaultSkill2;
        
        
            Dbg.Log($"[TutorialManager] Tutorial 초기화 완료: {tutorialPlayerData}");
        
        // BGM 재생
        PlayTutorialBGM();
    }
    
    /// <summary>
    /// 클래스에 맞는 무기 반환
    /// </summary>
    private EquipmentData GetWeaponForClass(PlayerType playerClass)
    {
        switch (playerClass)
        {
            case PlayerType.Warrior:
                return swordEquipment;
            case PlayerType.Assasin:
                return bowEquipment;
            case PlayerType.Wizard:
                return staffEquipment;
            default:
                Debug.LogWarning($"[TutorialManager] 알 수 없는 클래스: {playerClass}");
                return bowEquipment; // 기본값
        }
    }
    
    /// <summary>
    /// Tutorial BGM 재생
    /// </summary>
    private void PlayTutorialBGM()
    {
        if (BGMController.Instance != null)
        {
            BGMController.Instance.PlayDefaultBGM("bgm.tutorial", null);
            
        }
    }
    
    /// <summary>
    /// Tutorial 완료 처리
    /// </summary>
    public void CompleteTutorial()
    {
        if (isTutorialCompleted)
            return;
        
        isTutorialCompleted = true;
        
            Dbg.Log("[TutorialManager] Tutorial 완료!");
    }
    
    /// <summary>
    /// Lobby로 복귀
    /// </summary>
    public void ReturnToLobby()
    {
            Dbg.Log("[TutorialManager] Lobby로 복귀 중...");
        
        // 🔧 의미 있는 이벤트: 로비 복귀 → 저장
        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsSlotSelected)
        {
            PlayerDataManager.Instance.SaveOnMeaningfulEvent("Tutorial_ReturnToLobby");
        }
        
        // 단순히 Lobby 씬 로드
        // TutorialManager는 씬 언로드 시 자동으로 파괴됨
        SceneManager.LoadScene("Lobby");
    }
    
    /// <summary>
    /// 현재 플레이어 클래스 반환
    /// </summary>
    public PlayerType GetPlayerClass()
    {
        return tutorialPlayerData?.playerClass ?? PlayerType.None;
    }
    
    /// <summary>
    /// 클래스 변경 (Tutorial 중 클래스 선택 옵션이 있을 경우)
    /// </summary>
    public void ChangePlayerClass(PlayerType newClass)
    {
        if (tutorialPlayerData == null)
            return;
        
        tutorialPlayerData.playerClass = newClass;
        
        // 무기도 변경
        EquipmentData newWeapon = GetWeaponForClass(newClass);
        if (newWeapon != null)
        {
            tutorialPlayerData.SetDefaultWeapon(newWeapon);
            
        }
    }
    
    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            
        }
    }
    
    #region 디버그 메서드
    
    /// <summary>
    /// Tutorial 상태 출력 (디버그용)
    /// </summary>
    [ContextMenu("Print Tutorial Status")]
    public void PrintTutorialStatus()
    {
        if (tutorialPlayerData != null)
        {
        }
        else
        {
            Debug.LogWarning("TutorialPlayerData가 초기화되지 않았습니다.");
        }
    }
    
    #endregion
}

