using UnityEngine;

/// <summary>
/// 전투 공식 설정값 (ScriptableObject)
/// Inspector에서 실시간 조정 가능
/// </summary>
[CreateAssetMenu(fileName = "CombatFormulaConfig", menuName = "Combat/Formula Config")]
public class CombatFormulaConfig : ScriptableObject
{
    [Header("방어력 시스템 - Dynamic K")]
    [Tooltip("기본 방어력 상수 (기본 100)\n레벨 1에서의 K 값")]
    public float baseDefenseConstant = 100f;
    
    [Tooltip("레벨당 방어력 상수 증가량 (기본 10)\nK = baseK + (attackerLevel × kGainPerLevel)")]
    public float defenseConstantPerLevel = 10f;
    
    [Header("데미지 제한")]
    [Tooltip("최소 데미지 (기본 1)")]
    public int minDamage = 1;
    
    [Tooltip("최대 데미지 (0 = 제한 없음)")]
    public int maxDamage = 0;
    
    [Header("디버그 설정")]
    [Tooltip("상세 로그 활성화 (Step별 데미지 추적)")]
    public bool enableDetailedLogs = true;
    
    [Header("정보 (읽기 전용)")]
    [TextArea(5, 10)]
    public string info = "전투 공식 표준 설정 - Dynamic K 시스템\n\n" +
                         "Dynamic K 공식:\n" +
                         "K = baseDefenseConstant + (attackerLevel × defenseConstantPerLevel)\n\n" +
                         "예시 (baseK=100, kGain=10):\n" +
                         "- Lv1 공격자: K=110 → 방어력 100 시 47.6% 감소\n" +
                         "- Lv30 공격자: K=400 → 방어력 100 시 20% 감소\n" +
                         "- Lv60 공격자: K=700 → 방어력 100 시 12.5% 감소\n\n" +
                         "효과: 고레벨 공격자는 낮은 방어력을 더 쉽게 관통";
}

