// DEPRECATED: 이 클래스는 GameManager로 통합되었습니다.
// 기존 참조 에러를 방지하기 위한 임시 빈 클래스입니다.

using UnityEngine;

/// <summary>
/// [사용 중단] GameManager로 통합되었습니다.
/// 기존 코드의 호환성을 위해 GameManager로 연결합니다.
/// </summary>
[System.Obsolete("GameDataManager는 더 이상 사용되지 않습니다. GameManager를 사용하세요.")]
public class GameDataManager : MonoBehaviour
{
    // 기존 코드 호환성을 위한 정적 참조
    public static GameManager Instance
    {
        get
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("[GameDataManager] GameDataManager는 더 이상 사용되지 않습니다. GameManager를 사용하세요.");
                return null;
            }
            return GameManager.Instance;
        }
    }
    
    // 기존 코드 호환성을 위한 selectedPlayerData 참조
    public SelectedPlayerData selectedPlayerData
    {
        get
        {
            if (GameManager.Instance != null)
                return GameManager.Instance.selectedPlayerData;
            return null;
        }
        set
        {
            // 🔧 수정: GameManager 대신 PlayerDataManager 사용
            if (PlayerDataManager.Instance != null)
                PlayerDataManager.Instance.selectedPlayerData = value;
        }
    }
    
    private void Awake()
    {
        Debug.LogWarning("[GameDataManager] 이 클래스는 더 이상 사용되지 않습니다. GameManager를 사용하도록 코드를 수정해주세요.");
        
        // 즉시 파괴
        Destroy(gameObject);
    }
}
