using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using System;

public class SaveManager : MonoBehaviour
{
    private static SaveManager _instance;
    public static SaveManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("@SaveManager");
                _instance = go.AddComponent<SaveManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private string _savePath;

    private JsonSerializerSettings _settings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.Auto
    };

    private void Awake()
    {
        _savePath = Path.Combine(Application.persistentDataPath, "PlayerData.json");
        Debug.Log($"SaveManager: 세이브 파일 경로 - {_savePath}");
    }

    public PlayerModel LoadPlayerData()
    {
        if (!File.Exists(_savePath))
        {
            Debug.Log("SaveManager: 세이브 파일이 없습니다. 신규 유저 데이터를 생성합니다.");
            return CreateNewPlayerData();
        }

        try
        {
            string json = File.ReadAllText(_savePath);

            PlayerModel data = JsonConvert.DeserializeObject<PlayerModel>(json, _settings);

            Debug.Log("SaveManager: 유저 데이터 로드 성공!");
            return data;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SaveManager: 세이브 파일이 손상되었습니다! - {e.Message}");
            return CreateNewPlayerData();
        }
    }

    public void SavePlayerData(PlayerModel data)
    {
        try
        {
            string json = JsonConvert.SerializeObject(data, Formatting.Indented, _settings);

            File.WriteAllText(_savePath, json);
            Debug.Log("SaveManager: 유저 데이터 저장 완료!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SaveManager: 데이터 저장 실패 - {e.Message}");
        }
    }

    public PlayerModel CreateNewPlayerData()
    {
        PlayerModel newPlayer = new PlayerModel();
        newPlayer.PlayerName = "Shadow_Agent";
        newPlayer.CurrentCredit = 50000;

        WeaponModel mainWeapon = new WeaponModel
        {
            InstanceId = Guid.NewGuid().ToString(),
            ItemId = "Item_Weapon_AR_01",
            CurrentStackCount = 1,
            CurrentAmmo = 0,
            CurrentDurability = 100f
        };
        newPlayer.InventoryItems.Add(mainWeapon);

        ItemModel emergencyMedkit = new ItemModel
        {
            InstanceId = Guid.NewGuid().ToString(),
            ItemId = "Item_Medical_Kit_01",
            CurrentStackCount = 2
        };
        newPlayer.InventoryItems.Add(emergencyMedkit);


        ItemModel reserveAmmo = new ItemModel
        {
            InstanceId = Guid.NewGuid().ToString(),
            ItemId = "Item_Ammo_556",
            CurrentStackCount = 60
        };
        newPlayer.StashItems.Add(reserveAmmo);

        ItemModel repairTool = new ItemModel
        {
            InstanceId = Guid.NewGuid().ToString(),
            ItemId = "Item_Loot_Tool",
            CurrentStackCount = 1
        };
        newPlayer.StashItems.Add(repairTool);

        ItemModel assetGold = new ItemModel
        {
            InstanceId = Guid.NewGuid().ToString(),
            ItemId = "Item_Loot_Gold",
            CurrentStackCount = 1
        };
        newPlayer.StashItems.Add(assetGold);

        UnityEngine.Debug.Log("SaveManager: [프로젝트 섀도우 런] 실제 테이블 기준 초기 보급품이 지급된 신규 세이브를 생성했습니다.");
        return newPlayer;
    }
}
