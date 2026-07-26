using System;
using UnityEngine;
using UnityEngine.AI;


public abstract class BaseEnemyState : IState
{
    private const float FRONT_ANGLE = 90f;
    private const float SIDE_ANGLE = 180f;
    protected const float PATROL_RELOAD_RATIO = 0.3f;
    protected const float COMBAT_RELOAD_RATIO = 0f;
    protected float _visionTimer;
    protected const float COMBAT_VISION_INTERVAL = 0.1f;
    protected const float PATROL_VISION_INTERVAL = 0.2f;
    protected const float IDLE_VISION_INTERVAL = 1f;
    // 자식 상태 클래스들이 공용으로 사용할 상태머신 참조
    protected EnemyStateMachine _stateMachine;

    // 기본 감지 반경 세팅 (필요시 자식에서 따로 정의 가능)
    protected float _detectionRadius = 20f;
    
    float _currentSpeed;
    // 생성자
    public BaseEnemyState(EnemyStateMachine stateMachine)
    {
        this._stateMachine = stateMachine;
    }

    public abstract void EnterState();
    public abstract void UpdateState();
    public abstract void ExitState();

    /// <summary>
    /// 모든 자식 상태(Idle, Patrol, Investigate 등)가 쓸 수 있는 플레이어 감지 함수
    /// </summary>
    protected bool TryDetectTarget(out IBattleAgent newTarget, PatrolPhase patrolPhase)//플레이어를 탐지하고 레이캐스트로 적중했을시, Patrol 상태에서 사용하기 위한 보호막
    {
        bool isInAngle = false;
        Collider[] hitColliders = Physics.OverlapSphere(_stateMachine.transform.position, _detectionRadius, _stateMachine._battleAgentLayerMask);

        newTarget = null;
        if (hitColliders.Length > 0)
        {
            foreach (Collider col in hitColliders)
            {
                IBattleAgent targetObj = col.GetComponentInParent<IBattleAgent>();
                if (targetObj == null)
                {
                    continue;
                }
                if (targetObj.Team == _stateMachine._enemyBase.Team)
                {
                    continue;
                }
                Vector3 startPos = _stateMachine.transform.position + Vector3.up * 1f;

                Vector3 targetPos = targetObj.Transform.position + Vector3.up * 1f;
                Vector3 direction = (targetPos - startPos).normalized;

                float distance = Vector3.Distance(startPos, targetPos);
                float angle = Vector3.Angle(_stateMachine.transform.forward, direction);
                Debug.Log("타겟 설정 진입");
                
                switch (patrolPhase) 
                {
                    case PatrolPhase.Patrol: isInAngle = IsInDetectionRange(angle, distance); break;
                    case PatrolPhase.Search: isInAngle = true; break;
                }
                if (isInAngle)
                {
                    if (CanSeeTarget(targetObj))
                    {
                        if (newTarget == null)
                        {
                            newTarget = targetObj;
                            Debug.Log("새타겟 설정 완료");
                        }
                        else if (distance < Vector3.Distance(startPos, newTarget.Transform.position + Vector3.up * 1f))
                        {
                            newTarget = targetObj;
                            Debug.Log("타겟 교체 완료");
                        }
                        Debug.Log("타겟 설정 그대로");
                    }
                    Debug.Log("타겟 추적 불가");
                }
                Debug.Log("타겟 앵글에 없음");
            }
            if (newTarget != null)
            {
                return true;
            }
        }
        return false;
    }
    protected bool CanAttackTargetAgent()
    {
        if (_stateMachine._targetBattleAgent == null) {
            Debug.Log("[CanAttackTarget] 사격이 불가능 합니다. 타겟이 없습니다.");
            return false;
        }

        Vector3 startPos = _stateMachine.transform.position + Vector3.up * 1f;
        Vector3 targetPos = _stateMachine._targetBattleAgent.Transform.position + Vector3.up * 1f;

        return CanAttackTarget(startPos,targetPos);
    }
    protected bool CanAttackTarget(Vector3 startPos, Vector3 targetPos) 
    {
        if (_stateMachine._targetBattleAgent == null)
        {
            Debug.Log("[CanAttackTarget] 사격이 불가능 합니다. 타겟이 없습니다.");
            return false;
        }
        Vector3 direction = (targetPos - startPos).normalized;
        float distance = Vector3.Distance(startPos, targetPos);
        if (distance > _stateMachine._enemyBase.CurrentWeapon.Range)
        {
            Debug.Log("[CanAttackTarget] 사격이 불가능 합니다. 거리가 안됩니다.");
            return false;
        }
        if (Physics.Raycast(startPos, direction, out RaycastHit hit, distance, _stateMachine._shootLayerMask))
        {
            Debug.Log($"맞은 오브젝트 : {hit.transform.name}, Layer : {LayerMask.LayerToName(hit.transform.gameObject.layer)}");
            return hit.transform == _stateMachine._targetBattleAgent.Transform;
        }
        Debug.Log("[CanAttackTarget] 사격이 불가능 합니다. 그외.");
        return false;
    }
    protected void UpdateMoveAnimation() 
    {
        _currentSpeed = _stateMachine._agent.velocity.magnitude;
        if (_currentSpeed < 0.2f)
        {
            _currentSpeed = 0f;
        }
        _stateMachine._animController.ChangeAnimState(EnemyAnimState.Move, _currentSpeed);
    }
    protected bool NeedReload(float ratio)
    {
        int remainBullets = _stateMachine._enemyBase.CurrentWeapon.RemainBullets;
        int magazineSize = _stateMachine._enemyBase.CurrentWeapon.MagazineSize;

        return remainBullets <= magazineSize * ratio;
    }
    protected void EvaluateCombatState() //독립적인 행동이 끝나고 다음 행동을 이어나가기 위함(장전 등)
    {
        if (_stateMachine._targetBattleAgent == null)
        {
            _stateMachine.ChangeState(_stateMachine._patrolState);
        }
        else if (CanAttackTargetAgent())
        {
            _stateMachine.ChangeState(_stateMachine._attackState);
        }
        else
        {
            _stateMachine.ChangeState(_stateMachine._chaseState);
        }
    }
    protected bool IsInDetectionRange(float angle, float distance) //(Patrol상태 전용) 탐지거리내에 있는지
    {
        if (angle <= FRONT_ANGLE * 0.5f)
            return distance <= _stateMachine._enemyBase.FrontDetectDistance;

        if (angle <= SIDE_ANGLE * 0.5f)
            return distance <= _stateMachine._enemyBase.SideDetectDistance;

        return distance <= _stateMachine._enemyBase.BackDetectDistance;
    }
    protected bool CanSeeTarget(IBattleAgent target) {//적이 내 시야에 있는지(방향 상관 안함)(거리도 상관 안하도록 변경 필요)

        if (target == null || target.Team == _stateMachine._enemyBase.Team)
            return false;

        Vector3 startPos = _stateMachine.transform.position + Vector3.up;
        Vector3 targetPos = target.Transform.position + Vector3.up;

        Vector3 direction = (targetPos - startPos).normalized;
        float distance = Vector3.Distance(startPos, targetPos);

        RaycastHit[] hits = Physics.RaycastAll(startPos,direction,distance,_stateMachine._sightLayerMask);

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            // 전투 유닛인가?
            IBattleAgent hitAgent = hit.collider.GetComponentInParent<IBattleAgent>();

            if (hitAgent != null)
            {
                // 아군이면 통과
                if (hitAgent.Team == _stateMachine._enemyBase.Team)
                    continue;

                // 목표를 먼저 만났으면 추적 가능
                if (ReferenceEquals(hitAgent, target))
                    return true;

                // 다른 적군이 먼저 있으면 막힘
                return false;
            }

            // 전투 유닛이 아닌 물체(엄폐물, 벽 등)를 먼저 맞으면 시야 차단
            return false;
        }

