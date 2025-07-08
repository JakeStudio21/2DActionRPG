using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sword : MonoBehaviour, IWeapon
{
    [SerializeField] private GameObject slashPrefab;
    [SerializeField] private Transform slashSpawnPoint;
    [SerializeField] private WeaponInfo weaponInfo;

    private Animator myAnimator;
    private Transform weaponCollider;
    private ActiveWeapon activeWeapon;

    private GameObject slashAnim;

    // private float baseSwordX = 0.2f; // 오른손 기준 위치 [미사용]
    // private float baseColliderX = 0.2f; // 오른손 기준 위치 [미사용]

    private void Awake() {
        myAnimator = GetComponent<Animator>();
    }

    private void Start() {
        weaponCollider = FindObjectOfType<PlayerController>().GetWeaponCollider();
        slashSpawnPoint = GameObject.Find("SlashSpawnPoint").transform;
        activeWeapon = FindObjectOfType<ActiveWeapon>();
    }

    public WeaponInfo GetWeaponInfo() 
    {
        return weaponInfo;
    }

    public void Attack() {

            myAnimator.SetTrigger("Attack");
            weaponCollider.gameObject.SetActive(true);
            slashAnim = GamePoolManager.Instance.SpawnFromPool("Slash Prefab", slashSpawnPoint.position, Quaternion.identity);
            slashAnim.transform.parent = this.transform.parent;
    }

    public void DoneAttackingAnimEnvet() {
        weaponCollider.gameObject.SetActive(false);
    }

    public void SwingUpFlipAnimEvent() {
        slashAnim.gameObject.transform.rotation = Quaternion.Euler(-180, 0, 0);

        if (FindObjectOfType<PlayerController>().FacingLeft) { 
            slashAnim.GetComponent<SpriteRenderer>().flipX = true;
        }
    }

    public void SwingDownFlipAnimEvent() {
        slashAnim.gameObject.transform.rotation = Quaternion.Euler(0, 0, 0);

        if (FindObjectOfType<PlayerController>().FacingLeft)
        {
            slashAnim.GetComponent<SpriteRenderer>().flipX = true;
        }
    }

    public void UpdateDirection(Vector2 direction, bool facingLeft)
    {
        // 검 SpriteRenderer flipX (이중 반전 방지 위해 주석처리)
        // var sr = GetComponent<SpriteRenderer>();
        // if (sr != null) sr.flipX = facingLeft;

        // [신규] ActiveWeapon의 localScale.x를 ±1로 반전
        if (transform.parent != null)
        {
            Vector3 scale = transform.parent.localScale;
            scale.x = facingLeft ? -1 : 1;
            transform.parent.localScale = scale;
            Debug.Log($"[ActiveWeapon] localScale.x: {transform.parent.localScale.x}, facingLeft: {facingLeft}");
        }

        // [백업: 기존 위치 하드코딩 방식]
        // transform.localPosition = new Vector3(facingLeft ? -baseSwordX : baseSwordX, transform.localPosition.y, transform.localPosition.z);
        // Debug.Log($"[Sword] {gameObject.name} localPosition.x: {transform.localPosition.x}, facingLeft: {facingLeft}");
        // if (transform.parent != null)
        // {
        //     var weaponCollider = transform.parent.Find("WeaponCollider");
        //     if (weaponCollider != null)
        //     {
        //         weaponCollider.localPosition = new Vector3(facingLeft ? -baseColliderX : baseColliderX, weaponCollider.localPosition.y, weaponCollider.localPosition.z);
        //         Debug.Log($"[WeaponCollider] {weaponCollider.name} localPosition.x: {weaponCollider.localPosition.x}, facingLeft: {facingLeft}");
        //     }
        //     else
        //     {
        //         Debug.LogWarning("[WeaponCollider] WeaponCollider 오브젝트를 찾지 못했습니다.");
        //     }
        // }
    }
} 