using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageSource : MonoBehaviour
{
    private int baseDamageAmount;  // ⭐ 기본 데미지 저장용
    private Warrior warrior;       // ⭐ Warrior 컴포넌트 참조
    private Assasin assasin; // 🆕 Assasin 컴포넌트 참조

    private void Start() {
        var activeWeapon = FindObjectOfType<ActiveWeapon>();
        MonoBehaviour currentActiveweapon = activeWeapon.CurrentActiveWeapon;
        
        // WeaponInfo → EquipmentData 변경
        EquipmentData equipmentData = (currentActiveweapon as IWeapon).GetEquipmentData();
        baseDamageAmount = (int)equipmentData.attackDamage;  // float → int 변환 추가
        
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

        // 🆕 Assasin 컴포넌트 찾기
        assasin = FindObjectOfType<Assasin>();
        if (assasin != null) {
            Debug.Log($"🏹 [DamageSource] Assasin 감지! 기본 데미지: {baseDamageAmount}");
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
            
            // 🆕 Assasin 크리티컬/백어택 적용
            if (assasin != null && assasin.IsActiveClass) {
                try {
                    finalDamage = Mathf.RoundToInt(assasin.GetModifiedDamage(baseDamageAmount));
                    
                    // 백어택 보너스 추가 판정
                    float backAttackMultiplier = assasin.GetBackAttackMultiplier(
                        transform.position, 
                        other.transform.position, 
                        other.transform.right // 적의 방향
                    );
                    finalDamage = Mathf.RoundToInt(finalDamage * backAttackMultiplier);
                    
                    Debug.Log($"🏹 [DamageSource] Assasin 데미지 적용! {baseDamageAmount} → {finalDamage}");
                } catch (System.Exception ex) {
                    Debug.LogWarning($"🟡 [DamageSource] Assasin 데미지 계산 실패: {ex.Message}");
                    finalDamage = baseDamageAmount;
                }
            }
            
            enemyHealth.TakeDamage(finalDamage);
        }
    }
}
