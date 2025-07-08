// DEPRECATED: 이 클래스는 FSMStageController로 통합되었습니다.
// 기존 참조 에러를 방지하기 위한 임시 호환성 클래스입니다.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [사용 중단] FSMStageController로 통합되었습니다.
/// 기존 코드의 호환성을 위해 FSMStageController로 연결합니다.
/// </summary>
[System.Obsolete("SceneManagement는 더 이상 사용되지 않습니다. FSMStageController를 사용하세요.")]
public class SceneManagement : Singleton<SceneManagement>
{
    public string SceneTransitionName 
    { 
        get 
        {
            if (FSMStageController.Instance != null)
            {
                return FSMStageController.Instance.GetTransitionInfo();
            }
            return "";
        } 
        private set 
        {
            if (FSMStageController.Instance != null)
            {
                FSMStageController.Instance.SetTransitionInfo(value);
            }
        } 
    }

    protected override void Awake()
    {
        base.Awake();
        Debug.LogWarning("[SceneManagement] 이 클래스는 더 이상 사용되지 않습니다. FSMStageController를 사용하도록 코드를 수정해주세요.");
    }

    public void SetTransitionName(string sceneTransitionName) 
    {
        Debug.LogWarning("[SceneManagement] SetTransitionName는 더 이상 사용되지 않습니다. FSMStageController.SetTransitionInfo를 사용하세요.");
        
        if (FSMStageController.Instance != null)
        {
            FSMStageController.Instance.SetTransitionInfo(sceneTransitionName);
        }
        else
        {
            SceneTransitionName = sceneTransitionName;
        }
    }
}
