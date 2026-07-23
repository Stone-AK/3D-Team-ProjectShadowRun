using System;
using System.Collections.Generic;
using UnityEngine;

public class StashViewModel : ViewModelBase
{
    public int _maxStashSlot = 30;
    public int _maxInventorySlot = 30;

    public StashItemSlotViewModel[] StashSlots { get; set; }
    public StashItemSlotViewModel[] InventorySlots { get; set; }

    public StashViewModel()
    {
        StashSlots = new StashItemSlotViewModel[_maxStashSlot];
        for (int i = 0; i < _maxStashSlot; i++)
        {
            StashSlots[i] = new StashItemSlotViewModel
            {
                SlotIndex = i,
                SlotType = ShopItemSlotType.Stash, 
                IsSlotEmpty = true
            };
        }

        InventorySlots = new StashItemSlotViewModel[_maxInventorySlot];
        for (int i = 0; i < _maxInventorySlot; i++)
        {
            InventorySlots[i] = new StashItemSlotViewModel
            {
                SlotIndex = i,
                SlotType = ShopItemSlotType.Inventory, 
                IsSlotEmpty = true
            };
        }
    }

    public void InvokeOnceOnInit()
    {
        OnPropertyChanged(nameof(CurPlayerCredit));
        OnPropertyChanged(nameof(HoveredItemId));
    }

    public int CurPlayerCredit
    {
        get
        {
            return SaveManager.Instance.LoadPlayerData().CurrentCredit;
        }
        set
        {
            var playerData = SaveManager.Instance.LoadPlayerData();
            // 값이 달라졌다면 진짜 PlayerModel의 돈을 깎고 UI 새로고침을 알립니다.
            if (playerData.CurrentCredit != value)
            {
                playerData.CurrentCredit = value;
                SaveManager.Instance.SavePlayerData(playerData);
                OnPropertyChanged(nameof(CurPlayerCredit));
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
