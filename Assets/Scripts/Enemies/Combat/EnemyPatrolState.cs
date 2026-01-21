using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI; // NavMeshAgent (Phase 3)

public class EnemyPatrolState : IEnemyState
{
    private readonly IEnemy enemy;
    private Vector2 patrolTarget;
    private Vector2 homePosition;
    private float patrolRadius = 3f;
    
    // 가감속 시스템 변수들
    private float baseSpeed; // 기본 이동속도
    private float currentSpeed; // 현재 실제 속도
    private float targetSpeed; // 목표 속도
    private float speedChangeVelocity; // SmoothDamp용 속도 변화율
    
    // 🔑 대기 시스템 변수들 추가
    private bool isPausing = false; // 현재 대기 중인지
    private float pauseTimer = 0f; // 대기 경과 시간
    private float pauseDuration = 0f; // 이번 대기 지속 시간
    private float lastMoveDistance = 0f; // 마지막 이동 거리
    private Vector2 lastPosition; // 이전 위치
    private float movementCheckTimer = 0f; // 이동 거리 체크 타이머
    
    // 🔑 방향 노이즈 시스템 변수들 추가
    private Vector2 currentDirection; // 현재 실제 이동 방향
    private Vector2 targetDirection; // 목표 방향 (노이즈 적용 전)
    private Vector2 noiseDirection; // 노이즈가 적용된 방향
    private Vector2 directionVelocity; // SmoothDamp용 방향 변화율
    private float noiseUpdateTimer = 0f; // 노이즈 업데이트 타이머
    private float currentNoiseAngle = 0f; // 현재 노이즈 각도
    private float noiseTime = 0f; // 노이즈 시간 (Perlin Noise용)
    
    private float patrolTimer = 0f;
    private float maxPatrolTime = 5f;
    private bool returningToHome = false;

    public EnemyPatrolState(IEnemy enemy)
    {
        this.enemy = enemy;
        
        // 기본 속도 설정
        baseSpeed = enemy.MoveSpeed;
        
        // 시스템들 초기화
        InitializeSpeedSystem();
        InitializePauseSystem();
        
        // 🔑 방향 노이즈 시스템 초기화
        InitializeDirectionNoiseSystem();
        
        // 기존 HomePosition 설정 코드들...
        if (enemy is BaseEnemy baseEnemy)
        {
            homePosition = baseEnemy.HomePosition;
            patrolRadius = baseEnemy.PatrolRadius;
            
            if (baseEnemy.EnableDebugLogs)
            {
                Debug.Log($"[EnemyPatrolState] {enemy.name} 기본속도: {baseSpeed:F1}, 목표속도: {targetSpeed:F1}");
            }
        }
        else
        {
            // fallback 코드들...
            if (enemy is BlueSlime blueSlime)
            {
                homePosition = blueSlime.SpawnPoint;
                patrolRadius = blueSlime.PatrolRadius;
            }
            else if (enemy is Grape grape)
            {
                homePosition = grape.SpawnPoint;
                patrolRadius = grape.PatrolRadius;
            }
            else if (enemy is Ghost ghost)
            {
                homePosition = ghost.SpawnPoint;
                patrolRadius = ghost.PatrolRadius;
            }
            else
            {
                homePosition = enemy.transform.position;
            }
            
            if (baseSpeed <= 0)
            {
                baseSpeed = 1.5f;
                InitializeSpeedSystem();
            }
        }
    }

    /// <summary>
    /// 🔑 가감속 시스템 초기화
    /// </summary>
    private void InitializeSpeedSystem()
    {
        PatrolTuning tuning = GetPatrolTuning();
        
        if (tuning != null)
        {
            // PatrolTuning 기반 목표 속도 설정
            float speedMultiplier = Random.Range(
                tuning.Acceleration.minSpeedMultiplier, 
                tuning.Acceleration.maxSpeedMultiplier
            );
            targetSpeed = baseSpeed * speedMultiplier;
        }
        else
        {
            // 기본값
            targetSpeed = baseSpeed;
        }
        
        // 초기 속도는 0에서 시작 (점진적 가속)
        currentSpeed = 0f;
        speedChangeVelocity = 0f;
    }

