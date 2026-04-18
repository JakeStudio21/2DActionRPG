using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemy Animation Controller - DirectionPreset 기반 8방향 시스템
/// PlayerAnimationController와 동일한 구조로 E4M/E8 방향 애니메이션 지원
/// </summary>
public class EnemyAnimationController : MonoBehaviour
{
    [Header("컴포넌트 참조")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    [Header("🗺️ 아이소메트릭 설정")]
    [SerializeField] private bool useIsometricData = true; // 아이소메트릭 데이터 사용 여부
    
    [Header("디버그 설정")]
    
    // 내부 참조
    private IEnemy enemy; // BaseEnemy 참조로 IsometricData 접근
    private DirectionPreset currentDirectionPreset = DirectionPreset.E4M;
    
    [Header("이동 판정 임계값 (히스테리시스)")]
    [SerializeField] private float startMoveThreshold = 0.12f;  // 이 값 이상이면 이동 시작
    [SerializeField] private float stopMoveThreshold = 0.06f;   // 이 값 미만이면 이동 종료
    
    // Animation Parameter Hashes (성능 최적화)
    private readonly int MOVE_X_HASH = Animator.StringToHash("moveX");
    private readonly int MOVE_Y_HASH = Animator.StringToHash("moveY");
    private readonly int SPEED_HASH = Animator.StringToHash("speed");
    private readonly int IS_MOVING_HASH = Animator.StringToHash("isMoving");
    private readonly int ATTACK_TRIGGER_HASH = Animator.StringToHash("Attack");
    private readonly int HIT_TRIGGER_HASH = Animator.StringToHash("Hit");
    private readonly int DIE_TRIGGER_HASH = Animator.StringToHash("Die");
    
    // ⚡ 스킬 관련 Parameters (엘리트/보스 전용)
    private readonly int IS_SKILL_CASTING_HASH = Animator.StringToHash("isSkillCasting");
    private readonly int IS_SKILL_ACTION_HASH = Animator.StringToHash("isSkillAction");
    private readonly int SKILL_CAST_TRIGGER_HASH = Animator.StringToHash("SkillCast");
    private readonly int SKILL_ACTION_TRIGGER_HASH = Animator.StringToHash("SkillAction");
    
    // 마지막 이동 방향 저장 (정지 시 방향 유지용)
    private Vector2 lastMoveDirection = Vector2.down; // 기본값: 남쪽
    
    // 현재 방향 저장 (공격 시 사용)
    private Vector2 currentDirection = Vector2.down;
    
    // 내부 이동 상태 캐시 (바운싱 방지)
    private bool isCurrentlyMoving = false;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
            
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
            
        // BaseEnemy 참조 획득 (IsometricData 접근용)
        enemy = GetComponent<IEnemy>();
            
        if (animator == null)
        {
            Debug.LogError($"[EnemyAnimationController] {gameObject.name} - Animator 컴포넌트를 찾을 수 없습니다!");
        }
        
        if (spriteRenderer == null)
        {
            Debug.LogError($"[EnemyAnimationController] {gameObject.name} - SpriteRenderer 컴포넌트를 찾을 수 없습니다!");
        }
        
        // 🗺️ DirectionPreset 초기화
        InitializeDirectionPreset();
    }
    
    /// <summary>
    /// DirectionPreset 초기화 (BaseEnemy의 IsometricData에서 가져옴)
    /// </summary>
    private void InitializeDirectionPreset()
    {
        if (!useIsometricData)
        {
            currentDirectionPreset = DirectionPreset.E4M; // 기본값
            return;
        }
        
        // BaseEnemy에서 DirectionPreset 가져오기
        var baseEnemy = GetComponent<BaseEnemy>();
        if (baseEnemy != null)
        {
            currentDirectionPreset = baseEnemy.GetDirectionPreset();
            
        }
        else
        {
            currentDirectionPreset = DirectionPreset.E4M; // fallback
            Debug.LogWarning($"[EnemyAnimationController] {gameObject.name} - BaseEnemy를 찾을 수 없어 E4M 기본값 사용");
        }
    }
    
