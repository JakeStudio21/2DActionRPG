using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq; // LINQ 사용
using StageSystem; // ✅ 추가: WaveTriggerId 사용을 위한 네임스페이스

/// <summary>
/// ⭐ 수정: 씬 내 포털 이동 시스템 (기존 씬 이동에서 변경)
/// FSMStageController를 통한 통합 관리
/// </summary>
public class AreaExit : MonoBehaviour
{
    [Header("포털 설정")]
    [SerializeField] private string targetAreaName; // AreaEntrance의 transitionName과 매칭
    [SerializeField] private string portalName; // 포털 식별자 (디버그용)
    [SerializeField] private bool requiresBossDefeat = false; // 보스 격파 필요 여부

    [Header("✅ 범용 트리거 설정")]
    [SerializeField] private bool isTriggerGate = false;      // 트리거 발동 게이트 여부
    [SerializeField] private WaveTriggerId triggerToActivate = WaveTriggerId.None; // 발동할 트리거 ID
    [SerializeField] private string gateTag = "BossGate";    // 게이트 식별 태그

    [Header("직접 위치 설정 (선택사항)")]
    [SerializeField] private Transform directTargetPosition; // AreaEntrance 대신 직접 위치 지정 가능
    
    [Header("포털 게이트")]
    [SerializeField] private GameObject portalGate; // 문 오브젝트 연결

    [Header("⭐ 사용 중단 예정 (기존 호환성)")]
    [SerializeField] private string sceneToLoad; // 사용 안함 (기존 호환성 유지)
    [SerializeField] private string SceneTransitionName; // targetAreaName으로 대체됨

    // ✅ 외부 접근용 프로퍼티 추가
    public string GateTag => gateTag;
    public GameObject PortalGate => portalGate;

    private void Start()
    {
        // 기존 필드값을 새 필드로 마이그레이션
        if (string.IsNullOrEmpty(targetAreaName) && !string.IsNullOrEmpty(SceneTransitionName))
        {
            targetAreaName = SceneTransitionName;
        }
        
        if (string.IsNullOrEmpty(portalName))
        {
            portalName = gameObject.name;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.gameObject.GetComponent<PlayerController>() && !other.CompareTag("Player"))
        {
            return;
        }

        // ⭐ 수정: FSMStageController를 통한 포털 이동
        if (FSMStageController.Instance != null)
        {
            // ✅ 범용 트리거 게이트인 경우 StageManager에 트리거 알림
            if (isTriggerGate && triggerToActivate != WaveTriggerId.None)
            {
                var stageManager = FindObjectOfType<StageManager>();
                if (stageManager != null)
                {
                    stageManager.TriggerWave(triggerToActivate);
                }
            }
            
            // 직접 위치가 지정된 경우
            if (directTargetPosition != null)
            {
                bool success = FSMStageController.Instance.TryPortalMovementWithBossCheck(
                    directTargetPosition.position, 
                    targetAreaName, 
                    requiresBossDefeat
                );
                return;
            }

            // AreaEntrance를 찾아서 해당 위치로 이동
            AreaEntrance targetEntrance = FindTargetAreaEntrance();
            if (targetEntrance != null)
            {
                FSMStageController.Instance.TryPortalMovementWithBossCheck(
                    targetEntrance.transform.position, 
                    targetAreaName, 
                    requiresBossDefeat
                );
            }
        }
    }

    /// <summary>
    /// 목표 AreaEntrance 찾기
    /// </summary>
    private AreaEntrance FindTargetAreaEntrance()
    {
        AreaEntrance[] entrances = FindObjectsOfType<AreaEntrance>();
        
        foreach (var entrance in entrances)
        {
            // Reflection으로 transitionName 가져오기
            var field = entrance.GetType().GetField("transitionName", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                string entranceName = (string)field.GetValue(entrance);
                if (entranceName == targetAreaName)
                {
                    return entrance;
                }
            }
        }
        
        return null;
    }

    private void Update()
    {
        UpdatePortalGate();
    }

    /// <summary>
    /// 포털 게이트 상태 업데이트
    /// </summary>
    private void UpdatePortalGate()
    {
        if (portalGate == null) return;

        bool shouldGateBeOpen = !requiresBossDefeat || AreAllBossesDefeated();
        
        // 게이트가 닫혀있어야 할 때는 활성화, 열려있어야 할 때는 비활성화
        if (portalGate.activeSelf == shouldGateBeOpen)
        {
            portalGate.SetActive(!shouldGateBeOpen);
            
            if (!shouldGateBeOpen)
            {
            }
            else
            {
            }
        }
    }

    /// <summary>
    /// 모든 보스가 처치되었는지 확인
    /// </summary>
    private bool AreAllBossesDefeated()
    {
        // 씬에 있는 모든 EnemyHealth 중 isBoss == true인 적 찾기
        EnemyHealth[] allEnemies = FindObjectsOfType<EnemyHealth>();
        foreach (var enemy in allEnemies)
        {
            if (enemy.IsBoss() && !enemy.isDead) // ⭐ 수정: enemy.isBoss → enemy.IsBoss()
            {
                return false; // 살아있는 보스가 있음
            }
        }
        return true; // 모든 보스가 죽었거나 보스가 없음
    }

    /// <summary>
    /// 포털 정보 가져오기 (디버그용)
    /// </summary>
    public string GetPortalInfo()
    {
        return $"Portal: {portalName} → {targetAreaName} (Boss Required: {requiresBossDefeat})";
    }
}

// ⭐ 기존 코드 제거됨 (씬 이동 로직)
