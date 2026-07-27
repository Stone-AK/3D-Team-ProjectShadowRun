using System.Collections.Generic;
using UnityEngine;

public class NetworkStashService
{
    private StashViewModel _stashViewModel;

    public StashViewModel GetStashViewModel()
    {
        if (_stashViewModel == null)
        {
            _stashViewModel = new StashViewModel();
        }
        return _stashViewModel;
    }

    public void InitStashAndInventoryData()
    {
        var stashVm = GetStashViewModel();
        PlayerModel playerData = PlayerStatus.Instance.Model;

        foreach (var slotVm in stashVm.StashSlots)
        {
            slotVm.IsSlotEmpty = true;
        }

        if (playerData.StashItems != null)
        {
            for (int i = 0; i < playerData.StashItems.Count; i++)
            {
                if (i >= stashVm._maxStashSlot) break;

                var savedItem = playerData.StashItems[i];
                var targetSlot = stashVm.StashSlots[i];

                targetSlot.ItemUniqueId = savedItem.InstanceId;
                targetSlot.ItemDataId = savedItem.ItemId;
                targetSlot.ItemStackCount = savedItem.CurrentStackCount;
                targetSlot.IsSlotEmpty = false;
            }
        }
    }

    public void SyncDataOnClose()
    {
        if (PlayerStatus.Instance == null || PlayerStatus.Instance.Model == null) return;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SavePlayerData(PlayerStatus.Instance.Model);
            Debug.Log("창고 데이터를 안전하게 저장했습니다.");
        }
    }
}