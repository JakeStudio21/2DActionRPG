using UnityEngine;

/// <summary>
/// 몬스터 성장 프로필 - 레벨 스케일링 규칙 정의
/// 모든 몬스터가 공통으로 사용하는 성장 곡선과 타입별 배율
/// </summary>
[CreateAssetMenu(fileName = "MonsterGrowthProfile", menuName = "Enemy/Monster Growth Profile")]
public class MonsterGrowthProfile : ScriptableObject
{
    [Header("📈 레벨 성장 곡선")]
    [Tooltip("체력 성장률 (1 + level * hpGrowthRate)")]
    [SerializeField] private float hpGrowthRate = 0.15f;
    
    [Tooltip("공격력 성장률")]
    [SerializeField] private float atkGrowthRate = 0.12f;
    
    [Tooltip("방어력 성장률")]
    [SerializeField] private float defGrowthRate = 0.08f;
    
    [Tooltip("이동속도 성장률")]
    [SerializeField] private float speedGrowthRate = 0.05f;

    [Header("📊 성장 곡선 형태")]
    [Tooltip("성장 곡선 지수 (1.0 = 선형, 1.1 = 약간 곡선, 1.2 = 강한 곡선)")]
    [SerializeField] private float growthCurve = 1.1f;

    [Header("🎯 타입별 배율")]
    [Tooltip("엘리트 몬스터 배율")]
    [SerializeField] private float eliteMultiplier = 1.5f;
    
    [Tooltip("보스 몬스터 배율")]
    [SerializeField] private float bossMultiplier = 3.0f;

    [Header("💰 보상 시스템")]
    [Tooltip("보상 성장 배율")]
    [SerializeField] private float rewardMultiplier = 1.1f;

    /// <summary>
    /// 레벨과 타입에 따른 체력 배율 계산
    /// </summary>
    public float GetHealthMultiplier(int level, EnemyType enemyType)
    {
        float levelMultiplier = Mathf.Pow(1 + level * hpGrowthRate, growthCurve);
        float typeMultiplier = GetTypeMultiplier(enemyType);
        return levelMultiplier * typeMultiplier;
    }

    /// <summary>
    /// 레벨과 타입에 따른 공격력 배율 계산
    /// </summary>
    public float GetAttackMultiplier(int level, EnemyType enemyType)
    {
        float levelMultiplier = Mathf.Pow(1 + level * atkGrowthRate, growthCurve);
        float typeMultiplier = GetTypeMultiplier(enemyType);
        return levelMultiplier * typeMultiplier;
    }

    /// <summary>
    /// 레벨과 타입에 따른 방어력 배율 계산
    /// </summary>
    public float GetDefenseMultiplier(int level, EnemyType enemyType)
    {
        float levelMultiplier = Mathf.Pow(1 + level * defGrowthRate, growthCurve);
        float typeMultiplier = GetTypeMultiplier(enemyType);
        return levelMultiplier * typeMultiplier;
    }

    /// <summary>
    /// 레벨과 타입에 따른 이동속도 배율 계산
    /// </summary>
    public float GetSpeedMultiplier(int level, EnemyType enemyType)
    {
        float levelMultiplier = Mathf.Pow(1 + level * speedGrowthRate, growthCurve);
        // 이동속도는 타입별 배율 적용 안함 (게임플레이 밸런스)
        return levelMultiplier;
    }

    /// <summary>
    /// 레벨에 따른 보상 배율 계산
    /// </summary>
    public float GetRewardMultiplier(int level)
    {
        return Mathf.Pow(rewardMultiplier, level - 1);
    }

    /// <summary>
    /// 타입별 기본 배율 반환
    /// </summary>
    private float GetTypeMultiplier(EnemyType enemyType)
    {
        return enemyType switch
        {
            EnemyType.Basic => 1.0f,
            EnemyType.Elite => eliteMultiplier,
            EnemyType.Boss => bossMultiplier,
            _ => 1.0f
        };
    }

    /// <summary>
    /// Inspector에서 설정값 검증
    /// </summary>
    private void OnValidate()
    {
        // 성장률 범위 제한
        hpGrowthRate = Mathf.Clamp(hpGrowthRate, 0f, 1f);
        atkGrowthRate = Mathf.Clamp(atkGrowthRate, 0f, 1f);
        defGrowthRate = Mathf.Clamp(defGrowthRate, 0f, 1f);
        speedGrowthRate = Mathf.Clamp(speedGrowthRate, 0f, 0.2f);

        // 곡선 범위 제한
        growthCurve = Mathf.Clamp(growthCurve, 1f, 2f);

        // 배율 최소값 제한
        eliteMultiplier = Mathf.Max(1f, eliteMultiplier);
        bossMultiplier = Mathf.Max(1f, bossMultiplier);
        rewardMultiplier = Mathf.Max(1f, rewardMultiplier);
    }

    /// <summary>
    /// 디버그용 정보 출력
    /// </summary>
    public string GetDebugInfo(int level, EnemyType enemyType)
    {
        return $"Level {level} {enemyType}\n" +
               $"HP: x{GetHealthMultiplier(level, enemyType):F2}\n" +
               $"ATK: x{GetAttackMultiplier(level, enemyType):F2}\n" +
               $"DEF: x{GetDefenseMultiplier(level, enemyType):F2}\n" +
               $"Speed: x{GetSpeedMultiplier(level, enemyType):F2}\n" +
               $"Reward: x{GetRewardMultiplier(level):F2}";
    }
}
