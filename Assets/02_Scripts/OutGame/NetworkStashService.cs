using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public class NetworkStashService
{
    private StashViewModel _stashViewModel;

    public StashViewModel GetStashViewModel()
    {
        if (_stashViewModel == null)
        {
            CreateStashViewModel();
        }

        return _stashViewModel;
    }

    private StashViewModel CreateStashViewModel()
    {
        var stashVm = new StashViewModel();
        _stashViewModel = stashVm;
        return stashVm;
    }

    public void InitStashAndInventoryData()
    {
        var stashVm = GetStashViewModel();
        PlayerModel playerData = SaveManager.Instance.LoadPlayerData();

        // 1. 창고 슬롯 초기화
        foreach (var slotVm in stashVm.StashSlots)
        {
            slotVm.IsSlotEmpty = true;
        }

        // 2. 창고 데이터 로드 
        if (playerData.StashItems != null)
        {
            for (int i = 0; i < playerData.StashItems.Count; i++)
            {
                if (i >= stashVm._maxStashSlot)
                {
                    Debug.LogWarning("NetworkStashService: 보관함 최대 슬롯을 초과하는 데이터가 있습니다!");
                    break;
                }

                var savedItem = playerData.StashItems[i];
                var targetSlot = stashVm.StashSlots[i];

                targetSlot.ItemUniqueId = savedItem.InstanceId;
                targetSlot.ItemDataId = savedItem.ItemId;
                targetSlot.ItemStackCount = savedItem.CurrentStackCount;
                targetSlot.IsSlotEmpty = false;
            }

            /* 
            // TODO: (나중에 기능 고도화 시 사용) 저장된 위치(SlotIndex)대로 배치하는 로직 
            foreach (var savedItem in playerData.StashItems)
            {
                bool isPlaced = false;
                if (savedItem.SlotIndex >= 0 && savedItem.SlotIndex < stashVm._maxStashSlot)
                {
                    var targetSlot = stashVm.StashSlots[savedItem.SlotIndex];
                    if (targetSlot.IsSlotEmpty)
                    {
                        targetSlot.ItemUniqueId = savedItem.InstanceId;
                        targetSlot.ItemDataId = savedItem.ItemId;
                        targetSlot.ItemStackCount = savedItem.CurrentStackCount;
                        targetSlot.IsSlotEmpty = false;
                        isPlaced = true;
                    }
                }
                // 위치가 안 맞거나 겹치면 빈 칸 찾아 배치
                if (!isPlaced)
                {
                    foreach (var slot in stashVm.StashSlots)
                    {
                        if (slot.IsSlotEmpty)
                        {
                            slot.ItemUniqueId = savedItem.InstanceId;
                            slot.ItemDataId = savedItem.ItemId;
                            slot.ItemStackCount = savedItem.CurrentStackCount;
                            slot.IsSlotEmpty = false;
                            break;
                        }
                    }
                }
            }
            */
        }
    }

    public void SyncDataOnClose()
    {
        var stashVm = GetStashViewModel();
        PlayerModel playerData = SaveManager.Instance.LoadPlayerData();

        List<ItemModel> newStash = new List<ItemModel>();
        foreach (var slotVm in stashVm.StashSlots)
        {
            if (!slotVm.IsSlotEmpty)
            {
                newStash.Add(new ItemModel
                {
                    InstanceId = slotVm.ItemUniqueId,
                    ItemId = slotVm.ItemDataId,
                    CurrentStackCount = slotVm.ItemStackCount

                    // SlotIndex = slotVm.SlotIndex // TODO: 나중에 위치 저장 기능 필요 시 주석 해제
                });
            }
        }

        playerData.StashItems = newStash;
        SaveManager.Instance.SavePlayerData(playerData);

    }

    public void AddItem(string itemDataId, int addItemCount)
    {
        var stashVm = GetStashViewModel();
        string uniqueId = System.Guid.NewGuid().ToString();

        foreach (var slotVm in stashVm.StashSlots)
        {
            if (slotVm.IsSlotEmpty)
            {
                slotVm.ItemUniqueId = uniqueId;
                slotVm.ItemDataId = itemDataId;
                slotVm.ItemStackCount = addItemCount;
                slotVm.IsSlotEmpty = false;
                return;
            }
        }
    }

    private void RequestRemoveItem(string removeTargetUniqueId)
    {
        var stashVm = GetStashViewModel();

        foreach(var slotVm in stashVm.StashSlots)
        {
            if((slotVm.IsSlotEmpty == false) && (slotVm.ItemUniqueId == removeTargetUniqueId))
            {
                slotVm.ItemUniqueId = string.Empty;
                slotVm.ItemDataId = string.Empty;
                slotVm.ItemStackCount = 0;
                slotVm.IsSlotEmpty = true;

                return;
            }
        }
        //NetworkManager.Inst.SaveLoadService.RequstSaveData();
    }

}
