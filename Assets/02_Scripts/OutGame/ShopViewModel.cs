using System.Collections.Generic;
using UnityEngine;

public class ShopViewModel : ViewModelBase
{
    public List<ShopItemSlotViewModel> ShopItemSlotList { get; } = new List<ShopItemSlotViewModel>();
    public List<ShopItemSlotViewModel> InventoryItemSlotList { get; } = new List<ShopItemSlotViewModel>();
    public List<ShopItemSlotViewModel> StashItemSlotList { get; } = new List<ShopItemSlotViewModel>();

    public ShopViewModel() //임시로 구현. 추후 리스트의 길이를 읽어오는 등의 처리로 바꿀 것. NetWorkService 쪽으로 빼면 될까.
    {
        for (int i = 0; i < 10; i++) 
        { 
            ShopItemSlotList.Add(new ShopItemSlotViewModel { SlotIndex = i, SlotType = ShopItemSlotType.Shop });
        }

        for (int i = 0; i < 30; i++)
        {
            InventoryItemSlotList.Add(new ShopItemSlotViewModel { SlotIndex = i, SlotType = ShopItemSlotType.Inventory });
            StashItemSlotList.Add(new ShopItemSlotViewModel { SlotIndex = i, SlotType = ShopItemSlotType.Stash });
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
