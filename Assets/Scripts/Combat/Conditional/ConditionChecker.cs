using UnityEngine;

/// <summary>
/// 조건부 모디파이어의 조건 판정 시스템
/// ⚡ Static 클래스로 GC 최소화
/// ⚙️ Phase 4: ConditionalModifier 시스템
/// </summary>
public static class ConditionChecker
{
    /// <summary>
    /// 조건 판정 메인 메서드
    /// </summary>
    /// <param name="type">조건 타입</param>
    /// <param name="paramValue">조건 파라미터 (float)</param>
    /// <param name="paramString">조건 파라미터 (string)</param>
    /// <param name="context">전투 컨텍스트</param>
    /// <param name="applyPhase">현재 적용 중인 페이즈 (3=공격, 5=방어)</param>
    /// <returns>조건 만족 여부</returns>
    public static bool Check(
        EConditionType type,
        float paramValue,
        string paramString,
        CombatContext context,
        int applyPhase = -1
    )
    {
        switch (type)
        {
            case EConditionType.None:
                return true; // 무조건 적용
            
            // === 타겟 관련 ===
            case EConditionType.TargetIsBoss:
                return CheckTargetIsBoss(context);
            
            case EConditionType.TargetIsElite:
                return CheckTargetIsElite(context);
            
            case EConditionType.TargetIsBossAndDamageType:
                return CheckTargetIsBoss(context) && CheckDamageType(paramString, context);
            
            case EConditionType.TargetIsBossAndHpAbove:
                // 복합 조건: 대상이 보스이면서 HP가 특정 비율 이상
                return CheckTargetIsBoss(context) && context.defenderHpPercent > paramValue;
            
            case EConditionType.TargetHpAbove:
                return context.defenderHpPercent > paramValue;
            
            case EConditionType.TargetHpBelow:
                return context.defenderHpPercent < paramValue;
            
            // === 자신 관련 (페이즈 기반 스마트 스위칭) ===
            case EConditionType.SelfHpBelow:
                // Phase 3 (공격 페이즈): 공격자 HP 체크
                // Phase 5 (방어 페이즈): 피격자 HP 체크
                return GetSelfHpPercent(context, applyPhase) < paramValue;
            
            case EConditionType.SelfHpAbove:
                return GetSelfHpPercent(context, applyPhase) > paramValue;
            
            // === 전투 상태 ===
            case EConditionType.IsBackAttack:
                return context.isBackAttack;
            
            case EConditionType.HasBuff:
                // TODO: 버프 시스템 구현 후 연동
                return false;
            
            case EConditionType.HasDebuff:
                // TODO: 디버프 시스템 구현 후 연동
                return false;
            
            default:
                Debug.LogWarning($"[ConditionChecker] 알 수 없는 조건 타입: {type}");
                return false;
        }
    }
    
    #region 개별 조건 판정 메서드
    
    /// <summary>
    /// "Self" HP 비율을 페이즈에 따라 스마트하게 반환
    /// Phase 3 (공격 페이즈): attackerHpPercent 반환
    /// Phase 5 (방어 페이즈): defenderHpPercent 반환
    /// Phase 미지정 (-1): attackerHpPercent 기본값
    /// </summary>
    private static float GetSelfHpPercent(CombatContext context, int applyPhase)
    {
        // Phase 5 (Defense): 피격자(Defender) 관점
        if (applyPhase == 5)
        {
            return context.defenderHpPercent;
        }
        // Phase 3 (Modifier) 또는 미지정: 공격자(Attacker) 관점
        else
        {
            return context.attackerHpPercent;
        }
    }
    
    /// <summary>
    /// 대상이 보스인지 확인
    /// </summary>
    private static bool CheckTargetIsBoss(CombatContext context)
    {
        if (context.target == null)
            return false;
        
        return context.target.IsBoss();
    }
    
    /// <summary>
    /// 대상이 엘리트인지 확인
    /// </summary>
    private static bool CheckTargetIsElite(CombatContext context)
    {
        if (context.target == null)
            return false;
        
        return context.target.IsElite();
    }
    
    /// <summary>
    /// 데미지 타입 확인
    /// </summary>
    private static bool CheckDamageType(string targetDamageType, CombatContext context)
    {
        if (string.IsNullOrEmpty(targetDamageType) || string.IsNullOrEmpty(context.damageType))
            return false;
        
        // 대소문자 무시 비교
        return context.damageType.Equals(targetDamageType, System.StringComparison.OrdinalIgnoreCase);
    }
    
    #endregion
}

