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
    public CoverWallInfo CurrentCoverWallInfo { get ; set ; }
    //public EnemyData Data { get; private set; }
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