    #region ⭐ DirectionPreset 기반 방향 시스템 (PlayerAnimationController와 동일)
    
    /// <summary>
    /// 이동 방향에 따른 애니메이션 업데이트 (DirectionPreset 기반)
    /// </summary>
    public void UpdateMovementAnimation(Vector2 direction)
    {
        if (animator == null || spriteRenderer == null) return;
        
        // 속도 계산
        float speed = direction.magnitude;
        
        if (HasParameter("speed"))
            animator.SetFloat(SPEED_HASH, speed);
            
        UpdateMotionFlags(speed);
        
        
        // 정지 시 마지막 방향 유지
        if (speed < 0.1f)
        {
            SetIdleDirection(lastMoveDirection);
            return;
        }
        
        // 이동 중: 현재 방향 저장 및 애니메이션 적용
        Vector2 dir = direction.normalized;
        lastMoveDirection = dir;
        currentDirection = dir;
        
        ApplyDirectionToAnimator(dir);
        
    }

    /// <summary>
    /// 실제 이동 속도 벡터(velocity)로 애니메이션 업데이트
    /// - velocity의 크기를 speed로 사용하여 Idle/Walk 전환을 정확히 반영
    /// - 방향은 velocity.normalized를 사용
    /// </summary>
    public void UpdateMovementByVelocity(Vector2 velocity)
    {
        if (animator == null || spriteRenderer == null) return;

        float speed = velocity.magnitude;

        if (HasParameter("speed"))
            animator.SetFloat(SPEED_HASH, speed);

        UpdateMotionFlags(speed);

        if (speed < 0.1f)
        {
            SetIdleDirection(lastMoveDirection);
            return;
        }

        Vector2 dirWorld = velocity.normalized;
        
        // ⭐ 임시: 변환 없이 월드 좌표 그대로 사용 (테스트용)
        Vector2 dirBlendTree = dirWorld;
        
        // ⭐ 월드 좌표계 기반 flipX 결정
        bool shouldFlipX = dirWorld.x < 0;
        spriteRenderer.flipX = shouldFlipX;
        
        lastMoveDirection = dirBlendTree;
        currentDirection = dirBlendTree;
        
        // ⭐ flipX가 이미 설정되었으므로 WithoutFlip 메서드 사용
        ApplyDirectionToAnimatorWithoutFlip(dirBlendTree);

    }
    
    /// <summary>
    /// 공격 방향 설정 (공격 전 호출)
    /// </summary>
    public void UpdateAttackDirection(Vector2 targetDirection)
    {
        if (targetDirection.magnitude < 0.1f)
        {
            // 방향이 없으면 마지막 방향 사용
            targetDirection = lastMoveDirection;
        }
        
        Vector2 dir = targetDirection.normalized;
        currentDirection = dir;
        lastMoveDirection = dir;
        
        ApplyDirectionToAnimator(dir);
        
    }
    
    /// <summary>
    /// ⭐ 신규: 공격 방향 + flipX 동시 설정 (아이소메트릭 좌표계 지원)
    /// </summary>
    public void UpdateAttackDirectionWithFlip(Vector2 targetDirection, bool shouldFlipX)
    {
        if (spriteRenderer == null) return;
        
        if (targetDirection.magnitude < 0.1f)
        {
            // 방향이 없으면 마지막 방향 사용
            targetDirection = lastMoveDirection;
        }
        
        Vector2 dir = targetDirection.normalized;
        currentDirection = dir;
        lastMoveDirection = dir;
        
        // ⭐ flipX를 먼저 설정 (월드 좌표계 기반)
        spriteRenderer.flipX = shouldFlipX;
        
        // ⭐ 아이소메트릭 좌표계 방향 적용 (flipX 고려하여)
        ApplyDirectionToAnimatorWithoutFlip(dir);
        
    }
    
    /// <summary>
    /// 정지 시 마지막 방향 유지 (Idle 애니메이션)
    /// </summary>
    public void SetIdleDirection(Vector2 direction)
    {
        if (direction.magnitude < 0.1f)
        {
            direction = lastMoveDirection;
        }
        
        Vector2 dir = direction.normalized;
        ApplyDirectionToAnimator(dir);
        
    }

