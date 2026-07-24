using System.Collections.Generic;
using UnityEngine;

public class NetworkShopService
{
    private ShopViewModel _shopViewModel;

    public ShopViewModel GetShopViewModel()
    {
        if (_shopViewModel == null)
        {
            CreateShopViewModel();
        }

        return _shopViewModel;
    }

    private ShopViewModel CreateShopViewModel()
    {
        var shopVm = new ShopViewModel();
        _shopViewModel = shopVm;
        return shopVm;
    }

    public void RefreshShopInventory()
    {
        var vm = GetShopViewModel();
        int slotIndex = 0;

        foreach (var slot in vm.ShopItemSlotList)
        {
            slot.IsSlotEmpty = true;
        }

        List<string> fixedItemIds = new List<string> { "Item_Medical_Bandage", "Item_Food_Water" };
        foreach (var fixedId in fixedItemIds)
        {
            if (slotIndex >= vm.ShopItemSlotList.Count)
            {
                break;
            }

            var itemData = DataManager.Instance.GetItemData(fixedId);
            if(itemData != null)
            {
                SetShopItemSlot(vm.ShopItemSlotList[slotIndex], fixedId, itemData.MaxStackCount);
                slotIndex++;
            }
        }

        List<string> randomCandidateIds = new List<string>();
        foreach (var item in DataManager.Instance._itemDataDic.Values)
        {
            if(fixedItemIds.Contains(item.Id))
            {
                continue;
            }

            if (item.ItemType != "Material")
            {
                randomCandidateIds.Add(item.Id);
            }
        }

        ShuffleList(randomCandidateIds);

        int randomItemLimit = 5;
        int addedRandomCount = 0;
        foreach (var randomId in randomCandidateIds)
        {
            if (slotIndex >= vm.ShopItemSlotList.Count || addedRandomCount >= randomItemLimit) break;

            var itemData = DataManager.Instance.GetItemData(randomId);
            if (itemData != null)
            {
                SetShopItemSlot(vm.ShopItemSlotList[slotIndex], randomId, itemData.MaxStackCount);
                slotIndex++;
                addedRandomCount++;
            }
        }

        Debug.Log("NetworkShopService: 상점 판매 목록이 새로고침 되었습니다.");
    }

    public void SyncPlayerInventoryToShop()
    {
        var vm = GetShopViewModel();
        PlayerModel activePlayerData = PlayerStatus.Instance.Model;

        vm.CurPlayerCredit = activePlayerData.CurrentCredit;

        var inventoryItems = InventoryManager.Instance.ItemList;
        LoadPlayerItemsToShopZone(new List<ItemModel>(inventoryItems), vm.InventoryItemSlotList);

        if (activePlayerData.StashItems == null) activePlayerData.StashItems = new List<ItemModel>();
        LoadPlayerItemsToShopZone(activePlayerData.StashItems, vm.StashItemSlotList);
    }

