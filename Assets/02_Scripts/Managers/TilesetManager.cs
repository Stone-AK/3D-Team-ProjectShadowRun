using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class TilesetManager : MonoBehaviour // 타일셋 관리 매니저
{
    public static TilesetManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this; // < 추후 게임 오브젝트 매니저에서 실행할 수 있도록
    }

    public async UniTask<GameObject> GetRandomTilePrefabAsync(string type) // type에 해당하는 타일 데이터 중 가중치에 따라 랜덤으로 선택된 타일 프리팹을 반환
    {
        List<TileData> candidates = DataManager.Instance.GetTileDataListByType(type);

        if (candidates == null || candidates.Count == 0)
        {
            Debug.LogError($"TilesetManager: {type} 타입 타일 데이터가 없습니다");
            return null;
        }

        TileData selectedTile = SelectTileByWeight(candidates);

        if (selectedTile == null) return null;

        GameObject tilePrefab = await ResourceManager.Inst.LoadAsset<GameObject>(selectedTile.PrefabPath);

        if (tilePrefab == null)
        {
            Debug.LogError($"TilesetManager: {selectedTile.PrefabPath} 경로의 타일 프리팹을 로드하지 못했습니다");
            return null;
        }

        return tilePrefab;
    }

    private TileData SelectTileByWeight(List<TileData> candidates) // 가중치 기반 랜덤 선택
    {
        int totalWeight = 0;

        // 모든 후보군의 가중치 총합 계산
        foreach (var tile in candidates)
        {
            // 혹시 실수로 가중치를 0 이하로 적었다면 최소 1로 보정
            int weight = Mathf.Max(1, tile.SpawnWeight);
            totalWeight += weight;
        }

        // totalWeight 사이의 난수 생성
        int randomPick = Random.Range(0, totalWeight);
        int currentAccumulatedWeight = 0;

        // 다시 리스트를 순회하며 누적 가중치를 계산하여 당첨자 선택
        foreach (var tile in candidates)
        {
            currentAccumulatedWeight += Mathf.Max(1, tile.SpawnWeight);

            if (randomPick < currentAccumulatedWeight)
            {
                return tile; // 당첨된 타일 데이터 반환
            }
        }

        // 만약 루프가 비정상적으로 끝났다면 마지막 요소라도 반환
        return candidates[candidates.Count - 1];
    }
}