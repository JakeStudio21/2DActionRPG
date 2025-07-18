using UnityEngine;

[CreateAssetMenu(fileName = "SkillData", menuName = "Skill System/Skill Data", order = 0)]
public class SkillData : ScriptableObject
{
    [Header("기본 정보")]
    public string skillName = "New Skill";
    public Sprite icon;

    [Header("스킬 수치")]
    public float cooldown = 2f;
    public float damage = 10f;
    public float range = 5f;

    [Header("발사체/이펙트")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 10f;
    public int projectileCount = 1;
    public float spreadAngle = 0f;
    public Vector3 projectileScale = Vector3.one;
    public GameObject effectPrefab;
}
