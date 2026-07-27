using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private List<Transform> _spawnPoList = new List<Transform>();
    
    void Start()
    {
        int randomIndex =UnityEngine.Random.Range(0,2);
        // if (Input.GetKeyDown(KeyCode.L))

        switch (randomIndex)
        {
            case 0:
                EnemyFactory.Instance.CreateEnemy(_spawnPoList[0], BattleAgentTeamType.TeamA);
                EnemyFactory.Instance.CreateEnemy(_spawnPoList[1], BattleAgentTeamType.TeamA);
                break;
            case 1:
                EnemyFactory.Instance.CreateEnemy(_spawnPoList[0], BattleAgentTeamType.TeamB);
                EnemyFactory.Instance.CreateEnemy(_spawnPoList[1], BattleAgentTeamType.TeamB);
                break;
            default: break;
        }

    }
}
