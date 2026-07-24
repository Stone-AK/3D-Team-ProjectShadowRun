using Cysharp.Threading.Tasks; // 비동기 처리를 위한 네임스페이스 추가
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TileGenerator : MonoBehaviour
{
    [Header("Map Generation Settings")]
    [SerializeField] private int _maxRoomCount = 50;
    [SerializeField] private GameObject _startRoomPrefab; // 시작 방은 기존처럼 인스펙터 할당 유지
    [SerializeField] private string _normalRoomType = "Normal"; // 배열 대신 데이터 테이블 검색용 타입 문자열로 변경
    [SerializeField] private GameObject _endRoomPrefab;

    [Header("Physics Settings")]
    [SerializeField] private LayerMask _tileLayerMask;

    [Header("Generation Safety Settings")]
    [SerializeField] private int _maxRetryCount = 50;
    [SerializeField] private int _maxExtraRoomCount = 5;

    // [개선] Transform 리스트에서 TileSocket 타입 리스트로 변경하여 타입 안정성 확보
    private List<TileSocket> _globalOpenSockets = new List<TileSocket>();
    private int _currentRoomCount = 0;
    private TileController _spawnedStartRoom;


    private void Start()
    {
        // 비동기 맵 생성 루틴 시작
        InitializeAndGenerate().Forget();
    }

    private async UniTaskVoid InitializeAndGenerate()
    {
        // DataManager가 JSON을 파싱할 때까지 아주 잠깐(1프레임 혹은 데이터 체크) 대기합니다.
        // 만약 데이터 매니저에 IsInitialized 같은 bool 변수가 있다면 그것을 체크하는 것이 가장 좋습니다.
        await UniTask.Yield(PlayerLoopTiming.Update);

        // 이제 데이터가 확실히 들어왔으므로 맵 생성을 시작합니다.
        GenerateMapAsync().Forget();
    }


    public async UniTaskVoid GenerateMapAsync()
    {
        // 1. 시작 방 생성 및 초기화
        GameObject startRoomObj = GameObjectManager.Instance.SpawnObject(_startRoomPrefab, Vector3.zero, Quaternion.identity);
        TileController startRoom = startRoomObj.GetComponent<TileController>();

        if (startRoom == null)
        {
            Debug.LogError("[CRITICAL] TileGenerator: 시작 방 프리팹에 TileController가 없습니다!");
            return;
        }

        _spawnedStartRoom = startRoom;
        _currentRoomCount++;
        _globalOpenSockets.AddRange(startRoom.OpenSockets); // TileController의 프로퍼티 활용
        Debug.Log($"시작 방 생성 완료: ID {startRoomObj.name}");

        // 2. 맵 생성 메인 루프 (Layer 1)
        int failedAttempts = 0;
        int maxFailures = _maxRetryCount;

        while (_currentRoomCount < _maxRoomCount - 1 && failedAttempts < maxFailures)
        {
            if (_globalOpenSockets.Count == 0) break; // 더 이을 소켓이 없으면 즉시 루프 탈출

            bool success = await TryConnectRandomRoomAsync();
            if (success)
            {
                _currentRoomCount++;
                // 성공 시에는 failedAttempts를 올리지 않습니다!
            }
            else
            {
                failedAttempts++; // 실패했을 때만 깎습니다.
            }
        }

        bool endRoomSuccess = TryConnectEndRoom();

        if (!endRoomSuccess)
        {
            int extraAttempts = 0;

            while (!endRoomSuccess && extraAttempts < _maxExtraRoomCount)
            {
                extraAttempts++;

                bool extended = await TryConnectRandomRoomAsync();

                if (extended)
                {
                    _currentRoomCount++;
                    endRoomSuccess = TryConnectEndRoom();
                }
            }
        }

        if (!endRoomSuccess)
        {
            Debug.LogWarning("[WARNING] 공간 부족으로 정상 스폰 실패. 최후의 보루: 기존 막다른 일반 방을 끝 지점으로 교체합니다.");
            endRoomSuccess = TryForceReplaceAndCull();
        }


        if (!endRoomSuccess)
        {
            Debug.LogError("[CRITICAL] 교체 가능한 막다른 방이 없거나 끝 지점 프리팹 미할당으로 최종 실패!");

        }
        else
        {
            _currentRoomCount++; // 끝 지점 방도 카운트에 포함
            Debug.Log($"[SUCCESS] 끝 지점(인스펙터 프리팹) 배치 성공! 최종 방 개수: {_currentRoomCount}");
        }

        // 결과 리포트
        if (_currentRoomCount < _maxRoomCount)
            Debug.LogWarning($"[WARNING] TileGenerator: 공간 부족으로 목표치 미달. 최종: {_currentRoomCount}개 (실패 수: {failedAttempts})");
        else
            Debug.Log($"[SUCCESS] TileGenerator: 절차적 맵 생성 완료! 총 {_currentRoomCount}개 방 배치됨.");

        // 🚨 [추가된 부분] 맵 생성이 끝난 후, 짝을 찾지 못해 남겨진 모든 외부 소켓(문)을 닫아줍니다!
        foreach (TileSocket remainingSocket in _globalOpenSockets)
        {
            if (remainingSocket != null)
            {
                remainingSocket.SetPassageState(false);
            }
        }

        Debug.Log($"[TileGenerator] 남은 외부 연결 통로 {_globalOpenSockets.Count}개를 모두 안전하게 폐쇄했습니다.");
    }


    // ============================== 방 생성 및 스폰 (Layer 2) ==============================
    private async UniTask<bool> TryConnectRandomRoomAsync()
    {
        if (_globalOpenSockets.Count == 0) return false;

        // 1. 무작위 기준 소켓 A 선정
        int randomIndex = Random.Range(0, _globalOpenSockets.Count);
        TileSocket socketA = _globalOpenSockets[randomIndex];

        // 2. TilesetManager를 통한 비동기 프리팹 데이터 로드
        GameObject randomPrefab = await TilesetManager.Instance.GetRandomTilePrefabAsync(_normalRoomType);
        if (randomPrefab == null) return false;

        // 3. GameObjectManager를 통한 스폰
        GameObject nextRoomObj = GameObjectManager.Instance.SpawnObject(randomPrefab, Vector3.zero, Quaternion.identity);
        TileController nextRoom = nextRoomObj.GetComponent<TileController>();

        if (nextRoom == null)
        {
            Destroy(nextRoomObj); // 유효하지 않은 프리팹 즉시 폐기
            return false;
        }

        return ProcessRoomPlacement(nextRoom, socketA);
    }

    // ============================== 끝 지점(End Room) 생성 ==============================
    private bool TryConnectEndRoom()
    {
        // 1. 열린 소켓이 없거나 프리팹이 안 잠겨있으면 예외 처리
        if (_globalOpenSockets.Count == 0) return false;

        if (_endRoomPrefab == null)
        {
            Debug.LogError("[CRITICAL] TileGenerator: 끝 지점 프리팹(_endRoomPrefab)이 인스펙터에 할당되지 않았습니다!");
            return false;
        }

        // 2. 무작위 기준 소켓 A 선정
        int randomIndex = Random.Range(0, _globalOpenSockets.Count);
        TileSocket socketA = _globalOpenSockets[randomIndex];

        // 3. 인스펙터에 등록된 프리팹을 GameObjectManager를 통해 바로 스폰 (비동기 대기 없음)
        GameObject endRoomObj = GameObjectManager.Instance.SpawnObject(_endRoomPrefab, Vector3.zero, Quaternion.identity);
        TileController endRoom = endRoomObj.GetComponent<TileController>();

        if (endRoom == null)
        {
            Destroy(endRoomObj); // 유효하지 않은 프리팹 즉시 폐기
            return false;
        }

        // 4. 물리 검사 및 최종 배치 처리 수행 (기존 Layer 3 로직 그대로 재활용)
        return ProcessRoomPlacement(endRoom, socketA, true);
    }

    // ============================== 물리 검사 및 사후 처리 (Layer 3) ==============================
    private bool ProcessRoomPlacement(TileController nextRoom, TileSocket socketA, bool isEndRoom = false)
    {
        // 🌟 [핵심 개선] 새 방이 가진 '모든 문(Socket)'을 하나씩 다 끼워 맞춰 봅니다!
        foreach (TileSocket socketB in nextRoom.OpenSockets)
        {
            // 1. 해당 문(socketB)을 기준으로 타일 정렬 및 회전
            TilePhysics.AlignRoom(nextRoom, socketA, socketB);

            // 2. 물리 겹침 검사
            if (TilePhysics.IsRoomOverlapping(nextRoom, _tileLayerMask))
            {
                // 겹치면 부수지 말고, 다음 문(socketB)으로 회전시켜서 다시 시도!
                continue;
            }

            // 3. 조기 끊김 검사 (끝 지점은 예외)
            if (!isEndRoom && IsMapCutOffPrematurely(nextRoom, socketB))
            {
                continue; // 맵이 끊길 위험이 있으면 다음 문으로 회전!
            }

            // 4. 모든 검사를 통과했다면 최종 배치 처리 후 성공(true) 반환!
            FinalizeRoomPlacement(nextRoom, socketA, socketB);
            return true;
        }

        // 방이 가진 모든 문을 다 돌려가며 껴봤는데도 전부 겹치면, 그제서야 스폰 실패 처리
        HandleSpawnFailure(nextRoom.gameObject);
        return false;
    }


    private void HandleSpawnFailure(GameObject roomObj)
    {
        // 겹침 판정 시 임시 스폰된 방을 즉시 파괴하여 흔적을 지웁니다.
        // (GameObjectManager의 ID 체계와 꼬이지 않도록 엔진 Destroy 사용 권장)
        Destroy(roomObj);
    }


    private void FinalizeRoomPlacement(TileController nextRoom, TileSocket socketA, TileSocket socketB)
    {
        // 통로 상태 갱신
        socketA.SetPassageState(true);
        socketB.SetPassageState(true);

        // 연결 상태 갱신
        socketA.IsConnected = true;
        socketB.IsConnected = true;

        // 사용된 소켓은 글로벌 리스트에서 제거하고, 새 방의 남은 소켓들을 추가
        _globalOpenSockets.Remove(socketA);

        foreach (var socket in nextRoom.OpenSockets)
        {
            if (socket != socketB)
            {
                _globalOpenSockets.Add(socket);
            }
        }
    }


    private bool IsMapCutOffPrematurely(TileController nextRoom, TileSocket socketB)
    {
        // 새 방에서 추가될 유효 소켓 수 계산 (연결에 쓰인 socketB 제외)
        int newlyAddedSockets = nextRoom.OpenSockets.Count - 1;

        // 현재 열린 전체 소켓 수 - 이번 연결에 소모될 socketA(1개) + 새 방의 소켓들
        int expectedOpenSockets = _globalOpenSockets.Count - 1 + newlyAddedSockets;

        // 아직 마지막 방을 배치할 차례가 아닌데, 예상 잔여 소켓이 0개 이하라면 맵 확장이 영구 정지됨
        if (_currentRoomCount < _maxRoomCount - 1 && expectedOpenSockets <= 0)
        {
            Debug.LogWarning($"[WARNING] TileGenerator: 맵 조기 끊김 감지! {nextRoom.gameObject.name} 배치를 반려합니다.");
            return true;
        }

        return false;
    }

    private HashSet<TileController> GetMainIslandRooms(TileController startRoom, TileController ignoredRoom)
    {
        TileController[] allRooms = FindObjectsByType<TileController>(FindObjectsSortMode.None);
        HashSet<TileController> visited = new HashSet<TileController>();
        Queue<TileController> queue = new Queue<TileController>();

        if (startRoom == null) return visited;

        queue.Enqueue(startRoom);
        visited.Add(startRoom);

        int safetyCounter = 0; // 🌟 무한 루프 방지용 안전 상한선

        while (queue.Count > 0 && safetyCounter < 500)
        {
            safetyCounter++;
            TileController current = queue.Dequeue();

            foreach (TileSocket socket in current.OpenSockets)
            {
                if (socket == null || !socket.IsConnected) continue;

                // 이 소켓과 맞닿아 있는 상대방 소켓 찾기
                TileSocket neighborSocket = FindConnectedParentSocket(socket, allRooms, ignoredRoom);
                if (neighborSocket != null)
                {
                    TileController neighborRoom = neighborSocket.GetComponentInParent<TileController>();

                    // 무시할 방이 아니고, 아직 방문하지 않은 본토 방이라면 큐에 추가
                    if (neighborRoom != null && neighborRoom != ignoredRoom && !visited.Contains(neighborRoom))
                    {
                        visited.Add(neighborRoom);
                        queue.Enqueue(neighborRoom);
                    }
                }
            }
        }
        return visited;
    }


    // ============================== 헬퍼: 맞닿은 소켓 찾기 (거리 기반 안전 검사) ==============================
    private TileSocket FindConnectedParentSocket(TileSocket myConnectedSocket, TileController[] allRooms, TileController myRoom)
    {
        if (myConnectedSocket == null || myConnectedSocket.ConnectionPoint == null) return null;

        float threshold = 0.2f; // 문과 문이 맞닿아 있는 표준 오차 범위
        Transform myPoint = myConnectedSocket.ConnectionPoint;

        foreach (TileController room in allRooms)
        {
            if (room == null || room == myRoom) continue;
            // 만약 대상 방이 현재 숨겨져(SetActive(false)) 있다면 탐색에서 제외합니다!
            if (!room.gameObject.activeInHierarchy) continue;

            foreach (TileSocket otherSocket in room.OpenSockets)
            {
                if (otherSocket == null || otherSocket.ConnectionPoint == null) continue;

                Transform otherPoint = otherSocket.ConnectionPoint;

                // 위치가 거의 일치하고 연결 상태가 참인 경우
                if (Vector3.Distance(myPoint.position, otherPoint.position) < threshold)
                {
                    return otherSocket;
                }
            }
        }
        return null;
    }

    private bool TryForceReplaceAndCull()
    {
        if (_endRoomPrefab == null || _spawnedStartRoom == null) return false;

        TileController startRoomInstance = _spawnedStartRoom;
        TileController[] allRooms = FindObjectsByType<TileController>(FindObjectsSortMode.None);

        List<TileController> candidateRooms = new List<TileController>();
        foreach (var room in allRooms)
        {
            if (room != null && room != startRoomInstance)
            {
                candidateRooms.Add(room);
            }
        }

        // 후보 방들을 순회하며 '진짜 막다른 방'을 찾아 끝 지점으로 교체합니다.
        foreach (TileController targetRoom in candidateRooms)
        {
            if (targetRoom == null) continue;

            // 1. 연결된 소켓이 딱 1개뿐인 '진짜 막다른 방'인지 확인합니다. (다른 길을 끊어먹지 않기 위함)
            List<TileSocket> connectedSockets = new List<TileSocket>();
            foreach (TileSocket socket in targetRoom.OpenSockets)
            {
                if (socket.IsConnected)
                {
                    connectedSockets.Add(socket);
                }
            }

            if (connectedSockets.Count != 1) continue; // 막다른 방이 아니라면 패스!

            TileSocket targetSocket = connectedSockets[0];

            // 2. targetRoom을 잠시 숨겨서 물리 충돌을 막고, 이 방이 본토와 연결되어 있던 부모 소켓을 찾습니다.
            targetRoom.gameObject.SetActive(false);
            TileSocket parentSocket = FindConnectedParentSocket(targetSocket, allRooms, targetRoom);

            if (parentSocket == null)
            {
                targetRoom.gameObject.SetActive(true);
                continue;
            }

            parentSocket.IsConnected = false;

            // 3. 비워진 그 자리에 끝 지점 방 배치를 시도합니다.
            GameObject endRoomObj = GameObjectManager.Instance.SpawnObject(_endRoomPrefab, Vector3.zero, Quaternion.identity);
            TileController endRoom = endRoomObj.GetComponent<TileController>();

            bool placementSuccess = (endRoom != null && ProcessRoomPlacement(endRoom, parentSocket, true));

            if (placementSuccess)
            {
                Debug.Log("[SUCCESS] 최후의 보루: 막다른 일반 방을 끝 지점으로 안전하게 교체했습니다!");

                // 4. 배치가 성공했으므로 기존 막다른 방은 영구 파괴하여 공간을 완전히 내어줍니다.
                _globalOpenSockets.RemoveAll(socket => targetRoom.OpenSockets.Contains(socket));
                Destroy(targetRoom.gameObject);

                // 끝 지점의 남은 열린 소켓들을 글로벌 리스트에 안전하게 추가합니다.
                foreach (TileSocket socket in endRoom.OpenSockets)
                {
                    if (!socket.IsConnected) _globalOpenSockets.Add(socket);
                }

                // 기존 방 1개가 사라지고 끝 방 1개가 들어왔으므로, 
                // GenerateMapAsync의 바깥쪽 카운트 증가와 맞추기 위해 미리 1을 빼둡니다.
                _currentRoomCount--;

                return true;
            }
            else
            {
                // 5. 실패했다면 원상 복구합니다. (중요: 다른 방을 또 시도해야 하므로 원복 필수!)
                if (endRoomObj != null) Destroy(endRoomObj);
                parentSocket.IsConnected = true;
                targetRoom.gameObject.SetActive(true);
            }
        }

        return false;
    }
}