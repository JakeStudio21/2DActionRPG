using UnityEngine;

[ExecuteAlways]
public class ColliderGizmosDrawer : MonoBehaviour
{
    public Color gizmoColor = Color.green;
    public static bool showGizmos = true;
    
    [Header("Monster Range Settings")]
    public bool showMonsterRanges = true;
    public Color attackRangeColor = Color.red;
    public Color roamingRangeColor = Color.magenta;
    public Color detectionRangeColor = Color.yellow;
    public Color chaseRangeColor = new Color(1f, 0.5f, 0f, 1f); // 주황색

    void Update()
    {
        // Shift+F1 단축키로 토글
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.F1))
        {
            showGizmos = !showGizmos;
            Debug.Log("콜라이더 표시: " + (showGizmos ? "ON" : "OFF") + " (Shift+F1로 토글)");
        }
        
        // Shift+F2 단축키로 몬스터 범위 토글
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.F2))
        {
            showMonsterRanges = !showMonsterRanges;
            Debug.Log("몬스터 범위 표시: " + (showMonsterRanges ? "ON" : "OFF") + " (Shift+F2로 토글)");
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // 기존 콜라이더 표시
        DrawColliderGizmos();
        
        // 몬스터 범위 표시
        if (showMonsterRanges)
        {
            DrawMonsterRanges();
        }
    }
    
    private void DrawColliderGizmos()
    {
        Gizmos.color = gizmoColor;

        // BoxCollider2D
        var box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.offset, box.size);
        }

        // CircleCollider2D
        var circle = GetComponent<CircleCollider2D>();
        if (circle != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            DrawWireCircle(circle.offset, circle.radius);
        }

        // CapsuleCollider2D
        var capsule = GetComponent<CapsuleCollider2D>();
        if (capsule != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            DrawWireCapsule2D(capsule);
        }
        
        // CompositeCollider2D 🆕 추가
        var composite = GetComponent<CompositeCollider2D>();
        if (composite != null)
        {
            Gizmos.matrix = Matrix4x4.identity; // CompositeCollider2D는 월드 좌표 사용
            DrawCompositeCollider2D(composite);
        }
        
        // PolygonCollider2D 🆕 추가
        var polygon = GetComponent<PolygonCollider2D>();
        if (polygon != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            DrawPolygonCollider2D(polygon);
        }
    }
    
    private void DrawMonsterRanges()
    {
        Gizmos.matrix = Matrix4x4.identity; // 월드 좌표계 사용
        
        // BlueSlime 범위들
        var blueSlime = GetComponent<BlueSlime>();
        if (blueSlime != null)
        {
            float attackRange = blueSlime.AttackRange;
            Vector2 spawnPoint = blueSlime.SpawnPoint;
            float patrolRadius = blueSlime.PatrolRadius;
            
            // ✅ 스폰 지점 중심 로밍 범위 (보라색)
            Gizmos.color = roamingRangeColor;
            Gizmos.DrawWireSphere(spawnPoint, patrolRadius);
            
            // ✅ 공격 범위 표시 (빨간색)
            Gizmos.color = attackRangeColor;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // ✅ 스폰 지점 마커 (흰색 큐브) - 더 크게 표시
            Gizmos.color = Color.white;
            Gizmos.DrawCube(spawnPoint, Vector3.one * 0.5f); // 0.3f → 0.5f로 크기 증가
            
            // ✅ 스폰 지점 테두리 (검은색)
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 현재 위치와 스폰 지점 연결선 (회색)
            if (Vector2.Distance(transform.position, spawnPoint) > 0.1f)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(transform.position, spawnPoint);
            }
            
            // ✅ 감지 범위 - MeleeAttack에서 가져오기
            var meleeAttack = GetComponent<MeleeAttack>();
            if (meleeAttack != null)
            {
                float detectionRange = meleeAttack.GetDetectionRange();
                float chaseRange = meleeAttack.GetChaseRange();
                
                // 🟡 감지 범위 (노란색) - 플레이어를 처음 발견하는 범위
                Gizmos.color = detectionRangeColor;
                Gizmos.DrawWireSphere(transform.position, detectionRange);
                
                // 🟠 추격 범위 (주황색) - 추격을 포기하는 범위
                Gizmos.color = chaseRangeColor;
                Gizmos.DrawWireSphere(transform.position, chaseRange);
                
#if UNITY_EDITOR
                if (Application.isPlaying)
                {
                    Vector2 currentPos = transform.position;
                    float distanceFromSpawn = Vector2.Distance(currentPos, spawnPoint);
                    
                    UnityEditor.Handles.Label(transform.position + Vector3.up * 2, 
                        $"BlueSlime\\n로밍: {patrolRadius:F1}f\\n감지: {detectionRange:F1}f\\n추격: {chaseRange:F1}f\\n공격: {attackRange:F1}f\\n스폰거리: {distanceFromSpawn:F1}f\\n스폰지점: ({spawnPoint.x:F1}, {spawnPoint.y:F1})");
                }
#endif
            }
            
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                // 플레이어와의 거리 표시
                if (blueSlime.TargetPlayer != null)
                {
                    float dist = Vector2.Distance(transform.position, blueSlime.TargetPlayer.transform.position);
                    UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, 
                        $"Distance: {dist:F2}");
                    
                    // 플레이어와의 연결선
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(transform.position, blueSlime.TargetPlayer.transform.position);
                }
            }
#endif
        }
        
        // Grape 범위들
        var grape = GetComponent<Grape>();
        if (grape != null)
        {
            Vector2 spawnPoint = grape.SpawnPoint;
            float patrolRadius = grape.PatrolRadius;
            
            // ✅ 스폰 지점 중심 로밍 범위 (보라색)
            Gizmos.color = roamingRangeColor;
            Gizmos.DrawWireSphere(spawnPoint, patrolRadius);
            
            // ✅ 스폰 지점 마커 (흰색 큐브) - 더 크게 표시
            Gizmos.color = Color.white;
            Gizmos.DrawCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 스폰 지점 테두리 (검은색)
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 현재 위치와 스폰 지점 연결선 (회색)
            if (Vector2.Distance(transform.position, spawnPoint) > 0.1f)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(transform.position, spawnPoint);
            }
            
            // ✅ RangedAttack 범위들
            var rangedAttack = GetComponent<RangedAttack>();
            if (rangedAttack != null)
            {
                float detectionRange = rangedAttack.GetDetectionRange();
                float chaseRange = rangedAttack.GetChaseRange();
                
                // 🟡 감지 범위 (노란색) - 플레이어를 처음 발견하는 범위
                Gizmos.color = detectionRangeColor;
                Gizmos.DrawWireSphere(transform.position, detectionRange);
                
                // 🟠 추격 범위 (주황색) - 추격을 포기하는 범위
                Gizmos.color = chaseRangeColor;
                Gizmos.DrawWireSphere(transform.position, chaseRange);
                
#if UNITY_EDITOR
                if (Application.isPlaying)
                {
                    Vector2 currentPos = transform.position;
                    float distanceFromSpawn = Vector2.Distance(currentPos, spawnPoint);
                    
                    UnityEditor.Handles.Label(transform.position + Vector3.up * 2, 
                        $"Grape\\n로밍: {patrolRadius:F1}f\\n감지: {detectionRange:F1}f\\n추격: {chaseRange:F1}f\\n공격: 3.5f\\n스폰거리: {distanceFromSpawn:F1}f\\n스폰지점: ({spawnPoint.x:F1}, {spawnPoint.y:F1})");
                }
#endif
            }
            
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                // 플레이어와의 거리 표시
                if (grape.TargetPlayer != null)
                {
                    float dist = Vector2.Distance(transform.position, grape.TargetPlayer.transform.position);
                    UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, 
                        $"Distance: {dist:F2}");
                    
                    // 플레이어와의 연결선
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(transform.position, grape.TargetPlayer.transform.position);
                }
            }
