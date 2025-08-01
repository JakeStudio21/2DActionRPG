using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Knockback : MonoBehaviour
{
    public bool GettingKnockedBack { get; private set; }

    // ❌ 제거: 설정값 필드들
    // [SerializeField] private float knockBackTime = .2f;
    // [SerializeField] private float defaultKnockBackThrust = 10f;
    
    // ✅ 런타임 설정값 (주입받음)
    private float knockBackTime = 0.2f;
    private float defaultKnockBackThrust = 10f;

    private Rigidbody2D rb;

    // ✅ 외부 접근용 프로퍼티 유지
    public float DefaultKnockBackThrust => defaultKnockBackThrust;

    private void Awake() {
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// ⭐ SRP: 넉백 설정값 주입 (BaseClassBehaviour에서 호출)
    /// </summary>
    public void SetKnockbackSettings(float thrust, float time)
    {
        defaultKnockBackThrust = thrust;
        knockBackTime = time;
        
        Debug.Log($"🔧 [Knockback] 설정 적용: Thrust={thrust}, Time={time}초");
    }

    public void GetKnockedBack(Transform damageSource, float knockBackThrust) {
        GettingKnockedBack = true;
        Vector2 difference = (transform.position - damageSource.position).normalized * knockBackThrust * rb.mass;
        rb.AddForce(difference, ForceMode2D.Impulse); 
        StartCoroutine(KnockRoutine());
    }

    private IEnumerator KnockRoutine() {
        yield return new WaitForSecondsRealtime(knockBackTime);
        rb.velocity = Vector2.zero;
        GettingKnockedBack = false;
    }
}
