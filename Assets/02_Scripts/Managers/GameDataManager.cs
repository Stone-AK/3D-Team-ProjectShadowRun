using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

public class GameDataManager : MonoBehaviour
{
    private static GameDataManager _instance;
    public static GameDataManager Instance { get { return _instance; } }

    private Dictionary<string, ItemData> _itemDataDict = new Dictionary<string, ItemData>();
    private Dictionary<string, TileData> _tileDataDict = new Dictionary<string, TileData>(); // 김동혁 - 타일 데이터 딕셔너리 추가

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
            LoadStaticDataFromJson();
            LoadTileDataFromJson(); // 김동혁 - 타일 관련 데이터 로드 메서드 호출 추가
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    private void LoadStaticDataFromJson()
    {
        TextAsset jsonText = Resources.Load<TextAsset>("Data/ItemData");

        if (jsonText == null)
        {
            Debug.LogError("GameDataManager: ItemData.json 파일을 찾을 수 없습니다! Resources/Data 폴더를 확인하세요.");
            return;
        }

        List<ItemData> dataList = JsonConvert.DeserializeObject<List<ItemData>>(jsonText.text);

        foreach (ItemData item in dataList)
        {
            if (!_itemDataDict.ContainsKey(item.Id))
            {
                _itemDataDict.Add(item.Id, item);
            }
        }

        Debug.Log($"GameDataManager: JSON 데이터 파싱 성공! 총 {_itemDataDict.Count}개의 아이템 데이터를 로드했습니다.");
    }

    public ItemData GetItemDataById(string itemId)
    {
        if (_itemDataDict.TryGetValue(itemId, out ItemData data))
        {
            return data;
        }

        Debug.LogError($"GameDataManager: ID {itemId}에 해당하는 아이템 데이터가 없습니다!");
        return null;
    }


    #region [============================================================김동혁 - 타일 관련 메서드 추가============================================================]
    private void LoadTileDataFromJson() // 김동혁 - 타일 관련 데이터 로드 메서드
    {
        TextAsset jsonText = Resources.Load<TextAsset>("Data/TileData");

        if (jsonText == null)
        {
            Debug.LogError("GameDataManager: TileData.json 파일을 찾을 수 없습니다! Resources/Data 폴더를 확인하세요.");
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

        Debug.Log($"GameDataManager: JSON 데이터 파싱 성공! 총 {_tileDataDict.Count}개의 타일 데이터를 로드했습니다.");
    }

    public TileData GetTileDataById(string tileId) // 김동혁 - 타일 ID에 따른 데이터 반환 메서드
    {
        if (_tileDataDict.TryGetValue(tileId, out TileData data))
        {
            return data;
        }

        Debug.LogError($"GameDataManager: ID {tileId}에 해당하는 타일 데이터가 없습니다!");
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
            Debug.LogWarning($"GameDataManager: {tileType} 타입에 해당하는 타일 데이터가 없습니다!");
        }
        return result;
    }
    #endregion

}