#endif
        }
        
        // StoneGolem 범위들
        var stoneGolem = GetComponent<StoneGolem>();
        if (stoneGolem != null)
        {
            Vector2 spawnPoint = stoneGolem.SpawnPosition;
            float patrolRadius = stoneGolem.PatrolRadius;
            float attackRange = stoneGolem.AttackRange;
            float detectionRange = stoneGolem.DetectionRange;
            float chaseRange = stoneGolem.ChaseRange;
            
            // ✅ 스폰 지점 중심 로밍 범위 (보라색)
            Gizmos.color = roamingRangeColor;
            Gizmos.DrawWireSphere(spawnPoint, patrolRadius);
            
            // ✅ 공격 범위 표시 (빨간색)
            Gizmos.color = attackRangeColor;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // 🟡 감지 범위 (노란색) - 플레이어를 처음 발견하는 범위
            Gizmos.color = detectionRangeColor;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // 🟠 추격 범위 (주황색) - 추격을 포기하는 범위
            Gizmos.color = chaseRangeColor;
            Gizmos.DrawWireSphere(transform.position, chaseRange);
            
            // ✅ 스폰 지점 마커 (흰색 큐브)
            Gizmos.color = Color.white;
            Gizmos.DrawCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 스폰 지점 테두리 (검은색)
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 현재 위치와 스폰 지점 연결선 (회색)
            if (Vector2.Distance(transform.position, spawnPoint) > 0.1f)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(transform.position, spawnPoint);
            }
            
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2, 
                    $"StoneGolem\n공격: {attackRange:F1} | 감지: {detectionRange:F1}\n추격: {chaseRange:F1} | 순찰: {patrolRadius:F1}");
            }
#endif
        }
        
        // CrystalGolem 범위들
        var crystalGolem = GetComponent<CrystalGolem>();
        if (crystalGolem != null)
        {
            Vector2 spawnPoint = crystalGolem.SpawnPosition;
            float patrolRadius = crystalGolem.PatrolRadius;
            float attackRange = crystalGolem.AttackRange;
            float detectionRange = crystalGolem.DetectionRange;
            float chaseRange = crystalGolem.ChaseRange;
            
            // ✅ 스폰 지점 중심 로밍 범위 (보라색)
            Gizmos.color = roamingRangeColor;
            Gizmos.DrawWireSphere(spawnPoint, patrolRadius);
            
            // ✅ 공격 범위 표시 (빨간색)
            Gizmos.color = attackRangeColor;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // 🟡 감지 범위 (노란색) - 플레이어를 처음 발견하는 범위
            Gizmos.color = detectionRangeColor;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // 🟠 추격 범위 (주황색) - 추격을 포기하는 범위
            Gizmos.color = chaseRangeColor;
            Gizmos.DrawWireSphere(transform.position, chaseRange);
            
            // ✅ 스폰 지점 마커 (흰색 큐브)
            Gizmos.color = Color.white;
            Gizmos.DrawCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 스폰 지점 테두리 (검은색)
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 현재 위치와 스폰 지점 연결선 (회색)
            if (Vector2.Distance(transform.position, spawnPoint) > 0.1f)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(transform.position, spawnPoint);
            }
            
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2, 
                    $"CrystalGolem (AOE)\n공격: {attackRange:F1} | 감지: {detectionRange:F1}\n추격: {chaseRange:F1} | 순찰: {patrolRadius:F1}");
            }
#endif
        }
        
        // Bear 범위들 (Melee)
        var bear = GetComponent<Bear>();
        if (bear != null)
        {
            Vector2 spawnPoint = bear.SpawnPosition;
            float patrolRadius = bear.PatrolRadius;
            float attackRange = bear.AttackRange;
            float detectionRange = bear.DetectionRange;
            float chaseRange = bear.ChaseRange;
            
            // ✅ 스폰 지점 중심 로밍 범위 (보라색)
            Gizmos.color = roamingRangeColor;
            Gizmos.DrawWireSphere(spawnPoint, patrolRadius);
            
            // ✅ 공격 범위 표시 (빨간색)
            Gizmos.color = attackRangeColor;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // 🟡 감지 범위 (노란색) - 플레이어를 처음 발견하는 범위
            Gizmos.color = detectionRangeColor;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // 🟠 추격 범위 (주황색) - 추격을 포기하는 범위
            Gizmos.color = chaseRangeColor;
            Gizmos.DrawWireSphere(transform.position, chaseRange);
            
            // ✅ 스폰 지점 마커 (흰색 큐브)
            Gizmos.color = Color.white;
            Gizmos.DrawCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 스폰 지점 테두리 (검은색)
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 현재 위치와 스폰 지점 연결선 (회색)
            if (Vector2.Distance(transform.position, spawnPoint) > 0.1f)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(transform.position, spawnPoint);
            }
            
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2, 
                    $"Bear (Melee)\n공격: {attackRange:F1} | 감지: {detectionRange:F1}\n추격: {chaseRange:F1} | 순찰: {patrolRadius:F1}");
            }
