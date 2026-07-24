using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("@GameManager");
                _instance = go.AddComponent<GameManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    public enum GameState { Core, OutGame, InGame }
    private GameState _currentGameState;
    public GameState CurrentGameState
    {
        get { return _currentGameState; }
        set { _currentGameState = value; }
    }

    [Header("작업 공간 프리팹 설정")]
    [SerializeField] private GameObject _outGameWorkspacePrefab;
    [SerializeField] private GameObject _inGameWorkspacePrefab;

    [Header("카메라 설정")]
    [SerializeField] private CameraBinder _playerCinemachineCamera;
    public CameraBinder PlayerCinemachineCamera => _playerCinemachineCamera;

    private GameObject _currentWorkspaceInstance;


    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(this.gameObject);

        InitializeGame();
    }

    private void InitializeGame()
    {
        _currentGameState = GameState.Core;
        Debug.Log("GameManager: 코어 시스템 초기화 완료. 아웃게임(로비) 프리팹을 생성합니다.");

        UIManager.Instance.OpenTitleUI();
        
    }

    public void ChangeState(GameState newState)
    {
        if (_currentGameState == newState && _currentWorkspaceInstance != null) return;

        _currentGameState = newState;

        if (_currentWorkspaceInstance != null)
        {
            Destroy(_currentWorkspaceInstance);
            _currentWorkspaceInstance = null;
        }

        switch (_currentGameState)
        {
            case GameState.OutGame:
                if (GameObjectManager.Instance != null)
                {
                    GameObjectManager.Instance.ClearAllSpawnedObjects();
                }

                if (_outGameWorkspacePrefab != null)
                {
                    _currentWorkspaceInstance = Instantiate(_outGameWorkspacePrefab);
                    _currentWorkspaceInstance.name = "[Workspace] OutGame_Lobby";
                    Debug.Log("GameManager: 아웃게임 프리팹 배치 완료.");

                    if (NetworkManager.Inst != null && NetworkManager.Inst.ShopService != null)
                    {
                        NetworkManager.Inst.ShopService.RefreshShopInventory();
                    }

                    LobbySpawnPos lobbyWorkspace = _currentWorkspaceInstance.GetComponent<LobbySpawnPos>();
                    if (lobbyWorkspace != null && lobbyWorkspace.LobbySpawnPoint != null)
                    {
                        SetupLobbyPositions(lobbyWorkspace.LobbySpawnPoint);
                    }
                }
                else
                {
                    Debug.LogError("GameManager: OutGame 프리팹이 인스펙터에 할당되지 않았습니다!");
                }
                break;

            case GameState.InGame:
                if (GameObjectManager.Instance != null)
                {
                    GameObjectManager.Instance.ClearAllSpawnedObjects();
                }

                if (_inGameWorkspacePrefab != null)
                {
                    _currentWorkspaceInstance = Instantiate(_inGameWorkspacePrefab);
                    _currentWorkspaceInstance.name = "[Workspace] InGame_Battle";
                    Debug.Log("GameManager: 인게임 프리팹(맵+플레이어) 배치 완료.");
                }
                else
                {
                    Debug.LogError("GameManager: InGame 프리팹이 인스펙터에 할당되지 않았습니다!");
                }
                break;
        }
    }

    public void ReturnToOutGame()
    {
        ChangeState(GameState.OutGame);
    }

    public void StartInGame()
    {
        ChangeState(GameState.InGame);
    }

    private void SetupLobbyPositions(Transform lobbySpawnTarget)
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            player.transform.position = lobbySpawnTarget.position;
            player.transform.rotation = lobbySpawnTarget.rotation;

            if (controller != null)
            {
                controller.enabled = true;
            }

            Debug.Log("GameManager: 플레이어 캐릭터를 LobbySpawnPos로 순간이동 시켰습니다.");
        }
    }
}