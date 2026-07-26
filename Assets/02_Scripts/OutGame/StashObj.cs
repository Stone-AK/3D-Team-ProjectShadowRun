using UnityEngine;

public class StashObj : MonoBehaviour, ILobbyInteractable
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var inputHandler = other.GetComponent<PlayerInputHandler>();
            Debug.Log("StashObj: 플레이어가 창고 범위에 진입했습니다.");

            Lobby.Instance.SetInteractableTarget(this, inputHandler);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 플레이어가 영역 밖으로 나가면 상점 닫기 및 감지 해제
        if (other.CompareTag("Player"))
        {
            Debug.Log("StashObj: 플레이어가 창고 범위를 벗어났습니다.");

            Lobby.Instance.ClearInteractableTarget();
        }
    }

    public void OnInteract()
    {
        NetworkManager.Inst.StashService.InitStashAndInventoryData();

        UIManager.Instance.OpenContentUI(UIType.StashUI);
        var stashUI = UIManager.Instance.GetOpenedUI(UIRootType.ContentUI, UIType.StashUI) as StashUI;

        if (stashUI != null)
        {
            Debug.Log("StashObj: 창고 UI를 열었습니다. ViewModel 바인딩 성공!");
        }
        else
        {
            Debug.LogError("stashUI를 가져오지 못했습니다!");
        }
    }

    public void OnCancel()
    {
        NetworkManager.Inst.StashService.SyncDataOnClose();
        UIManager.Instance.CloseUI(UIRootType.ContentUI, UIType.StashUI);

        Debug.Log("StashObj: 창고 UI를 닫았습니다.");
    }
}
