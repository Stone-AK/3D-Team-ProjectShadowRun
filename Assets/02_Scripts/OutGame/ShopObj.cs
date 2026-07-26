using UnityEngine;

public class ShopObj : MonoBehaviour, ILobbyInteractable
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var inputHandler = other.GetComponent<PlayerInputHandler>();
            Debug.Log("ShopObj: 플레이어가 상점 범위에 진입했습니다.");

            Lobby.Instance.SetInteractableTarget(this, inputHandler);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("ShopObj: 플레이어가 상점 범위를 벗어났습니다.");

            Lobby.Instance.ClearInteractableTarget();
        }
    }

    public void OnInteract()
    {
        UIManager.Instance.OpenContentUI(UIType.ShopUI);
        var shopUI = UIManager.Instance.GetOpenedUI(UIRootType.ContentUI, UIType.ShopUI) as ShopUI;

        if (shopUI != null)
        {
            NetworkManager.Inst.ShopService.SyncPlayerInventoryToShop();
            shopUI.BindViewModel(NetworkManager.Inst.ShopService.GetShopViewModel());
            Debug.Log("ShopObj: 상점 UI를 열었습니다.");
        }
    }

    public void OnCancel()
    {
        NetworkManager.Inst.ShopService.SyncDataOnClose();
        UIManager.Instance.CloseUI(UIRootType.ContentUI, UIType.ShopUI);
        Debug.Log("ShopObj: 상점 UI를 닫았습니다.");
    }
}
