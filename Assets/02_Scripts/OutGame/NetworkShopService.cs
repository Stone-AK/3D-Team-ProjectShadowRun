using System.Collections.Generic;
using UnityEngine;

public class NetworkShopService
{
    private ShopViewModel _shopViewModel;

    public ShopViewModel GetShopViewModel()
    {
        if (_shopViewModel == null)
        {
            _shopViewModel = new ShopViewModel();
        }

        return _shopViewModel;
    }

    public void RefreshShopInventory()
    {
        var vm = GetShopViewModel();
        int slotIndex = 0;

        foreach (var slot in vm.ShopItemSlotList) slot.IsSlotEmpty = true;

        List<string> fixedItemIds = new List<string> { "Item_Medical_Bandage", "Item_Food_Water", "Item_Ammo_556" };
        foreach (var fixedId in fixedItemIds)
        {
            if (slotIndex >= vm.ShopItemSlotList.Count)
            {
                break;
            }

            var itemData = DataManager.Instance.GetItemData(fixedId);
            if (itemData != null)
            {
                SetShopItemSlot(vm.ShopItemSlotList[slotIndex], fixedId, itemData.MaxStackCount);
                slotIndex++;
            }
        }

        List<string> randomCandidateIds = new List<string>();
        foreach (var item in DataManager.Instance._itemDataDic.Values)
        {
            if (fixedItemIds.Contains(item.Id))
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
            if (slotIndex >= vm.ShopItemSlotList.Count || addedRandomCount >= randomItemLimit)
            {
                break;
            }

            var itemData = DataManager.Instance.GetItemData(randomId);
            if (itemData != null)
            {
                SetShopItemSlot(vm.ShopItemSlotList[slotIndex], randomId, itemData.MaxStackCount);
                slotIndex++;
                addedRandomCount++;
            }
        }
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
        if (PlayerStatus.Instance == null || PlayerStatus.Instance.Model == null)
        {
            return;
        }

        PlayerStatus.Instance.Model.CurrentCredit = GetShopViewModel().CurPlayerCredit;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SavePlayerData(PlayerStatus.Instance.Model);
        }
    }

    private void SetShopItemSlot(ShopItemSlotViewModel slot, string dataId, int count)
    {
        var itemData = DataManager.Instance.GetItemData(dataId);
        slot.ItemUniqueId = string.Empty;
        slot.ItemDataId = itemData.Id;
        slot.ItemStackCount = count;
        slot.ItemSellingPrice = itemData.SellingPrice;
        slot.IsSlotEmpty = false;
    }

    private void LoadPlayerItemsToShopZone(List<ItemModel> savedItems, List<ShopItemSlotViewModel> targetSlots)
    {
        foreach (var slot in targetSlots) slot.IsSlotEmpty = true;

        for (int i = 0; i < savedItems.Count; i++)
        {
            if (i >= targetSlots.Count)
            {
                break;
            }

            var savedItem = savedItems[i];
            var itemData = DataManager.Instance.GetItemData(savedItem.ItemId);

            targetSlots[i].ItemUniqueId = savedItem.InstanceId;
            targetSlots[i].ItemDataId = savedItem.ItemId;
            targetSlots[i].ItemStackCount = savedItem.CurrentStackCount;
            targetSlots[i].ItemSellingPrice = itemData != null ? itemData.SellingPrice : 0;
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

    public int RequestBuyItem(string itemDataId, int requestCount, ShopItemSlotType targetZoneType)
    {
        if (requestCount <= 0)
        {
            return 0;
        }

        var itemData = DataManager.Instance.GetItemData(itemDataId);
        if (itemData == null)
        {
            return 0;
        }

        var vm = GetShopViewModel();
        int maxAffordable = (vm.CurPlayerCredit / itemData.SellingPrice);

        if (maxAffordable == 0)
        {
            Debug.LogWarning("크레딧이 부족합니다.");
            return 0;
        }

        int buyCount = Mathf.Min(requestCount, maxAffordable);
        int actualAddedCount = 0;

        if (itemData.ItemType == "Weapon" || itemData is WeaponData)
        {
            for (int i = 0; i < buyCount; i++)
            {
                WeaponModel newWeapon = new WeaponModel
                {
                    InstanceId = System.Guid.NewGuid().ToString(),
                    ItemId = itemDataId,
                    CurrentStackCount = 1,
                    AttachedParts = new List<ItemModel>()
                };

                int leftover = NetworkManager.Inst.TransferService.PlaceItemSafely(newWeapon, targetZoneType);
                if (leftover == 0)
                {
                    actualAddedCount++;
                }
                else
                {
                    break;
                }
            }
        }
        else
        {
            ItemModel newItem = new ItemModel
            {
                InstanceId = System.Guid.NewGuid().ToString(),
                ItemId = itemDataId,
                CurrentStackCount = buyCount
            };

            int leftover = NetworkManager.Inst.TransferService.PlaceItemSafely(newItem, targetZoneType);
            actualAddedCount = buyCount - leftover;
        }

        if (actualAddedCount > 0)
        {
            vm.CurPlayerCredit -= actualAddedCount * itemData.SellingPrice;
        }

        return actualAddedCount;
    }

    public void RequestSellItem(string itemDataId, int count)
    {
        if (count <= 0) return;
        var itemData = DataManager.Instance.GetItemData(itemDataId);
        if (itemData == null) return;

        GetShopViewModel().CurPlayerCredit += itemData.SellingPrice * count;
    }
}