    /// <summary>
    /// Idle 상태 강제 적용: speed=0, isMoving=false, 마지막 방향 유지
    /// </summary>
    public void ForceIdle()
    {
        if (HasParameter("speed"))
            animator.SetFloat(SPEED_HASH, 0f);
        if (HasParameter("isMoving"))
            animator.SetBool(IS_MOVING_HASH, false);
        isCurrentlyMoving = false;
        SetIdleDirection(lastMoveDirection);
    }
    
    /// <summary>
    /// ⭐ 신규: 현재 방향을 유지하면서 Idle 상태만 적용 (공격 중 사용)
    /// </summary>
    public void ForceIdleKeepDirection()
    {
        if (animator == null) return;
        
        if (HasParameter("speed"))
            animator.SetFloat(SPEED_HASH, 0f);
        if (HasParameter("isMoving"))
            animator.SetBool(IS_MOVING_HASH, false);
        isCurrentlyMoving = false;
        
        // ⚠️ SetIdleDirection 호출 안 함 → 방향 유지!
    }

    /// <summary>
    /// 이동/정지 판정에 히스테리시스 적용 (깜빡임 방지)
    /// </summary>
    private void UpdateMotionFlags(float speed)
    {
        // 시작/종료 임계값 사이에서는 이전 상태 유지
        if (!isCurrentlyMoving)
        {
            if (speed > startMoveThreshold)
            {
                isCurrentlyMoving = true;
            }
        }
        else
        {
            if (speed < stopMoveThreshold)
            {
                isCurrentlyMoving = false;
            }
        }
        
        if (HasParameter("isMoving"))
            animator.SetBool(IS_MOVING_HASH, isCurrentlyMoving);
        if (HasParameter("speed"))
            animator.SetFloat(SPEED_HASH, speed);
    }
    
    /// <summary>
    /// 방향을 Animator 파라미터로 적용 (DirectionPreset 기반)
    /// </summary>
    private void ApplyDirectionToAnimator(Vector2 dir)
    {
        if (animator == null || spriteRenderer == null) 
        {
            Debug.LogError($"❌ [{gameObject.name}] Animator 또는 SpriteRenderer가 null!");
            return;
        }
        
        // 🗺️ 원본 방향 사용 (BlendTree 자동 보간)
        Vector2 convertedDir = ConvertToDirection(dir);
        
        // ⭐ 강제 디버그: SetFloat 호출 전 값 확인
        
        // ⬅️ 좌측 방향: flipX = true, moveX는 양수로 변환
        if (convertedDir.x < -0.1f)
        {
            spriteRenderer.flipX = true;
            
            float finalMoveX = Mathf.Abs(convertedDir.x);
            float finalMoveY = convertedDir.y;
            
            if (HasParameter("moveX"))
                animator.SetFloat(MOVE_X_HASH, finalMoveX);
            if (HasParameter("moveY"))
                animator.SetFloat(MOVE_Y_HASH, finalMoveY);
            
            // ⭐ 강제 디버그: SetFloat 호출 후 실제 값 확인
        }
        // ➡️ 우측 방향: flipX = false, 그대로 사용
        else if (convertedDir.x > 0.1f)
        {
            spriteRenderer.flipX = false;
            
            float finalMoveX = convertedDir.x;
            float finalMoveY = convertedDir.y;
            
            if (HasParameter("moveX"))
                animator.SetFloat(MOVE_X_HASH, finalMoveX);
            if (HasParameter("moveY"))
                animator.SetFloat(MOVE_Y_HASH, finalMoveY);
            
            // ⭐ 강제 디버그: SetFloat 호출 후 실제 값 확인
        }
        // ⬆️⬇️ 수직 방향: flipX 유지, moveX = 0
        else
        {
            float finalMoveX = 0f;
            float finalMoveY = convertedDir.y;
            
            if (HasParameter("moveX"))
                animator.SetFloat(MOVE_X_HASH, finalMoveX);
            if (HasParameter("moveY"))
                animator.SetFloat(MOVE_Y_HASH, finalMoveY);
            
            // ⭐ 강제 디버그: SetFloat 호출 후 실제 값 확인
        }
    }
    