#endif
        }
        
        // Spider 범위들 (Melee)
        var spider = GetComponent<Spider>();
        if (spider != null)
        {
            Vector2 spawnPoint = spider.SpawnPosition;
            float patrolRadius = spider.PatrolRadius;
            float attackRange = spider.AttackRange;
            float detectionRange = spider.DetectionRange;
            float chaseRange = spider.ChaseRange;
            
            // ✅ 스폰 지점 중심 로밍 범위 (보라색)
            Gizmos.color = roamingRangeColor;
            Gizmos.DrawWireSphere(spawnPoint, patrolRadius);
            
            // ✅ 공격 범위 표시 (빨간색)
            Gizmos.color = attackRangeColor;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // 🟡 감지 범위 (노란색) - 플레이어를 처음 발견하는 범위
            Gizmos.color = detectionRangeColor;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // 🟠 추격 범위 (주황색) - 추격을 포기하는 범위
            Gizmos.color = chaseRangeColor;
            Gizmos.DrawWireSphere(transform.position, chaseRange);
            
            // ✅ 스폰 지점 마커 (흰색 큐브)
            Gizmos.color = Color.white;
            Gizmos.DrawCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 스폰 지점 테두리 (검은색)
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 현재 위치와 스폰 지점 연결선 (회색)
            if (Vector2.Distance(transform.position, spawnPoint) > 0.1f)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(transform.position, spawnPoint);
            }
            
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2, 
                    $"Spider (Melee)\n공격: {attackRange:F1} | 감지: {detectionRange:F1}\n추격: {chaseRange:F1} | 순찰: {patrolRadius:F1}");
            }
#endif
        }
        
        // Beetle 범위들 (Melee)
        var beetle = GetComponent<Beetle>();
        if (beetle != null)
        {
            Vector2 spawnPoint = beetle.SpawnPosition;
            float patrolRadius = beetle.PatrolRadius;
            float attackRange = beetle.AttackRange;
            float detectionRange = beetle.DetectionRange;
            float chaseRange = beetle.ChaseRange;
            
            // ✅ 스폰 지점 중심 로밍 범위 (보라색)
            Gizmos.color = roamingRangeColor;
            Gizmos.DrawWireSphere(spawnPoint, patrolRadius);
            
            // ✅ 공격 범위 표시 (빨간색)
            Gizmos.color = attackRangeColor;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // 🟡 감지 범위 (노란색) - 플레이어를 처음 발견하는 범위
            Gizmos.color = detectionRangeColor;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // 🟠 추격 범위 (주황색) - 추격을 포기하는 범위
            Gizmos.color = chaseRangeColor;
            Gizmos.DrawWireSphere(transform.position, chaseRange);
            
            // ✅ 스폰 지점 마커 (흰색 큐브)
            Gizmos.color = Color.white;
            Gizmos.DrawCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 스폰 지점 테두리 (검은색)
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 현재 위치와 스폰 지점 연결선 (회색)
            if (Vector2.Distance(transform.position, spawnPoint) > 0.1f)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(transform.position, spawnPoint);
            }
            
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2, 
                    $"Beetle (Melee)\n공격: {attackRange:F1} | 감지: {detectionRange:F1}\n추격: {chaseRange:F1} | 순찰: {patrolRadius:F1}");
            }
#endif
        }
        
        // Anubis 범위들 (Melee)
        var anubis = GetComponent<Anubis>();
        if (anubis != null)
        {
            Vector2 spawnPoint = anubis.SpawnPosition;
            float patrolRadius = anubis.PatrolRadius;
            float attackRange = anubis.AttackRange;
            float detectionRange = anubis.DetectionRange;
            float chaseRange = anubis.ChaseRange;
            
            // ✅ 스폰 지점 중심 로밍 범위 (보라색)
            Gizmos.color = roamingRangeColor;
            Gizmos.DrawWireSphere(spawnPoint, patrolRadius);
            
            // ✅ 공격 범위 표시 (빨간색)
            Gizmos.color = attackRangeColor;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // 🟡 감지 범위 (노란색) - 플레이어를 처음 발견하는 범위
            Gizmos.color = detectionRangeColor;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // 🟠 추격 범위 (주황색) - 추격을 포기하는 범위
            Gizmos.color = chaseRangeColor;
            Gizmos.DrawWireSphere(transform.position, chaseRange);
            
            // ✅ 스폰 지점 마커 (흰색 큐브)
            Gizmos.color = Color.white;
            Gizmos.DrawCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 스폰 지점 테두리 (검은색)
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 현재 위치와 스폰 지점 연결선 (회색)
            if (Vector2.Distance(transform.position, spawnPoint) > 0.1f)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(transform.position, spawnPoint);
            }
            
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2, 
                    $"Anubis (Melee)\n공격: {attackRange:F1} | 감지: {detectionRange:F1}\n추격: {chaseRange:F1} | 순찰: {patrolRadius:F1}");
            }
#endif
        }
        
        // GoblinWarrior 범위들 (Melee)
        var goblinWarrior = GetComponent<GoblinWarrior>();
        if (goblinWarrior != null)
        {
            Vector2 spawnPoint = goblinWarrior.SpawnPosition;
            float patrolRadius = goblinWarrior.PatrolRadius;
            float attackRange = goblinWarrior.AttackRange;
            float detectionRange = goblinWarrior.DetectionRange;
            float chaseRange = goblinWarrior.ChaseRange;
            
            // ✅ 스폰 지점 중심 로밍 범위 (보라색)
            Gizmos.color = roamingRangeColor;
            Gizmos.DrawWireSphere(spawnPoint, patrolRadius);
            
            // ✅ 공격 범위 표시 (빨간색)
            Gizmos.color = attackRangeColor;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // 🟡 감지 범위 (노란색) - 플레이어를 처음 발견하는 범위
            Gizmos.color = detectionRangeColor;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // 🟠 추격 범위 (주황색) - 추격을 포기하는 범위
            Gizmos.color = chaseRangeColor;
            Gizmos.DrawWireSphere(transform.position, chaseRange);
            
            // ✅ 스폰 지점 마커 (흰색 큐브)
            Gizmos.color = Color.white;
            Gizmos.DrawCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 스폰 지점 테두리 (검은색)
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 현재 위치와 스폰 지점 연결선 (회색)
            if (Vector2.Distance(transform.position, spawnPoint) > 0.1f)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(transform.position, spawnPoint);
            }
            
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2, 
                    $"GoblinWarrior (Melee)\n공격: {attackRange:F1} | 감지: {detectionRange:F1}\n추격: {chaseRange:F1} | 순찰: {patrolRadius:F1}");
            }
