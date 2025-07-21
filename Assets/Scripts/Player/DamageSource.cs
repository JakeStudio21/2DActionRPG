using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageSource : MonoBehaviour
{
    private int baseDamageAmount;  // ⭐ 기본 데미지 저장용
    private Warrior warrior;       // ⭐ Warrior 컴포넌트 참조

    private void Start() {
        var activeWeapon = FindObjectOfType<ActiveWeapon>();
        MonoBehaviour currentActiveweapon = activeWeapon.CurrentActiveWeapon;
        baseDamageAmount = (currentActiveweapon as IWeapon).GetWeaponInfo().weaponDamage;
        
        // ⭐ [Phase B] Warrior 컴포넌트 찾기
        warrior = FindObjectOfType<Warrior>();
        if (warrior != null)
        {
            Debug.Log($"⚔️ [DamageSource] Warrior 감지! 기본 데미지: {baseDamageAmount}");
        }
        else
        {
            Debug.Log($"🔵 [DamageSource] 일반 무기 데미지: {baseDamageAmount}");
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        EnemyHealth enemyHealth = other.gameObject.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            // ⭐ [Phase B] Warrior 데미지 배율 적용
            int finalDamage = baseDamageAmount;
            
            if (warrior != null && warrior.IsActiveClass)  // ⭐ 수정: isActive → IsActiveClass
            {
                try
                {
                    finalDamage = Mathf.RoundToInt(warrior.GetModifiedDamage(baseDamageAmount));
                    Debug.Log($"⚔️ [DamageSource] Warrior 데미지 적용! {baseDamageAmount} → {finalDamage}");
                    
                    // 버서커 모드 상태도 로그
                    if (warrior.IsInBerserkerMode())
                    {
                        Debug.Log($"🔥 [DamageSource] 버서커 모드 활성! 추가 데미지 보너스");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"🟡 [DamageSource] Warrior 데미지 계산 실패: {ex.Message}");
                    finalDamage = baseDamageAmount; // 기본값 사용
                }
            }
            
            enemyHealth.TakeDamage(finalDamage);
        }
    }
}