        return false;
    }
    public bool IsTargetCovered(IBattleAgent target)
    {
        if (target == null || target.Team == _stateMachine._enemyBase.Team)
            return false;

        Vector3 startPos = _stateMachine.transform.position + Vector3.up;
        Vector3 targetPos = target.Transform.position + Vector3.up;

        Vector3 direction = (targetPos - startPos).normalized;
        float distance = Vector3.Distance(startPos, targetPos);

        if (Physics.Raycast(startPos, direction, out RaycastHit hit, distance, _stateMachine._coverWallLayerMask))
        {
            Debug.LogError($"맞은 벽 : {hit.transform.name}, Layer : {LayerMask.LayerToName(hit.transform.gameObject.layer)}");
            return true;
        }

        return false;
    }
    protected bool NeedChangeCover() { return true; }
    protected bool IsVisionUpdateTime(float visionInterval)
    {
        _visionTimer += Time.deltaTime;

        if (_visionTimer < visionInterval)
            return false;

        _visionTimer = 0f;
        return true;
    }
    protected void RotateTowardsToPoint(Vector3 point)
    {
        Vector3 direction = (point - _stateMachine.transform.position).normalized;
        direction.y = 0; // 몬스터가 하늘이나 바닥으로 기울어지는 것 방지

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            _stateMachine.transform.rotation = Quaternion.Slerp(_stateMachine.transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }
}
public class EnemyIdleState : BaseEnemyState
{
    private float _idleDetectionRadius = 50f;
    public EnemyIdleState(EnemyStateMachine stateMachine) : base(stateMachine) { }

