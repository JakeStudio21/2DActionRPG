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

    [Header("Weapon Infos")]
    public WeaponInfo swordWeaponInfo; // Warrior의 기본 무기 정보
    public WeaponInfo bowWeaponInfo;   // Assassin의 기본 무기 정보
    public WeaponInfo staffWeaponInfo; // Wizard의 기본 무기 정보

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
        // ⭐ 수정: 런타임 데이터 사용
        if (GameManager.Instance == null)
        {
            Debug.LogError("[PlayerSpawner] GameManager가 없습니다!");
            return;
        }

        var runtimeData = GameManager.Instance.GetRuntimePlayerData();
        if (runtimeData == null)
        {
            Debug.LogError("[PlayerSpawner] 런타임 플레이어 데이터가 없습니다!");
            return;
        }

        Debug.Log($"[PlayerSpawner] 런타임 데이터 확인: {runtimeData.selectedType}, {runtimeData.weaponName}");

        if (runtimeData.selectedType == PlayerType.None)
        {
            Debug.LogError("[PlayerSpawner] 런타임 데이터에 선택된 플레이어 타입이 없습니다!");
            return;
        }

        var selectedType = runtimeData.selectedType;
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

        // ⭐ 수정: 런타임 데이터 사용
        var runtimeData = GameManager.Instance.GetRuntimePlayerData();
        if (runtimeData == null)
        {
            Debug.LogError("[PlayerSpawner] 런타임 플레이어 데이터를 가져올 수 없습니다.");
            return;
        }

        Debug.Log($"[PlayerSpawner] 런타임 데이터: selectedType={runtimeData.selectedType}, weaponName={runtimeData.weaponName}");

        string weaponNameToEquip = runtimeData.weaponName;
        if (string.IsNullOrEmpty(weaponNameToEquip))
        {
            Debug.LogError("[PlayerSpawner] weaponName이 비어있습니다!");
            return;
        }

        WeaponInfo weaponInfoToEquip = GetWeaponInfoByName(weaponNameToEquip);

        if (weaponInfoToEquip == null)
        {
            Debug.LogError($"[PlayerSpawner] '{weaponNameToEquip}'에 해당하는 무기 정보를 찾을 수 없습니다!");
            Debug.LogError($"[PlayerSpawner] 현재 WeaponInfo 상태:");
            Debug.LogError($"  - swordWeaponInfo: {(swordWeaponInfo != null ? swordWeaponInfo.name : "NULL")}");
            Debug.LogError($"  - bowWeaponInfo: {(bowWeaponInfo != null ? bowWeaponInfo.name : "NULL")}");
            Debug.LogError($"  - staffWeaponInfo: {(staffWeaponInfo != null ? staffWeaponInfo.name : "NULL")}");
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
    /// 무기 이름에 따른 WeaponInfo 반환
    /// </summary>
    private WeaponInfo GetWeaponInfoByName(string weaponName)
    {
        Debug.Log($"[PlayerSpawner] 무기 검색: {weaponName}");
        
        WeaponInfo result = weaponName switch
        {
            "Sword" => swordWeaponInfo,
            "Bow" => bowWeaponInfo,
            "Staff" => staffWeaponInfo,
            _ => null
        };
        
        Debug.Log($"[PlayerSpawner] 무기 검색 결과: {(result != null ? result.name : "NOT FOUND")}");
        return result;
    }

    /// <summary>
    /// 플레이어에게 무기 장착
    /// </summary>
    private void EquipWeaponToPlayer(WeaponInfo weaponInfo)
    {
        Debug.Log($"[PlayerSpawner] 무기 장착 시도: {weaponInfo.name}");
        
        if (weaponInfo.weaponPrefab == null)
        {
            Debug.LogError($"[PlayerSpawner] WeaponInfo '{weaponInfo.name}'의 weaponPrefab이 null입니다!");
            return;
        }

        Debug.Log($"[PlayerSpawner] 무기 프리팹 확인됨: {weaponInfo.weaponPrefab.name}");

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

        activeWeapon.EquipWeapon(weaponInfo);
        Debug.Log($"[PlayerSpawner] 자동 장착 완료: {weaponInfo.name}");
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


// using UnityEngine;

// /// <summary>
// /// 플레이어 캐릭터를 스폰하는 스포너
// /// 선택된 캐릭터 타입에 따라 적절한 프리팹을 생성
// /// </summary>
// public class PlayerSpawner : MonoBehaviour
// {
//     [Header("Prefabs")]
//     public GameObject warriorPrefab;
//     public GameObject assassinPrefab;
//     public GameObject wizardPrefab; // 마법사 추가

//     [Header("Weapon Infos")]
//     public WeaponInfo swordWeaponInfo; // Warrior의 기본 무기 정보
//     public WeaponInfo bowWeaponInfo;   // Assassin의 기본 무기 정보
//     public WeaponInfo staffWeaponInfo; // Wizard의 기본 무기 정보

//     [Header("Spawn")]
//     public Transform spawnPoint;

//     private void Start()
//     {
//         SpawnSelectedPlayer();
//     }

//     /// <summary>
//     /// 선택된 플레이어 캐릭터 스폰
//     /// </summary>
//     private void SpawnSelectedPlayer()
//     {
//         // 디버그: selectedPlayerData 상태 출력
//         if (GameManager.Instance != null)
//         {
//             Debug.Log($"[PlayerSpawner] selectedPlayerData 인스턴스: {GameManager.Instance.selectedPlayerData}");
//             if (GameManager.Instance.selectedPlayerData != null)
//             {
//                 Debug.Log($"[PlayerSpawner] selectedPlayerType: {GameManager.Instance.selectedPlayerData.selectedPlayerType}, weaponName: {GameManager.Instance.selectedPlayerData.weaponName}");
//             }
//         }
//         if (GameManager.Instance == null || GameManager.Instance.selectedPlayerData == null)
//         {
//             Debug.LogError("[PlayerSpawner] GameManager 또는 selectedPlayerData가 없습니다!");
//             return;
//         }

//         var selectedType = GameManager.Instance.selectedPlayerData.selectedPlayerType;
//         Debug.Log($"[PlayerSpawner] 스포너 시작. 선택된 클래스: {selectedType}");

//         GameObject prefabToSpawn = GetPrefabByType(selectedType);

//         if (prefabToSpawn != null)
//         {
//             SpawnPlayer(prefabToSpawn);
//             SetupPlayerCamera();
//             EquipStartingWeapon();
//         }
//         else
//         {
//             Debug.LogError("[PlayerSpawner] 스폰할 플레이어 프리팹이 없습니다!");
//         }
//     }

//     /// <summary>
//     /// 플레이어 타입에 따른 프리팹 반환
//     /// </summary>
//     private GameObject GetPrefabByType(PlayerType playerType)
//     {
//         return playerType switch
//         {
//             PlayerType.Warrior => warriorPrefab,
//             PlayerType.Assasin => assassinPrefab,
//             PlayerType.Wizard => wizardPrefab,
//             _ => GetDefaultPrefab()
//         };
//     }

//     /// <summary>
//     /// 기본 프리팹 반환 (선택되지 않았을 때)
//     /// </summary>
//     private GameObject GetDefaultPrefab()
//     {
//         Debug.LogWarning("[PlayerSpawner] 선택된 클래스가 없습니다. 기본값(Warrior)으로 스폰합니다.");
//         return warriorPrefab;
//     }

//     /// <summary>
//     /// 플레이어 실제 스폰
//     /// </summary>
//     private void SpawnPlayer(GameObject prefab)
//     {
//         if (GamePoolManager.Instance != null)
//         {
//             // 풀에서 가져오기
//             GameObject player = GamePoolManager.Instance.SpawnFromPool(
//                 prefab.name, 
//                 spawnPoint.position, 
//                 Quaternion.identity
//             );
//             Debug.Log($"[PlayerSpawner] {prefab.name} 스폰 완료 (풀링)");
//         }
//         else
//         {
//             // 직접 생성
//             GameObject player = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
//             Debug.Log($"[PlayerSpawner] {prefab.name} 스폰 완료 (인스턴시에이트)");
//         }
//     }

//     /// <summary>
//     /// 카메라가 플레이어를 따라가도록 설정
//     /// </summary>
//     private void SetupPlayerCamera()
//     {
//         if (CameraController.Instance != null)
//         {
//             CameraController.Instance.SetPlayerCameraFollow();
//         }
//         else
//         {
//             Debug.LogWarning("[PlayerSpawner] CameraController를 찾을 수 없습니다.");
//         }
//     }

//     /// <summary>
//     /// 시작 무기 자동 장착
//     /// </summary>
//     private void EquipStartingWeapon()
//     {
//         if (GameManager.Instance?.selectedPlayerData == null)
//         {
//             Debug.LogError("[PlayerSpawner] GameManager 또는 selectedPlayerData가 할당되지 않았습니다.");
//             return;
//         }

//         string weaponNameToEquip = GameManager.Instance.selectedPlayerData.weaponName;
//         WeaponInfo weaponInfoToEquip = GetWeaponInfoByName(weaponNameToEquip);

//         if (weaponInfoToEquip == null)
//         {
//             Debug.LogWarning($"[PlayerSpawner] '{weaponNameToEquip}'에 해당하는 무기 정보를 찾을 수 없습니다.");
//             return;
//         }

//         EquipWeaponToPlayer(weaponInfoToEquip);
//     }

//     /// <summary>
//     /// 무기 이름에 따른 WeaponInfo 반환
//     /// </summary>
//     private WeaponInfo GetWeaponInfoByName(string weaponName)
//     {
//         return weaponName switch
//         {
//             "Sword" => swordWeaponInfo,
//             "Bow" => bowWeaponInfo,
//             "Staff" => staffWeaponInfo,
//             _ => null
//         };
//     }

//     /// <summary>
//     /// 플레이어에게 무기 장착
//     /// </summary>
//     private void EquipWeaponToPlayer(WeaponInfo weaponInfo)
//     {
//         var activeWeapon = FindObjectOfType<ActiveWeapon>();
//         if (activeWeapon == null)
//         {
//             Debug.LogError("[PlayerSpawner] ActiveWeapon 오브젝트를 찾을 수 없습니다.");
//             return;
//         }

//         activeWeapon.EquipWeapon(weaponInfo);
//         Debug.Log($"[PlayerSpawner] 자동 장착 완료: {weaponInfo.name}");
//     }

//     // ⭐ 수정: Update에서 지속적인 로그 제거 (성능 최적화)
//     // Update는 매 프레임마다 실행되므로 디버그 로그로 인한 성능 저하 방지
// }