    /// <summary>
    /// 🔑 대기 시스템 초기화
    /// </summary>
    private void InitializePauseSystem()
    {
        isPausing = false;
        pauseTimer = 0f;
        pauseDuration = 0f;
        lastPosition = enemy.transform.position;
        lastMoveDistance = 0f;
        movementCheckTimer = 0f;
    }

    /// <summary>
    /// 🔑 방향 노이즈 시스템 초기화
    /// </summary>
    private void InitializeDirectionNoiseSystem()
    {
        currentDirection = Vector2.right; // 초기 방향
        targetDirection = Vector2.right;
        noiseDirection = Vector2.right;
        directionVelocity = Vector2.zero;
        noiseUpdateTimer = 0f;
        currentNoiseAngle = 0f;
        
        // 각 몬스터마다 다른 노이즈 시드 사용
        noiseTime = Random.Range(0f, 1000f);
    }

    /// <summary>
    /// 🔑 PatrolTuning 가져오기
    /// </summary>
    private PatrolTuning GetPatrolTuning()
    {
        if (enemy is BaseEnemy baseEnemy && baseEnemy.HasPatrolTuning)
        {
            return baseEnemy.PatrolTuning;
        }
        return null;
    }

    /// <summary>
    /// 🔑 목표 속도 업데이트 (상황에 따른 속도 변경)
    /// </summary>
    private void UpdateTargetSpeed()
    {
        if (isPausing) 
        {
            targetSpeed = 0f; // 대기 중에는 속도 0
            return;
        }
        
        PatrolTuning tuning = GetPatrolTuning();
        if (tuning == null) 
        {
            targetSpeed = baseSpeed;
            return;
        }

        float newTargetSpeed;
        
        if (returningToHome)
        {
            // 집으로 돌아갈 때는 조금 더 빠르게
            newTargetSpeed = baseSpeed * tuning.Acceleration.maxSpeedMultiplier;
        }
        else
        {
            // 일반 패트롤 시에는 랜덤한 속도 변화
            float speedMultiplier = Random.Range(
                tuning.Acceleration.minSpeedMultiplier, 
                tuning.Acceleration.maxSpeedMultiplier
            );
            newTargetSpeed = baseSpeed * speedMultiplier;
        }
        
        // 목표 속도가 크게 변경되었을 때만 업데이트
        if (Mathf.Abs(newTargetSpeed - targetSpeed) > 0.2f)
        {
            targetSpeed = newTargetSpeed;
            
            if (enemy is BaseEnemy baseEnemy && baseEnemy.EnableDebugLogs)
            {
                Debug.Log($"[EnemyPatrolState] {enemy.name} 목표속도 변경: {targetSpeed:F1}");
            }
        }
    }

    /// <summary>
    /// 🔑 대기 시작 (거리 기반 또는 확률 기반)
    /// </summary>
    private void StartPause(bool isWaypointPause = false)
    {
        PatrolTuning tuning = GetPatrolTuning();
        if (tuning == null) return;

        isPausing = true;
        pauseTimer = 0f;
        
        if (isWaypointPause)
        {
            // 웨이포인트 도착 시 대기
            pauseDuration = Random.Range(
                tuning.Pause.waypointPauseMin, 
                tuning.Pause.waypointPauseMax
            );
        }
        else
        {
            // 일반 이동 중 대기
            pauseDuration = Random.Range(
                tuning.Pause.pauseDurationMin, 
                tuning.Pause.pauseDurationMax
            );
        }
        
        // 🔑 대기 중에는 속도를 0으로 설정
        targetSpeed = 0f;
        
        if (enemy is BaseEnemy baseEnemy && baseEnemy.EnableDebugLogs)
        {
            string pauseType = isWaypointPause ? "웨이포인트" : "이동 중";
            Debug.Log($"[EnemyPatrolState] {enemy.name} {pauseType} 대기 시작: {pauseDuration:F1}초");
        }
    }

