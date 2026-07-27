using System.Collections.Generic;
using UnityEngine;

public class NetworkWeaponCustomService
{
    private WeaponCustomViewModel _viewModel;
    private ItemModel _cachedTargetWeaponModel;

    public WeaponCustomViewModel GetWeaponCustomViewModel()
    {
        if (_viewModel == null)
        {
            _viewModel = new WeaponCustomViewModel();
        }
        return _viewModel;
    }

    public void InitInventoryAndStashData()
    {
        var vm = GetWeaponCustomViewModel();
        PlayerModel playerData = PlayerStatus.Instance.Model;

        foreach (var slotVm in vm.StashSlots)
        {
            slotVm.IsSlotEmpty = true;
            slotVm.ItemUniqueId = string.Empty;
            slotVm.ItemDataId = string.Empty;
            slotVm.ItemStackCount = 0;
        }

        if (playerData.StashItems != null)
        {
            for (int i = 0; i < playerData.StashItems.Count; i++)
            {
                if (i >= vm.MaxStashSlot) break;
                var savedItem = playerData.StashItems[i];
                var targetSlot = vm.StashSlots[i];

                targetSlot.ItemUniqueId = savedItem.InstanceId;
                targetSlot.ItemDataId = savedItem.ItemId;
                targetSlot.ItemStackCount = savedItem.CurrentStackCount;
                targetSlot.IsSlotEmpty = false;
            }
        }
    }

    public void SyncDataOnClose()
    {
        var vm = GetWeaponCustomViewModel();
        if (PlayerStatus.Instance == null || PlayerStatus.Instance.Model == null) return;

        PlayerModel playerData = PlayerStatus.Instance.Model;
        List<ItemModel> currentStash = playerData.StashItems;
        if (currentStash == null) currentStash = new List<ItemModel>();

        List<ItemModel> newStash = new List<ItemModel>();

        foreach (var slotVm in vm.StashSlots)
        {
            if (!slotVm.IsSlotEmpty)
            {
                ItemModel originalItem = null;
                foreach (var stashItem in currentStash)
                {
                    if (!string.IsNullOrEmpty(slotVm.ItemUniqueId) && stashItem.InstanceId == slotVm.ItemUniqueId)
                    {
                        originalItem = stashItem;
                        break;
                    }
                    else if (string.IsNullOrEmpty(slotVm.ItemUniqueId) && stashItem.ItemId == slotVm.ItemDataId)
                    {
                        originalItem = stashItem;
                        break;
                    }
                }

                if (originalItem != null)
                {
                    originalItem.CurrentStackCount = slotVm.ItemStackCount;
                    newStash.Add(originalItem);
                    currentStash.Remove(originalItem);
                }
                else
                {
                    newStash.Add(new ItemModel
                    {
                        InstanceId = slotVm.ItemUniqueId,
                        ItemId = slotVm.ItemDataId,
                        CurrentStackCount = slotVm.ItemStackCount
                    });
                }
            }
        }

        playerData.StashItems = newStash;
        if (SaveManager.Instance != null) SaveManager.Instance.SavePlayerData(playerData);
    }

    public void SetTargetWeapon(ItemModel weaponModel)
    {
        if (weaponModel == null) return;

        RestoreTargetWeaponToInventory();

        var vm = GetWeaponCustomViewModel();
        _cachedTargetWeaponModel = weaponModel;
        vm.TargetWeaponUniqueId = weaponModel.InstanceId;
        vm.TargetWeaponDataId = weaponModel.ItemId;

        RefreshPartsSlots();
    }

    public void RestoreTargetWeaponToInventory()
    {
        if (_cachedTargetWeaponModel != null && InventoryManager.Instance != null)
        {
            if (_cachedTargetWeaponModel is WeaponModel weapon)
            {
                InventoryManager.Instance.TryAddWeapon(weapon);
            }
            else
            {
                var itemData = DataManager.Instance.GetItemData(_cachedTargetWeaponModel.ItemId);
                InventoryManager.Instance.TryAddItem(itemData, _cachedTargetWeaponModel.CurrentStackCount);
            }
        }
        ClearTargetWeapon();
    }

