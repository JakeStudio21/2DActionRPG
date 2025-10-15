using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CueSystem; // 🆕 Cue 시스템 네임스페이스 추가
using System.Diagnostics; // 🔍 스택 트레이스용

public class Bow : MonoBehaviour, IWeapon
{
    // 🔍 디버깅용 static 카운터 (강화)
    private static int attackCallCount = 0;
    private static int spawnArrowCallCount = 0;
    private static float sessionStartTime = -1f;

    [SerializeField] private EquipmentData equipmentData;  // WeaponInfo → EquipmentData
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private Transform arrowSpawnPoint;

    // 🆕 추가: 마지막 공격 방향 저장 (Animation Event 지연 대비)
    private Vector2 lastAttackDirection = Vector2.right;
    private Quaternion lastAttackRotation = Quaternion.identity;
    
    // ⭐ 중복 호출 방지
    private float lastAttackTime = -1f;
    private float attackCooldown = 0.1f; // 0.1초 내 중복 호출 방지

    // ⭐ 애니메이션 트리거는 PlayerAnimationController에서 관리하므로 제거
    // readonly int FIRM_HASH = Animator.StringToHash("Fire");
    // private Animator myAnimator;

    private void Awake()
    {
        // ⭐ Animator 참조 제거 - PlayerAnimationController에서 관리
        // myAnimator = GetComponent<Animator>();
    }

    public void Attack()
    {
        // 🔍 호출 카운터 증가
        attackCallCount++;
        if (sessionStartTime < 0) sessionStartTime = Time.time;
        
        float currentTime = Time.time;
        float sessionTime = currentTime - sessionStartTime;
        
        UnityEngine.Debug.Log($"🔥🔥🔥 [Bow] Attack() 호출 #{attackCallCount} - 시간: {currentTime:F3} (세션: {sessionTime:F3}초)");
        UnityEngine.Debug.Log($"🔍 [Bow] 호출 스택 추적:");
        UnityEngine.Debug.Log($"   - lastAttackTime: {lastAttackTime:F3}");
        UnityEngine.Debug.Log($"   - attackCooldown: {attackCooldown}");
        UnityEngine.Debug.Log($"   - 시간 차이: {currentTime - lastAttackTime:F3}");
        
        // ⭐ 중복 호출 방지 체크
        if (currentTime - lastAttackTime < attackCooldown)
        {
            UnityEngine.Debug.LogWarning($"🟡 [Bow] Attack() 중복 호출 방지! #{attackCallCount} 차단됨");
            return;
        }
        lastAttackTime = currentTime;
        
        UnityEngine.Debug.Log($"✅ [Bow] Attack() #{attackCallCount} 실행 승인 → SpawnArrow() 호출");
        
        // 발사체 생성
        SpawnArrow();
    }
    
    /// <summary>
    /// 화살 생성 로직 (순수 스폰 담당)
    /// </summary>
    public void SpawnArrow()
    {
        // 🔍 호출 카운터 증가
        spawnArrowCallCount++;
        float currentTime = Time.time;
        float sessionTime = sessionStartTime > 0 ? currentTime - sessionStartTime : 0;
        
        UnityEngine.Debug.Log($"🎯🎯🎯 [Bow] SpawnArrow() 호출 #{spawnArrowCallCount} - 시간: {currentTime:F3} (세션: {sessionTime:F3}초)");
        
        // 🔍 호출 경로 추적 (스택 트레이스)
        System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
        UnityEngine.Debug.Log($"🔍 [Bow] SpawnArrow() 호출 경로:");
        for (int i = 1; i < Mathf.Min(4, stackTrace.FrameCount); i++)
        {
            var frame = stackTrace.GetFrame(i);
            UnityEngine.Debug.Log($"   #{i}: {frame.GetMethod().DeclaringType?.Name}.{frame.GetMethod().Name}()");
        }
        
        // N/S 방향 특별 확인 (저장된 회전값만 사용)
        Vector3 savedEuler = lastAttackRotation.eulerAngles;
        bool isNorthSouth = (savedEuler.z > 70f && savedEuler.z < 110f) || (savedEuler.z > 250f && savedEuler.z < 290f);
        if (isNorthSouth)
        {
            string directionName = (savedEuler.z > 70f && savedEuler.z < 110f) ? "NORTH" : "SOUTH";
            UnityEngine.Debug.Log($"🧭 [Bow] #{spawnArrowCallCount} {directionName} 방향 화살 발사! 각도: {savedEuler.z:F1}°");
        }
        
        // 🔧 저장된 회전값 사용 (Animation Event 지연 문제 해결)
        GameObject newArrow = GamePoolManager.Instance.SpawnFromPool(
            arrowPrefab.name, 
            arrowSpawnPoint.position, 
            lastAttackRotation  // ← 저장된 회전값 사용
        );
        
        if (newArrow != null)
        {
            UnityEngine.Debug.Log($"✅ [Bow] 화살 #{spawnArrowCallCount} 생성 성공! 각도: {savedEuler.z:F1}°");
            
            if (newArrow.TryGetComponent(out Projectile projectile))
            {
                projectile.UpdateProjectileRange(equipmentData.WeaponRange);
            }
        }
        else
        {
            UnityEngine.Debug.LogError($"❌ [Bow] 화살 #{spawnArrowCallCount} 생성 실패! 풀: {arrowPrefab.name}");
        }
    }

    public EquipmentData GetEquipmentData()  // WeaponInfo → EquipmentData
    {
        return equipmentData;
    }

    public void UpdateDirection(Vector2 direction, bool facingLeft)
    {
        // 조이스틱 방향에 따른 무기 회전 (facingLeft 무시)
        if (direction.magnitude > 0.1f)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
            
            // 🔍 N/S 방향만 특별 추적 (디버깅 간소화)
            bool isNorthSouth = Mathf.Abs(direction.x) < 0.3f && Mathf.Abs(direction.y) > 0.7f;
            if (isNorthSouth)
            {
                string directionName = direction.y > 0 ? "NORTH" : "SOUTH";
                UnityEngine.Debug.Log($"🧭 [Bow] {directionName} 방향 저장! 각도: {angle:F1}°");
            }
            
            // 🆕 방향 저장: Animation Event 지연을 대비해 입력 시점의 방향 보존
            lastAttackDirection = direction.normalized;
            lastAttackRotation = Quaternion.Euler(0, 0, angle);
        }
    }

} 