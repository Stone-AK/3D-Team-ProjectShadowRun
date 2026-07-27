using System.Collections.Generic;
using UnityEngine;

public class EnemyFactory : MonoBehaviour
{
    public static EnemyFactory Instance { get; private set; }
    [SerializeField] private List<GameObject> _enemyPrefabList;
    [SerializeField] private List<GameObject> _weaponPrefabList;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }
  
    public EnemyBase CreateEnemy(Transform spawnPo,BattleAgentTeamType type) 
    {
        //if (!DataManager.Instance._enemyDataDic.TryGetValue(enemyID, out EnemyData enemyData))
        //{
        //    Debug.LogError($"EnemyData가 존재하지 않습니다. ID : {enemyID}");
        //    return null;
        //}
        //GameObject enemyObject = ObjectPoolManager.Instance.GetFromPool(enemyData.PrefabAddress);
        //if (enemyObject == null)
        //{
        //    Debug.LogError($"Enemy Pool 생성 실패 : {enemyData.PrefabAddress}");
        //    return null;
        //}
        //EnemyBase enemy = enemyObject.GetComponent<EnemyBase>();
        //if (enemy == null)
        //{
        //    Debug.LogError($"{enemyData.PrefabAddress} 프리팹에 EnemyBase가 없습니다.");
        //    return null;
        //}
        GameObject enemyObject = null;
        switch (type)
        {
            case BattleAgentTeamType.TeamA:
                enemyObject = GameObjectManager.Instance.SpawnObject(_enemyPrefabList[0], spawnPo.position, Quaternion.identity);
                break;
            case BattleAgentTeamType.TeamB:
                enemyObject = GameObjectManager.Instance.SpawnObject(_enemyPrefabList[1], spawnPo.position, Quaternion.identity);
                break;
            default: break;

        }
        if (enemyObject == null) return null;

        enemyObject.GetComponent<Animator>().enabled = false;
        enemyObject.GetComponent<Animator>().enabled = true;
        EnemyBase enemy = enemyObject.GetComponent<EnemyBase>();
        TestWeaponBase weapon = GetWeapon();
        enemy.Initialize(weapon);
        return enemy;
    }
    public TestWeaponBase GetWeapon() 
    {
        GameObject weaponObject = GameObjectManager.Instance.SpawnObject(_weaponPrefabList[0],transform.position, Quaternion.identity);
        TestWeaponBase weapon = weaponObject.GetComponent<TestWeaponBase>();
        WeaponData weaponData = DataManager.Instance.GetItemData("Item_Weapon_AR_01") as WeaponData;
        if (weaponData == null) Debug.Log("weaponData없음");
        weapon.Initialize(weaponData, null);
        return weapon;
    }
}