    public override void EnterState()
    {
        Debug.Log("상태 진입: [Idle] 대기상태.");
        _stateMachine.SetDebugStateColor(EnemyState.Idle);
        _stateMachine._animController.ResetAnimState();
        _stateMachine.ClearTarget();
    }

    public override void UpdateState()
    {
        if (IsVisionUpdateTime(IDLE_VISION_INTERVAL))
        {
            if (CheckPlayer())//플레이어가 주변에 있을 때만 순찰로 전환
            {
                _stateMachine._patrolState.StartPatrol();
                _stateMachine.ChangeState(_stateMachine._patrolState);
            }
        }
    }

    public override void ExitState() { }
    private bool CheckPlayer() 
    {
        return Physics.CheckSphere(_stateMachine.transform.position,_idleDetectionRadius, _stateMachine._playerLayerMask);
    }
}
public enum PatrolPhase
{
    Patrol,     // 일반 순찰
    Search      // 전투 직후 주변 확인
}
public class EnemyPatrolState : BaseEnemyState
{
    private float _patrolRadius = 10f;
    private Vector3 _patrolTarget;
    private float _arriveDistance = 0.5f;
    private float _searchTimer=0f;
    private float _maxSearchTime = 1f;
    private PatrolPhase _currentPhase = PatrolPhase.Patrol;
    public EnemyPatrolState(EnemyStateMachine stateMachine) : base(stateMachine)
    {
        this._detectionRadius = 15f;
    }

    public override void EnterState()
    {
        Debug.Log("상태 진입: [Patrol] 순찰을 시작합니다.");
        _stateMachine.ClearTarget();
        _visionTimer = PATROL_VISION_INTERVAL;
        _stateMachine.SetDebugStateColor(EnemyState.Patrol);
        _stateMachine._agent.speed = 1.0f;   // 순찰 속도
        _searchTimer = 0f;
        DoPatrol();
    }

    public override void UpdateState()
    {
        switch (_currentPhase)
        {
            case PatrolPhase.Patrol:
                if (IsVisionUpdateTime(PATROL_VISION_INTERVAL))
                {
                    if (TryDetectTarget(out IBattleAgent target, _currentPhase))
                    {
                        _stateMachine.SetTarget(target);
                        _stateMachine.ChangeState(_stateMachine._chaseState);
                        return;
                    }
                }
                UpdateMoveAnimation();
                if (!_stateMachine._agent.pathPending && _stateMachine._agent.remainingDistance <= _arriveDistance)
                {
                    DoPatrol();
                }
                break;
            case PatrolPhase.Search:
                _searchTimer += Time.deltaTime;
                if (IsVisionUpdateTime(PATROL_VISION_INTERVAL))
                {
                    if (TryDetectTarget(out IBattleAgent target, _currentPhase))
                    {
                        _stateMachine.SetTarget(target);
                        _stateMachine.ChangeState(_stateMachine._chaseState);
                        return;
                    }
                }
                UpdateMoveAnimation();
                if (_searchTimer >= _maxSearchTime)
                {
                    StartPatrol();
                }
                break;
        }

        if (NeedReload(PATROL_RELOAD_RATIO))
        {
            _stateMachine._previousEnemyState = EnemyState.Patrol;
            _stateMachine.ChangeState(_stateMachine._reloadState);
            return;
        }

    }

