using System.Collections.Generic;
using UnityEngine;

public class NetworkItemTransferService
{
    public ItemModel PickupItemSafely(string uniqueId, string itemId, ShopItemSlotType slotType, int amount = -1)
    {
        var targetList = GetTargetList(slotType);
        if (targetList == null) return null;

        for (int i = 0; i < targetList.Count; i++)
        {
            var item = targetList[i];
            bool isMatch = !string.IsNullOrEmpty(uniqueId) ? (item.InstanceId == uniqueId) : (item.ItemId == itemId);

            if (isMatch)
            {
                if (item is WeaponModel || amount == -1 || amount >= item.CurrentStackCount)
                {
                    targetList.RemoveAt(i);
                    return item;
                }
                else
                {
                    item.CurrentStackCount -= amount;
                    return new ItemModel
                    {
                        InstanceId = System.Guid.NewGuid().ToString(),
                        ItemId = item.ItemId,
                        CurrentStackCount = amount
                    };
                }
            }
        }
        return null;
    }

    public int PlaceItemSafely(ItemModel itemModel, ShopItemSlotType targetSlotType)
    {
        if (itemModel == null || itemModel.CurrentStackCount <= 0)
        {
            return 0;
        }

        var targetList = GetTargetList(targetSlotType);
        if (targetList == null)
        {
            return itemModel.CurrentStackCount;
        }

        var itemData = DataManager.Instance.GetItemData(itemModel.ItemId);
        int maxSlots = 30; 

        if (itemModel is WeaponModel || itemData.ItemType == "Weapon" || itemData.MaxStackCount <= 1)
        {
            if (itemModel is WeaponModel)
            {
                if (targetList.Count < maxSlots)
                {
                    targetList.Add(itemModel);
                    return 0; 
                }
                return itemModel.CurrentStackCount;
            }
            else
            {
                while (itemModel.CurrentStackCount > 0 && targetList.Count < maxSlots)
                {
                    var splitItem = new ItemModel
                    {
                        InstanceId = System.Guid.NewGuid().ToString(),
                        ItemId = itemModel.ItemId,
                        CurrentStackCount = 1
                    };
                    targetList.Add(splitItem);
                    itemModel.CurrentStackCount -= 1;
                }
                return itemModel.CurrentStackCount;
            }
        }
        else
        {
            foreach (var existItem in targetList)
            {
                if (existItem.ItemId == itemModel.ItemId && existItem.CurrentStackCount < itemData.MaxStackCount)
                {
                    int space = itemData.MaxStackCount - existItem.CurrentStackCount;
                    int addAmount = Mathf.Min(space, itemModel.CurrentStackCount);

                    existItem.CurrentStackCount += addAmount;
                    itemModel.CurrentStackCount -= addAmount; 

                    if (itemModel.CurrentStackCount <= 0) break;
                }
            }

            while (itemModel.CurrentStackCount > 0 && targetList.Count < maxSlots)
            {
                int addAmount = Mathf.Min(itemData.MaxStackCount, itemModel.CurrentStackCount);

                var splitItem = new ItemModel
                {
                    InstanceId = System.Guid.NewGuid().ToString(),
                    ItemId = itemModel.ItemId,
                    CurrentStackCount = addAmount
                };
                targetList.Add(splitItem);
                itemModel.CurrentStackCount -= addAmount; 
            }
        }

        return itemModel.CurrentStackCount;
    }

    private List<ItemModel> GetTargetList(ShopItemSlotType slotType)
    {
        if (PlayerStatus.Instance == null || PlayerStatus.Instance.Model == null) return null;

        if (slotType == ShopItemSlotType.Inventory)
        {
            return PlayerStatus.Instance.Model.InventoryItems;
        }
        else if (slotType == ShopItemSlotType.Stash)
        {
            return PlayerStatus.Instance.Model.StashItems;
        }

        return null; 
    }
}
