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
            
            // ⭐ 수정: DelayedWeaponEquipCoroutine() → SetupPlayerPostSpawn() 호출
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

        // 미니맵 시스템에 플레이어 참조 주입
        // MinimapManager.Start()는 플레이어 스폰 전에 실행되므로 여기서 직접 전달한다.
        if (MinimapManager.Instance != null && spawnedPlayer != null)
        {
            var playerController = spawnedPlayer.GetComponent<PlayerController>();
            MinimapManager.Instance.SetPlayer(spawnedPlayer.transform, playerController);
            Debug.Log("[PlayerSpawner] MinimapManager에 플레이어 참조 주입 완료");
        }

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
        
        // 카메라 설정 (더 안전한 방식) - 즉시 실행
        yield return StartCoroutine(SetupPlayerCameraCoroutine());
        
        // ⭐ 수정: 무기 장착을 지연 실행으로 변경
        yield return StartCoroutine(DelayedWeaponEquipment());
        
        Debug.Log("[PlayerSpawner] 플레이어 후처리 완료");
    }

    /// <summary>
    /// 클래스 초기화를 기다린 후 무기 장착
    /// </summary>
    private IEnumerator DelayedWeaponEquipment()
    {
        Debug.Log("[PlayerSpawner] 지연된 무기 장착 시작 - 클래스 초기화 대기");
        
        // 클래스 시스템 초기화 대기
        int maxAttempts = 30; // 1.5초 대기
        int attempts = 0;
        bool classSystemReady = false;
        
        while (attempts < maxAttempts && !classSystemReady)
        {
            classSystemReady = CheckClassSystemReady();
            
            if (!classSystemReady)
            {
                Debug.Log($"[PlayerSpawner] 클래스 초기화 대기 중... {attempts + 1}/{maxAttempts}");
                yield return new WaitForSeconds(0.1f);
                attempts++;
            }
        }
        
        // 무기 장착 시도
        if (classSystemReady)
        {
            Debug.Log("[PlayerSpawner] ✅ 클래스 시스템 초기화 완료! 무기 장착 진행");
            EquipStartingWeapon();
        }
        else
        {
            Debug.LogWarning("[PlayerSpawner] ⚠️ 클래스 시스템 초기화 시간 초과! 강제 무기 장착 시도");
            
            // 강제 무기 장착 시도
            try
            {
                EquipStartingWeapon();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PlayerSpawner] 강제 무기 장착 실패: {e.Message}");
            }
        }
    }

    /// <summary>
    /// 클래스 시스템 준비 상태 확인
    /// </summary>
    private bool CheckClassSystemReady()
    {
        if (spawnedPlayer == null) return false;
        
        var allClasses = spawnedPlayer.GetComponents<BaseClassBehaviour>();
        if (allClasses.Length == 0)
        {
            Debug.Log($"[PlayerSpawner] BaseClassBehaviour 컴포넌트 없음");
            return false;
        }
        
        // ✅ 수정: 더 관대한 조건으로 변경
        foreach (var classComp in allClasses)
        {
            // 클래스가 초기화되었고 활성화되었거나, 또는 단순히 enabled 상태라면 준비된 것으로 간주
            if (classComp.IsActiveClass || (classComp.enabled && !string.IsNullOrEmpty(classComp.ClassName)))
            {
                Debug.Log($"[PlayerSpawner] 활성 클래스 발견: {classComp.ClassName}");
                return true;
            }
        }
        
        // ✅ 추가: 디버깅 정보
        Debug.Log($"[PlayerSpawner] 클래스 상태 확인:");
        for (int i = 0; i < allClasses.Length; i++)
        {
            Debug.Log($"  - {allClasses[i].GetType().Name}: enabled={allClasses[i].enabled}, IsActive={allClasses[i].IsActiveClass}, ClassName={allClasses[i].ClassName}");
        }
        
        return false;
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
        Debug.Log($"🔍 [PlayerSpawner] === SpawnPlayer 시작 ===");
        Debug.Log($"🔍 [PlayerSpawner] prefab: {(prefab != null ? prefab.name : "NULL")}");
        Debug.Log($"🔍 [PlayerSpawner] 현재 spawnedPlayer: {(spawnedPlayer != null ? spawnedPlayer.name : "NULL")}");
        
        // ✅ 추가: 기존 플레이어들 완전 정리
        var existingPlayers = FindObjectsOfType<PlayerController>();
        Debug.Log($"🗑️ [PlayerSpawner] 씬에서 발견된 기존 플레이어 개수: {existingPlayers.Length}");
        
        for (int i = 0; i < existingPlayers.Length; i++)
        {
            Debug.Log($"🗑️ [PlayerSpawner] 기존 플레이어 제거: {existingPlayers[i].name} (ID: {existingPlayers[i].GetInstanceID()})");
            Destroy(existingPlayers[i].gameObject);
        }
        
        // ✅ 추가: PlayerHealth도 확인
        var existingHealths = FindObjectsOfType<PlayerHealth>();
        Debug.Log($"🗑️ [PlayerSpawner] 씬에서 발견된 기존 PlayerHealth 개수: {existingHealths.Length}");
        
        // 기존 참조도 초기화
        spawnedPlayer = null;
        
        if (spawnPoint == null)
        {
            spawnPoint = transform;
        }

        // 한 프레임 대기 후 새 플레이어 생성
        StartCoroutine(SpawnPlayerDelayed(prefab));
    }

    /// <summary>
    /// 플레이어 실제 스폰 (플레이어는 풀링 사용하지 않고 직접 생성)
    /// </summary>
    private IEnumerator SpawnPlayerDelayed(GameObject prefab)
    {
        yield return null; // 한 프레임 대기
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
    /// 플레이어에게 시작 무기 장착
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

        // 🆕 수정: PlayerDataManager에서 장착된 무기 우선 확인
        EquipmentData equippedWeapon = null;
        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsSlotSelected)
        {
            var equippedItems = PlayerDataManager.Instance.selectedPlayerData.RuntimeEquippedItems;
            if (equippedItems.ContainsKey(EquipmentSlot.MainWeapon) && equippedItems[EquipmentSlot.MainWeapon] != null)
            {
                equippedWeapon = equippedItems[EquipmentSlot.MainWeapon];
                Debug.Log($"[PlayerSpawner] 저장된 장착 무기 발견: {equippedWeapon.name}");
            }
        }
        
        // 저장된 장착 무기가 없으면 기본 무기 사용
        if (equippedWeapon == null)
        {
            string weaponNameToEquip = GameManager.Instance.selectedPlayerData.weaponName;
            if (string.IsNullOrEmpty(weaponNameToEquip))
            {
                Debug.LogError("[PlayerSpawner] weaponName이 비어있습니다!");
                return;
            }
            
            equippedWeapon = GetEquipmentDataByName(weaponNameToEquip);
            Debug.Log($"[PlayerSpawner] 기본 무기 사용: {weaponNameToEquip}");
        }

        if (equippedWeapon == null)
        {
            Debug.LogError("[PlayerSpawner] 무기 정보를 찾을 수 없습니다!");
            return;
        }

        Debug.Log($"[PlayerSpawner] 무기 정보 찾음: {equippedWeapon.name}");

        // 무기 장착
        EquipWeaponToPlayer(equippedWeapon);
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
        
        if (equipmentData.equipmentPrefab == null)
        {
            Debug.LogError($"[PlayerSpawner] EquipmentData '{equipmentData.name}'의 equipmentPrefab이 null입니다!");
            Debug.LogError($"   - equipmentType: {equipmentData.equipmentType}");
            Debug.LogError($"   - equipmentName: {equipmentData.equipmentName}");
            return;
        }

        Debug.Log($"[PlayerSpawner] 무기 프리팹 확인됨: {equipmentData.equipmentPrefab.name}");

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