#endif
        }
        
        // GoblinBomb 범위들 (Ranged)
        var goblinBomb = GetComponent<GoblinBomb>();
        if (goblinBomb != null)
        {
            Vector2 spawnPoint = goblinBomb.SpawnPosition;
            float patrolRadius = goblinBomb.PatrolRadius;
            float attackRange = goblinBomb.AttackRange;
            float detectionRange = goblinBomb.DetectionRange;
            float chaseRange = goblinBomb.ChaseRange;
            
            // ✅ 스폰 지점 중심 로밍 범위 (보라색)
            Gizmos.color = roamingRangeColor;
            Gizmos.DrawWireSphere(spawnPoint, patrolRadius);
            
            // ✅ 공격 범위 표시 (빨간색)
            Gizmos.color = attackRangeColor;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // 🟡 감지 범위 (노란색) - 플레이어를 처음 발견하는 범위
            Gizmos.color = detectionRangeColor;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // 🟠 추격 범위 (주황색) - 추격을 포기하는 범위
            Gizmos.color = chaseRangeColor;
            Gizmos.DrawWireSphere(transform.position, chaseRange);
            
            // ✅ 스폰 지점 마커 (흰색 큐브)
            Gizmos.color = Color.white;
            Gizmos.DrawCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 스폰 지점 테두리 (검은색)
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 현재 위치와 스폰 지점 연결선 (회색)
            if (Vector2.Distance(transform.position, spawnPoint) > 0.1f)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(transform.position, spawnPoint);
            }
            
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2, 
                    $"GoblinBomb (Ranged)\n공격: {attackRange:F1} | 감지: {detectionRange:F1}\n추격: {chaseRange:F1} | 순찰: {patrolRadius:F1}");
            }
#endif
        }
        
        // GoblinCart 범위들 (Ranged)
        var goblinCart = GetComponent<GoblinCart>();
        if (goblinCart != null)
        {
            Vector2 spawnPoint = goblinCart.SpawnPosition;
            float patrolRadius = goblinCart.PatrolRadius;
            float attackRange = goblinCart.AttackRange;
            float detectionRange = goblinCart.DetectionRange;
            float chaseRange = goblinCart.ChaseRange;
            
            // ✅ 스폰 지점 중심 로밍 범위 (보라색)
            Gizmos.color = roamingRangeColor;
            Gizmos.DrawWireSphere(spawnPoint, patrolRadius);
            
            // ✅ 공격 범위 표시 (빨간색)
            Gizmos.color = attackRangeColor;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // 🟡 감지 범위 (노란색) - 플레이어를 처음 발견하는 범위
            Gizmos.color = detectionRangeColor;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // 🟠 추격 범위 (주황색) - 추격을 포기하는 범위
            Gizmos.color = chaseRangeColor;
            Gizmos.DrawWireSphere(transform.position, chaseRange);
            
            // ✅ 스폰 지점 마커 (흰색 큐브)
            Gizmos.color = Color.white;
            Gizmos.DrawCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 스폰 지점 테두리 (검은색)
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 현재 위치와 스폰 지점 연결선 (회색)
            if (Vector2.Distance(transform.position, spawnPoint) > 0.1f)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(transform.position, spawnPoint);
            }
            
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2, 
                    $"GoblinCart (Ranged)\n공격: {attackRange:F1} | 감지: {detectionRange:F1}\n추격: {chaseRange:F1} | 순찰: {patrolRadius:F1}");
            }
#endif
        }
        
        // Scorpion 범위들 (Melee + Poison) - Red/Blue/Brown 모두 지원
        var scorpion = GetComponent<Scorpion>();
        if (scorpion != null)
        {
            Vector2 spawnPoint = scorpion.SpawnPosition;
            float patrolRadius = scorpion.PatrolRadius;
            float attackRange = scorpion.AttackRange;
            float detectionRange = scorpion.DetectionRange;
            float chaseRange = scorpion.ChaseRange;
            
            // ✅ 스폰 지점 중심 로밍 범위 (보라색)
            Gizmos.color = roamingRangeColor;
            Gizmos.DrawWireSphere(spawnPoint, patrolRadius);
            
            // ✅ 공격 범위 표시 (빨간색)
            Gizmos.color = attackRangeColor;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // 🟡 감지 범위 (노란색) - 플레이어를 처음 발견하는 범위
            Gizmos.color = detectionRangeColor;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // 🟠 추격 범위 (주황색) - 추격을 포기하는 범위
            Gizmos.color = chaseRangeColor;
            Gizmos.DrawWireSphere(transform.position, chaseRange);
            
            // ✅ 스폰 지점 마커 (흰색 큐브)
            Gizmos.color = Color.white;
            Gizmos.DrawCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 스폰 지점 테두리 (검은색)
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 현재 위치와 스폰 지점 연결선 (회색)
            if (Vector2.Distance(transform.position, spawnPoint) > 0.1f)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(transform.position, spawnPoint);
            }
            
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2, 
                    $"Scorpion (Poison)\n공격: {attackRange:F1} | 감지: {detectionRange:F1}\n추격: {chaseRange:F1} | 순찰: {patrolRadius:F1}");
            }
#endif
        }
        
        // Cobra 범위들 (Melee)
        var cobra = GetComponent<Cobra>();
        if (cobra != null)
        {
            Vector2 spawnPoint = cobra.SpawnPosition;
            float patrolRadius = cobra.PatrolRadius;
            float attackRange = cobra.AttackRange;
            float detectionRange = cobra.DetectionRange;
            float chaseRange = cobra.ChaseRange;
            
            // ✅ 스폰 지점 중심 로밍 범위 (보라색)
            Gizmos.color = roamingRangeColor;
            Gizmos.DrawWireSphere(spawnPoint, patrolRadius);
            
            // ✅ 공격 범위 표시 (빨간색)
            Gizmos.color = attackRangeColor;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // 🟡 감지 범위 (노란색) - 플레이어를 처음 발견하는 범위
            Gizmos.color = detectionRangeColor;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // 🟠 추격 범위 (주황색) - 추격을 포기하는 범위
            Gizmos.color = chaseRangeColor;
            Gizmos.DrawWireSphere(transform.position, chaseRange);
            
            // ✅ 스폰 지점 마커 (흰색 큐브)
            Gizmos.color = Color.white;
            Gizmos.DrawCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 스폰 지점 테두리 (검은색)
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 현재 위치와 스폰 지점 연결선 (회색)
            if (Vector2.Distance(transform.position, spawnPoint) > 0.1f)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(transform.position, spawnPoint);
            }
            
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2, 
                    $"Cobra (Melee)\n공격: {attackRange:F1} | 감지: {detectionRange:F1}\n추격: {chaseRange:F1} | 순찰: {patrolRadius:F1}");
            }
