using System.Collections.Generic;
using UnityEngine;

public class WeaponCustomViewModel : ViewModelBase
{
    public int MaxInventorySlot = 30;
    public int MaxStashSlot = 30;

    public StashItemSlotViewModel[] InventorySlots { get; set; }
    public StashItemSlotViewModel[] StashSlots { get; set; }
    public List<WeaponPartsSlotViewModel> PartsSlotList { get; } = new List<WeaponPartsSlotViewModel>();

    public WeaponCustomViewModel()
    {
        InventorySlots = new StashItemSlotViewModel[MaxInventorySlot];
        for (int i = 0; i < MaxInventorySlot; i++)
        {
            InventorySlots[i] = new StashItemSlotViewModel { SlotIndex = i, SlotType = ShopItemSlotType.Inventory, IsSlotEmpty = true };
        }

        StashSlots = new StashItemSlotViewModel[MaxStashSlot];
        for (int i = 0; i < MaxStashSlot; i++)
        {
            StashSlots[i] = new StashItemSlotViewModel { SlotIndex = i, SlotType = ShopItemSlotType.Stash, IsSlotEmpty = true };
        }
    }

    public void InvokeOnceOnInit()
    {
        OnPropertyChanged(nameof(TargetWeaponUniqueId));
        OnPropertyChanged(nameof(TargetWeaponDataId));
        OnPropertyChanged(nameof(HoveredItemId));
    }

    private string _targetWeaponUniqueId;
    public string TargetWeaponUniqueId
    {
        get => _targetWeaponUniqueId;
        set
        {
            if (_targetWeaponUniqueId != value)
            {
                _targetWeaponUniqueId = value;
                OnPropertyChanged(nameof(TargetWeaponUniqueId));
            }
        }
    }

    private string _targetWeaponDataId;
    public string TargetWeaponDataId
    {
        get => _targetWeaponDataId;
        set
        {
            if (_targetWeaponDataId != value)
            {
                _targetWeaponDataId = value;
                OnPropertyChanged(nameof(TargetWeaponDataId));
            }
        }
    }

    private string _hoveredItemId;
    public string HoveredItemId
    {
        get => _hoveredItemId;
        set
        {
            if (_hoveredItemId != value)
            {
                _hoveredItemId = value;
                OnPropertyChanged(nameof(HoveredItemId));
            }
        }
    }

    public void OnSlotPointerEnter(string itemDataId)
    {
        HoveredItemId = itemDataId;
    }

    public void OnSlotPointerExit()
    {
        HoveredItemId = null;
    }
}
