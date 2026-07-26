using UnityEngine;
using System.Collections.Generic;

public class WeaponPartsSlotViewModel : ViewModelBase
{
    public int SlotIndex { get; set; }
    public WeaponPartsType RequiredPartsType { get; set; }

    public void InvokeOnceOnInit()
    {
        OnPropertyChanged(nameof(ItemUniqueId));
        OnPropertyChanged(nameof(ItemDataId));
        OnPropertyChanged(nameof(IsSlotEmpty));
    }

    private string _itemUniqueId;
    public string ItemUniqueId
    {
        get => _itemUniqueId;
        set
        {
            if (_itemUniqueId != value)
            {
                _itemUniqueId = value;
                OnPropertyChanged(nameof(ItemUniqueId));
            }
        }
    }

    private string _itemDataId;
    public string ItemDataId
    {
        get => _itemDataId;
        set
        {
            if (_itemDataId != value)
            {
                _itemDataId = value;
                OnPropertyChanged(nameof(ItemDataId));
            }
        }
    }

    private bool _isSlotEmpty = true;
    public bool IsSlotEmpty
    {
        get => _isSlotEmpty;
        set
        {
            if (_isSlotEmpty != value)
            {
                _isSlotEmpty = value;
                OnPropertyChanged(nameof(IsSlotEmpty));
            }
        }
    }
}
