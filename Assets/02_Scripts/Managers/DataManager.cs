using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;


public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    private JsonSerializerSettings _settings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.Auto
    };

    private void Awake()
    {
        Debug.Log("[DataManager] Awake 실행");

        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[DataManager] 중복 인스턴스 제거");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        Debug.Log("[DataManager] Instance 등록 완료");

        LoadTileDataFromJson();
        LoadFullData();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }


    public Dictionary<string, PreloadData> _preloadDataDic { get; private set; } = new Dictionary<string, PreloadData>();
    public Dictionary<string, ObjectPoolData> _objectPoolDataDic { get; private set; } = new Dictionary<string, ObjectPoolData>();
    public Dictionary<string, EnemyData> _enemyDataDic { get; private set; } = new Dictionary<string, EnemyData>();

    public Dictionary<string, ItemData> _itemDataDic { get; private set; } = new Dictionary<string, ItemData>();

    public Dictionary<string, ShopItemData> _shopItemDataDic { get; private set; } = new Dictionary<string, ShopItemData>();
    private Dictionary<string, TileData> _tileDataDict = new Dictionary<string, TileData>();

    private Dictionary<string, T> LoadData<T>(string tableName) where T : BaseData
    {
        string resourcePath = $"Data/{tableName}";
        TextAsset jsonAsset = Resources.Load<TextAsset>(resourcePath);

        if (jsonAsset == null)
        {
            Debug.LogError($"[DataManager] {tableName} JSON 파일을 찾을 수 없습니다.");
            return new Dictionary<string, T>();
        }

        try
        {
            List<T> dataList = JsonConvert.DeserializeObject<List<T>>(jsonAsset.text, _settings);

            if (dataList != null)
            {
                Debug.Log($"[DataManager] {tableName} 데이터를 {dataList.Count}개 로드했습니다.");
                Dictionary<string, T> dic = new Dictionary<string, T>();
                foreach (var item in dataList)
                {
                    string key = item.Id.ToString();
                    if (!dic.ContainsKey(key))
                    {
                        dic.Add(key, item);
                    }
                    else
                    {
                        Debug.LogWarning($"[DataManager] 중복된 ID 발견! 덮어쓰기를 무시합니다: {key}");
                    }
                }
                return dic;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{typeof(T).Name} JSON 로드 오류] {ex.Message}");
        }

        return new Dictionary<string, T>();
    }

    public void LoadPreloadData(string jsonPath) => _preloadDataDic = LoadData<PreloadData>(jsonPath);
    public void LoadObjectPoolData(string jsonPath) => _objectPoolDataDic = LoadData<ObjectPoolData>(jsonPath);
    public void LoadEnemyData(string jsonPath) => _enemyDataDic = LoadData<EnemyData>(jsonPath);
    public void LoadItemData(string jsonPath) => _itemDataDic = LoadData<ItemData>(jsonPath);
    public void LoadShopItemData(string jsonPath) => _shopItemDataDic = LoadData<ShopItemData>(jsonPath);

    public PreloadData GetPreloadData(string id) => _preloadDataDic != null && _preloadDataDic.TryGetValue(id, out var data) ? data : null;
    public ObjectPoolData GetObjectPoolData(string id) => _objectPoolDataDic != null && _objectPoolDataDic.TryGetValue(id, out var data) ? data : null;
    public EnemyData GetEnemyData(string id) => _enemyDataDic != null && _enemyDataDic.TryGetValue(id, out var data) ? data : null;
    public ItemData GetItemData(string id) => _itemDataDic != null && _itemDataDic.TryGetValue(id, out var data) ? data : null;

    public void LoadFullData()
    {
        LoadPreloadData("PreloadData");
        LoadObjectPoolData("ObjectPoolData");
        LoadEnemyData("EnemyData");
        LoadItemData("ItemData");
        LoadShopItemData("ShopItemData");
    }

    private void LoadTileDataFromJson() // 김동혁 - 타일 관련 데이터 로드 메서드
    {
        TextAsset jsonText = Resources.Load<TextAsset>("Data/TileData");

        if (jsonText == null)
        {
            Debug.LogError("DataManager: TileData.json 파일을 찾을 수 없습니다! Resources/Data 폴더를 확인하세요.");
            return;
        }

        List<TileData> dataList = JsonConvert.DeserializeObject<List<TileData>>(jsonText.text);

        foreach (TileData tile in dataList)
        {
            if (!_tileDataDict.ContainsKey(tile.Id))
            {
                _tileDataDict.Add(tile.Id, tile);
            }
        }

        Debug.Log($"DataManager: JSON 데이터 파싱 성공! 총 {_tileDataDict.Count}개의 타일 데이터를 로드했습니다.");
    }

    public TileData GetTileDataById(string tileId) // 김동혁 - 타일 ID에 따른 데이터 반환 메서드
    {
        if (_tileDataDict.TryGetValue(tileId, out TileData data))
        {
            return data;
        }

        Debug.LogError($"DataManager: ID {tileId}에 해당하는 타일 데이터가 없습니다!");
        return null;
    }

    public List<TileData> GetTileDataListByType(string tileType) // 김동혁 - 타일 타입에 따른 데이터 리스트 반환 메서드
    {
        List<TileData> result = new List<TileData>();
        foreach (var tile in _tileDataDict.Values)
        {
            if (tile.Type == tileType) result.Add(tile);
        }

        if (result.Count == 0)
        {
            Debug.LogWarning($"DataManager: {tileType} 타입에 해당하는 타일 데이터가 없습니다!");
        }
        return result;
    }
}