    /// <summary>
    /// ⭐ 신규: flipX 없이 방향만 Animator에 적용 (이미 flipX가 설정된 경우)
    /// </summary>
    private void ApplyDirectionToAnimatorWithoutFlip(Vector2 dir)
    {
        if (animator == null || spriteRenderer == null) 
        {
            Debug.LogError($"❌ [{gameObject.name}] Animator 또는 SpriteRenderer가 null!");
            return;
        }
        
        // 🗺️ 원본 방향 사용 (BlendTree 자동 보간)
        Vector2 convertedDir = ConvertToDirection(dir);
        
        // ⭐ flipX에 따라 moveX 처리
        // flipX가 true면 moveX를 양수로, false면 그대로
        float finalMoveX = spriteRenderer.flipX ? Mathf.Abs(convertedDir.x) : convertedDir.x;
        float finalMoveY = convertedDir.y;
        
        if (HasParameter("moveX"))
            animator.SetFloat(MOVE_X_HASH, finalMoveX);
        if (HasParameter("moveY"))
            animator.SetFloat(MOVE_Y_HASH, finalMoveY);
        
    }
    
    /// <summary>
    /// DirectionPreset에 따른 방향 변환
    /// </summary>
    private Vector2 ConvertToDirection(Vector2 direction)
    {
        direction = direction.normalized;
        
        // ⭐ BlendTree 자동 보간 방식 사용 (권장)
        // BlendTree가 moveX, moveY 값으로 자동으로 가장 가까운 애니메이션을 선택하고 보간함
        // 5방향 애니메이션이든 8방향 애니메이션이든 상관없이 작동
        
        return direction; // ⭐ 항상 원본 방향 반환
        
        /* 
        // ❌ 이전 방식: 방향 스냅 (BlendTree 보간을 무력화시킴)
        if (currentDirectionPreset == DirectionPreset.E4M)
        {
            return ConvertToE4M(direction);  // 8개 방향 중 하나로 강제 스냅
        }
        else if (currentDirectionPreset == DirectionPreset.E8)
        {
            return ConvertToE8(direction);   // 8개 방향 중 하나로 강제 스냅
        }
        */
    }
    
    /// <summary>
    /// E4M (4방향+미러링) 변환
    /// 5개 방향(E, NE, N, SE, S)만 사용하고 좌측은 미러링
    /// </summary>
    private Vector2 ConvertToE4M(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;
        
        // 4방향+미러링 변환 (45도 간격)
        if (angle >= 337.5f || angle < 45f)
            return Vector2.right;                    // E (0°)
        else if (angle >= 45f && angle < 90f)
            return new Vector2(0.7071f, 0.7071f);   // NE (45°)
        else if (angle >= 90f && angle < 135f)
            return Vector2.up;                       // N (90°)
        else if (angle >= 135f && angle < 180f)
            return new Vector2(-0.7071f, 0.7071f);  // NW (135°) - 미러링으로 NE 사용
        else if (angle >= 180f && angle < 225f)
            return Vector2.left;                     // W (180°) - 미러링으로 E 사용
        else if (angle >= 225f && angle < 270f)
            return new Vector2(-0.7071f, -0.7071f); // SW (225°) - 미러링으로 SE 사용
        else if (angle >= 270f && angle < 315f)
            return Vector2.down;                     // S (270°)
        else
            return new Vector2(0.7071f, -0.7071f);  // SE (315°)
    }
    