    public override void ExitState() { }
    public void DoPatrol() 
    {
        float radius = _patrolRadius;

        Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
        float distance = UnityEngine.Random.Range(5f, _patrolRadius);

        Vector3 randomPos = _stateMachine.transform.position + new Vector3(dir.x, 0f, dir.y) * distance;

        if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, radius, NavMesh.AllAreas))
        {
            _patrolTarget = hit.position;
            _stateMachine._agent.SetDestination(_patrolTarget);
        }
    }
    public void StartPatrol()
    {
        _currentPhase = PatrolPhase.Patrol;
    }

    public void StartSearch()
    {
        _currentPhase = PatrolPhase.Search;
    }

}
public class EnemyInvestigateState : BaseEnemyState
{
    // 생성자
    public EnemyInvestigateState(EnemyStateMachine stateMachine) : base(stateMachine) 
    { 
        this._detectionRadius = 20f; 
    }
    public override void EnterState() { }  // 이 상태로 처음 들어왔을 때 
    public override void UpdateState() { UpdateMoveAnimation(); } // 매 프레임 실행될 로직 
    public override void ExitState() { }   // 이 상태에서 빠져나갈 때
}
public class EnemyChaseState : BaseEnemyState
{
    private float lostTimer;
    private float maxLostTime = 5.0f; // 시야에서 사라진 후 5초 동안은 마지막 위치로 추격

    // 이미 추적 중일 때는 더 넓은 범위(놓치는 범위)를 적용하기 위해 부모의 반경을 덮어씌움
    private float chaseLoseRadius = 25f;

    public EnemyChaseState(EnemyStateMachine stateMachine) : base(stateMachine)
    {
        // 추적 중일 때의 감지 반경 설정
        this._detectionRadius = chaseLoseRadius;
    }

    public override void EnterState()
    {
        Debug.Log("[Chase] 추적 시작. 실시간 추격을 가동합니다.");
        _stateMachine.SetDebugStateColor(EnemyState.Chase);
        lostTimer = 0f;
        _visionTimer = COMBAT_VISION_INTERVAL;
        // 진입 순간의 속도를 추격 속도로 올려줍니다
        _stateMachine._agent.speed = 3f;
    }

    public override void UpdateState()
    {
        UpdateMoveAnimation();
        if (_stateMachine._targetBattleAgent == null)
        {
            _stateMachine._patrolState.StartPatrol();
            _stateMachine.ChangeState(_stateMachine._patrolState);
            return;
        }
        if (IsVisionUpdateTime(COMBAT_VISION_INTERVAL))
        {
            if (CanSeeTarget(_stateMachine._targetBattleAgent))
            {
                lostTimer = 0f;

                _stateMachine._lastDetectPosition = _stateMachine._targetTransform.position;
                _stateMachine._agent.stoppingDistance = 0.2f;
                _stateMachine._agent.SetDestination(_stateMachine._lastDetectPosition);

                if (CanAttackTargetAgent())
                {
                    Debug.Log("[Chase] 플레이어를 포착했습니다. 공격(Attack) 상태로 전환합니다.");
                    _stateMachine.ChangeState(_stateMachine._attackState);
                }
            }
            else
            {
                lostTimer += COMBAT_VISION_INTERVAL;
                _stateMachine._agent.stoppingDistance = 0.2f;
                _stateMachine._agent.SetDestination(_stateMachine._lastDetectPosition);

                if (lostTimer >= maxLostTime)
                {
                    Debug.Log("[Chase] 플레이어를 완전히 놓쳤습니다. 수색(Investigate) 상태로 전환합니다.");

                    // stateMachine.ChangeState(stateMachine.investigateState);
                    _stateMachine._patrolState.StartPatrol();
                    _stateMachine.ChangeState(_stateMachine._patrolState);
                    return;
                }
            }
        }
    }

    public override void ExitState()
    {
        Debug.Log("[Chase] 추적 종료.");
    }
}
public enum AttackPhase
{
    Aim,
    Burst,
    Cooldown
}

public class EnemyAttackState : BaseEnemyState
{
    private float aimTimer; //조준 후에 발사까지 걸리는 시간 
    private float aimDuration = 0.5f; //추후에 무기 등에서 값을 받아올수도 있음 *EnemyData에 필요

    private Vector3 aimPosition;

    private float targetLostTimer = 0f;
    private float maxTargetLostTime = 0.8f;

