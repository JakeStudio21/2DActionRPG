using UnityEngine;
using System.Collections;

/// <summary>
/// 플레이어 캐릭터를 스폰하는 스포너
/// 선택된 캐릭터 타입에 따라 적절한 프리팹을 생성
/// </summary>
public class PlayerSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject warriorPrefab;
    public GameObject assassinPrefab;
    public GameObject wizardPrefab; // 마법사 추가

    [Header("Equipment Data")]
    public EquipmentData swordEquipmentData; // Warrior의 기본 무기 정보
    public EquipmentData bowEquipmentData;   // Assassin의 기본 무기 정보  
    public EquipmentData staffEquipmentData; // Wizard의 기본 무기 정보

    [Header("Spawn")]
    public Transform spawnPoint;

    private GameObject spawnedPlayer; // 스폰된 플레이어 참조

    private void Start()
    {
        StartCoroutine(SpawnSelectedPlayerCoroutine());
    }

    /// <summary>
    /// 안전한 플레이어 스폰 코루틴
    /// </summary>
    private IEnumerator SpawnSelectedPlayerCoroutine()
    {
        // GameManager 준비 대기
        while (GameManager.Instance == null)
        {
            Debug.Log("[PlayerSpawner] GameManager를 기다리는 중...");
            yield return new WaitForSeconds(0.1f);
        }

        // selectedPlayerData 준비 대기
        while (GameManager.Instance.selectedPlayerData == null)
        {
            Debug.Log("[PlayerSpawner] selectedPlayerData를 기다리는 중...");
            yield return new WaitForSeconds(0.1f);
        }

        SpawnSelectedPlayer();
    }

    /// <summary>
    /// 선택된 플레이어 캐릭터 스폰
    /// </summary>
    private void SpawnSelectedPlayer()
    {
        // ⭐ 수정: 기존 SelectedPlayerData 사용 (원래 방식 복구)
        if (GameManager.Instance == null)
        {
            Debug.LogError("[PlayerSpawner] GameManager가 없습니다!");
            return;
        }

        if (GameManager.Instance.selectedPlayerData == null)
        {
            Debug.LogError("[PlayerSpawner] SelectedPlayerData가 없습니다!");
            return;
        }

        Debug.Log($"[PlayerSpawner] 데이터 확인: {GameManager.Instance.selectedPlayerData}");

        if (GameManager.Instance.selectedPlayerData.selectedPlayerType == PlayerType.None)
        {
            Debug.LogError("[PlayerSpawner] 선택된 플레이어 타입이 없습니다!");
            return;
        }

        var selectedType = GameManager.Instance.selectedPlayerData.selectedPlayerType;
        Debug.Log($"[PlayerSpawner] 스포너 시작. 선택된 클래스: {selectedType}");

        GameObject prefabToSpawn = GetPrefabByType(selectedType);

        if (prefabToSpawn != null)
        {
            SpawnPlayer(prefabToSpawn);
            
            // 스폰 후 처리를 코루틴으로 실행
            StartCoroutine(SetupPlayerPostSpawn());
        }
        else
        {
            Debug.LogError("[PlayerSpawner] 스폰할 플레이어 프리팹이 없습니다!");
        }
    }

    /// <summary>
    /// 플레이어 스폰 후 후처리 (카메라, 무기 등)
    /// </summary>
    private IEnumerator SetupPlayerPostSpawn()
    {
        // 한 프레임 대기하여 플레이어가 완전히 활성화되도록 함
        yield return null;
        
        Debug.Log("[PlayerSpawner] 플레이어 후처리 시작");
        
        // 🆕 핵심 수정: PlayerDataManager에 현재 캐릭터 타입 설정
        if (PlayerDataManager.Instance != null && GameManager.Instance?.selectedPlayerData != null)
        {
            PlayerType selectedType = GameManager.Instance.selectedPlayerData.selectedPlayerType;
            PlayerDataManager.Instance.SetCurrentPlayerType(selectedType);
            Debug.Log($"💾 [PlayerSpawner] PlayerDataManager에 캐릭터 타입 설정: {selectedType}");
        }
        else
        {
            Debug.LogError("💥 [PlayerSpawner] PlayerDataManager 또는 GameManager 데이터가 없습니다!");
        }
        
        // ⭐ 추가: PlayerAttackInput 컴포넌트 자동 추가
        AddPlayerAttackInput();
        
        // 카메라 설정 (더 안전한 방식)
        yield return StartCoroutine(SetupPlayerCameraCoroutine());
        
        // 무기 장착
        EquipStartingWeapon();
        
        Debug.Log("[PlayerSpawner] 플레이어 후처리 완료");
    }

    /// <summary>
    /// 카메라가 플레이어를 따라가도록 설정 (강화된 버전)
    /// </summary>
    private IEnumerator SetupPlayerCameraCoroutine()
    {
        Debug.Log("[PlayerSpawner] 카메라 설정 시작");
        
        // CameraController 대기
        float timeout = 5f;
        float elapsed = 0f;
        
        while (CameraController.Instance == null && elapsed < timeout)
        {
            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
            Debug.Log($"[PlayerSpawner] CameraController 대기 중... ({elapsed:F1}초)");
        }
        
        if (CameraController.Instance == null)
        {
            Debug.LogError("[PlayerSpawner] CameraController를 찾을 수 없습니다!");
            yield break;
        }
        
        // 플레이어 존재 확인
        if (spawnedPlayer == null)
        {
            Debug.LogError("[PlayerSpawner] 스폰된 플레이어가 없습니다!");
            yield break;
        }
        
        // 카메라 설정 실행
        CameraController.Instance.SetPlayerCameraFollow();
        Debug.Log("[PlayerSpawner] 카메라 설정 요청 완료");
    }

    /// <summary>
    /// 플레이어 타입에 따른 프리팹 반환
    /// </summary>
    private GameObject GetPrefabByType(PlayerType playerType)
    {
        return playerType switch
        {
            PlayerType.Warrior => warriorPrefab,
            PlayerType.Assasin => assassinPrefab,
            PlayerType.Wizard => wizardPrefab,
            _ => GetDefaultPrefab()
        };
    }

    /// <summary>
    /// 기본 프리팹 반환 (선택되지 않았을 때)
    /// </summary>
    private GameObject GetDefaultPrefab()
    {
        Debug.LogWarning("[PlayerSpawner] 선택된 클래스가 없습니다. 기본값(Warrior)으로 스폰합니다.");
        return warriorPrefab;
    }

    /// <summary>
    /// 플레이어 실제 스폰 (플레이어는 풀링 사용하지 않고 직접 생성)
    /// </summary>
    private void SpawnPlayer(GameObject prefab)
    {
        if (spawnPoint == null)
        {
            spawnPoint = transform; // 스폰 포인트가 없으면 자신의 위치 사용
        }

        // 플레이어는 씬에 하나만 존재하므로 직접 생성
        spawnedPlayer = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
        Debug.Log($"[PlayerSpawner] {prefab.name} 스폰 완료 (직접 생성) - spawnedPlayer: {spawnedPlayer?.name}");
        
        // 스폰된 플레이어 유효성 검사
        if (spawnedPlayer == null)
        {
            Debug.LogError($"[PlayerSpawner] {prefab.name} 스폰 실패! spawnedPlayer가 null입니다.");
        }
        else if (!spawnedPlayer.activeInHierarchy)
        {
            Debug.LogWarning($"[PlayerSpawner] {prefab.name} 스폰되었지만 비활성화 상태입니다.");
            spawnedPlayer.SetActive(true);
        }
    }

    /// <summary>
    /// 시작 무기 자동 장착
    /// </summary>
    private void EquipStartingWeapon()
    {
        Debug.Log("[PlayerSpawner] 무기 장착 시작");
        
        // null 체크들
        if (GameManager.Instance == null)
        {
            Debug.LogError("[PlayerSpawner] GameManager.Instance가 null입니다!");
            return;
        }

        // ⭐ 수정: 기존 SelectedPlayerData 사용 (원래 방식 복구)
        if (GameManager.Instance.selectedPlayerData == null)
        {
            Debug.LogError("[PlayerSpawner] SelectedPlayerData를 가져올 수 없습니다.");
            return;
        }

        Debug.Log($"[PlayerSpawner] 데이터: {GameManager.Instance.selectedPlayerData}");

        string weaponNameToEquip = GameManager.Instance.selectedPlayerData.weaponName;
        if (string.IsNullOrEmpty(weaponNameToEquip))
        {
            Debug.LogError("[PlayerSpawner] weaponName이 비어있습니다!");
            return;
        }

        EquipmentData weaponInfoToEquip = GetEquipmentDataByName(weaponNameToEquip);

        if (weaponInfoToEquip == null)
        {
            Debug.LogError($"[PlayerSpawner] '{weaponNameToEquip}'에 해당하는 무기 정보를 찾을 수 없습니다!");
            Debug.LogError($"[PlayerSpawner] 현재 EquipmentData 상태:");
            Debug.LogError($"  - swordEquipmentData: {(swordEquipmentData != null ? swordEquipmentData.name : "NULL")}");
            Debug.LogError($"  - bowEquipmentData: {(bowEquipmentData != null ? bowEquipmentData.name : "NULL")}");
            Debug.LogError($"  - staffEquipmentData: {(staffEquipmentData != null ? staffEquipmentData.name : "NULL")}");
            return;
        }

        Debug.Log($"[PlayerSpawner] 무기 정보 찾음: {weaponInfoToEquip.name}");

        // 무기 장착 시도 (실패해도 치명적이지 않음)
        try
        {
            EquipWeaponToPlayer(weaponInfoToEquip);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerSpawner] 무기 장착 중 오류 발생: {e.Message}");
            Debug.LogError($"[PlayerSpawner] 스택 트레이스: {e.StackTrace}");
        }
    }

    /// <summary>
    /// 무기 이름에 따른 EquipmentData 반환
    /// </summary>
    private EquipmentData GetEquipmentDataByName(string weaponName)
    {
        Debug.Log($"[PlayerSpawner] 무기 검색: {weaponName}");
        
        EquipmentData result = weaponName switch
        {
            "Sword" => swordEquipmentData,
            "Bow" => bowEquipmentData,
            "Staff" => staffEquipmentData,
            _ => null
        };
        
        Debug.Log($"[PlayerSpawner] 무기 검색 결과: {(result != null ? result.name : "NOT FOUND")}");
        return result;
    }

    /// <summary>
    /// 플레이어에게 무기 장착
    /// </summary>
    private void EquipWeaponToPlayer(EquipmentData equipmentData)  // 매개변수명 변경
    {
        Debug.Log($"[PlayerSpawner] 무기 장착 시도: {equipmentData.name}");
        
        if (equipmentData.equipmentPrefab == null)  // weaponPrefab → equipmentPrefab
        {
            Debug.LogError($"[PlayerSpawner] EquipmentData '{equipmentData.name}'의 equipmentPrefab이 null입니다!");
            return;
        }

        Debug.Log($"[PlayerSpawner] 무기 프리팹 확인됨: {equipmentData.equipmentPrefab.name}");  // weaponPrefab → equipmentPrefab

        var activeWeapon = FindObjectOfType<ActiveWeapon>();
        if (activeWeapon == null)
        {
            Debug.LogError("[PlayerSpawner] ActiveWeapon 오브젝트를 찾을 수 없습니다. 무기 장착을 건너뜁니다.");
            
            // 씬에서 모든 ActiveWeapon 찾기 시도
            var allActiveWeapons = FindObjectsOfType<ActiveWeapon>();
            Debug.LogError($"[PlayerSpawner] 씬에서 찾은 ActiveWeapon 개수: {allActiveWeapons.Length}");
            
            return;
        }

        Debug.Log($"[PlayerSpawner] ActiveWeapon 찾음: {activeWeapon.name}");

        activeWeapon.EquipWeapon(equipmentData);
        Debug.Log($"[PlayerSpawner] 자동 장착 완료: {equipmentData.name}");
    }

    /// <summary>
    /// 외부에서 스폰된 플레이어 참조를 가져올 수 있는 메서드
    /// </summary>
    public GameObject GetSpawnedPlayer()
    {
        return spawnedPlayer;
    }

    /// <summary>
    /// 스폰된 플레이어에 PlayerAttackInput 컴포넌트 자동 추가
    /// </summary>
    private void AddPlayerAttackInput()
    {
        Debug.Log("🔵 [PlayerSpawner] AddPlayerAttackInput() 시작");
        
        if (spawnedPlayer == null)
        {
            Debug.LogError("🔴 [PlayerSpawner] 스폰된 플레이어가 없어서 PlayerAttackInput을 추가할 수 없습니다!");
            return;
        }

        Debug.Log("🔵 [PlayerSpawner] 스폰된 플레이어: " + spawnedPlayer.name);

        // 이미 PlayerAttackInput이 있는지 확인
        var existingInput = spawnedPlayer.GetComponent<PlayerAttackInput>();
        if (existingInput != null)
        {
            Debug.Log("🟡 [PlayerSpawner] PlayerAttackInput이 이미 존재합니다.");
            return;
        }

        // PlayerAttackInput 컴포넌트 추가
        var playerAttackInput = spawnedPlayer.AddComponent<PlayerAttackInput>();
        if (playerAttackInput != null)
        {
            Debug.Log("✅ [PlayerSpawner] PlayerAttackInput 컴포넌트 자동 추가 완료!");
            
            // 추가된 컴포넌트 확인
            var verifyInput = spawnedPlayer.GetComponent<PlayerAttackInput>();
            Debug.Log("🔍 [PlayerSpawner] 컴포넌트 추가 확인: " + (verifyInput != null ? "성공" : "실패"));
        }
        else
        {
            Debug.LogError("🔴 [PlayerSpawner] PlayerAttackInput 컴포넌트 추가 실패!");
        }
    }
}