    /// <summary>
    /// E8 (8방향) 변환
    /// 8개 방향 모두 개별 애니메이션 사용
    /// </summary>
    private Vector2 ConvertToE8(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;
        
        // 8방향 정밀 변환 (22.5도 간격)
        if (angle >= 337.5f || angle < 22.5f)
            return Vector2.right;                    // E (0°)
        else if (angle >= 22.5f && angle < 67.5f)
            return new Vector2(0.7071f, 0.7071f);   // NE (45°)
        else if (angle >= 67.5f && angle < 112.5f)
            return Vector2.up;                       // N (90°)
        else if (angle >= 112.5f && angle < 157.5f)
            return new Vector2(-0.7071f, 0.7071f);  // NW (135°)
        else if (angle >= 157.5f && angle < 202.5f)
            return Vector2.left;                     // W (180°)
        else if (angle >= 202.5f && angle < 247.5f)
            return new Vector2(-0.7071f, -0.7071f); // SW (225°)
        else if (angle >= 247.5f && angle < 292.5f)
            return Vector2.down;                     // S (270°)
        else
            return new Vector2(0.7071f, -0.7071f);  // SE (315°)
    }
    
    /// <summary>
    /// 현재 바라보는 방향 조회
    /// </summary>
    public Vector2 GetCurrentDirection()
    {
        return currentDirection;
    }
    
    /// <summary>
    /// 마지막 이동 방향 조회
    /// </summary>
    public Vector2 GetLastMoveDirection()
    {
        return lastMoveDirection;
    }
    
    #endregion
    
    #region ⭐ 기존 트리거 메서드들 (유지)
    
    public void PlayAttack()  
    {
        if (animator != null)
        {
            animator.SetTrigger(ATTACK_TRIGGER_HASH);
            
        }
        else
        {
            Debug.LogError($"[EnemyAnimationController] {gameObject.name} - Animator가 null입니다!");
        }
    }
    
    public void PlayHit()     
    { 
        if (animator != null)
        {
            animator.SetTrigger(HIT_TRIGGER_HASH);
            
        }
    }
    
    public void PlayDie()     
    { 
        if (animator != null)
        {
            animator.SetTrigger(DIE_TRIGGER_HASH);
            
        }
    }
    
    #endregion
    
    #region ⭐ 유틸리티 메서드
    
    /// <summary>
    /// Animator에 특정 파라미터가 존재하는지 확인
    /// </summary>
    private bool HasParameter(string parameterName)
    {
        if (animator == null) return false;
        
        foreach (var parameter in animator.parameters)
        {
            if (parameter.name == parameterName)
                return true;
        }
        return false;
    }
    
    #endregion
    
    #region ⚡ 스킬 애니메이션 (엘리트/보스 전용)
    
    /// <summary>
    /// 스킬 캐스팅 트리거
    /// </summary>
    public void TriggerSkillCast()
    {
        if (animator != null)
        {
            animator.SetTrigger(SKILL_CAST_TRIGGER_HASH);
            animator.SetBool(IS_SKILL_CASTING_HASH, true);
            
        }
    }
    
    /// <summary>
    /// 스킬 액션 트리거
    /// </summary>
    public void TriggerSkillAction()
    {
        if (animator != null)
        {
            animator.SetTrigger(SKILL_ACTION_TRIGGER_HASH);
            animator.SetBool(IS_SKILL_CASTING_HASH, false);
            animator.SetBool(IS_SKILL_ACTION_HASH, true);
            
        }
    }
    
    /// <summary>
    /// 스킬 캐스팅 상태 설정
    /// </summary>
    public void SetSkillCasting(bool value)
    {
        if (animator != null)
        {
            animator.SetBool(IS_SKILL_CASTING_HASH, value);
            
        }
    }
    
    /// <summary>
    /// 스킬 액션 상태 설정
    /// </summary>
    public void SetSkillAction(bool value)
    {
        if (animator != null)
        {
            animator.SetBool(IS_SKILL_ACTION_HASH, value);
            
        }
    }
    
    /// <summary>
    /// 모든 스킬 상태 리셋
    /// </summary>
    public void ResetSkillStates()
    {
        if (animator != null)
        {
            animator.SetBool(IS_SKILL_CASTING_HASH, false);
            animator.SetBool(IS_SKILL_ACTION_HASH, false);
            
        }
    }
    
    #endregion
}