#endif
        }
        
        // Sphinx 범위들 (Melee)
        var sphinx = GetComponent<Sphinx>();
        if (sphinx != null)
        {
            Vector2 spawnPoint = sphinx.SpawnPosition;
            float patrolRadius = sphinx.PatrolRadius;
            float attackRange = sphinx.AttackRange;
            float detectionRange = sphinx.DetectionRange;
            float chaseRange = sphinx.ChaseRange;
            
            // ✅ 스폰 지점 중심 로밍 범위 (보라색)
            Gizmos.color = roamingRangeColor;
            Gizmos.DrawWireSphere(spawnPoint, patrolRadius);
            
            // ✅ 공격 범위 표시 (빨간색)
            Gizmos.color = attackRangeColor;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // 🟡 감지 범위 (노란색) - 플레이어를 처음 발견하는 범위
            Gizmos.color = detectionRangeColor;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // 🟠 추격 범위 (주황색) - 추격을 포기하는 범위
            Gizmos.color = chaseRangeColor;
            Gizmos.DrawWireSphere(transform.position, chaseRange);
            
            // ✅ 스폰 지점 마커 (흰색 큐브)
            Gizmos.color = Color.white;
            Gizmos.DrawCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 스폰 지점 테두리 (검은색)
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 현재 위치와 스폰 지점 연결선 (회색)
            if (Vector2.Distance(transform.position, spawnPoint) > 0.1f)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(transform.position, spawnPoint);
            }
            
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2, 
                    $"Sphinx (Melee)\n공격: {attackRange:F1} | 감지: {detectionRange:F1}\n추격: {chaseRange:F1} | 순찰: {patrolRadius:F1}");
            }
#endif
        }
        
        // LadyBug 범위들 (Melee)
        var ladyBug = GetComponent<LadyBug>();
        if (ladyBug != null)
        {
            Vector2 spawnPoint = ladyBug.SpawnPosition;
            float patrolRadius = ladyBug.PatrolRadius;
            float attackRange = ladyBug.AttackRange;
            float detectionRange = ladyBug.DetectionRange;
            float chaseRange = ladyBug.ChaseRange;
            
            // ✅ 스폰 지점 중심 로밍 범위 (보라색)
            Gizmos.color = roamingRangeColor;
            Gizmos.DrawWireSphere(spawnPoint, patrolRadius);
            
            // ✅ 공격 범위 표시 (빨간색)
            Gizmos.color = attackRangeColor;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // 🟡 감지 범위 (노란색) - 플레이어를 처음 발견하는 범위
            Gizmos.color = detectionRangeColor;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // 🟠 추격 범위 (주황색) - 추격을 포기하는 범위
            Gizmos.color = chaseRangeColor;
            Gizmos.DrawWireSphere(transform.position, chaseRange);
            
            // ✅ 스폰 지점 마커 (흰색 큐브)
            Gizmos.color = Color.white;
            Gizmos.DrawCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 스폰 지점 테두리 (검은색)
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 현재 위치와 스폰 지점 연결선 (회색)
            if (Vector2.Distance(transform.position, spawnPoint) > 0.1f)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(transform.position, spawnPoint);
            }
            
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2, 
                    $"LadyBug (Melee)\n공격: {attackRange:F1} | 감지: {detectionRange:F1}\n추격: {chaseRange:F1} | 순찰: {patrolRadius:F1}");
            }
#endif
        }
        
        // Mimic 범위들 (Melee)
        var mimic = GetComponent<Mimic>();
        if (mimic != null)
        {
            Vector2 spawnPoint = mimic.SpawnPosition;
            float patrolRadius = mimic.PatrolRadius;
            float attackRange = mimic.AttackRange;
            float detectionRange = mimic.DetectionRange;
            float chaseRange = mimic.ChaseRange;
            
            // ✅ 스폰 지점 중심 로밍 범위 (보라색)
            Gizmos.color = roamingRangeColor;
            Gizmos.DrawWireSphere(spawnPoint, patrolRadius);
            
            // ✅ 공격 범위 표시 (빨간색)
            Gizmos.color = attackRangeColor;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // 🟡 감지 범위 (노란색) - 플레이어를 처음 발견하는 범위
            Gizmos.color = detectionRangeColor;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // 🟠 추격 범위 (주황색) - 추격을 포기하는 범위
            Gizmos.color = chaseRangeColor;
            Gizmos.DrawWireSphere(transform.position, chaseRange);
            
            // ✅ 스폰 지점 마커 (흰색 큐브)
            Gizmos.color = Color.white;
            Gizmos.DrawCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 스폰 지점 테두리 (검은색)
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 현재 위치와 스폰 지점 연결선 (회색)
            if (Vector2.Distance(transform.position, spawnPoint) > 0.1f)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(transform.position, spawnPoint);
            }
            
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2, 
                    $"Mimic (Melee)\n공격: {attackRange:F1} | 감지: {detectionRange:F1}\n추격: {chaseRange:F1} | 순찰: {patrolRadius:F1}");
            }
#endif
        }
        
        // Elite_SandGolem 범위들 (Elite Melee)
        var eliteSandGolem = GetComponent<Elite_SandGolem>();
        if (eliteSandGolem != null)
        {
            Vector2 spawnPoint = eliteSandGolem.SpawnPosition;
            float patrolRadius = eliteSandGolem.PatrolRadius;
            float attackRange = eliteSandGolem.AttackRange;
            float detectionRange = eliteSandGolem.DetectionRange;
            float chaseRange = eliteSandGolem.ChaseRange;
            
            // ✅ 스폰 지점 중심 로밍 범위 (보라색)
            Gizmos.color = roamingRangeColor;
            Gizmos.DrawWireSphere(spawnPoint, patrolRadius);
            
            // ✅ 공격 범위 표시 (빨간색)
            Gizmos.color = attackRangeColor;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // 🟡 감지 범위 (노란색) - 플레이어를 처음 발견하는 범위
            Gizmos.color = detectionRangeColor;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // 🟠 추격 범위 (주황색) - 추격을 포기하는 범위
            Gizmos.color = chaseRangeColor;
            Gizmos.DrawWireSphere(transform.position, chaseRange);
            
            // ✅ 스폰 지점 마커 (흰색 큐브)
            Gizmos.color = Color.white;
            Gizmos.DrawCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 스폰 지점 테두리 (검은색)
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 현재 위치와 스폰 지점 연결선 (회색)
            if (Vector2.Distance(transform.position, spawnPoint) > 0.1f)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(transform.position, spawnPoint);
            }
            
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2, 
                    $"Elite SandGolem\n공격: {attackRange:F1} | 감지: {detectionRange:F1}\n추격: {chaseRange:F1} | 순찰: {patrolRadius:F1}");
            }
#endif
        }
        
        // Elite_Orc 범위들 (Elite Melee)
        var eliteOrc = GetComponent<Elite_Orc>();
        if (eliteOrc != null)
        {
            Vector2 spawnPoint = eliteOrc.SpawnPosition;
            float patrolRadius = eliteOrc.PatrolRadius;
            float attackRange = eliteOrc.AttackRange;
            float detectionRange = eliteOrc.DetectionRange;
            float chaseRange = eliteOrc.ChaseRange;
            
            // ✅ 스폰 지점 중심 로밍 범위 (보라색)
            Gizmos.color = roamingRangeColor;
            Gizmos.DrawWireSphere(spawnPoint, patrolRadius);
            
            // ✅ 공격 범위 표시 (빨간색)
            Gizmos.color = attackRangeColor;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // 🟡 감지 범위 (노란색) - 플레이어를 처음 발견하는 범위
            Gizmos.color = detectionRangeColor;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // 🟠 추격 범위 (주황색) - 추격을 포기하는 범위
            Gizmos.color = chaseRangeColor;
            Gizmos.DrawWireSphere(transform.position, chaseRange);
            
            // ✅ 스폰 지점 마커 (흰색 큐브)
            Gizmos.color = Color.white;
            Gizmos.DrawCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 스폰 지점 테두리 (검은색)
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 현재 위치와 스폰 지점 연결선 (회색)
            if (Vector2.Distance(transform.position, spawnPoint) > 0.1f)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(transform.position, spawnPoint);
            }
            
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2, 
                    $"Elite Orc\n공격: {attackRange:F1} | 감지: {detectionRange:F1}\n추격: {chaseRange:F1} | 순찰: {patrolRadius:F1}");
            }
