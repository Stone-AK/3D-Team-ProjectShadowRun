using UnityEngine;

public class TileSocket : MonoBehaviour // 방 프리팹 안의 소켓(문) 오브젝트에 붙는 스크립트, 문 파츠를 관리하고 외부에서 접근할 수 있도록 제공
{
    [Header("Passage Settings")]
    [SerializeField] private GameObject _openPart;
    [SerializeField] private GameObject _closedPart;
    [SerializeField] private bool _isInitiallyOpen = true;

    [Header("Connection Point")]
    [SerializeField] private Transform _connectionPoint; // 진짜 위치/회전 기준점

    // 외부에서 진짜 기준점을 가져갈 수 있도록 프로퍼티 개방 (할당 안 됐을 땐 자기 자신 반환)
    public Transform ConnectionPoint => _connectionPoint != null ? _connectionPoint : transform;

    private TileController _tileController;
    private bool _isConnected;

    public TileController Tile => _tileController;

    public bool IsConnected
    {
        get => _isConnected;
        set => _isConnected = value;
    }


    private void Awake()
    {
        AutoFindPassageParts();

        SetPassageState(_isInitiallyOpen);
    }


    public void SetPassageState(bool isOpen) // 문 파츠의 상태를 바꿔주는 함수
    {
        // [수정] 둘 다 없으면 경고하지 않고, 있는 파츠만 찾아서 상태를 바꿔줍니다.
        if (_openPart != null)
        {
            _openPart.SetActive(isOpen);
        }

        if (_closedPart != null)
        {
            _closedPart.SetActive(!isOpen);
        }
    }

    public void InitializeSocket(TileController owner) // 소켓을 소유한 타일 컨트롤러를 초기화
    {
        _tileController = owner;
    }



    private void AutoFindPassageParts() // 에디터 상에서 Open/Closed 파츠를 자동으로 찾아 할당
    {
        // 이미 할당되어 있다면 연산을 생략
        if (_openPart != null && _closedPart != null) return;

        // GetComponentsInChildren(true)를 사용해 비활성화된(숨겨진) 자식 오브젝트까지 전부 확안
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);

        foreach (Transform child in allChildren)
        {
            if (child == this.transform) continue;

            string childNameLower = child.name.ToLower();

            // Open 파츠 찾기
            if (_openPart == null && childNameLower == "open") {}

            // Closed 파츠 찾기
            if (_closedPart == null && childNameLower == "close") {}

            // 진짜 기준점 찾기
            if (_connectionPoint == null && childNameLower == "socket")
            {
                _connectionPoint = child;

#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }

        if (_openPart == null)
        {
            Debug.LogWarning($"[경고] {gameObject.name}의 하위에서 문 파츠를 자동으로 찾지 못했습니다");
        }
    }


#if UNITY_EDITOR
    private void OnDrawGizmos() // 에디터 상에서 소켓의 위치와 바라보는 방향을 시각적으로 표시
    {
        // 소켓의 위치와 바라보는 방향(Forward)을 노란색 선으로 그려줍니다.
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 0.2f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);
    }
#endif


#if UNITY_EDITOR
    private void OnValidate() // 에디터 상에서 체크 끄고 켜서 상태를 바꿀 수 있도록 함
    {
        AutoFindPassageParts();

        if (_openPart != null && _closedPart != null)
        {
            SetPassageState(_isInitiallyOpen);
        }
    }
#endif
}
