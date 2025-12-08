using UnityEngine;

/// <summary>
/// Scorpion 몬스터 클래스 - 근접 공격 + 독 특화 (8방향 스프라이트)
/// 컬러 베리에이션 지원 (Red/Blue/Green Scorpion)
/// ⭐ [Complete Reset] 완전한 데이터 기반 시스템
/// </summary>
public class Scorpion : BaseEnemy
{
    [Header("⭐ Scorpion 전용 컴포넌트")]
    [SerializeField] private MeleeAttack meleeAttack;

    [Header("🎨 Color Variation (선택사항)")]
    [SerializeField] private Color colorTint = Color.white;

    #region ⭐ BaseEnemy 추상 속성 구현 - 완전한 데이터 기반

    public override float PatrolRadius 
    { 
        get 
        {
            // 스폰 시 설정된 값 우선 사용
            if (patrolRadius > 0) 
                return patrolRadius;
            
            // 데이터 기반 fallback (Scorpion은 넓은 패트롤 범위)
            if (enemyData != null)
                return enemyData.PatrolRadius;
            
            return 5f; // 기본값 (경계 정찰형)
        } 
    }
    
    public override float AttackRange 
    { 
        get 
        {
            // 1순위: AttackData
            if (meleeAttack != null && meleeAttack.AttackData != null)
                return meleeAttack.AttackData.AttackRange;
                
            Debug.LogError($"[Scorpion] {gameObject.name}: MeleeAttack 또는 AttackData가 없습니다!");
            return 1.8f; // Scorpion은 중간 공격 범위 (독침)
        } 
    }
    
    public override float DetectionRange
    {
        get
        {
            if (EnemyData != null)
            {
                float range = EnemyData.DetectionRange;
                Debug.Log($"[Scorpion] {gameObject.name} DetectionRange: {range} (from EnemyData)");
                return range;
            }
                
            Debug.LogError($"[Scorpion] {gameObject.name}: EnemyData가 없어서 DetectionRange 확인 불가! fallback 6f 사용");
            return 6f; // Scorpion은 넓은 감지 범위 (경계 강함)
        }
    }
    
    public override float ChaseRange 
    { 
        get 
        {
            if (EnemyData != null)
            {
                float range = EnemyData.ChaseRange;
                Debug.Log($"[Scorpion] {gameObject.name} ChaseRange: {range} (from EnemyData)");
                return range;
            }
                
            Debug.LogError($"[Scorpion] {gameObject.name}: EnemyData가 없어서 ChaseRange 확인 불가! fallback 8f 사용");
            return 8f; // Scorpion은 중간 추적 범위
        } 
    }

    #endregion

    #region ⭐ BaseEnemy 추상 메서드 구현

    protected override void OnAwakeInitialize()
    {
        // MeleeAttack 컴포넌트 확인
        if (meleeAttack == null)
            meleeAttack = GetComponent<MeleeAttack>();
            
        // 색상 적용 (Material Tint 방식)
        ApplyColorTint();
            
        Debug.Log($"[Scorpion] {gameObject.name} Awake 초기화 완료");
    }

    protected override void OnStartInitialize()
    {
        ApplyScorpionSpecificSettings();
        Debug.Log($"[Scorpion] {gameObject.name} Start 초기화 완료");
    }

    protected override void InitializeAttackSystem()
    {
        if (meleeAttack != null)
        {
            meleeAttack.Initialize();
            Debug.Log($"[Scorpion] {gameObject.name} MeleeAttack 시스템 초기화 완료");
        }
        else
        {
            Debug.LogError($"[Scorpion] {gameObject.name}: MeleeAttack 컴포넌트가 없습니다!");
        }
    }

    public override void Attack()
    {
        if (meleeAttack != null && meleeAttack.CanAttack())
        {
            meleeAttack.Attack();
            Debug.Log($"[Scorpion] {gameObject.name} 독침 공격 실행!");
        }
    }

    #endregion

    #region 🎨 Color Variation 시스템

    /// <summary>
    /// 색상 적용 (Material Tint 방식)
    /// </summary>
    private void ApplyColorTint()
    {
        if (spriteRenderer != null && colorTint != Color.white)
        {
            spriteRenderer.color = colorTint;
            Debug.Log($"[Scorpion] {gameObject.name} 색상 적용: {colorTint}");
        }
    }

    /// <summary>
    /// 런타임 색상 변경 (외부 호출용)
    /// </summary>
    public void SetColorTint(Color newColor)
    {
        colorTint = newColor;
        ApplyColorTint();
    }

    /// <summary>
    /// 베리에이션별 프리셋 색상
    /// </summary>
    public enum ScorpionVariant
    {
        Red,
        Blue,
        Green
    }

    /// <summary>
    /// 베리에이션에 따른 색상 자동 적용
    /// </summary>
    public void SetVariant(ScorpionVariant variant)
    {
        switch (variant)
        {
            case ScorpionVariant.Red:
                SetColorTint(new Color(1.0f, 0.3f, 0.3f, 1.0f)); // 붉은색
                break;
            case ScorpionVariant.Blue:
                SetColorTint(new Color(0.3f, 0.5f, 1.0f, 1.0f)); // 파란색
                break;
            case ScorpionVariant.Green:
                SetColorTint(new Color(0.3f, 1.0f, 0.3f, 1.0f)); // 초록색
                break;
        }
        
        Debug.Log($"[Scorpion] {gameObject.name} 베리에이션 설정: {variant}");
    }

    #endregion

    #region ⭐ Scorpion 전용 설정