#endif
        }
        
        // Elite_Boar 범위들 (Elite Melee)
        var eliteBoar = GetComponent<Elite_Boar>();
        if (eliteBoar != null)
        {
            Vector2 spawnPoint = eliteBoar.SpawnPosition;
            float patrolRadius = eliteBoar.PatrolRadius;
            float attackRange = eliteBoar.AttackRange;
            float detectionRange = eliteBoar.DetectionRange;
            float chaseRange = eliteBoar.ChaseRange;
            
            // ✅ 스폰 지점 중심 로밍 범위 (보라색)
            Gizmos.color = roamingRangeColor;
            Gizmos.DrawWireSphere(spawnPoint, patrolRadius);
            
            // ✅ 공격 범위 표시 (빨간색)
            Gizmos.color = attackRangeColor;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // 🟡 감지 범위 (노란색) - 플레이어를 처음 발견하는 범위
            Gizmos.color = detectionRangeColor;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // 🟠 추격 범위 (주황색) - 추격을 포기하는 범위
            Gizmos.color = chaseRangeColor;
            Gizmos.DrawWireSphere(transform.position, chaseRange);
            
            // ✅ 스폰 지점 마커 (흰색 큐브)
            Gizmos.color = Color.white;
            Gizmos.DrawCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 스폰 지점 테두리 (검은색)
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 현재 위치와 스폰 지점 연결선 (회색)
            if (Vector2.Distance(transform.position, spawnPoint) > 0.1f)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(transform.position, spawnPoint);
            }
            
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2, 
                    $"Elite Boar\n공격: {attackRange:F1} | 감지: {detectionRange:F1}\n추격: {chaseRange:F1} | 순찰: {patrolRadius:F1}");
            }
#endif
        }
        
        // Boss_SandElemental 범위들 (Boss Melee + Skills) ⭐ 보스 전용
        var bossSandElemental = GetComponent<Boss_SandElemental>();
        if (bossSandElemental != null)
        {
            Vector2 spawnPoint = bossSandElemental.SpawnPosition;
            float patrolRadius = bossSandElemental.PatrolRadius;
            float attackRange = bossSandElemental.AttackRange;
            float detectionRange = bossSandElemental.DetectionRange;
            float chaseRange = bossSandElemental.ChaseRange;
            
            // ✅ 스폰 지점 중심 로밍 범위 (보라색)
            Gizmos.color = roamingRangeColor;
            Gizmos.DrawWireSphere(spawnPoint, patrolRadius);
            
            // ✅ 공격 범위 표시 (빨간색)
            Gizmos.color = attackRangeColor;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // 🟡 감지 범위 (노란색) - 플레이어를 처음 발견하는 범위
            Gizmos.color = detectionRangeColor;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // 🟠 추격 범위 (주황색) - 추격을 포기하는 범위
            Gizmos.color = chaseRangeColor;
            Gizmos.DrawWireSphere(transform.position, chaseRange);
            
            // ⭐ 보스 전용: Melee Attack Range (근거리/원거리 구분선 - 시안색)
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 6f); // meleeAttackRange
            
            // ✅ 스폰 지점 마커 (흰색 큐브 - 보스는 더 크게)
            Gizmos.color = Color.white;
            Gizmos.DrawCube(spawnPoint, Vector3.one * 0.8f);
            
            // ✅ 스폰 지점 테두리 (금색 - 보스 강조)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(spawnPoint, Vector3.one * 0.8f);
            
            // ✅ 현재 위치와 스폰 지점 연결선 (회색)
            if (Vector2.Distance(transform.position, spawnPoint) > 0.1f)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(transform.position, spawnPoint);
            }
            
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 3, 
                    $"🐲 BOSS SandElemental\n공격: {attackRange:F1} | 감지: {detectionRange:F1}\n추격: {chaseRange:F1} | 순찰: {patrolRadius:F1}\n거리 구분선: 6.0f (시안색)");
            }
#endif
        }
        
        // Boss_ForestElemental 범위들 (Boss Melee + Skills) ⭐ 보스 전용
        var bossForestElemental = GetComponent<Boss_ForestElemental>();
        if (bossForestElemental != null)
        {
            Vector2 spawnPoint = bossForestElemental.SpawnPosition;
            float patrolRadius = bossForestElemental.PatrolRadius;
            float attackRange = bossForestElemental.AttackRange;
            float detectionRange = bossForestElemental.DetectionRange;
            float chaseRange = bossForestElemental.ChaseRange;
            
            // ✅ 스폰 지점 중심 로밍 범위 (보라색)
            Gizmos.color = roamingRangeColor;
            Gizmos.DrawWireSphere(spawnPoint, patrolRadius);
            
            // ✅ 공격 범위 표시 (빨간색)
            Gizmos.color = attackRangeColor;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // 🟡 감지 범위 (노란색) - 플레이어를 처음 발견하는 범위
            Gizmos.color = detectionRangeColor;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // 🟠 추격 범위 (주황색) - 추격을 포기하는 범위
            Gizmos.color = chaseRangeColor;
            Gizmos.DrawWireSphere(transform.position, chaseRange);
            
            // ⭐ 보스 전용: Melee Attack Range (근거리/원거리 구분선 - 시안색)
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 6f); // meleeAttackRange
            
            // ✅ 스폰 지점 마커 (흰색 큐브 - 보스는 더 크게)
            Gizmos.color = Color.white;
            Gizmos.DrawCube(spawnPoint, Vector3.one * 0.8f);
            
            // ✅ 스폰 지점 테두리 (금색 - 보스 강조)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(spawnPoint, Vector3.one * 0.8f);
            
            // ✅ 현재 위치와 스폰 지점 연결선 (회색)
            if (Vector2.Distance(transform.position, spawnPoint) > 0.1f)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(transform.position, spawnPoint);
            }
            
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 3, 
                    $"🐲 BOSS ForestElemental\n공격: {attackRange:F1} | 감지: {detectionRange:F1}\n추격: {chaseRange:F1} | 순찰: {patrolRadius:F1}\n거리 구분선: 6.0f (시안색)");
            }
