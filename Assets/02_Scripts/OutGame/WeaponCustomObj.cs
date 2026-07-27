using UnityEngine;

public class WeaponCustomObj : MonoBehaviour, ILobbyInteractable
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var inputHandler = other.GetComponent<PlayerInputHandler>();
            Debug.Log("WeaponCustomObj: 플레이어가 무기 개조대 범위에 진입했습니다.");

            Lobby.Instance.SetInteractableTarget(this, inputHandler);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("WeaponCustomObj: 플레이어가 무기 개조대 범위를 벗어났습니다.");

            Lobby.Instance.ClearInteractableTarget();
        }
    }

    public void OnInteract()
    {
        NetworkManager.Inst.WeaponCustomService.InitInventoryAndStashData();

        UIManager.Instance.OpenContentUI(UIType.WeaponCustomUI);
        var customUI = UIManager.Instance.GetOpenedUI(UIRootType.ContentUI, UIType.WeaponCustomUI);

        if (customUI != null)
        {
            Debug.Log("WeaponCustomObj: 무기 커스텀 UI를 열었습니다.");
        }
    }

    public void OnCancel()
    {
        NetworkManager.Inst.WeaponCustomService.RestoreTargetWeaponToInventory();
        NetworkManager.Inst.WeaponCustomService.SyncDataOnClose();

        UIManager.Instance.CloseUI(UIRootType.ContentUI, UIType.WeaponCustomUI); 

        Debug.Log("WeaponCustomObj: 무기 커스텀 UI를 닫았습니다.");
    }
}