    public void ClearTargetWeapon()
    {
        var vm = GetWeaponCustomViewModel();
        _cachedTargetWeaponModel = null;
        vm.TargetWeaponUniqueId = string.Empty;
        vm.TargetWeaponDataId = string.Empty;
        vm.PartsSlotList.Clear();
    }

    public void RefreshPartsSlots()
    {
        var vm = GetWeaponCustomViewModel();
        vm.PartsSlotList.Clear();

        if (_cachedTargetWeaponModel == null) return;

        WeaponModel targetWeapon = _cachedTargetWeaponModel as WeaponModel;

        if (targetWeapon == null)
        {
            var itemData = DataManager.Instance.GetItemData(_cachedTargetWeaponModel.ItemId);

            if (itemData != null && itemData.ItemType == "Weapon")
            {
                targetWeapon = new WeaponModel
                {
                    InstanceId = _cachedTargetWeaponModel.InstanceId,
                    ItemId = _cachedTargetWeaponModel.ItemId,
                    CurrentStackCount = _cachedTargetWeaponModel.CurrentStackCount,
                    CurrentDurability = 100f,
                    CurrentAmmo = 0,
                    AttachedParts = new List<ItemModel>()
                };
                _cachedTargetWeaponModel = targetWeapon;
                Debug.Log($"[{itemData.Name}] 아이템 모델을 무기 모델(WeaponModel)로 승급시켰습니다.");
            }
            else
            {
                Debug.LogWarning("무기 데이터가 아니므로 파츠 슬롯을 생성하지 않습니다.");
                return;
            }
        }

        WeaponPartsType[] defaultSlots = {
            WeaponPartsType.Muzzle, WeaponPartsType.Scope,
            WeaponPartsType.Magazine, WeaponPartsType.Grip, WeaponPartsType.Stock
        };

        for (int i = 0; i < defaultSlots.Length; i++)
        {
            WeaponPartsType partsType = defaultSlots[i];
            var slotVm = new WeaponPartsSlotViewModel
            {
                SlotIndex = i,
                RequiredPartsType = partsType,
                IsSlotEmpty = true
            };

            if (targetWeapon.AttachedParts != null)
            {
                foreach (var attachedPart in targetWeapon.AttachedParts)
                {
                    var partData = DataManager.Instance.GetItemData(attachedPart.ItemId) as WeaponPartsData;
                    if (partData != null && partData.PartsType == partsType)
                    {
                        slotVm.ItemUniqueId = attachedPart.InstanceId;
                        slotVm.ItemDataId = attachedPart.ItemId;
                        slotVm.IsSlotEmpty = false;
                        break;
                    }
                }
            }
            vm.PartsSlotList.Add(slotVm);
        }
        vm.InvokeOnceOnInit();
    }

    public ItemModel PickupItemSafely(string itemDataId, string uniqueId, ShopItemSlotType slotType)
    {
        var itemData = DataManager.Instance.GetItemData(itemDataId);
        if (itemData == null) return null;

        if (slotType == ShopItemSlotType.Inventory)
        {
            var items = PlayerStatus.Instance.Model.InventoryItems;
            for (int i = 0; i < items.Count; i++)
            {
                if ((items[i].InstanceId == uniqueId && !string.IsNullOrEmpty(uniqueId)) ||
                    (items[i].ItemId == itemDataId && string.IsNullOrEmpty(uniqueId)))
                {
                    var item = items[i];
                    items.RemoveAt(i);
                    return item;
                }
            }
        }
        else if (slotType == ShopItemSlotType.Stash)
        {
            var stashItems = PlayerStatus.Instance.Model.StashItems;
            if (stashItems != null)
            {
                for (int i = 0; i < stashItems.Count; i++)
                {
                    if (stashItems[i].InstanceId == uniqueId && stashItems[i].ItemId == itemDataId)
                    {
                        var item = stashItems[i];
                        stashItems.RemoveAt(i);
                        return item;
                    }
                }
            }
        }
        return null;
    }