#endif
        }
        
        // WaterGolem 범위들 (AOE)
        var waterGolem = GetComponent<WaterGolem>();
        if (waterGolem != null)
        {
            Vector2 spawnPoint = waterGolem.SpawnPosition;
            float patrolRadius = waterGolem.PatrolRadius;
            float attackRange = waterGolem.AttackRange;
            float detectionRange = waterGolem.DetectionRange;
            float chaseRange = waterGolem.ChaseRange;
            
            // ✅ 스폰 지점 중심 로밍 범위 (보라색)
            Gizmos.color = roamingRangeColor;
            Gizmos.DrawWireSphere(spawnPoint, patrolRadius);
            
            // ✅ 공격 범위 표시 (빨간색)
            Gizmos.color = attackRangeColor;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // 🟡 감지 범위 (노란색) - 플레이어를 처음 발견하는 범위
            Gizmos.color = detectionRangeColor;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // 🟠 추격 범위 (주황색) - 추격을 포기하는 범위
            Gizmos.color = chaseRangeColor;
            Gizmos.DrawWireSphere(transform.position, chaseRange);
            
            // ✅ 스폰 지점 마커 (흰색 큐브)
            Gizmos.color = Color.white;
            Gizmos.DrawCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 스폰 지점 테두리 (검은색)
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 현재 위치와 스폰 지점 연결선 (회색)
            if (Vector2.Distance(transform.position, spawnPoint) > 0.1f)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(transform.position, spawnPoint);
            }
            
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2, 
                    $"WaterGolem (AOE)\n공격: {attackRange:F1} | 감지: {detectionRange:F1}\n추격: {chaseRange:F1} | 순찰: {patrolRadius:F1}");
            }
#endif
        }
        
        // MeleeAttack 공격 범위 (개별 몬스터가 아닌 경우만 표시)
        var meleeAttackComponent = GetComponent<MeleeAttack>();
        if (meleeAttackComponent != null && 
            GetComponent<BlueSlime>() == null && 
            GetComponent<StoneGolem>() == null && 
            GetComponent<Bear>() == null && 
            GetComponent<Spider>() == null && 
            GetComponent<Beetle>() == null && 
            GetComponent<Anubis>() == null && 
            GetComponent<GoblinWarrior>() == null && 
            GetComponent<GoblinBomb>() == null && 
            GetComponent<GoblinCart>() == null && 
            GetComponent<Scorpion>() == null && 
            GetComponent<Cobra>() == null && 
            GetComponent<Sphinx>() == null && 
            GetComponent<LadyBug>() == null && 
            GetComponent<Mimic>() == null && 
            GetComponent<Elite_SandGolem>() == null && 
            GetComponent<Elite_Orc>() == null && 
            GetComponent<Elite_Boar>() == null && 
            GetComponent<Boss_SandElemental>() == null && 
            GetComponent<Boss_ForestElemental>() == null && 
            GetComponent<CrystalGolem>() == null && 
            GetComponent<WaterGolem>() == null) // 개별 표시가 있는 몬스터 제외
        {
            Gizmos.color = attackRangeColor;
            float attackRange = meleeAttackComponent.GetAttackRange();
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, 
                $"MeleeAttack\nRange: {attackRange:F1}");
#endif
        }
        
        // RangedAttack 원거리 공격 범위
        var grapeRangedAttack = GetComponent<RangedAttack>();
        if (grapeRangedAttack != null)
        {
            // 발사 위치 시각화 (빨간색 구체)
            var projectileSpawnPoint = grapeRangedAttack.GetProjectileSpawnPoint();
            if (projectileSpawnPoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(projectileSpawnPoint.position, 0.3f);
                
                // 원거리 공격 범위 시각화 (반투명 빨간색)
                Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
                Gizmos.DrawWireSphere(transform.position, 3.5f); // 원거리 공격 범위
                
                // ✅ Grape 감지 범위 - RangedAttack에서 가져오기
                float detectionRange = grapeRangedAttack.GetDetectionRange();
                Gizmos.color = detectionRangeColor;
                Gizmos.DrawWireSphere(transform.position, detectionRange);
            }
            
            // 런타임 중 예측 조준선 표시
            if (Application.isPlaying)
            {
                var predictedPos = grapeRangedAttack.GetPredictedPlayerPosition();
                if (predictedPos != Vector3.zero)
                {
                    // 조준선 (노란색)
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(projectileSpawnPoint.position, predictedPos);
                    
                    // 예측 위치 (노란색 큐브)
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawCube(predictedPos, Vector3.one * 0.5f);
                }
            }
            
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                float detectionRange = grapeRangedAttack.GetDetectionRange();
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2, $"Grape (원거리)\n감지: {detectionRange:F1}f");
            }
