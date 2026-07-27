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
            NetworkManager.Inst.TransferService.PlaceItemSafely(_cachedTargetWeaponModel, ShopItemSlotType.Inventory);
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
            NetworkManager.Inst.TransferService.PlaceItemSafely(removedPart, ShopItemSlotType.Inventory);
        }

        targetSlot.ItemUniqueId = string.Empty;
        targetSlot.ItemDataId = string.Empty;
        targetSlot.IsSlotEmpty = true;
    }
}