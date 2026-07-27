using System;
using UnityEngine;
public enum BattleAgentTeamType 
{
    None,
    Player,
    TeamA,
    TeamB
}

public class EnemyBase : MonoBehaviour,IDamageable,IBattleAgent
{
    public TestTestWeaponBase CurrentWeapon { get; private set; }
    [SerializeField] Transform _weaponSpawnPo;
    [SerializeField] TestTestWeaponBase _testWeapon;
    [SerializeField] BattleAgentTeamType _battleAgentTeamType;
    public Collider _enemyCollider;
    public float Hp { get; private set; } = 100;
    public bool IsDead { get; private set; } = false;

    
    public float FrontDetectDistance = 20f; //추후에 데이터에서 받아와 사용
    public float SideDetectDistance = 10f;
    public float BackDetectDistance = 3f;
    public BattleAgentTeamType Team { get => _battleAgentTeamType; }
    public Transform Transform { get => this.transform; }
    public Action OnEnemyDeadAct;
    public Action OnEnemyTakeDamageAct;
    public CoverWallInfo CurrentCoverWallInfo { get; set; }
    //public EnemyData Data { get; private set; }
    // 상태 이상 수신용 3줄 추가
    public float SpeedMultiplier { get; private set; } = 1f;
    public void SetSpeedMultiplier( float multiplier ) => SpeedMultiplier = multiplier;
    public void SetStunned( bool isStunned ) { } // 스턴 상태 확인이 필요없다면 빈 칸 유지
    public void Awake()
    {
        SetWeapon(_testWeapon);
        _enemyCollider = GetComponent<Collider>();
    }
    void Update()
    {
        Debug.DrawRay(transform.position + Vector3.up * 1f, transform.forward * 5f, Color.blue);
    }
    public void SetWeapon(TestTestWeaponBase weapon)
    {
        CurrentWeapon = weapon;

        weapon.transform.SetParent(_weaponSpawnPo, false);
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;
    }

    public void TakeDamage(float damage) 
    {
        float newHp = Hp - damage;
        Hp = (newHp>=0) ? newHp : 0;
        Debug.Log($"{damage}피해 입음, 남은 체력 {Hp}");
        OnEnemyTakeDamageAct?.Invoke();
        if (Hp <= 0 && IsDead == false) 
        {
            IsDead = true;
            OnEnemyDead();
        }
    }
    public void UseWeapon() { }
    public void Initialize(EnemyData enemyData) { }
    public void OnEnemyDead() 
    {
        Debug.Log("OnEnemyDeadAct?.Invoke");        
        OnEnemyDeadAct?.Invoke();
    }
}