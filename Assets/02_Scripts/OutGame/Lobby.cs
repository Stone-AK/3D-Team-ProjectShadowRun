using UnityEngine;

public interface ILobbyInteractable
{
    void OnInteract();

    void OnCancel();
}

public class Lobby : MonoBehaviour
{
    public static Lobby Instance { get; private set; }

    private ILobbyInteractable _currentInteractableTarget;

    private bool _isUIOpen = false;
    private PlayerInputHandler _currentPlayerInput; 

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && _currentInteractableTarget != null)
        {
            if (!_isUIOpen)
            {
                OpenCurrentTargetUI();
            }
            else
            {
                CloseCurrentTargetUI();
            }
        }
        else if (Input.GetKeyDown(KeyCode.Escape) && _isUIOpen)
        {
            CloseCurrentTargetUI();
        }
    }

    public void SetInteractableTarget(ILobbyInteractable target, PlayerInputHandler playerInput)
    {
        _currentInteractableTarget = target;
        _currentPlayerInput = playerInput;
    }

    public void ClearInteractableTarget()
    {
        if (_isUIOpen) CloseCurrentTargetUI();

        _currentInteractableTarget = null;
        _currentPlayerInput = null;
    }

    private void OpenCurrentTargetUI()
    {
        _isUIOpen = true;
        SetPlayerControlState(true);

        _currentInteractableTarget.OnInteract();
    }

    public void CloseCurrentTargetUI()
    {
        if (!_isUIOpen) return;

        _isUIOpen = false;
        SetPlayerControlState(false); 

        if (_currentInteractableTarget != null)
        {
            _currentInteractableTarget.OnCancel(); 
        }
    }

    private void SetPlayerControlState(bool isUIOpened)
    {
        if (_currentPlayerInput != null)
        {
            _currentPlayerInput.SetGameplayInputBlocked(isUIOpened);
        }

        Cursor.visible = isUIOpened;
        Cursor.lockState = isUIOpened ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