    // 리스트를 무작위로 섞어주는 유틸리티 메서드
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int rnd = UnityEngine.Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[rnd];
            list[rnd] = temp;
        }
    }

    public void SyncDataOnClose()
    {
        var vm = GetShopViewModel();

        if (PlayerStatus.Instance == null || PlayerStatus.Instance.Model == null)
        {
            return;
        }

        PlayerModel activePlayerData = PlayerStatus.Instance.Model;

        activePlayerData.CurrentCredit = vm.CurPlayerCredit;

        List<ItemModel> newStash = new List<ItemModel>();
        foreach (var slot in vm.StashItemSlotList)
        {
            if (!slot.IsSlotEmpty)
            {
                newStash.Add(new ItemModel
                {
                    InstanceId = slot.ItemUniqueId,
                    ItemId = slot.ItemDataId,
                    CurrentStackCount = slot.ItemStackCount
                });
            }
        }

        activePlayerData.StashItems = newStash;

        if (SaveManager.Instance == null) return;

        SaveManager.Instance.SavePlayerData(activePlayerData);
    }

    private void SetShopItemSlot(ShopItemSlotViewModel slot, string dataId, int count)
    {
        var itemData = DataManager.Instance.GetItemData(dataId);

        slot.ItemUniqueId = string.Empty;
        slot.ItemDataId = itemData.Id;
        slot.ItemStackCount = count;
        slot.ItemSellingPrice = itemData.SellingPrice; // 구매가
        slot.IsSlotEmpty = false;
    }

    private void LoadPlayerItemsToShopZone(List<ItemModel> savedItems, List<ShopItemSlotViewModel> targetSlots)
    {
        foreach (var slot in targetSlots) slot.IsSlotEmpty = true;

        for (int i = 0; i < savedItems.Count; i++)
        {
            if (i >= targetSlots.Count) break;

            var savedItem = savedItems[i];
            var itemData = DataManager.Instance.GetItemData(savedItem.ItemId);

            targetSlots[i].ItemUniqueId = savedItem.InstanceId; // 유저 아이템은 유니크 ID 유지
            targetSlots[i].ItemDataId = savedItem.ItemId;
            targetSlots[i].ItemStackCount = savedItem.CurrentStackCount;
            targetSlots[i].ItemSellingPrice = itemData != null ? itemData.SellingPrice : 0; // 판매가
            targetSlots[i].IsSlotEmpty = false;
        }
    }

    public void RefreshStashData()
    {
        var vm = GetShopViewModel();
        PlayerModel activePlayerData = PlayerStatus.Instance.Model;

        if (activePlayerData.StashItems == null)
        {
            activePlayerData.StashItems = new List<ItemModel>();
        }

        LoadPlayerItemsToShopZone(activePlayerData.StashItems, vm.StashItemSlotList);
    }

    public int RequestBuyItem(string itemDataId, int requestCount, ShopItemSlotType targetZoneType, ShopItemSlotViewModel targetSlot = null)
    {
        if (requestCount <= 0) return 0;

        var itemData = DataManager.Instance.GetItemData(itemDataId);
        if(itemData == null) return 0;

        var vm = GetShopViewModel();
        
        int maxAffordable = (vm.CurPlayerCredit / itemData.SellingPrice);

        if (maxAffordable == 0)
        {
            Debug.LogError("크레딧이 부족합니다.");
            return 0;
        }

        int buyCount = Mathf.Min(requestCount, maxAffordable);
        int actualAddedCount = 0;

        if (targetZoneType == ShopItemSlotType.Inventory)
        {
            int remain = InventoryManager.Instance.TryAddItem(itemData, buyCount);
            actualAddedCount = buyCount - remain;
        }
        else if (targetZoneType == ShopItemSlotType.Stash && targetSlot != null)
        {
            if (targetSlot.IsSlotEmpty)
            {
                targetSlot.ItemDataId = itemDataId;
                targetSlot.ItemUniqueId = System.Guid.NewGuid().ToString();
                targetSlot.ItemSellingPrice = itemData.SellingPrice;
                targetSlot.ItemStackCount = buyCount;
                targetSlot.IsSlotEmpty = false;
                actualAddedCount = buyCount;
            }
            else if (targetSlot.ItemDataId == itemDataId)
            {
                int maxCanAdd = itemData.MaxStackCount - targetSlot.ItemStackCount;
                actualAddedCount = Mathf.Min(buyCount, maxCanAdd);
                targetSlot.ItemStackCount += actualAddedCount;
            }
        }

        if (actualAddedCount > 0)
        {
            vm.CurPlayerCredit -= actualAddedCount * itemData.SellingPrice;
            Debug.Log($"구매 성공! {itemData.Name} {actualAddedCount}개 구매 완료.");
        }

        return actualAddedCount;
    }

    public void RequestSellItem(string itemDataId, int count)
    {
        if (count <= 0) return;

        var itemData = DataManager.Instance.GetItemData(itemDataId);
        if (itemData == null) return;

        var vm = GetShopViewModel();

        vm.CurPlayerCredit += itemData.SellingPrice * count;

        Debug.Log($"판매 성공! {itemData.Name} {count}개 판매 (획득: {itemData.SellingPrice * count} C)");
    }
}