    private float _hideWaitTimer = 0f;
    private float _maxHideWaitTime = 5f;

    private AttackPhase currentPhase;
    private int maxBurstCount = 3;
    private int currentBurstCount=0;

    private float burstInterval = 0.3f;
    private float burstTimer;

    private bool _canSee;
    private bool _IsCovered;
    private bool _canAttack;
    public EnemyAttackState(EnemyStateMachine stateMachine) : base(stateMachine)
    {
        this._detectionRadius = 20f; // 사격 감지 범위
    }

    public override void EnterState()
    {
        if (_stateMachine._targetBattleAgent == null) 
        {
            _stateMachine.ChangeState(_stateMachine._patrolState);
        }
        Debug.Log("[Attack] 상태 시작");
        _stateMachine.SetDebugStateColor(EnemyState.Attack);
        _stateMachine._agent.ResetPath();
        _visionTimer = COMBAT_VISION_INTERVAL;
        targetLostTimer = 0f;
        _stateMachine._agent.speed = 3f;
        StartAim();
    }

    public override void UpdateState()
    {
        if (_stateMachine._targetBattleAgent.IsDead == true)
        {
            _stateMachine.ClearTarget();
            _stateMachine._patrolState.StartSearch();
            _stateMachine.ChangeState(_stateMachine._patrolState);
        }
        if (_stateMachine._targetBattleAgent == null)
        {
            return;
        }

        if (IsVisionUpdateTime(COMBAT_VISION_INTERVAL))
        {
            _canSee = CanSeeTarget(_stateMachine._targetBattleAgent);
            _IsCovered = IsTargetCovered(_stateMachine._targetBattleAgent);
            _canAttack = CanAttackTargetAgent();
        }
        if (_canSee)
        {
            targetLostTimer = 0f;
            _stateMachine._lastDetectPosition = _stateMachine._targetTransform.position;
        }
        else
        {
            targetLostTimer += COMBAT_VISION_INTERVAL;

            if (targetLostTimer >= maxTargetLostTime)
            {
                _stateMachine.ChangeState(_stateMachine._chaseState);
                Debug.Log("[Attack] 플레이어가 시야에서 완전히 사라졌습니다. 추격으로 전환합니다.");
            }

            return;
        }
        if (_IsCovered)
        {
            _hideWaitTimer += Time.deltaTime;
            // 현재 사격 가능한 상황인지 판단
            if (!_canAttack && _hideWaitTimer >= _maxHideWaitTime)
            {
                _stateMachine.ChangeState(_stateMachine._chaseState);
                Debug.Log("[Attack] 사격이 불가능 합니다. 추격으로 전환합니다.");
                return;
            }
            return;
        }
        else
        {
            _hideWaitTimer = 0f;
        }
        RotateTowardsToPoint(aimPosition);
        switch (currentPhase)
        {
            case AttackPhase.Aim:

                aimTimer += Time.deltaTime;

                if (aimTimer >= aimDuration)
                {
                    currentBurstCount = 0;
                    burstTimer = 0;

                    currentPhase = AttackPhase.Burst;
                }

                break;
            case AttackPhase.Burst:

                burstTimer += Time.deltaTime;

                if (burstTimer >= burstInterval)
                {
                    burstTimer = 0f;

                    EnemyAttack();

                    currentBurstCount++;

                    if (currentBurstCount >= maxBurstCount)
                    {
                        _stateMachine._animController.ChangeAnimState(EnemyAnimState.Move, 0f);
                        _stateMachine._coverActionState.StartNewCoverAction();
                        _stateMachine.ChangeState(_stateMachine._coverActionState);
                        return;
                    }
                }
                break;
        }

    }

    public override void ExitState()
    {
        Debug.Log("[Attack] 상태 중지.");
        _stateMachine._animController.ResetAnimState();
    }

    public void EnemyAttack()
    {
        _stateMachine._animController.ChangeAnimState(EnemyAnimState.Attack);

        Vector3 muzzle = _stateMachine.transform.position + Vector3.up * 1.5f;

        Vector3 dir = (aimPosition - muzzle).normalized;
        Debug.Log("[Attack] 발사.");
        _stateMachine._enemyBase.CurrentWeapon.Fire(muzzle,dir);
       
    }
  