    public void PlaceItemSafely(ItemModel itemModel, ShopItemSlotType targetSlotType)
    {
        if (itemModel == null) return;
        var itemData = DataManager.Instance.GetItemData(itemModel.ItemId);

        if (targetSlotType == ShopItemSlotType.Inventory)
        {
            var inventoryItems = PlayerStatus.Instance.Model.InventoryItems;

            if (itemData.ItemType == "Weapon")
            {
                inventoryItems.Add(itemModel);
            }
            else
            {
                bool merged = false;
                foreach (var invItem in inventoryItems)
                {
                    if (invItem.ItemId == itemModel.ItemId && invItem.CurrentStackCount < itemData.MaxStackCount)
                    {
                        int space = itemData.MaxStackCount - invItem.CurrentStackCount;
                        int addAmount = Mathf.Min(space, itemModel.CurrentStackCount);
                        invItem.CurrentStackCount += addAmount;
                        itemModel.CurrentStackCount -= addAmount;

                        if (itemModel.CurrentStackCount <= 0)
                        {
                            merged = true;
                            break;
                        }
                    }
                }
                if (!merged && itemModel.CurrentStackCount > 0)
                {
                    inventoryItems.Add(itemModel);
                }
            }
        }
        else if (targetSlotType == ShopItemSlotType.Stash)
        {
            var stashItems = PlayerStatus.Instance.Model.StashItems;
            if (stashItems == null) { stashItems = new List<ItemModel>(); PlayerStatus.Instance.Model.StashItems = stashItems; }

            if (itemData.ItemType == "Weapon")
            {
                stashItems.Add(itemModel);
            }
            else
            {
                bool merged = false;
                foreach (var stashItem in stashItems)
                {
                    if (stashItem.ItemId == itemModel.ItemId && stashItem.CurrentStackCount < itemData.MaxStackCount)
                    {
                        int space = itemData.MaxStackCount - stashItem.CurrentStackCount;
                        int addAmount = Mathf.Min(space, itemModel.CurrentStackCount);
                        stashItem.CurrentStackCount += addAmount;
                        itemModel.CurrentStackCount -= addAmount;

                        if (itemModel.CurrentStackCount <= 0)
                        {
                            merged = true;
                            break;
                        }
                    }
                }
                if (!merged && itemModel.CurrentStackCount > 0) stashItems.Add(itemModel);
            }
        }
    }

    public bool TryEquipPart(WeaponPartsSlotViewModel targetSlot, ItemModel partModel)
    {
        if (_cachedTargetWeaponModel == null || !(_cachedTargetWeaponModel is WeaponModel targetWeapon)) return false;

        var partData = DataManager.Instance.GetItemData(partModel.ItemId) as WeaponPartsData;
        if (partData == null || partData.PartsType != targetSlot.RequiredPartsType)
        {
            Debug.LogWarning("해당 슬롯에 장착할 수 없는 부위입니다.");
            return false;
        }

        if (targetWeapon.AttachedParts == null) targetWeapon.AttachedParts = new List<ItemModel>();

        if (!targetSlot.IsSlotEmpty) UnequipPart(targetSlot);

        targetWeapon.AttachedParts.Add(partModel);

        targetSlot.ItemUniqueId = partModel.InstanceId;
        targetSlot.ItemDataId = partModel.ItemId;
        targetSlot.IsSlotEmpty = false;

        return true;
    }

    public void UnequipPart(WeaponPartsSlotViewModel targetSlot)
    {
        if (targetSlot.IsSlotEmpty) return;
        if (_cachedTargetWeaponModel == null || !(_cachedTargetWeaponModel is WeaponModel targetWeapon)) return;

        ItemModel removedPart = null;
        for (int i = 0; i < targetWeapon.AttachedParts.Count; i++)
        {
            if (targetWeapon.AttachedParts[i].InstanceId == targetSlot.ItemUniqueId)
            {
                removedPart = targetWeapon.AttachedParts[i];
                targetWeapon.AttachedParts.RemoveAt(i);
                break;
            }
        }

        if (removedPart != null)
        {
            PlayerStatus.Instance.Model.InventoryItems.Add(removedPart);
        }

        targetSlot.ItemUniqueId = string.Empty;
        targetSlot.ItemDataId = string.Empty;
        targetSlot.IsSlotEmpty = true;
    }
}