    /// <summary>
    /// 🔑 대기 종료
    /// </summary>
    private void EndPause()
    {
        isPausing = false;
        pauseTimer = 0f;
        
        // 🔑 대기 종료 후 새로운 목표 속도 설정
        UpdateTargetSpeed();
        
        if (enemy is BaseEnemy baseEnemy && baseEnemy.EnableDebugLogs)
        {
            Debug.Log($"[EnemyPatrolState] {enemy.name} 대기 종료, 새 목표속도: {targetSpeed:F1}");
        }
    }

    /// <summary>
    /// 🔑 이동 중 대기 조건 체크
    /// </summary>
    private void CheckMovementPause()
    {
        if (isPausing) return; // 이미 대기 중이면 체크하지 않음
        
        PatrolTuning tuning = GetPatrolTuning();
        if (tuning == null) return;

        movementCheckTimer += Time.deltaTime;
        
        // 0.5초마다 대기 조건 체크
        if (movementCheckTimer >= 0.5f)
        {
            movementCheckTimer = 0f;
            
            // 이동 거리 계산
            float currentMoveDistance = Vector2.Distance(enemy.transform.position, lastPosition);
            lastPosition = enemy.transform.position;
            
            // 일정 거리 이동했고, 확률에 따라 대기
            if (currentMoveDistance > tuning.Pause.pauseDistanceMin)
            {
                float randomValue = Random.Range(0f, 1f);
                if (randomValue < tuning.Pause.movementPauseChance)
                {
                    StartPause(false); // 이동 중 대기
                }
            }
        }
    }

    /// <summary>
    /// 🔑 대기 상태 업데이트
    /// </summary>
    private void UpdatePauseState()
    {
        if (!isPausing) return;
        
        pauseTimer += Time.deltaTime;
        
        // 대기 시간이 끝나면 대기 종료
        if (pauseTimer >= pauseDuration)
        {
            EndPause();
        }
    }

    /// <summary>
    /// 🔑 노이즈가 적용된 방향 계산
    /// </summary>
    private Vector2 CalculateNoisyDirection(Vector2 baseDirection)
    {
        PatrolTuning tuning = GetPatrolTuning();
        if (tuning == null || tuning.DirectionNoise.noiseStrength <= 0f)
        {
            return baseDirection; // 노이즈 없음
        }

        // 🔑 노이즈 업데이트 체크 (설정된 간격마다)
        noiseUpdateTimer += Time.deltaTime;
        if (noiseUpdateTimer >= tuning.DirectionNoise.noiseUpdateInterval)
        {
            noiseUpdateTimer = 0f;
            UpdateNoiseAngle(tuning);
        }

        // 🔑 기본 방향에 노이즈 각도 적용
        float baseAngle = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;
        float noisyAngle = baseAngle + currentNoiseAngle;
        
        Vector2 noisyDirection = new Vector2(
            Mathf.Cos(noisyAngle * Mathf.Deg2Rad),
            Mathf.Sin(noisyAngle * Mathf.Deg2Rad)
        ).normalized;

        return noisyDirection;
    }

    /// <summary>
    /// 🔑 노이즈 각도 업데이트 (Perlin Noise 사용)
    /// </summary>
    private void UpdateNoiseAngle(PatrolTuning tuning)
    {
        // Perlin Noise를 사용한 자연스러운 노이즈 생성
        noiseTime += tuning.DirectionNoise.noiseFrequency * Time.deltaTime;
        
        // -1 ~ 1 범위의 Perlin Noise를 최대 편차 각도 범위로 변환
        float noiseValue = (Mathf.PerlinNoise(noiseTime, 0f) - 0.5f) * 2f; // -1 ~ 1
        float maxDeviation = tuning.DirectionNoise.maxDeviationAngle;
        
        // 🔑 개성 가중치 적용
        float noiseWeight = tuning.PersonalityWeights.noiseWeight;
        float targetNoiseAngle = noiseValue * maxDeviation * tuning.DirectionNoise.noiseStrength * noiseWeight;
        
        // 부드러운 각도 전환 (급격한 방향 변화 방지)
        currentNoiseAngle = Mathf.LerpAngle(currentNoiseAngle, targetNoiseAngle, 
            Time.deltaTime / tuning.DirectionNoise.directionSmoothTime);
        
        if (enemy is BaseEnemy baseEnemy && baseEnemy.EnableDebugLogs && Random.Range(0f, 1f) < 0.05f) // 5% 확률로 로그
        {
            Debug.Log($"[DirectionNoise] {enemy.name} 노이즈각도: {currentNoiseAngle:F1}°, 강도: {tuning.DirectionNoise.noiseStrength:F1}");
        }
    }

