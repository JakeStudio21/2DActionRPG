using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraController : Singleton<CameraController>
{
    private CinemachineStateDrivenCamera stateDrivenCamera;
    private CinemachineVirtualCamera cinemachineVirtualCamera;

    // 줌 기본값 캐시 및 코루틴 관리
    private float defaultOrthographicSize = 8f;
    private Coroutine zoomCoroutine;

    public void SetPlayerCameraFollow() 
    {
        StartCoroutine(SetPlayerCameraFollowCoroutine());
    }

    // ───────────────────────────────────────────
    //  카메라 줌 API
    // ───────────────────────────────────────────

    /// <summary>
    /// CameraZoomData 설정으로 OrthographicSize를 부드럽게 전환.
    /// useZoom = false이면 아무 동작 안 함.
    /// </summary>
    public void SetZoom(CameraZoomData data)
    {
        if (data == null || !data.useZoom) return;

        if (zoomCoroutine != null) StopCoroutine(zoomCoroutine);
        zoomCoroutine = StartCoroutine(ZoomRoutine(data.targetSize, data.transitionDuration, data.easeCurve));
    }

    /// <summary>
    /// 기본 OrthographicSize로 복귀.
    /// StageManager.CompleteStage() 등 스테이지 종료 시점에 호출.
    /// </summary>
    public void ResetZoom(float duration = 1.0f)
    {
        if (zoomCoroutine != null) StopCoroutine(zoomCoroutine);
        zoomCoroutine = StartCoroutine(ZoomRoutine(defaultOrthographicSize, duration, null));
    }

    /// <summary>
    /// OrthographicSize를 targetSize까지 duration 초 동안 Lerp.
    /// easeCurve가 null이면 선형 보간 사용.
    /// </summary>
    private IEnumerator ZoomRoutine(float targetSize, float duration, AnimationCurve easeCurve)
    {
        float startSize = GetCurrentOrthographicSize();

        if (duration <= 0f)
        {
            ApplyOrthographicSize(targetSize);
            zoomCoroutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curvedT = easeCurve != null ? easeCurve.Evaluate(t) : t;
            ApplyOrthographicSize(Mathf.Lerp(startSize, targetSize, curvedT));
            yield return null;
        }

        ApplyOrthographicSize(targetSize);
        zoomCoroutine = null;
    }

    /// <summary>
    /// 현재 활성 가상 카메라의 OrthographicSize 반환.
    /// </summary>
    private float GetCurrentOrthographicSize()
    {
        if (cinemachineVirtualCamera != null)
            return cinemachineVirtualCamera.m_Lens.OrthographicSize;

        // StateDrivenCamera 하위의 첫 번째 VirtualCamera 사용
        var vcams = GetAllVirtualCameras();
        if (vcams.Length > 0)
            return vcams[0].m_Lens.OrthographicSize;

        return Camera.main != null ? Camera.main.orthographicSize : defaultOrthographicSize;
    }

    /// <summary>
    /// 모든 활성 가상 카메라에 OrthographicSize 동시 적용.
    /// </summary>
    private void ApplyOrthographicSize(float size)
    {
        if (cinemachineVirtualCamera != null)
        {
            var lens = cinemachineVirtualCamera.m_Lens;
            lens.OrthographicSize = size;
            cinemachineVirtualCamera.m_Lens = lens;
            return;
        }

        // StateDrivenCamera 사용 시 모든 자식 VCam에 일괄 적용
        var vcams = GetAllVirtualCameras();
        foreach (var vcam in vcams)
        {
            var lens = vcam.m_Lens;
            lens.OrthographicSize = size;
            vcam.m_Lens = lens;
        }
    }

    private CinemachineVirtualCamera[] GetAllVirtualCameras()
    {
        return FindObjectsOfType<CinemachineVirtualCamera>();
    }

    private IEnumerator SetPlayerCameraFollowCoroutine()
    {
        // 플레이어가 스폰될 때까지 대기
        PlayerController playerController = null;
        float timeout = 10f; // 10초 타임아웃으로 증가
        float elapsed = 0f;


        while (playerController == null && elapsed < timeout)
        {
            // 다양한 방법으로 플레이어 검색
            playerController = FindObjectOfType<PlayerController>();
            
            if (playerController == null)
            {
                // PlayerController를 찾지 못한 경우 다른 방법들 시도
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    playerController = playerObj.GetComponent<PlayerController>();
                }
            }
            
            if (playerController == null)
            {
                elapsed += 0.2f;
                yield return new WaitForSeconds(0.2f);
                
                // 5초마다 씬의 모든 오브젝트 목록 출력 (디버깅용)
                if (elapsed % 5f < 0.2f)
                {
                    LogSceneObjects();
                }
            }
        }

        if (playerController == null)
        {
            Debug.LogError("[CameraController] 플레이어를 찾을 수 없습니다! 카메라 설정을 건너뜁니다.");
            LogSceneObjects(); // 최종 실패 시 오브젝트 목록 출력
            yield break;
        }


        // 카메라 찾기 및 설정
        yield return StartCoroutine(SetupCameras(playerController));
    }

    private IEnumerator SetupCameras(PlayerController playerController)
    {
        
        // State-Driven Camera 우선 검색
        stateDrivenCamera = FindObjectOfType<CinemachineStateDrivenCamera>();
        
        if (stateDrivenCamera != null)
        {
            stateDrivenCamera.Follow = playerController.transform;
            stateDrivenCamera.LookAt = playerController.transform;
        }
        else
        {
            // Virtual Camera 검색
            cinemachineVirtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
            if (cinemachineVirtualCamera != null)
            {
                cinemachineVirtualCamera.Follow = playerController.transform;
                cinemachineVirtualCamera.LookAt = playerController.transform;
            }
            else
            {
                Debug.LogWarning("[CameraController] Cinemachine 카메라를 찾을 수 없습니다!");
                LogCameraObjects(); // 카메라 관련 오브젝트 검색
            }
        }

        // 한 프레임 대기 후 Main Camera 설정
        yield return null;

        // Main Camera 설정
        Camera mainCam = Camera.main;
        if (mainCam != null) 
        {
            mainCam.orthographic = true;
            mainCam.transform.rotation = Quaternion.identity;
        }
        else
        {
            Debug.LogWarning("[CameraController] Main Camera를 찾을 수 없습니다!");
        }

        // Virtual Camera Transform 설정
        if (cinemachineVirtualCamera != null)
        {
            cinemachineVirtualCamera.transform.rotation = Quaternion.identity;
        }

        // 기본 OrthographicSize 캐싱 (ResetZoom 복귀값으로 사용)
        defaultOrthographicSize = GetCurrentOrthographicSize();
    }

    /// <summary>
    /// 수동으로 카메라 재설정을 요청할 수 있는 메서드
    /// </summary>
    public void ResetCameraSettings()
    {
        SetPlayerCameraFollow();
    }
    
    /// <summary>
    /// 디버깅용: 씬의 모든 오브젝트 목록 출력
    /// </summary>
    private void LogSceneObjects()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int playerCount = 0;
        
        foreach (var obj in allObjects)
        {
            if (obj.name.ToLower().Contains("player") || obj.GetComponent<PlayerController>() != null)
            {
                playerCount++;
                if (obj.GetComponent<PlayerController>() != null)
                {
                }
            }
        }
        
    }
    
    /// <summary>
    /// 디버깅용: 카메라 관련 오브젝트 목록 출력
    /// </summary>
    private void LogCameraObjects()
    {
        
        var allCameras = FindObjectsOfType<Camera>();
        foreach (var cam in allCameras)
        {
        }
        
        var virtualCameras = FindObjectsOfType<CinemachineVirtualCamera>();
        foreach (var vcam in virtualCameras)
        {
        }
        
        var stateDrivenCameras = FindObjectsOfType<CinemachineStateDrivenCamera>();
        foreach (var sdcam in stateDrivenCameras)
        {
        }
    }
}