    private void StartAim()
    {
        currentPhase = AttackPhase.Aim;

        aimTimer = 0f;

        // 플레이어 현재 위치를 저장
        aimPosition = _stateMachine._targetTransform.position + Vector3.up * 1f;

        // 약간의 오차 추가
        //aimPosition += new Vector3(Random.Range(-0.25f, 0.25f),Random.Range(-0.1f, 0.1f), Random.Range(-0.25f, 0.25f));

        // 조준 애니메이션
       // stateMachine.animator.SetBool("IsAiming", true);
    }
}
public class EnemyReloadState : BaseEnemyState 
{
    private float _reloadTimer = 0f;
    private float _maxReloadTime;
    private int _reloadBulletAmount;
    public EnemyReloadState(EnemyStateMachine stateMachine) : base(stateMachine) { }
    public override void EnterState() 
    {
        _maxReloadTime = _stateMachine._enemyBase.CurrentWeapon.ReloadTime;
        _reloadBulletAmount = _stateMachine._enemyBase.CurrentWeapon.MagazineSize;
        _reloadTimer = 0f;
        Debug.Log("[Reload] 재장전 시작.");
        _stateMachine.SetDebugStateColor(EnemyState.Reload);
        _stateMachine._agent.speed = 0f;
        _stateMachine._animController.ChangeAnimState(EnemyAnimState.Reload);
    }  // 이 상태로 처음 들어왔을 때 
    public override void UpdateState() 
    {
        // 전투 중 장전이었다면 기존 타겟만 계속 확인
        if (_stateMachine._targetBattleAgent != null)
        {
            if (CanSeeTarget(_stateMachine._targetBattleAgent))
            {
                _stateMachine._lastDetectPosition = _stateMachine._targetTransform.position;
            }
        }
        // 순찰 중 장전이었다면 새로 플레이어를 탐지
        else
        {
            if (TryDetectTarget(out IBattleAgent target,PatrolPhase.Patrol))
            {
                _stateMachine.SetTarget(target);
            }
        }

        _reloadTimer += Time.deltaTime;

        if (_reloadTimer >= _maxReloadTime)
        {
            _stateMachine._enemyBase.CurrentWeapon.Reload(_reloadBulletAmount);

            Debug.Log($"[Reload] 재장전 끝 현재탄창 {_stateMachine._enemyBase.CurrentWeapon.RemainBullets}발.");

            ReturnToPreviousState();
        }


    } // 매 프레임 실행될 로직 
    public override void ExitState() 
    { 
        Debug.Log("[Reload] 재장전 종료.");
        _stateMachine._animController.ResetAnimState();

    }   // 이 상태에서 빠져나갈 때
    private void ReturnToPreviousState() 
    {
        switch (_stateMachine._previousEnemyState) 
        {
            case EnemyState.Patrol:
                _stateMachine.ChangeState(_stateMachine._patrolState);
                break;
            case EnemyState.Attack:
                _stateMachine.ChangeState(_stateMachine._attackState); 
                break;
            case EnemyState.CoverAction:
                _stateMachine.ChangeState(_stateMachine._coverActionState);
                break;
            default: break;
        }
    }
}
public enum CoverPhase
{
    FindCover,
    NoCoverCooldown,
    MoveToCover,
    Hide,
    MoveToPeek
}

public class EnemyCoverActionState : BaseEnemyState
{
    private CoverPhase _currentCoverPhase;
    private float _cooldownTimer = 0f;

