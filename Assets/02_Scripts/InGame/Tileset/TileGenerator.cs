using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TileGenerator : MonoBehaviour // 절차적 맵 생성기
{
    [Header("Map Generation Settings")] // 인스펙터에서 설정 가능한 맵 생성 관련 변수들, 추후 데이터 매니저에서 JSON으로 불러오도록 개선 가능
    [SerializeField] private int _maxRoomCount = 50;
    [SerializeField] private GameObject _startRoomPrefab;
    [SerializeField] private string _normalRoomType = "Normal";
    [SerializeField] private GameObject _endRoomPrefab;

    [Header("Physics Settings")]
    [SerializeField] private LayerMask _tileLayerMask;

    [Header("Generation Safety Settings")] // 맵 생성 실패 시 재시도 횟수 및 추가 방 배치 제한
    [SerializeField] private int _maxRetryCount = 50;
    [SerializeField] private int _maxExtraRoomCount = 5;


    private List<TileSocket> _globalOpenSockets = new List<TileSocket>(); // 현재 맵에서 열린 모든 소켓(문)들을 추적하는 글로벌 리스트
    private int _currentRoomCount = 0;
    private TileController _spawnedStartRoom; // 시작 방 인스턴스를 추적하여 맵 생성 후 최종 검증 및 연결 상태 확인에 활용


    /// <summary>
    /// 메서드 별로 다음과 같은 역할을 수행합니다:
    /// 
    /// 1. 생명 주기, 진입점
    /// ├ Start(): 초기화 및 맵 생성 루틴 시작
    /// └ InitializeAndGenerate(): 첫 프레임 대기 후 맵 생성 시작
    /// 
    /// 2. 메인 생성 파트
    /// ├ GenerateMapAsync(): 맵 생성 메인 루틴, 시작 방 생성, 일반 방 연결 시도, 끝 지점 방 연결 시도
    /// ├ TryConnectRandomRoomAsync(): 무작위 일반 방 연결 시도
    /// └ TryConnectEndRoom(): 끝 지점 방 연결 시도
    /// 
    /// 3. 물리 검사/배치 파트
    /// ├ ProcessRoomPlacement(): 방 배치 처리 및 물리 겹침 검사
    /// └ IsMapCutOffPrematurely(): 맵 조기 끊김 여부 검사
    /// 
    /// 4. 사후 처리 및 소켓 관리
    /// ├ FinalizeRoomPlacement(): 방 배치 최종 처리, 소켓 상태 갱신
    /// └ HandleSpawnFailure(): 스폰 실패 시 처리, 임시 방 파괴
    /// 
    /// 5. 예외 처리 및 복구
    /// ├ TryForceReplaceAndCull(): 기존 막다른 방을 끝 지점으로 교체 시도
    /// └ FindConnectedParentSocket(): 연결된 상대방 소켓 찾기
    /// </summary>


    private void Start()
    {
        // 비동기 맵 생성 루틴 시작
        InitializeAndGenerate().Forget();
    }

    private async UniTaskVoid InitializeAndGenerate()
    {
        await UniTask.Yield(PlayerLoopTiming.Update); // 첫 프레임이 끝날 때까지 기다려서, 다른 매니저들이 초기화될 시간 확보

        GenerateMapAsync().Forget(); // 맵 생성 시작
    }


    // ============================== 메인 생성 파트 ==============================
    public async UniTaskVoid GenerateMapAsync()
    {
        // 1. 시작 방 생성 및 초기화
        GameObject startRoomObj = GameObjectManager.Instance.SpawnObject(_startRoomPrefab, Vector3.zero, Quaternion.identity); // GameObjectManager를 통한 시작 방 스폰
        TileController startRoom = startRoomObj.GetComponent<TileController>(); // TileController 컴포넌트 확인

        if (startRoom == null)
        {
            Debug.LogError("[CRITICAL] TileGenerator: 시작 방 프리팹에 TileController가 없습니다!");
            return;
        }

        _spawnedStartRoom = startRoom;
        _currentRoomCount++;
        _globalOpenSockets.AddRange(startRoom.OpenSockets); // TileController의 프로퍼티 활용
        Debug.Log($"시작 방 생성 완료: ID {startRoomObj.name}");


        // 2. 맵 생성 메인 루프
        int failedAttempts = 0; // 실패 시 재시도 횟수 추적
        int maxFailures = _maxRetryCount; // 최대 재시도 횟수 제한 < 굳이 이걸 변수로 뺄 필요가 있을까 생각해보자

        while (_currentRoomCount < _maxRoomCount - 1 && failedAttempts < maxFailures) // while 사용 이유: for문은 반복 횟수가 정해져 있어, 실패 시 재시도 로직을 유연하게 처리하기 어려움. while문은 조건이 충족될 때까지 계속 반복할 수 있어, 실패 횟수에 따라 동적으로 루프를 제어할 수 있음
        {
            if (_globalOpenSockets.Count == 0) break; // 더 이을 소켓이 없으면 즉시 루프 탈출

            bool success = await TryConnectRandomRoomAsync(); // 비동기 방 연결 시도
            if (success)
            {
                _currentRoomCount++; // 성공 시 방 카운트 증가
            }
            else
            {
                failedAttempts++; // 실패 시 실패 카운트 증가
            }
        }


        // 3. 끝 지점 방 연결 시도
        bool endRoomSuccess = TryConnectEndRoom();

        if (!endRoomSuccess) // 끝 지점 방 연결 실패 시, 추가 방 배치 시도
        {
            int extraAttempts = 0;

            while (!endRoomSuccess && extraAttempts < _maxExtraRoomCount) // 추가 방 배치 시도 횟수 제한, while 사용 이유: 추가 방 배치 시도는 끝 지점 방 연결 실패 시에만 발생하며, 반복 횟수가 동적으로 결정되므로 while문 사용
            {
                extraAttempts++;

                bool extended = await TryConnectRandomRoomAsync(); // 추가 방 배치 시도

                if (extended)
                {
                    _currentRoomCount++; // 추가 방 배치 성공 시 방 카운트 증가
                    endRoomSuccess = TryConnectEndRoom(); // 추가 방 배치 후 끝 지점 방 연결 재시도
                }
            }
        }

        if (!endRoomSuccess)
        {
            Debug.LogWarning("[WARNING] 공간 부족으로 정상 스폰 실패. 최후의 보루: 기존 막다른 일반 방을 끝 지점으로 교체합니다.");
            endRoomSuccess = TryForceReplaceAndCull(); // 기존 막다른 방을 끝 지점으로 교체 시도
        }


        if (!endRoomSuccess) // 최종 실패 시, 로그 출력
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


        // 맵 생성이 끝난 후, 짝을 찾지 못해 남겨진 모든 소켓 폐쇄 처리
        foreach (TileSocket remainingSocket in _globalOpenSockets) // foreach 사용 이유: _globalOpenSockets 리스트에 남아 있는 모든 소켓을 순회하며 폐쇄 처리해야 하므로, 인덱스 기반 for문보다 간결하고 안전하게 모든 요소를 처리할 수 있는 foreach가 적합
        {
            if (remainingSocket != null)
            {
                remainingSocket.SetPassageState(false); // 남은 소켓을 모두 닫음
            }
        }

        Debug.Log($"[TileGenerator] 남은 외부 연결 통로 {_globalOpenSockets.Count}개를 모두 안전하게 폐쇄했습니다.");
    }


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

        // 4. 물리 검사 및 최종 배치 처리 수행
        return ProcessRoomPlacement(endRoom, socketA, true);
    }


    // ============================== 물리 검사 및 배치 ==============================
    private bool ProcessRoomPlacement(TileController nextRoom, TileSocket socketA, bool isEndRoom = false)
    {
        foreach (TileSocket socketB in nextRoom.OpenSockets)
        {
            // 1. 해당 문(socketB)을 기준으로 타일 정렬 및 회전
            TilePhysics.AlignRoom(nextRoom, socketA, socketB);

            // 2. 물리 겹침 검사
            if (TilePhysics.IsRoomOverlapping(nextRoom, _tileLayerMask))
            {
                // 겹치면 부수지 말고, 다음 문(socketB)으로 회전시켜서 다시 시도
                continue;
            }

            // 3. 조기 끊김 검사 (끝 지점은 예외)
            if (!isEndRoom && IsMapCutOffPrematurely(nextRoom, socketB))
            {
                continue; // 맵이 끊길 위험이 있으면 다음 문으로 회전
            }

            // 4. 모든 검사를 통과했다면 최종 배치 처리 후 성공(true) 반환
            FinalizeRoomPlacement(nextRoom, socketA, socketB);
            return true;
        }

        // 방의 모든 문을 다 돌려가며 껴봐도 전부 겹치면, 스폰 실패 처리
        HandleSpawnFailure(nextRoom.gameObject);
        return false;
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



    // ============================== 사후 처리 및 소켓 관리 ==============================
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
    
    
    private void HandleSpawnFailure(GameObject roomObj)
    {
        // 겹침 판정 시 임시 스폰된 방을 즉시 파괴하여 흔적을 지움
        // Destroy를 쓰는게 맞을까? ObjectPool을 쓸수도 있지 않은 지 생각해보자 
        Destroy(roomObj);
    }


    // ============================== 예외 처리 및 복구 ==============================
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

        // 후보 방들을 순회하며 막다른 방을 찾아 끝 지점으로 교체
        foreach (TileController targetRoom in candidateRooms)
        {
            if (targetRoom == null) continue;

            // 1. 연결된 소켓이 딱 1개뿐인 진짜 막다른 방인지 확인
            List<TileSocket> connectedSockets = new List<TileSocket>();
            foreach (TileSocket socket in targetRoom.OpenSockets)
            {
                if (socket.IsConnected)
                {
                    connectedSockets.Add(socket);
                }
            }

            if (connectedSockets.Count != 1) continue; // 막다른 방이 아니라면 패스

            TileSocket targetSocket = connectedSockets[0];

            // 2. targetRoom을 잠시 숨겨서 물리 충돌을 막고, 이 방이 본토와 연결되어 있던 부모 소켓을 찾음
            targetRoom.gameObject.SetActive(false);
            TileSocket parentSocket = FindConnectedParentSocket(targetSocket, allRooms, targetRoom);

            if (parentSocket == null)
            {
                targetRoom.gameObject.SetActive(true);
                continue;
            }

            parentSocket.IsConnected = false;

            // 3. 비워진 그 자리에 끝 지점 방 배치를 시도
            GameObject endRoomObj = GameObjectManager.Instance.SpawnObject(_endRoomPrefab, Vector3.zero, Quaternion.identity);
            TileController endRoom = endRoomObj.GetComponent<TileController>();

            bool placementSuccess = (endRoom != null && ProcessRoomPlacement(endRoom, parentSocket, true));

            if (placementSuccess)
            {
                Debug.Log("[SUCCESS] 최후의 보루: 막다른 일반 방을 끝 지점으로 안전하게 교체했습니다!");

                // 4. 배치가 성공했으므로 기존 막다른 방은 영구 파괴
                _globalOpenSockets.RemoveAll(socket => targetRoom.OpenSockets.Contains(socket));
                Destroy(targetRoom.gameObject); // 역시나 ObjectPool을 쓸 수도 있지 않은 지 생각해보자

                // 끝 지점의 남은 열린 소켓들을 글로벌 리스트에 안전하게 추가
                foreach (TileSocket socket in endRoom.OpenSockets)
                {
                    if (!socket.IsConnected) _globalOpenSockets.Add(socket);
                }

                // 기존 방 1개가 사라지고 끝 방 1개가 들어왔으므로, GenerateMapAsync의 바깥쪽 카운트 증가와 맞추기 위해 미리 1을 빼둠
                _currentRoomCount--;

                return true;
            }
            else
            {
                // 5. 실패했다면 원상 복구
                if (endRoomObj != null) Destroy(endRoomObj);
                parentSocket.IsConnected = true;
                targetRoom.gameObject.SetActive(true);
            }
        }

        return false;
    }


    private TileSocket FindConnectedParentSocket(TileSocket myConnectedSocket, TileController[] allRooms, TileController myRoom)
    {
        if (myConnectedSocket == null || myConnectedSocket.ConnectionPoint == null) return null;

        float threshold = 0.2f; // 문과 문이 맞닿아 있는 표준 오차 범위
        Transform myPoint = myConnectedSocket.ConnectionPoint;

        foreach (TileController room in allRooms)
        {
            if (room == null || room == myRoom) continue;
            // 만약 대상 방이 현재 숨겨져(SetActive(false)) 있다면 탐색에서 제외
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
}