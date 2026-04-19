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
            yield return new WaitForSeconds(0.1f);
        }
        
        // TutorialPlayerData 대기
        while (TutorialManager.Instance.TutorialPlayerData == null)
        {
            yield return new WaitForSeconds(0.1f);
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
        
            Dbg.Log($"[TutorialPlayerSpawner] 플레이어 스폰 완료: {playerData.playerClass} at {spawnPosition}");
        
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
    /// 플레이어 스폰 후 후처리 (무기, 스킬, 카메라, 공격 바인딩)
    /// </summary>
    private IEnumerator SetupPlayerAfterSpawn(TutorialPlayerData playerData)
    {
        // 컴포넌트 초기화 대기
        yield return new WaitForSeconds(0.5f);
        
        // 무기 장착
        EquipWeapon(playerData.equippedWeapon);
        
        // 스킬 주입 (새 스킬 시스템)
        SetupSkills(playerData);
        
        // 카메라 설정
        SetupCamera();

        // 공격 버튼 바인딩 (PlayerSpawner와 동일한 방식)
        AddPlayerAttackInput();
        BindAttackButtonController();
    }

    /// <summary>
    /// 스폰된 플레이어에 PlayerAttackInput 컴포넌트 추가
    /// </summary>
    private void AddPlayerAttackInput()
    {
        if (spawnedPlayer == null) return;

        var existing = spawnedPlayer.GetComponent<PlayerAttackInput>();
        if (existing != null) return;

        spawnedPlayer.AddComponent<PlayerAttackInput>();
    }

    /// <summary>
    /// AttackButtonController에 PlayerAttackInput 참조 주입
    /// </summary>
    private void BindAttackButtonController()
    {
        if (spawnedPlayer == null) return;

        var input = spawnedPlayer.GetComponent<PlayerAttackInput>();
        if (input == null)
        {
            Debug.LogWarning("[TutorialPlayerSpawner] PlayerAttackInput을 찾을 수 없어 AttackButtonController 바인딩 생략.");
            return;
        }

        var attackButtonController = FindObjectOfType<AttackButtonController>();
        if (attackButtonController == null)
        {
            Debug.LogWarning("[TutorialPlayerSpawner] AttackButtonController를 찾을 수 없습니다. HUD가 씬에 있는지 확인하세요.");
            return;
        }

        attackButtonController.Bind(input);
    }
    
    /// <summary>
    /// Tutorial 스킬 직접 주입 (PlayerDataManager 없이 동작)
    /// </summary>
    private void SetupSkills(TutorialPlayerData playerData)
    {
        if (spawnedPlayer == null) return;
        
        var skillManager = spawnedPlayer.GetComponent<PlayerSkillManager>();
        if (skillManager == null)
        {
            Debug.LogWarning("[TutorialPlayerSpawner] PlayerSkillManager를 찾을 수 없습니다!");
            return;
        }
        
        skillManager.SetTutorialSkills(playerData.tutorialSkill1, playerData.tutorialSkill2);
        
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
            
        }
        else
        {
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