    /// <summary>
    /// 🔑 부드러운 방향 전환 (SmoothDamp 사용)
    /// </summary>
    private Vector2 SmoothDirectionTransition(Vector2 newTargetDirection)
    {
        PatrolTuning tuning = GetPatrolTuning();
        float smoothTime = tuning != null ? tuning.DirectionNoise.directionSmoothTime : 0.2f;
        
        // SmoothDamp를 사용한 부드러운 방향 전환
        currentDirection = Vector2.SmoothDamp(
            currentDirection, 
            newTargetDirection, 
            ref directionVelocity, 
            smoothTime
        );
        
        return currentDirection.normalized;
    }

    /// <summary>
    /// 🔑 최종 이동 방향 계산 (노이즈 + 부드러운 전환)
    /// </summary>
    private Vector2 GetFinalMovementDirection(Vector2 baseDirection)
    {
        // 1. 기본 방향에 노이즈 적용
        Vector2 noisyDirection = CalculateNoisyDirection(baseDirection);
        
        // 2. 부드러운 방향 전환
        Vector2 finalDirection = SmoothDirectionTransition(noisyDirection);
        
        return finalDirection;
    }

    /// <summary>
    /// 🔑 현재 속도 업데이트 (매 프레임)
    /// </summary>
    private void UpdateCurrentSpeed()
    {
        PatrolTuning tuning = GetPatrolTuning();
        if (tuning == null)
        {
            currentSpeed = targetSpeed;
            return;
        }

        // 가속/감속율 결정
        float changeRate;
        if (currentSpeed < targetSpeed)
        {
            // 가속
            changeRate = tuning.Acceleration.accelerationRate;
        }
        else
        {
            // 감속
            changeRate = tuning.Acceleration.decelerationRate;
        }

        // SmoothDamp를 사용한 부드러운 속도 전환
        float smoothTime = tuning.Acceleration.speedTransitionSmoothing / changeRate;
        currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedChangeVelocity, smoothTime);
        