    /// <summary>
    /// Scorpion 전용 설정 적용
    /// </summary>
    private void ApplyScorpionSpecificSettings()
    {
        if (EnemyData != null)
        {
            Debug.Log($"[Scorpion] {gameObject.name} 전용 설정 적용:");
            Debug.Log($"  - 타입: {EnemyData.EnemyType}");
            Debug.Log($"  - 레벨: {CurrentLevel}");
            Debug.Log($"  - 체력: {GetScaledMaxHealth():F1}");
            Debug.Log($"  - 공격력: AttackData에서 관리됨");
            Debug.Log($"  - 방어력: {GetScaledDefense():F1}");
            Debug.Log($"  - 이동속도: {GetScaledMoveSpeed():F1}");
            Debug.Log($"  - 특성: 독 공격, 빠른 이동, 넓은 경계 범위");
        }
    }

    /// <summary>
    /// Elite 타입으로 강제 변경 (테스트용)
    /// </summary>
    [ContextMenu("Force Elite Type")]
    private void ForceEliteType()
    {
        if (EnemyData != null)
        {
            Debug.Log($"[Scorpion] Elite 타입 적용 - 새 체력: {GetScaledMaxHealth():F1}");
            Debug.Log($"[Scorpion] 공격력은 AttackData에서 관리됨");
        }
    }

    /// <summary>
    /// 실제 데미지 테스트 (AttackData 기반)
    /// </summary>
    [ContextMenu("Test Melee Damage")]
    private void TestMeleeDamage()
    {
        if (meleeAttack != null)
        {
            int actualDamage = meleeAttack.GetScaledDamage();
            Debug.Log($"[Scorpion] 실제 근접 데미지: {actualDamage}");
        }
        else
        {
            Debug.LogError($"[Scorpion] MeleeAttack 컴포넌트가 없어서 데미지 테스트 불가!");
        }
    }

    /// <summary>
    /// 색상 변경 테스트 (우클릭 메뉴)
    /// </summary>
    [ContextMenu("Test Color - Red")]
    private void TestColorRed()
    {
        SetVariant(ScorpionVariant.Red);
    }

    [ContextMenu("Test Color - Blue")]
    private void TestColorBlue()
    {
        SetVariant(ScorpionVariant.Blue);
    }

    [ContextMenu("Test Color - Green")]
    private void TestColorGreen()
    {
        SetVariant(ScorpionVariant.Green);
    }

    #endregion

    #region ⭐ 레벨업 시스템

    /// <summary>
    /// Scorpion 레벨업 처리
    /// </summary>
    public override void LevelUp()
    {
        CurrentLevel += 1;
        
        // 새 스탯 계산
        ApplyScorpionSpecificSettings();
        
        Debug.Log($"[Scorpion] {gameObject.name} 레벨업! 새 레벨: {CurrentLevel}");
        Debug.Log($"[Scorpion] 새 체력: {GetScaledMaxHealth():F1}");
        Debug.Log($"[Scorpion] 공격력은 AttackData에서 관리됨");
    }

    #endregion

    #region ⭐ 디버깅 도구

    /// <summary>
    /// Scorpion 상태 디버그 출력
    /// </summary>
    [ContextMenu("Debug Scorpion Info")]
    private void DebugScorpionInfo()
    {
        string info = $"=== Scorpion {gameObject.name} ===\n";
        info += $"Current Level: {CurrentLevel}\n";
        info += $"Attack Type: 근접 + 독 (Melee + Poison)\n";
        info += $"Color Tint: {colorTint}\n";
        info += $"Patrol Radius: {PatrolRadius}\n";
        info += $"Attack Range: {AttackRange}\n";
        info += $"Chase Range: {ChaseRange}\n\n";
        
        if (EnemyData != null)
        {
            info += "=== EnemyData 정보 ===\n";
            info += EnemyData.GetDebugInfo(CurrentLevel, GrowthProfile);
        }
        else
        {
            info += "❌ EnemyData가 할당되지 않았습니다!\n";
        }

        // AttackData 정보 추가
        if (meleeAttack?.AttackData != null)
        {
            info += "\n=== AttackData 정보 ===\n";
            info += $"실제 데미지: {meleeAttack.GetScaledDamage()}\n";
        }
        else
        {
            info += "\n❌ AttackData가 할당되지 않았습니다!\n";
        }
        
        Debug.Log(info);
    }

    #endregion
    
    #region 🗺️ 아이소메트릭 데이터 시스템 (BaseEnemy 추상 메서드 구현)
    
    [Header("아이소메트릭 설정")]
    [SerializeField] private IsometricCharacterData isometricData = new IsometricCharacterData();

    /// <summary>
    /// BaseEnemy 추상 메서드 구현 - 아이소메트릭 데이터 반환
    /// </summary>
    public override IsometricCharacterData GetIsometricData()
    {
        // isometricData가 유효하지 않으면 기본값 생성
        if (isometricData == null || !isometricData.IsValid())
        {
            return CreateDefaultIsometricData();
        }
        
        return isometricData;
    }

    private void OnValidate()
    {
        // 아이소메트릭 데이터 기본값 설정
        if (isometricData == null)
        {
            isometricData = new IsometricCharacterData();
            isometricData.SetDefaults();
        }
        
        if (!isometricData.IsValid())
        {
            Debug.LogWarning($"[Scorpion] {name}의 아이소메트릭 데이터가 유효하지 않습니다.");
            isometricData.SetDefaults();
        }
    }

    #endregion
}



