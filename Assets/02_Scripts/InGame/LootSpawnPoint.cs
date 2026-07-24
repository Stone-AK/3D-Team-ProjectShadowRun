using UnityEngine;

public class LootSpawnPoint : MonoBehaviour
{
    [Header("스폰 설정")]
    [Tooltip("스폰 확률 (0~100)")]
    [Range(0f, 100f)]
    public float SpawnProbability = 60f;

    [Tooltip("특정 아이템 타입만 스폰하고 싶다면 입력 (비워두면 아무거나 랜덤)")]
    public string TargetItemType = ""; 

    private void OnDrawGizmos()
    {
        Gizmos.color = string.IsNullOrEmpty(TargetItemType) ? Color.yellow : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }

    public bool ShouldSpawn() => Random.Range(0f, 100f) <= SpawnProbability;
}
