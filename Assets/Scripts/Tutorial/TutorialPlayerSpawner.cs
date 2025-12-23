using UnityEngine;
using System.Collections;

/// <summary>
/// Tutorial 씬 전용 플레이어 스포너
/// - PlayerSpawner 대체 (Tutorial에서만 사용)
/// - GameManager 의존 제거
/// - TutorialManager.TutorialPlayerData 사용
/// - 단순하고 직접적인 무기 장착
/// </summary>
public class TutorialPlayerSpawner : MonoBehaviour
{
    [Header("플레이어 프리팹")]
    [SerializeField] private GameObject warriorPrefab;
    [SerializeField] private GameObject assassinPrefab;
    [SerializeField] private GameObject wizardPrefab;
    
    [Header("스폰 설정")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private bool waitForTutorialManager = true;
    
    [Header("디버그")]
    [SerializeField] private bool showDebugLogs = true;
    
    private GameObject spawnedPlayer;
    
    void Start()
    {
        if (waitForTutorialManager)
        {
            StartCoroutine(WaitAndSpawn());
        }
        else
        {
            SpawnPlayer();
        }
    }
    
    /// <summary>
    /// TutorialManager 초기화 대기 후 스폰
    /// </summary>
    private IEnumerator WaitAndSpawn()
    {
        // TutorialManager 대기
        while (TutorialManager.Instance == null)
        {
            if (showDebugLogs)
                Debug.Log("[TutorialPlayerSpawner] TutorialManager 대기 중...");
            yield return new WaitForSeconds(0.1f);
        }
        
        // TutorialPlayerData 대기
        while (TutorialManager.Instance.TutorialPlayerData == null)
        {
            if (showDebugLogs)
                Debug.Log("[TutorialPlayerSpawner] TutorialPlayerData 대기 중...");
            yield return new WaitForSeconds(0.1f);
        }
        
        if (showDebugLogs)
        {
            Debug.Log("[TutorialPlayerSpawner] TutorialManager 준비 완료, 스폰 시작");
        }
        
        SpawnPlayer();
    }
    
    /// <summary>
    /// 플레이어 스폰
    /// </summary>
    private void SpawnPlayer()
    {
        if (TutorialManager.Instance == null)
        {
            Debug.LogError("[TutorialPlayerSpawner] TutorialManager가 없습니다!");
            return;
        }
        
        TutorialPlayerData playerData = TutorialManager.Instance.TutorialPlayerData;
        
        if (playerData == null)
        {
            Debug.LogError("[TutorialPlayerSpawner] TutorialPlayerData가 없습니다!");
            return;
        }
        
        // 클래스에 맞는 프리팹 가져오기
        GameObject prefabToSpawn = GetPrefabForClass(playerData.playerClass);
        
        if (prefabToSpawn == null)
        {
            Debug.LogError($"[TutorialPlayerSpawner] {playerData.playerClass} 프리팹을 찾을 수 없습니다!");
            return;
        }
        
        // 스폰 위치 결정
        Vector3 spawnPosition = (spawnPoint != null) ? spawnPoint.position : Vector3.zero;
        
        // 플레이어 스폰
        spawnedPlayer = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
        spawnedPlayer.name = $"Player_{playerData.playerClass}";
        
        if (showDebugLogs)
        {
            Debug.Log($"[TutorialPlayerSpawner] 플레이어 스폰 완료: {playerData.playerClass} at {spawnPosition}");
        }
        
        // 무기 장착 및 후처리
        StartCoroutine(SetupPlayerAfterSpawn(playerData));
    }
    
    /// <summary>
    /// 클래스에 맞는 프리팹 반환
    /// </summary>
    private GameObject GetPrefabForClass(PlayerType playerClass)
    {
        switch (playerClass)
        {
            case PlayerType.Warrior:
                return warriorPrefab;
            case PlayerType.Assasin:
                return assassinPrefab;
            case PlayerType.Wizard:
                return wizardPrefab;
            default:
                Debug.LogWarning($"[TutorialPlayerSpawner] 알 수 없는 클래스: {playerClass}");
                return assassinPrefab; // 기본값
        }
    }
    
    /// <summary>
    /// 플레이어 스폰 후 후처리 (무기, 카메라 등)
    /// </summary>
    private IEnumerator SetupPlayerAfterSpawn(TutorialPlayerData playerData)
    {
        // 컴포넌트 초기화 대기
        yield return new WaitForSeconds(0.5f);
        
        // 무기 장착
        EquipWeapon(playerData.equippedWeapon);
        
        // 카메라 설정
        SetupCamera();
        
        if (showDebugLogs)
        {
            Debug.Log("[TutorialPlayerSpawner] 플레이어 후처리 완료");
        }
    }
    
    /// <summary>
    /// 무기 장착 (직접 EquipmentData 사용)
    /// </summary>
    private void EquipWeapon(EquipmentData weapon)
    {
        if (weapon == null)
        {
            Debug.LogWarning("[TutorialPlayerSpawner] 장착할 무기가 없습니다!");
            return;
        }
        
        // ActiveWeapon 찾기
        ActiveWeapon activeWeapon = FindObjectOfType<ActiveWeapon>();
        
        if (activeWeapon == null)
        {
            Debug.LogError("[TutorialPlayerSpawner] ActiveWeapon을 찾을 수 없습니다!");
            return;
        }
        
        // 무기 장착
        activeWeapon.EquipWeapon(weapon);
        
        if (showDebugLogs)
        {
            Debug.Log($"[TutorialPlayerSpawner] 무기 장착 완료: {weapon.equipmentName}");
        }
    }
    
    /// <summary>
    /// 카메라 설정
    /// </summary>
    private void SetupCamera()
    {
        if (spawnedPlayer == null)
            return;
        
        // CameraController.Instance 사용 (Singleton 패턴)
        if (CameraController.Instance != null)
        {
            CameraController.Instance.SetPlayerCameraFollow();
            
            if (showDebugLogs)
            {
                Debug.Log("[TutorialPlayerSpawner] 카메라 설정 완료");
            }
        }
        else
        {
            if (showDebugLogs)
            {
                Debug.LogWarning("[TutorialPlayerSpawner] CameraController를 찾을 수 없습니다 (Tutorial은 카메라 없어도 작동)");
            }
        }
    }
    
    /// <summary>
    /// 스폰된 플레이어 반환
    /// </summary>
    public GameObject GetSpawnedPlayer()
    {
        return spawnedPlayer;
    }
}
