using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AreaEntrance : MonoBehaviour
{
    [SerializeField] private string transitionName;

    private void Start() {
        // ⭐ 수정: FSMStageController 우선 사용
        if (FSMStageController.Instance != null)
        {
            // FSMStageController가 자동 플레이어 위치 설정을 하지 않는 경우에만 처리
            if (!FSMStageController.Instance.GetType().GetField("autoSetPlayerPosition", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(FSMStageController.Instance).Equals(true) == true)
            {
                HandlePlayerPositioning();
            }
        }
        else
        {
            // ⭐ 백업: 기존 시스템 사용
            HandlePlayerPositioning();
        }
    }

    private void HandlePlayerPositioning()
    {
        var playerController = FindObjectOfType<PlayerController>();
        if (playerController == null)
        {
            Debug.LogWarning("[AreaEntrance] PlayerController를 찾을 수 없습니다!");
            return;
        }

        string currentTransitionName = "";
        
        // FSMStageController를 통해 전환 정보 가져오기
        if (FSMStageController.Instance != null)
        {
            currentTransitionName = FSMStageController.Instance.GetTransitionInfo();
        }
        else if (SceneManagement.Instance != null)
        {
            currentTransitionName = SceneManagement.Instance.SceneTransitionName;
        }

        if (transitionName == currentTransitionName) {
            playerController.transform.position = this.transform.position;
            Debug.Log($"[AreaEntrance] 플레이어 위치 설정: {this.transform.position} (전환: {transitionName})");
            
            if (CameraController.Instance != null)
            {
                CameraController.Instance.SetPlayerCameraFollow();
            }
            // UIFade.Instance.FadeToClear();
        }
    }
}