        // 최소 속도 보장 (정지 허용: 0까지 내려갈 수 있도록)
        currentSpeed = Mathf.Max(currentSpeed, 0f);
    }

    public void Enter()
    {
        // ⭐⭐⭐ NavMeshAgent 재개 (Phase 3 - 홈 복귀 활성화)
        BaseEnemy baseEnemy = enemy as BaseEnemy;
        if (baseEnemy != null && baseEnemy.IsUsingNavMesh)
        {
            baseEnemy.Agent.isStopped = false; // ✅ Agent 재개 (Chase에서 정지됨)
        }
        
        // 홈 위치에서 멀리 떨어져 있으면 먼저 홈으로 돌아가기
        float distToHome = Vector2.Distance(enemy.transform.position, homePosition);
        
        if (distToHome > patrolRadius)
        {
            returningToHome = true;
            patrolTarget = homePosition;
            
            if (baseEnemy != null && baseEnemy.EnableDebugLogs)
            {
                Debug.Log($"[EnemyPatrolState] {enemy.name} 홈으로 복귀 중... 거리: {distToHome:F1}");
            }
        }
        else
        {
            returningToHome = false;
            GenerateNewPatrolTarget();
            
            if (baseEnemy != null && baseEnemy.EnableDebugLogs)
            {
                Debug.Log($"[EnemyPatrolState] {enemy.name} 순찰 시작: {patrolTarget}");
            }
        }
        
        patrolTimer = 0f;
        
        // 시스템들 초기화
        InitializePauseSystem();
        InitializeDirectionNoiseSystem();
        
        // 🔑 초기 방향 설정
        Vector2 initialDirection = (patrolTarget - (Vector2)enemy.transform.position).normalized;
        currentDirection = initialDirection;
        targetDirection = initialDirection;
        
        UpdateTargetSpeed();
    }

    public void Execute()
    {
        // ⭐ BaseEnemy 참조를 메서드 시작 부분에서 한 번만 캐싱 (중복 선언 방지)
        BaseEnemy baseEnemy = enemy as BaseEnemy;
        
        patrolTimer += Time.deltaTime;

        // 대기 상태 업데이트
        UpdatePauseState();
        
        // 이동 중 대기 조건 체크
        if (!returningToHome)
        {
            CheckMovementPause();
        }

        // 매 프레임 속도 업데이트
        UpdateCurrentSpeed();

        // 플레이어 감지 체크
        if (enemy.TargetPlayer != null)
        {
            float distToPlayer = Vector2.Distance(enemy.transform.position, enemy.TargetPlayer.transform.position);
            float patrolDetectionRange = GetPatrolDetectionRange();
            
            if (distToPlayer < patrolDetectionRange)
            {
                if (enemy.EnableDebugLogs)
                {
                    Debug.Log($"[EnemyPatrolState] {enemy.name} 플레이어 감지! 추격 시작");
                }
                
                enemy.FSMController.ChangeState(new EnemyChaseState(enemy));
                return;
            }
        }

        // 대기 중이면 이동하지 않음
        if (isPausing)
        {
            // ⭐ 대기 중에는 속도 0 보장 + Idle 강제 적용 (깜빡임 방지)
            currentSpeed = 0f;
            if (baseEnemy != null)
            {
                baseEnemy.AnimationController?.ForceIdle();
            }
            return;
        }

        // ⭐⭐⭐ NavMesh 사용 여부 체크 (Phase 3)
        bool usingNavMesh = baseEnemy != null && baseEnemy.IsUsingNavMesh;
        
        // 🔍 디버그: NavMesh 사용 여부 로그 (첫 실행 시에만)
        if (baseEnemy != null && baseEnemy.EnableDebugLogs && patrolTimer < 0.1f && !isPausing)
        {
            Debug.Log($"🔍 [EnemyPatrolState] {enemy.name} NavMesh 사용: {usingNavMesh}");
            if (!usingNavMesh)
            {
                Debug.LogWarning($"⚠️ [EnemyPatrolState] {enemy.name}: NavMesh 비활성화! 직선 이동 방식 사용 중!");
            }
        }
        
        // 🔑 이동 로직 (노이즈 적용)
        if (returningToHome)
        {
            // ⭐ NavMesh 사용 시 (Phase 3)
            if (usingNavMesh)
            {
                // NavMeshAgent로 홈 위치로 경로 탐색
                baseEnemy.Agent.SetDestination(patrolTarget);
                baseEnemy.Agent.speed = currentSpeed; // 속도 동기화
                
                // ⭐ 8방향 애니메이션 업데이트 (NavMeshAgent velocity 사용)
                Vector2 velocity = new Vector2(baseEnemy.Agent.velocity.x, baseEnemy.Agent.velocity.y);
                baseEnemy.AnimationController?.UpdateMovementByVelocity(velocity);
                
                // ⭐⭐⭐ 도착 체크 (안전한 방식 - pathPending 체크!)
                float distToHome = Vector2.Distance(enemy.transform.position, homePosition);
                bool arrived = !baseEnemy.Agent.pathPending && baseEnemy.Agent.remainingDistance < 0.5f;
                
                if (distToHome <= 0.5f || arrived)
                {
                    returningToHome = false;
                    GenerateNewPatrolTarget();
                    patrolTimer = 0f;
                    
                    // 새로운 목표로의 방향 설정
                    Vector2 newDirection = (patrolTarget - (Vector2)enemy.transform.position).normalized;
                    targetDirection = newDirection;
                    
                    StartPause(true);
                    
                    if (baseEnemy != null && baseEnemy.EnableDebugLogs)
                    {
                        Debug.Log($"[EnemyPatrolState] {enemy.name} 홈 도착! 새 순찰 목표: {patrolTarget}");
                    }
                }
            }
            // ⭐ 기존 직선 이동 방식 (NavMesh 없을 때)
            else
            {
                // 홈으로 돌아가는 중 - 노이즈 약하게 적용
                Vector2 baseDirection = (patrolTarget - (Vector2)enemy.transform.position).normalized;
                Vector2 moveDirection = GetFinalMovementDirection(baseDirection);
                
                // 🔑 노이즈가 적용된 방향으로 이동
                Vector2 velocity = moveDirection * currentSpeed;
                enemy.transform.position += (Vector3)(velocity * Time.deltaTime);
                
                // ⭐ 8방향 애니메이션 업데이트
                if (baseEnemy != null)
                {
                    // 실제 속도 기반으로 speed/isMoving 반영
                    baseEnemy.AnimationController?.UpdateMovementByVelocity(velocity);
                }

                float distToHome = Vector2.Distance(enemy.transform.position, homePosition);
                if (distToHome <= 0.5f)
            {
                returningToHome = false;
                GenerateNewPatrolTarget();
                patrolTimer = 0f;
                
                // 새로운 목표로의 방향 설정
                Vector2 newDirection = (patrolTarget - (Vector2)enemy.transform.position).normalized;
                targetDirection = newDirection;
                
                StartPause(true);
                
                if (baseEnemy != null && baseEnemy.EnableDebugLogs)
                {
                    Debug.Log($"[EnemyPatrolState] {enemy.name} 홈 도착! 새 순찰 목표: {patrolTarget}");
                }
            }
                }
        }
        else
        {
            // ⭐ NavMesh 사용 시 (Phase 3)
            if (usingNavMesh)
            {
                // ⭐ NavMesh 위치 유효성 체크 (Phase 3)
                NavMeshHit hit;
                Vector3 validTarget = patrolTarget;
                if (NavMesh.SamplePosition(patrolTarget, out hit, 2f, NavMesh.AllAreas))
                {
                    validTarget = hit.position;
                }
                
                // NavMeshAgent로 순찰 목표로 경로 탐색
                baseEnemy.Agent.SetDestination(validTarget);
                baseEnemy.Agent.speed = currentSpeed; // 속도 동기화
                
                // ⭐ 8방향 애니메이션 업데이트 (NavMeshAgent velocity 사용)
                Vector2 velocity = new Vector2(baseEnemy.Agent.velocity.x, baseEnemy.Agent.velocity.y);
                baseEnemy.AnimationController?.UpdateMovementByVelocity(velocity);

                // ⭐⭐⭐ 목표 도달 체크 (안전한 방식 - pathPending 체크!)
                float distToTarget = Vector2.Distance(enemy.transform.position, patrolTarget);
                bool arrived = !baseEnemy.Agent.pathPending && baseEnemy.Agent.remainingDistance < 0.5f;
                
                if ((distToTarget < 0.5f || arrived) || patrolTimer > maxPatrolTime)
                {
                    GenerateNewPatrolTarget();
                    patrolTimer = 0f;
                    
                    // 새로운 목표로의 방향 설정
                    Vector2 newDirection = (patrolTarget - (Vector2)enemy.transform.position).normalized;
                    targetDirection = newDirection;
                    
                    StartPause(true);
                    
                    if (baseEnemy != null && baseEnemy.EnableDebugLogs)
                    {
                        Debug.Log($"[EnemyPatrolState] {enemy.name} 새 순찰 목표: {patrolTarget}, 속도: {currentSpeed:F1}");
                    }
                }

                // 순찰 범위 체크
                float distToHome = Vector2.Distance(enemy.transform.position, homePosition);
                if (distToHome > patrolRadius * 1.5f)
                {
                    returningToHome = true;
                    patrolTarget = homePosition;
                    
                    // 홈으로의 방향 설정
                    Vector2 homeDirection = (homePosition - (Vector2)enemy.transform.position).normalized;
                    targetDirection = homeDirection;
                    
                    UpdateTargetSpeed();
                    
                    if (baseEnemy != null && baseEnemy.EnableDebugLogs)
                    {
                        Debug.Log($"[EnemyPatrolState] {enemy.name} 순찰 범위 이탈! 홈으로 복귀");
                    }
                }
            }
            // ⭐ 기존 직선 이동 방식 (NavMesh 없을 때)
            else
            {
                // 정상 순찰 중 - 노이즈 정상 적용
                Vector2 baseDirection = (patrolTarget - (Vector2)enemy.transform.position).normalized;
                Vector2 moveDirection = GetFinalMovementDirection(baseDirection);
                
                // 🔑 노이즈가 적용된 방향으로 이동
                Vector2 velocity = moveDirection * currentSpeed;
                enemy.transform.position += (Vector3)(velocity * Time.deltaTime);
                
                // ⭐ 8방향 애니메이션 업데이트
                if (baseEnemy != null)
                {
                    // 실제 속도 기반으로 speed/isMoving 반영
                    baseEnemy.AnimationController?.UpdateMovementByVelocity(velocity);
                }

                // 목표 도달 또는 시간 초과 체크
                float distToTarget = Vector2.Distance(enemy.transform.position, patrolTarget);
                if (distToTarget < 0.5f || patrolTimer > maxPatrolTime)
            {
                GenerateNewPatrolTarget();
                patrolTimer = 0f;
                
                // 새로운 목표로의 방향 설정
                Vector2 newDirection = (patrolTarget - (Vector2)enemy.transform.position).normalized;
                targetDirection = newDirection;
                
                StartPause(true);
                
                if (baseEnemy != null && baseEnemy.EnableDebugLogs)
                {
                    Debug.Log($"[EnemyPatrolState] {enemy.name} 새 순찰 목표: {patrolTarget}, 속도: {currentSpeed:F1}");
                }
            }

                // 순찰 범위 체크
                float distToHome = Vector2.Distance(enemy.transform.position, homePosition);
                if (distToHome > patrolRadius * 1.5f)
                {
                    returningToHome = true;
                    patrolTarget = homePosition;
                    
                    // 홈으로의 방향 설정
                    Vector2 homeDirection = (homePosition - (Vector2)enemy.transform.position).normalized;
                    targetDirection = homeDirection;
                    
                    UpdateTargetSpeed();
                    
                    if (baseEnemy != null && baseEnemy.EnableDebugLogs)
                    {
                        Debug.Log($"[EnemyPatrolState] {enemy.name} 순찰 범위 이탈! 홈으로 복귀");
                    }
                }
            }
        }
    }

    public void Exit() 
    {
        if (enemy.EnableDebugLogs)
        {
            string pauseStatus = isPausing ? "대기 중" : "이동 중";
            Debug.Log($"[EnemyPatrolState] {enemy.name} 순찰 종료 - 상태: {pauseStatus}, 최종속도: {currentSpeed:F1}, 방향: {currentDirection}");
        }
    }

    /// <summary>
    /// 🔑 순찰 중 플레이어 감지 범위 계산
    /// </summary>
    private float GetPatrolDetectionRange()
    {
        // 기존 구현 그대로
        float detectionRange = 4f;
        
        if (enemy is BaseEnemy baseEnemy)
        {
            detectionRange = baseEnemy.DetectionRange * 0.8f;
        }
        else
        {
            // 기존 시스템 fallback
            if (enemy is BlueSlime blueSlime)
            {
                var meleeAttack = blueSlime.GetComponent<MeleeAttack>();
                if (meleeAttack != null)
                {
                    detectionRange = meleeAttack.GetDetectionRange() * 0.8f;
                }
            }
            else if (enemy is Grape grape)
            {
                var rangedAttack = grape.GetComponent<RangedAttack>();
                if (rangedAttack != null)
                {
                    detectionRange = rangedAttack.GetDetectionRange() * 0.8f;
                }
            }
            else if (enemy is Ghost ghost)
            {
                var multiShotAttack = ghost.GetComponent<MultiShotRangedAttack>();
                if (multiShotAttack != null)
                {
                    detectionRange = multiShotAttack.GetDetectionRange() * 0.8f;
                }
            }
        }
        
        return detectionRange;
    }

    /// <summary>
    /// 🔑 홈 위치 기준 새로운 순찰 목표 생성
    /// </summary>
    private void GenerateNewPatrolTarget()
    {
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        float randomDistance = Random.Range(1f, patrolRadius);
        patrolTarget = homePosition + (randomDirection * randomDistance);
    }
}