    public EnemyCoverActionState(EnemyStateMachine stateMachine) : base(stateMachine) { }
    public override void EnterState()
    {
        _stateMachine._agent.speed = 2f;
        _stateMachine._agent.stoppingDistance = 0.5f;
        Debug.Log("커버액션 엔터");
    }  // 이 상태로 처음 들어왔을 때 
    public override void UpdateState()
    {
        Debug.Log("커버액션 업데이트");
        Debug.Log($"현재상태{_currentCoverPhase}");
        switch (_currentCoverPhase)
        {
            case CoverPhase.FindCover:

                _stateMachine._agent.speed = 2f;
                if (TryFindCoverWall(out CoverWallInfo wallInfo))
                {
                   
                    if (_stateMachine._enemyBase.CurrentCoverWallInfo == null || wallInfo.SelectedHidePoint != _stateMachine._enemyBase.CurrentCoverWallInfo.SelectedHidePoint)
                    {
                        if (_stateMachine._enemyBase.CurrentCoverWallInfo != null)
                        {
                            _stateMachine._enemyBase.CurrentCoverWallInfo.Wall.ReleaseWallUser(_stateMachine._enemyBase);
                        }
                        wallInfo.Wall.RegisterUserToWall(_stateMachine._enemyBase);
                        _stateMachine._enemyBase.CurrentCoverWallInfo = wallInfo;
                    }
                    GetNewCoverActionPoint(_stateMachine._enemyBase.CurrentCoverWallInfo);
                    Vector3 newHidePoint = _stateMachine._enemyBase.CurrentCoverWallInfo.HidePosition;
                    MoveToCoverActionPoint(newHidePoint);
                    _currentCoverPhase = CoverPhase.MoveToCover;
                }
                else
                {
                    _cooldownTimer = 0f;
                    _currentCoverPhase = CoverPhase.NoCoverCooldown;
                }
                break;
            case CoverPhase.MoveToCover:
                UpdateMoveAnimation();
                if (IsArrivedToCoverActionPoint())
                {
                    _stateMachine._animController.ChangeAnimState(EnemyAnimState.Move, 0f);
                    _cooldownTimer = 0f;
                    _currentCoverPhase = CoverPhase.Hide;
                }
                break;
            case CoverPhase.NoCoverCooldown:

                _cooldownTimer += Time.deltaTime;
                if (NeedReload(COMBAT_RELOAD_RATIO))
                {
                    Debug.Log("NoCoverCooldown페이즈 장전");
                    _stateMachine._previousEnemyState = EnemyState.CoverAction;
                    _stateMachine.ChangeState(_stateMachine._reloadState);
                    return;
                }
                if (_cooldownTimer >= 0.5f)
                {
                    _stateMachine.ChangeState(_stateMachine._attackState);
                }
                break;
            case CoverPhase.Hide:
                if (NeedReload(COMBAT_RELOAD_RATIO))
                {
                    Debug.Log("Hide페이즈 장전");
                    // _currentCoverPhase = CoverPhase.Peek;
                    _stateMachine._previousEnemyState = EnemyState.CoverAction;
                    _stateMachine.ChangeState(_stateMachine._reloadState);
                    return;
                }
                RotateTowardsToPoint(_stateMachine._enemyBase.CurrentCoverWallInfo.PeekPoint.position);
                _cooldownTimer += Time.deltaTime;
                if (_cooldownTimer >= 1.5f)
                {
                    _stateMachine._agent.speed = 2f;
                    UpdateMoveAnimation();
                    Vector3 peekPoint = _stateMachine._enemyBase.CurrentCoverWallInfo.PeekPosition;
                    MoveToCoverActionPoint(peekPoint);
                    _currentCoverPhase = CoverPhase.MoveToPeek;
                }
                break;
            case CoverPhase.MoveToPeek:
                UpdateMoveAnimation();
                if (IsArrivedToCoverActionPoint())
                {
                    _stateMachine._animController.ChangeAnimState(EnemyAnimState.Move, 0f);
                    _currentCoverPhase = CoverPhase.FindCover;
                    _stateMachine.ChangeState(_stateMachine._attackState);
                }
                break;
        }

    }
    public override void ExitState()
    {
    }
    public bool TryFindCoverWall(out CoverWallInfo wallInfo)
    {
        wallInfo = null;
        Collider[] hitColliders = Physics.OverlapSphere(_stateMachine.transform.position, 50f, _stateMachine._coverWallLayerMask);

        foreach (Collider col in hitColliders)
        {
            CoverWall wall = col.GetComponentInParent<CoverWall>();

            if (wall == null)
            {
                continue;
            }
            if (!wall.CanRegisterUserToWall(_stateMachine._enemyBase))
            {
                continue;
            }
            CoverWallInfo info = new CoverWallInfo();
            info.Wall = wall;

            if (TrySelectCoverWall(info))
            {
                wallInfo = info;
                return true;
            }
        }

        return false;
    }
    public void MoveToCoverActionPoint(Vector3 newPosition)
    {
        _stateMachine._agent.SetDestination(newPosition);
        Debug.LogWarning(  $"{_stateMachine.name} Destination : {newPosition}"
);
    }

