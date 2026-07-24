using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapLootSpawner : MonoBehaviour
{
    [Header("스폰 설정")]
    [Tooltip("방이 생성(Start)될 때 자동으로 내부 아이템들을 스폰시킬지 여부")]
    [SerializeField] private bool _autoSpawnOnStart = true;

    private void Start()
    {
        if (_autoSpawnOnStart)
        {
            SpawnRoomLoots();
        }
    }

    public void SpawnRoomLoots()
    {
        if (DataManager.Instance == null || DataManager.Instance._itemDataDic == null)
        {
            Debug.LogError($"[{gameObject.name}] MapLootSpawner: DataManager가 준비되지 않았습니다!");
            return;
        }

        if (GameObjectManager.Instance == null)
        {
            Debug.LogError($"[{gameObject.name}] MapLootSpawner: GameObjectManager가 준비되지 않았습니다!");
            return;
        }

        LootSpawnPoint[] spawnPoints = GetComponentsInChildren<LootSpawnPoint>();

        if (spawnPoints.Length == 0) return;

        List<ItemData> validItems = DataManager.Instance._itemDataDic.Values
            .Where(item => !string.IsNullOrEmpty(item.PrefabPath))
            .ToList();

        if (validItems.Count == 0) return;

        foreach (var spawnPoint in spawnPoints)
        {
            if (!spawnPoint.ShouldSpawn()) continue;

            List<ItemData> candidateItems = string.IsNullOrEmpty(spawnPoint.TargetItemType)
                ? validItems
                : validItems.Where(item => item.ItemType == spawnPoint.TargetItemType).ToList();

            if (candidateItems.Count == 0) continue;

            ItemData selectedData = candidateItems[UnityEngine.Random.Range(0, candidateItems.Count)];

            GameObject prefab = Resources.Load<GameObject>(selectedData.PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[{gameObject.name}] 아이템 프리팹 로드 실패! 경로: Resources/{selectedData.PrefabPath}");
                continue;
            }

            GameObject spawnedObj = GameObjectManager.Instance.SpawnObject(
                prefab,
                spawnPoint.transform.position,
                spawnPoint.transform.rotation
            );

            ItemModel newModel = CreateItemModel(selectedData);

            FieldItem fieldItem = spawnedObj.GetComponent<FieldItem>();
            if (fieldItem != null)
            {
                fieldItem.Initialize(newModel, selectedData);
                Debug.Log($"[{gameObject.name}]  아이템 스폰 성공: {selectedData.Name}");
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] 프리팹({selectedData.Name})에 FieldItem 컴포넌트가 없습니다!");
            }
        }
    }

    private ItemModel CreateItemModel(ItemData data)
    {
        string newInstanceId = Guid.NewGuid().ToString();

        if (data is WeaponData weaponData)
        {
            return new WeaponModel
            {
                InstanceId = newInstanceId,
                ItemId = data.Name,
                CurrentStackCount = 1,
                CurrentAmmo = weaponData.MagazineSize,
                CurrentDurability = weaponData.MaxDurability,
                AttachedParts = new List<ItemModel>()
            };
        }
        else
        {
            return new ItemModel
            {
                InstanceId = newInstanceId,
                ItemId = data.Name,
                CurrentStackCount = 1
            };
        }
    }
}