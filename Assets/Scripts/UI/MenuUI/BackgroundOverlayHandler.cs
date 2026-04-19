using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 🎯 로비 배경 클릭 흡수용 투명 오버레이 핸들러
/// - 배경 클릭 시 아무 동작 안 함 (캐릭터 선택 유지)
/// - EventSystem 포커스 시스템 무간섭
/// - Button 대신 Image + IPointerClickHandler 사용
/// </summary>
public class BackgroundOverlayHandler : MonoBehaviour, IPointerClickHandler
{
    [Header("🎯 디버그 설정")]
    
    [Header("🔗 참조")]
    [SerializeField] private LobbyUIController lobbyUIController;
    
    private void Start()
    {
        // 🔗 LobbyUIController 자동 찾기
        if (lobbyUIController == null)
        {
            lobbyUIController = FindObjectOfType<LobbyUIController>();
        }
        
        if (lobbyUIController == null)
        {
            Debug.LogWarning("⚠️ [BackgroundOverlay] LobbyUIController를 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// 🖱️ 배경 클릭 이벤트 처리 (EventSystem Selection 보호)
    /// </summary>
    /// <param name="eventData">클릭 이벤트 데이터</param>
    public void OnPointerClick(PointerEventData eventData)
    {
        
        // 🎯 핵심: 현재 선택된 GameObject 백업 및 복원
        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;
        
        
        // 🔄 다음 프레임에서 선택 상태 복원 (EventSystem 처리 후)
        StartCoroutine(RestoreSelectionNextFrame(currentSelected));
    }
    
    /// <summary>
    /// 🔄 다음 프레임에서 선택 상태 복원
    /// </summary>
    private IEnumerator RestoreSelectionNextFrame(GameObject targetToRestore)
    {
        // 1프레임 대기 (EventSystem이 Selection을 변경한 후)
        yield return null;
        
        // 🎯 선택 상태 복원
        if (targetToRestore != null)
        {
            EventSystem.current.SetSelectedGameObject(targetToRestore);
            
        }
        else
        {
            // 🆕 선택된 객체가 없었다면 LobbyUIController에서 현재 캐릭터 선택 복원
            if (lobbyUIController != null)
            {
                lobbyUIController.RestoreCharacterSelection();
                
            }
        }
    }
}
