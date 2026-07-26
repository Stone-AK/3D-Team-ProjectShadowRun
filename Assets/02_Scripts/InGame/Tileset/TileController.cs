using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]

public class TileController : MonoBehaviour // 방 프리팹에 붙는 스크립트, 방 안의 소켓들을 관리하고 외부에서 접근할 수 있도록 제공
{

    private List<TileSocket> _openSockets = new List<TileSocket>();

    // 외부(TileGenerator 등)에서 안전하게 열려 있는 소켓 리스트에 접근할 수 있도록 프로퍼티 제공
    // 외부에서 함부로 요소를 바꾸지 못하도록 읽기 전용(IReadOnlyList)으로 제공
    public IReadOnlyList<TileSocket> OpenSockets => _openSockets;

    public BoxCollider TileCollider { get; private set; }


    private void Awake()
    {
        // 1. BoxCollider를 찾아서 프로퍼티에 저장
        InitializeCollider();
        // 2. 방 안의 소켓들을 열려있는 소켓 리스트에 추가
        InitializeSockets();
    }


    private void InitializeCollider() // BoxCollider를 찾아서 프로퍼티에 저장하는 메서드
    {
        TileCollider = GetComponent<BoxCollider>(); // BoxCollider를 찾아서 프로퍼티에 저장

        if (TileCollider == null)
        {
            Debug.LogError($"[ERROR] TileController: '{gameObject.name}' 오브젝트에 BoxCollider 컴포넌트가 누락되었습니다!");
        }
    }


    private void InitializeSockets() // 방 안의 소켓들을 열려있는 소켓 리스트에 추가하는 메서드
    {
        // 1. 자식 오브젝트들 중에서 TileSocket 스크립트가 붙은 모든 컴포넌트 수집
        // GetComponentsInChildren(false)를 사용하여 활성화되어 있는 소켓만 가져옴
        TileSocket[] sockets = GetComponentsInChildren<TileSocket>(false);

        if (sockets == null || sockets.Length == 0)
        {
            Debug.LogError($"[ERROR] TileController: '{gameObject.name}' 방 프리팹에 배치된 TileSocket 컴포넌트가 존재하지 않습니다. 프리팹 세팅을 확인하세요.");
            return;
        }

        // 2. 수집된 소켓들을 리스트에 안전하게 등록
        foreach (TileSocket socket in sockets)
        {
            _openSockets.Add(socket);
        }

        Debug.Log($"[SUCCESS] TileController: '{gameObject.name}' 방에서 {sockets.Length}개의 소켓 수집 완료.");
    }


    // 이미 수집해 둔 _openSockets 리스트의 첫 번째 요소를 프로퍼티나 함수로 반환
    public TileSocket GetFirstSocket()
    {
        if (_openSockets.Count > 0)
        {
            return _openSockets[0];
        }

        Debug.LogWarning($"[WARNING] TileController: '{gameObject.name}' 방에 남은 소켓이 존재하지 않습니다.");
        return null;
    }


#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (TileCollider != null)
        {
            // 90%로 설정한 박스 콜리더가 실제 월드에서 어떻게 그려지는지 빨간색 반투명 박스로 보여줍니다.
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(TileCollider.center, TileCollider.size);
        }
    }
#endif
}