#endif
        }
        
        // ✅ Ghost (MultiShotRangedAttack) 전용 시각화
        var ghost = GetComponent<Ghost>();
        if (ghost != null)
        {
            Vector2 spawnPoint = ghost.SpawnPoint;
            float patrolRadius = ghost.PatrolRadius;
            
            // ✅ 스폰 지점 중심 로밍 범위 (보라색)
            Gizmos.color = roamingRangeColor;
            Gizmos.DrawWireSphere(spawnPoint, patrolRadius);
            
            // ✅ 스폰 지점 마커 (흰색 큐브) - 더 크게 표시
            Gizmos.color = Color.white;
            Gizmos.DrawCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 스폰 지점 테두리 (검은색)
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(spawnPoint, Vector3.one * 0.5f);
            
            // ✅ 현재 위치와 스폰 지점 연결선 (회색)
            if (Vector2.Distance(transform.position, spawnPoint) > 0.1f)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(transform.position, spawnPoint);
            }
            
            // ✅ MultiShotRangedAttack 범위들
            var multiShotAttack = GetComponent<MultiShotRangedAttack>();
            if (multiShotAttack != null)
            {
                float detectionRange = multiShotAttack.GetDetectionRange();
                float chaseRange = multiShotAttack.GetChaseRange();
                float attackRange = ghost.AttackRange;
                
                // 🟡 감지 범위 (노란색) - 플레이어를 처음 발견하는 범위
                Gizmos.color = detectionRangeColor;
                Gizmos.DrawWireSphere(transform.position, detectionRange);
                
                // 🟠 추격 범위 (주황색) - 추격을 포기하는 범위
                Gizmos.color = chaseRangeColor;
                Gizmos.DrawWireSphere(transform.position, chaseRange);
                
                // 🔴 공격 범위 (빨간색) - 실제 공격하는 범위
                Gizmos.color = attackRangeColor;
                Gizmos.DrawWireSphere(transform.position, attackRange);
                
#if UNITY_EDITOR
                if (Application.isPlaying)
                {
                    Vector2 currentPos = transform.position;
                    float distToSpawn = Vector2.Distance(currentPos, spawnPoint);
                    float distToPlayer = ghost.TargetPlayer != null ? Vector2.Distance(currentPos, ghost.TargetPlayer.transform.position) : 0f;
                    
                    UnityEditor.Handles.Label(transform.position + Vector3.up * 2, 
                        $"Ghost (복합 원거리)\n" +
                        $"감지: {detectionRange:F1}f, 추격: {chaseRange:F1}f, 공격: {attackRange:F1}f\n" +
                        $"스폰거리: {distToSpawn:F1}f, 플레이어: {distToPlayer:F1}f");
                }
#endif
            }
        }
    }

    // 원을 선으로 그려주는 함수
    void DrawWireCircle(Vector2 center, float radius, int segments = 32)
    {
        float angle = 0f;
        Vector3 prevPoint = center + new Vector2(Mathf.Cos(0), Mathf.Sin(0)) * radius;
        for (int i = 1; i <= segments; i++)
        {
            angle = i * Mathf.PI * 2f / segments;
            Vector3 newPoint = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }

    // 캡슐을 선으로 그려주는 함수 (CapsuleCollider2D)
    void DrawWireCapsule2D(CapsuleCollider2D capsule, int segments = 16)
    {
        Vector2 center = capsule.offset;
        Vector2 size = capsule.size;
        float radius;
        float height;
        bool isVertical = (capsule.direction == CapsuleDirection2D.Vertical);

        if (isVertical)
        {
            radius = size.x / 2f;
            height = size.y - 2 * radius;
        }
        else
        {
            radius = size.y / 2f;
            height = size.x - 2 * radius;
        }

        // Draw rectangle part
        if (height > 0)
        {
            if (isVertical)
            {
                Vector2 top = center + Vector2.up * height / 2f;
                Vector2 bottom = center - Vector2.up * height / 2f;
                Gizmos.DrawLine(top + Vector2.left * radius, bottom + Vector2.left * radius);
                Gizmos.DrawLine(top + Vector2.right * radius, bottom + Vector2.right * radius);
            }
            else
            {
                Vector2 right = center + Vector2.right * height / 2f;
                Vector2 left = center - Vector2.right * height / 2f;
                Gizmos.DrawLine(right + Vector2.up * radius, left + Vector2.up * radius);
                Gizmos.DrawLine(right + Vector2.down * radius, left + Vector2.down * radius);
            }
        }

        // Draw semicircle ends
        float startAngle = isVertical ? 0 : Mathf.PI / 2f;
        float endAngle = isVertical ? Mathf.PI : Mathf.PI * 3f / 2f;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = Mathf.PI * i / segments;
            float angle2 = Mathf.PI * (i + 1) / segments;

            if (isVertical)
            {
                // Top semicircle
                Vector2 topCenter = center + Vector2.up * height / 2f;
                Vector2 p1 = topCenter + new Vector2(Mathf.Cos(angle1), Mathf.Sin(angle1)) * radius;
                Vector2 p2 = topCenter + new Vector2(Mathf.Cos(angle2), Mathf.Sin(angle2)) * radius;
                Gizmos.DrawLine(p1, p2);

                // Bottom semicircle
                Vector2 bottomCenter = center - Vector2.up * height / 2f;
                Vector2 p3 = bottomCenter + new Vector2(Mathf.Cos(angle1 + Mathf.PI), Mathf.Sin(angle1 + Mathf.PI)) * radius;
                Vector2 p4 = bottomCenter + new Vector2(Mathf.Cos(angle2 + Mathf.PI), Mathf.Sin(angle2 + Mathf.PI)) * radius;
                Gizmos.DrawLine(p3, p4);
            }
            else
            {
                // Right semicircle
                Vector2 rightCenter = center + Vector2.right * height / 2f;
                Vector2 p1 = rightCenter + new Vector2(Mathf.Cos(angle1 - Mathf.PI / 2f), Mathf.Sin(angle1 - Mathf.PI / 2f)) * radius;
                Vector2 p2 = rightCenter + new Vector2(Mathf.Cos(angle2 - Mathf.PI / 2f), Mathf.Sin(angle2 - Mathf.PI / 2f)) * radius;
                Gizmos.DrawLine(p1, p2);

                // Left semicircle
                Vector2 leftCenter = center - Vector2.right * height / 2f;
                Vector2 p3 = leftCenter + new Vector2(Mathf.Cos(angle1 + Mathf.PI / 2f), Mathf.Sin(angle1 + Mathf.PI / 2f)) * radius;
                Vector2 p4 = leftCenter + new Vector2(Mathf.Cos(angle2 + Mathf.PI / 2f), Mathf.Sin(angle2 + Mathf.PI / 2f)) * radius;
                Gizmos.DrawLine(p3, p4);
            }
        }
    }
    
    /// <summary>
    /// CompositeCollider2D 그리기 (🆕 추가)
    /// </summary>
    void DrawCompositeCollider2D(CompositeCollider2D composite)
    {
        // 각 path를 순회하면서 그리기
        for (int pathIndex = 0; pathIndex < composite.pathCount; pathIndex++)
        {
            Vector2[] pathPoints = new Vector2[composite.GetPathPointCount(pathIndex)];
            composite.GetPath(pathIndex, pathPoints);
            
            // 각 path의 점들을 연결해서 그리기
            for (int i = 0; i < pathPoints.Length; i++)
            {
                Vector2 currentPoint = pathPoints[i];
                Vector2 nextPoint = pathPoints[(i + 1) % pathPoints.Length]; // 마지막과 첫번째 연결
                
                Gizmos.DrawLine(currentPoint, nextPoint);
            }
        }
    }
    
    /// <summary>
    /// PolygonCollider2D 그리기 (🆕 추가)
    /// </summary>
    void DrawPolygonCollider2D(PolygonCollider2D polygon)
    {
        // PolygonCollider2D는 여러 path를 가질 수 있음 (구멍이 있는 폴리곤 등)
        for (int pathIndex = 0; pathIndex < polygon.pathCount; pathIndex++)
        {
            Vector2[] pathPoints = polygon.GetPath(pathIndex);
            
            // 점이 3개 미만이면 폴리곤이 아니므로 건너뛰기
            if (pathPoints.Length < 3) continue;
            
            // 각 점들을 연결해서 폴리곤 그리기
            for (int i = 0; i < pathPoints.Length; i++)
            {
                Vector2 currentPoint = pathPoints[i];
                Vector2 nextPoint = pathPoints[(i + 1) % pathPoints.Length]; // 마지막과 첫번째 연결
                
                Gizmos.DrawLine(currentPoint, nextPoint);
            }
        }
    }
}