    public bool IsArrivedToCoverActionPoint()
    {
        if (_stateMachine._agent.pathPending)
        {
            return false;
        }

        return _stateMachine._agent.remainingDistance <= _stateMachine._agent.stoppingDistance;
    }
    public void StartNewCoverAction()
    {
        _currentCoverPhase = CoverPhase.FindCover;
    }
    public bool TrySelectCoverWall(CoverWallInfo wallInfo)
    {
        if (TrySelectHidePoint(wallInfo))
        {
            if (CanAttackTarget(wallInfo.SelectedHidePoint.PeekLeft.position + Vector3.up * 1f, _stateMachine._targetTransform.position))
            {
                wallInfo.PeekPoint = wallInfo.SelectedHidePoint.PeekLeft;
                return true;
            }
            else if (CanAttackTarget(wallInfo.SelectedHidePoint.PeekRight.position + Vector3.up * 1f, _stateMachine._targetTransform.position))
            {
                wallInfo.PeekPoint = wallInfo.SelectedHidePoint.PeekRight;
                return true;
            }
            return false;
        }
        else
        {
            return false;
        }
    }
    public bool TrySelectHidePoint(CoverWallInfo wallInfo)
    {
        Vector3 startPos = _stateMachine._targetTransform.position + Vector3.up * 1f;
        Vector3 targetPos1 = wallInfo.Wall._coverHidePoint1.HidePoint.position + Vector3.up * 1f;
        Vector3 targetPos2 = wallInfo.Wall._coverHidePoint2.HidePoint.position + Vector3.up * 1f;

        if (IsWallBetween(startPos, targetPos1, wallInfo.Wall))
        {
            wallInfo.SelectedHidePoint = wallInfo.Wall._coverHidePoint1;
            return true;
        }
        else if (IsWallBetween(startPos, targetPos2, wallInfo.Wall))
        {
            wallInfo.SelectedHidePoint = wallInfo.Wall._coverHidePoint2;
            return true;
        }
        else
        {
            return false;
        }
    }
    public bool IsWallBetween(Vector3 startPos, Vector3 targetPos, CoverWall wall)
    {
        Vector3 direction = (targetPos - startPos).normalized;
        float distance = Vector3.Distance(startPos, targetPos);

        if (Physics.Raycast(startPos, direction, out RaycastHit hit, distance, _stateMachine._coverWallLayerMask))
        {
            return hit.collider.GetComponentInParent<CoverWall>() == wall;
        }
        else
        {
            return false;
        }
    }
    public void GetNewCoverActionPoint(CoverWallInfo wallInfo)
    {
         _stateMachine._enemyBase.CurrentCoverWallInfo.HidePosition = wallInfo.Wall.ReturnHidePoint(_stateMachine._enemyBase, _stateMachine._enemyBase.CurrentCoverWallInfo.SelectedHidePoint.HidePoint);
        _stateMachine._enemyBase.CurrentCoverWallInfo.PeekPosition = wallInfo.Wall.ReturnPeekPoint(_stateMachine._enemyBase, _stateMachine._enemyBase.CurrentCoverWallInfo.PeekPoint);
    }
}
    public class EnemyRetreatState : BaseEnemyState
    {
        // 생성자
        public EnemyRetreatState(EnemyStateMachine stateMachine) : base(stateMachine) { }
        public override void EnterState() { }  // 이 상태로 처음 들어왔을 때 
        public override void UpdateState() { } // 매 프레임 실행될 로직 
        public override void ExitState() { }   // 이 상태에서 빠져나갈 때
    }
    public class EnemyDeadState : BaseEnemyState
    {
        // 생성자
        public EnemyDeadState(EnemyStateMachine stateMachine) : base(stateMachine) { }
        public override void EnterState()
        {
            _stateMachine._agent.ResetPath();
            _stateMachine.ClearTarget();
            _stateMachine._enemyBase.CurrentCoverWallInfo.Wall.ReleaseWallUser(_stateMachine._enemyBase);
            _stateMachine._enemyBase._enemyCollider.enabled = false;
            _stateMachine._agent.enabled = false;
            _stateMachine._animController.ChangeAnimState(EnemyAnimState.Dead);
            Debug.Log("deadState진입");
        }  // 이 상태로 처음 들어왔을 때 
        public override void UpdateState()
        {

        } // 매 프레임 실행될 로직 
        public override void ExitState() { }   // 이 상태에서 빠져나갈 때
    }

