using UnityEngine;

public class ShopObj : MonoBehaviour
{
    private PlayerInputHandler InputHandler;

    private KeyCode _interactKey = KeyCode.E;

    private bool _isPlayerInside = false;
    //private PlayerController _playerController; 나중에 플레이어 컨트롤러가 추가되면 주석해제. 플레이어가 상점UI를 열고있을 때 플레이어의 움직임을 막기 위함.

    private void Update()
    {
        if (_isPlayerInside && Input.GetKeyDown(_interactKey))
        {
            if (UIManager.Instance.IsUIOpened(UIType.ShopUI))
            {
                CloseShop();
                return; 
            }

            OpenShop();
            Debug.Log("ShopObj: 상점 UI를 열었습니다.");
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if(UIManager.Instance.IsUIOpened(UIType.ShopUI))
            {
                CloseShop();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerInside = true;
            InputHandler = other.GetComponent<PlayerInputHandler>();
            Debug.Log("ShopObj: 플레이어가 상점 범위에 진입했습니다.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 플레이어가 영역 밖으로 나가면 상점 닫기 및 감지 해제
        if (other.CompareTag("Player"))
        {
            _isPlayerInside = false;
            Debug.Log("ShopObj: 플레이어가 상점 범위를 벗어났습니다.");

            CloseShop();
        }
    }

    private void OpenShop()
    {
        UIManager.Instance.OpenContentUI(UIType.ShopUI);
        var shopUI = UIManager.Instance.GetOpenedUI(UIRootType.ContentUI, UIType.ShopUI) as ShopUI;

        if (shopUI != null)
        {
            NetworkManager.Inst.ShopService.SyncPlayerInventoryToShop();

            shopUI.BindViewModel(NetworkManager.Inst.ShopService.GetShopViewModel());
            Debug.Log("ViewModel 바인딩 성공!");

            InputHandler.SetGameplayInputBlocked(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Debug.LogError("shopUI를 가져오지 못했습니다!");
        }
    }

    private void CloseShop()
    {
        NetworkManager.Inst.ShopService.SyncDataOnClose();
        UIManager.Instance.CloseUI(UIRootType.ContentUI, UIType.ShopUI);

        InputHandler.SetGameplayInputBlocked(false);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
