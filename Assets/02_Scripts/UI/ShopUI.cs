using NUnit.Framework.Interfaces;
using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum ShopItemSlotType
{
    Inventory,
    Stash,
    Shop
}

public class ShopUI : UIBase 
{
    [SerializeField] private TMP_Text Text_CurPlayerCredit;
    [SerializeField] private Button Button_CloseSelf;

    [SerializeField] private ShopItemSlotUI Prefab_ShopItemSlotUI;
    [SerializeField] private Transform Transform_ShopContent;
    [SerializeField] private Transform Transform_InventoryContent;
    [SerializeField] private Transform Transform_StashContent;

    [SerializeField] private ShopItemSlotUI DragSlotUI;

    private ShopItemSlotViewModel _originSlotVm;
    private ShopItemSlotViewModel _dragSlotVm;
    private int _heldStackCount = 0;

    private ShopViewModel _shopVm;

    private bool _isInitialized = false;

    private void OnEnable()
    {
        Button_CloseSelf.onClick.RemoveAllListeners();
        Button_CloseSelf.onClick.AddListener(OnClick_CloseButton);

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += RefreshInventoryUI;
        }
    }

    private void OnDisable()
    {
        if (_shopVm != null)
        {
            _shopVm.PropertyChanged -= OnPropertyChanged_View;
            InventoryManager.Instance.OnInventoryChanged -= RefreshInventoryUI;
        }
    }

    private void RefreshInventoryUI()
    {
        if (_shopVm == null || InventoryManager.Instance == null) return;

        var inventoryItems = InventoryManager.Instance.ItemList;
        var slotVms = _shopVm.InventoryItemSlotList;

        for (int i = 0; i < slotVms.Count; i++)
        {
            if (i < inventoryItems.Count)
            {
                var item = inventoryItems[i];
                var itemData = DataManager.Instance.GetItemData(item.ItemId);

                slotVms[i].ItemUniqueId = item.InstanceId;
                slotVms[i].ItemDataId = item.ItemId;
                slotVms[i].ItemStackCount = item.CurrentStackCount;
                slotVms[i].ItemSellingPrice = itemData != null ? itemData.SellingPrice : 0;
                slotVms[i].IsSlotEmpty = false;
            }
            else
            {
                ClearSlotData(slotVms[i]);
            }
        }
    }

    private void Update()
    {
        if (_dragSlotVm != null && !_dragSlotVm.IsSlotEmpty)
        {
            DragSlotUI.transform.position = Input.mousePosition;
        }
    }

    private void OnPropertyChanged_View(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ShopViewModel.CurPlayerCredit):
                {
                    Text_CurPlayerCredit.text = $"Player Credit : {_shopVm.CurPlayerCredit}";
                }
                break;
            case nameof(ShopViewModel.HoveredItemId):
                if (_shopVm.HoveredItemId != null)
                {
                    var popupUI = UIManager.Instance.OpenPopupUI(UIType.ShopItemPopupUI) as ShopItemPopupUI;

                    if (popupUI != null)
                    {
                        popupUI.SetItemData(_shopVm.HoveredItemId);
                    }
                }
                else
                {
                    UIManager.Instance.ClosePopupUI(UIType.ShopItemPopupUI);
                }
                break;
        }
    }

    public void BindViewModel(ShopViewModel vm)
    {
        if(_shopVm != null)
        {
            _shopVm.PropertyChanged -= OnPropertyChanged_View;
        }

        _shopVm = vm;
        _shopVm.PropertyChanged += OnPropertyChanged_View;

        if(_dragSlotVm == null)
        {
            _dragSlotVm = new ShopItemSlotViewModel { IsSlotEmpty = true };
            DragSlotUI.Bind(_dragSlotVm, null, null, null);
        }

        if (!_isInitialized)
        {
            InitSlotsZone(_shopVm.ShopItemSlotList, Transform_ShopContent);
            InitSlotsZone(_shopVm.InventoryItemSlotList, Transform_InventoryContent);
            InitSlotsZone(_shopVm.StashItemSlotList, Transform_StashContent);
            _isInitialized = true;
        }

        _shopVm.InvokeOnceOnInit();
    }

    private void InitSlotsZone(List<ShopItemSlotViewModel> slotVms, Transform parentContent)
    {
        foreach (var slotVm in slotVms)
        {
            ShopItemSlotUI slotUi = Instantiate(Prefab_ShopItemSlotUI, parentContent);
            slotUi.Bind(slotVm, _shopVm.OnSlotPointerEnter, _shopVm.OnSlotPointerExit, OnSlotClicked);
        }
    }

    private void OnClick_CloseButton()
    {
        CloseShopUI();
    }

    public void CloseShopUI()
    {
        NetworkManager.Inst.ShopService.SyncDataOnClose();
        UIManager.Instance.CloseContentUI(UIType.ShopUI);

        UIManager.Instance.ClosePopupUI(UIType.ShopItemPopupUI);
    }

    // ==========================================
    // 마우스 클릭 및 드래그 앤 드롭 로직
    // ==========================================
    private void OnSlotClicked(ShopItemSlotViewModel clickedSlotVm, PointerEventData.InputButton button)
    {
        if (button == PointerEventData.InputButton.Left)
        {
            HandleLeftClick(clickedSlotVm);
        }
        else if (button == PointerEventData.InputButton.Right)
        {
            HandleRightClick(clickedSlotVm);
        }

        if (_heldStackCount > 0)
        {
            DragSlotUI.UpdatePriceDisplay(_dragSlotVm.ItemSellingPrice, _heldStackCount);
        }
    }

    private void HandleLeftClick(ShopItemSlotViewModel clickedSlot)
    {
        bool isCtrlInput = ((Input.GetKey(KeyCode.LeftControl)) || (Input.GetKey(KeyCode.RightControl)));
        bool isShiftInput = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        bool isShopToShop = ((_heldStackCount > 0) && (_originSlotVm.SlotType == ShopItemSlotType.Shop) && (clickedSlot.SlotType == ShopItemSlotType.Shop));

        if (isShopToShop == true) 
        {
            bool isPickingUpMore = isCtrlInput && (clickedSlot.ItemDataId == _dragSlotVm.ItemDataId);

            if (isPickingUpMore == false)
            {
                RestoreItemToOrigin();
                return;
            }
        }

        if (_heldStackCount == 0)
        {
            if (isCtrlInput == true)
            {
                PickupOne(clickedSlot);
            }
            else if (isShiftInput)
            {
                PickupHalf(clickedSlot);
            }
            else
            {
                PickupAll(clickedSlot);
            }
        }
        else
        {
            if ((isCtrlInput == true) && (clickedSlot.ItemDataId == _dragSlotVm.ItemDataId))
            {
                PickupOne(clickedSlot);
            }
            else if (clickedSlot.IsSlotEmpty == true)
            {
                PlaceAll(clickedSlot);
            }
            else if (clickedSlot.ItemDataId == _dragSlotVm.ItemDataId)
            {
                MergeAll(clickedSlot);
            }
            else
            {
                SwapItems(clickedSlot);
            }
        }
    }

    private void HandleRightClick(ShopItemSlotViewModel clickedSlot)
    {
        bool isShopToShop = ((_heldStackCount > 0) && (_originSlotVm.SlotType == ShopItemSlotType.Shop) && (clickedSlot.SlotType == ShopItemSlotType.Shop));

        if (isShopToShop)
        {
            return;
        }

        if (_heldStackCount == 0)
        {
            return;
        }
        else
        {
            if ((clickedSlot.IsSlotEmpty == true) || (clickedSlot.ItemDataId == _dragSlotVm.ItemDataId))
            {
                PlaceOne(clickedSlot);
            }
        }
    }

    private void PickupOne(ShopItemSlotViewModel slotVm)
    {
        if (slotVm == null || slotVm.IsSlotEmpty)
        {
            return;
        }

        var itemData = DataManager.Instance.GetItemData(slotVm.ItemDataId);

        if (_heldStackCount >= itemData.MaxStackCount)
        {
            Debug.LogWarning($"최대 소지 개수({itemData.MaxStackCount}개)를 초과할 수 없습니다.");
            return;
        }

        string backupDataId = slotVm.ItemDataId;
        string backupUniqueId = slotVm.ItemUniqueId;
        int backupPrice = slotVm.ItemSellingPrice;

        if (slotVm.SlotType == ShopItemSlotType.Inventory)
        {
            if (InventoryManager.Instance.TryRemoveItem(backupDataId, 1) == false)
            {
                return;
            }
        }

        if (_heldStackCount == 0)
        {
            _originSlotVm = slotVm;
            DragSlotUI.gameObject.SetActive(true);
            _dragSlotVm.ItemDataId = backupDataId;
            _dragSlotVm.ItemUniqueId = backupUniqueId;
            _dragSlotVm.ItemSellingPrice = backupPrice;
            _dragSlotVm.IsSlotEmpty = false;
        }

        _heldStackCount++;
        _dragSlotVm.ItemStackCount = _heldStackCount;

        if ((slotVm.ItemStackCount != -1) && (slotVm.SlotType != ShopItemSlotType.Inventory))
        {
            slotVm.ItemStackCount--;

            if (slotVm.ItemStackCount == 0)
            {
                ClearSlotData(slotVm);
            }
        }
    }


    private void PickupHalf(ShopItemSlotViewModel slotVm)
    {
        if (slotVm == null || slotVm.IsSlotEmpty)
        {
            return;
        }

        string backupDataId = slotVm.ItemDataId;
        string backupUniqueId = slotVm.ItemUniqueId;
        int backupPrice = slotVm.ItemSellingPrice;

        int halfAmount = 0;

        if (slotVm.ItemStackCount == -1)
        {
            var itemData = DataManager.Instance.GetItemData(slotVm.ItemDataId);
            halfAmount = Mathf.CeilToInt(itemData.MaxStackCount / 2.0f);
        }
        else
        {
            halfAmount = Mathf.CeilToInt(slotVm.ItemStackCount / 2.0f);
        }

        if (slotVm.SlotType == ShopItemSlotType.Inventory)
        {
            if (InventoryManager.Instance.TryRemoveItem(backupDataId, halfAmount) == false)
            {
                return;
            }
        }

        _heldStackCount = halfAmount;
        _originSlotVm = slotVm;
        DragSlotUI.gameObject.SetActive(true);
        _dragSlotVm.ItemDataId = backupDataId;
        _dragSlotVm.ItemUniqueId = backupUniqueId;
        _dragSlotVm.ItemSellingPrice = backupPrice;
        _dragSlotVm.ItemStackCount = _heldStackCount;
        _dragSlotVm.IsSlotEmpty = false;

        if ((slotVm.ItemStackCount != -1) && (slotVm.SlotType != ShopItemSlotType.Inventory))
        {
            slotVm.ItemStackCount -= halfAmount;

            if (slotVm.ItemStackCount <= 0)
            {
                ClearSlotData(slotVm);
            }
        }
    }

    private void PickupAll(ShopItemSlotViewModel slotVm)
    {
        if (slotVm == null || slotVm.IsSlotEmpty)
        {
            return;
        }

        string backupDataId = slotVm.ItemDataId;
        string backupUniqueId = slotVm.ItemUniqueId;
        int backupPrice = slotVm.ItemSellingPrice;

        var itemData = DataManager.Instance.GetItemData(slotVm.ItemDataId);

        int pickupAmount = slotVm.ItemStackCount == -1 ? itemData.MaxStackCount : slotVm.ItemStackCount;

        if (pickupAmount > itemData.MaxStackCount)
        {
            pickupAmount = itemData.MaxStackCount;
        }

        if (slotVm.SlotType == ShopItemSlotType.Inventory)
        {
            if (InventoryManager.Instance.TryRemoveItem(backupDataId, pickupAmount) == false)
            {
                return;
            }
        }

        _heldStackCount = pickupAmount;
        _originSlotVm = slotVm;
        DragSlotUI.gameObject.SetActive(true);
        _dragSlotVm.ItemDataId = backupDataId;
        _dragSlotVm.ItemUniqueId = backupUniqueId;
        _dragSlotVm.ItemSellingPrice = backupPrice;
        _dragSlotVm.ItemStackCount = _heldStackCount;
        _dragSlotVm.IsSlotEmpty = false;

        if ((slotVm.ItemStackCount != -1) && (slotVm.SlotType != ShopItemSlotType.Inventory))
        {
            slotVm.ItemStackCount -= pickupAmount;

            if (slotVm.ItemStackCount <= 0)
            {
                ClearSlotData(slotVm);
            }
        }
    }

    private void DropAllIntoInventory()
    {
        var itemData = DataManager.Instance.GetItemData(_dragSlotVm.ItemDataId);

        if (_originSlotVm.SlotType == ShopItemSlotType.Shop)
        {
            int maxAffordable = _shopVm.CurPlayerCredit / itemData.SellingPrice;
            if (maxAffordable == 0)
            {
                Debug.LogWarning("크레딧이 부족합니다!");
                RestoreItemToOrigin(); return;
            }
            int tryCount = Mathf.Min(_heldStackCount, maxAffordable);
            int remain = InventoryManager.Instance.TryAddItem(itemData, tryCount);
            int added = tryCount - remain;

            _shopVm.CurPlayerCredit -= added * itemData.SellingPrice;
            _heldStackCount -= added;
        }
        else 
        {
            int remain = InventoryManager.Instance.TryAddItem(itemData, _heldStackCount);
            _heldStackCount = remain;
        }

        if (_heldStackCount > 0)
        {
            RestoreItemToOrigin(); // 인벤토리가 꽉 차서 남은 건 원위치
        }
        else
        {
            ClearCursorItem();
        }
    }

    private void PlaceAll(ShopItemSlotViewModel targetSlot)
    {
        var itemData = DataManager.Instance.GetItemData(_dragSlotVm.ItemDataId);

        // 타겟이 인벤토리인 경우
        if (targetSlot.SlotType == ShopItemSlotType.Inventory)
        {
            DropAllIntoInventory(); return;
        }

        // 타겟이 상점인 경우 (판매)
        if (targetSlot.SlotType == ShopItemSlotType.Shop)
        {
            if (_originSlotVm.SlotType != ShopItemSlotType.Shop)
            {
                _shopVm.CurPlayerCredit += itemData.SellingPrice * _heldStackCount;
            }
            ClearCursorItem(); return;
        }

        // 타겟이 창고인 경우: 상점에서 가져온 물건이면 돈 계산
        if (_originSlotVm.SlotType == ShopItemSlotType.Shop)
        {
            int maxAffordable = _shopVm.CurPlayerCredit / itemData.SellingPrice;

            if (maxAffordable == 0)
            {
                Debug.LogWarning("크레딧이 부족합니다!");
                RestoreItemToOrigin(); return;
            }

            int buyCount = Mathf.Min(_heldStackCount, maxAffordable);
            _shopVm.CurPlayerCredit -= buyCount * itemData.SellingPrice; 

            targetSlot.ItemDataId = _dragSlotVm.ItemDataId;
            targetSlot.ItemSellingPrice = itemData.SellingPrice;
            targetSlot.ItemStackCount = buyCount;
            targetSlot.IsSlotEmpty = false;

            _heldStackCount -= buyCount;
            if (_heldStackCount > 0) RestoreItemToOrigin(); // 돈 부족해서 못 산 나머지는 마우스/상점으로 원위치
            else ClearCursorItem();

            return;
        }

        // 인벤토리/창고 -> 창고 단순 이동일 경우
        targetSlot.ItemDataId = _dragSlotVm.ItemDataId;
        targetSlot.ItemSellingPrice = itemData.SellingPrice;
        targetSlot.ItemStackCount = _heldStackCount;
        targetSlot.IsSlotEmpty = false;
        ClearCursorItem();
    }

    private void PlaceOne(ShopItemSlotViewModel targetSlot)
    {
        var itemData = DataManager.Instance.GetItemData(_dragSlotVm.ItemDataId);

        if (targetSlot.SlotType == ShopItemSlotType.Inventory)
        {
            if (_originSlotVm.SlotType == ShopItemSlotType.Shop)
            {
                if (_shopVm.CurPlayerCredit >= itemData.SellingPrice)
                {
                    int remain = InventoryManager.Instance.TryAddItem(itemData, 1);
                    if (remain == 0)
                    {
                        _shopVm.CurPlayerCredit -= itemData.SellingPrice;
                        _heldStackCount--;
                    }
                }
                else { Debug.LogWarning("크레딧이 부족합니다!"); }
            }
            else
            {
                int remain = InventoryManager.Instance.TryAddItem(itemData, 1);
                if (remain == 0) _heldStackCount--;
            }

            if (_heldStackCount == 0) ClearCursorItem();
            else _dragSlotVm.ItemStackCount = _heldStackCount;
            return;
        }

        if (!targetSlot.IsSlotEmpty && targetSlot.ItemStackCount >= itemData.MaxStackCount)
        {
            return; 
        }

        if (_originSlotVm.SlotType == ShopItemSlotType.Shop)
        {
            if (_shopVm.CurPlayerCredit < itemData.SellingPrice)
            {
                Debug.LogWarning("크레딧이 부족합니다!");
                return;
            }
            _shopVm.CurPlayerCredit -= itemData.SellingPrice;
        }

        if (targetSlot.IsSlotEmpty)
        {
            targetSlot.ItemDataId = _dragSlotVm.ItemDataId;
            targetSlot.ItemSellingPrice = itemData.SellingPrice;
            targetSlot.ItemStackCount = 0;
            targetSlot.IsSlotEmpty = false;
        }

        targetSlot.ItemStackCount++;
        _heldStackCount--;

        if (_heldStackCount == 0) ClearCursorItem();
        else _dragSlotVm.ItemStackCount = _heldStackCount;
    }

    private void MergeAll(ShopItemSlotViewModel targetSlot)
    {
        if (targetSlot.SlotType == ShopItemSlotType.Inventory)
        {
            DropAllIntoInventory(); 
            return;
        }

        var itemData = DataManager.Instance.GetItemData(_dragSlotVm.ItemDataId);

        int maxCanAdd = itemData.MaxStackCount - targetSlot.ItemStackCount;

        if (maxCanAdd <= 0)
        {
            return;
        }

        int amountToAdd = Mathf.Min(_heldStackCount, maxCanAdd);

        if (_originSlotVm.SlotType == ShopItemSlotType.Shop)
        {
            int maxAffordable = _shopVm.CurPlayerCredit / itemData.SellingPrice;
            if (maxAffordable == 0)
            {
                Debug.LogWarning("크레딧이 부족합니다!");
                return;
            }

            amountToAdd = Mathf.Min(amountToAdd, maxAffordable);
            _shopVm.CurPlayerCredit -= amountToAdd * itemData.SellingPrice;
        }

        targetSlot.ItemStackCount += amountToAdd;
        _heldStackCount -= amountToAdd;

        if (_heldStackCount <= 0)
        {
            ClearCursorItem();
        }
        else
        {
            _dragSlotVm.ItemStackCount = _heldStackCount;
        }
    }

    private void RestoreItemToOrigin()
    {
        if (_originSlotVm == null) return;

        if (_originSlotVm.SlotType == ShopItemSlotType.Inventory)
        {
            var itemData = DataManager.Instance.GetItemData(_dragSlotVm.ItemDataId);
            InventoryManager.Instance.TryAddItem(itemData, _heldStackCount);
        }
        else
        {
            _originSlotVm.IsSlotEmpty = false;
            if (_originSlotVm.ItemStackCount != -1) _originSlotVm.ItemStackCount += _heldStackCount;
            _originSlotVm.ItemDataId = _dragSlotVm.ItemDataId;
            _originSlotVm.ItemSellingPrice = _dragSlotVm.ItemSellingPrice;
        }

        ClearCursorItem();
    }

    private void SwapItems(ShopItemSlotViewModel targetSlot)
    {
        if (_originSlotVm.SlotType == ShopItemSlotType.Shop || targetSlot.SlotType == ShopItemSlotType.Shop)
        {
            Debug.LogWarning("상점 물품과는 위치를 스왑(맞바꾸기) 할 수 없습니다!"); return;
        }

        // 인벤토리 매니저는 빈칸을 찾아 차례대로 채우는 구조이므로 맞바꾸기 로직 지원 불가
        if (targetSlot.SlotType == ShopItemSlotType.Inventory || _originSlotVm.SlotType == ShopItemSlotType.Inventory)
        {
            Debug.LogWarning("인벤토리는 자동 정렬되므로 맞바꾸기(스왑)를 지원하지 않습니다. 빈 공간을 이용해 주세요.");
            RestoreItemToOrigin(); return;
        }

        string tempId = targetSlot.ItemDataId;
        string tempUniqueId = targetSlot.ItemUniqueId;
        int tempCount = targetSlot.ItemStackCount;
        int tempPrice = targetSlot.ItemSellingPrice;

        targetSlot.ItemDataId = _dragSlotVm.ItemDataId;
        targetSlot.ItemUniqueId = _dragSlotVm.ItemUniqueId;
        targetSlot.ItemStackCount = _heldStackCount;
        targetSlot.ItemSellingPrice = _dragSlotVm.ItemSellingPrice;

        _dragSlotVm.ItemDataId = tempId;
        _dragSlotVm.ItemUniqueId = tempUniqueId;
        _heldStackCount = tempCount;
        _dragSlotVm.ItemStackCount = _heldStackCount;
        _dragSlotVm.ItemSellingPrice = tempPrice;
        _originSlotVm = targetSlot;
        _originSlotVm = targetSlot;
    }

    private void ClearCursorItem()
    {
        _heldStackCount = 0;
        _originSlotVm = null;
        ClearSlotData(_dragSlotVm);
        DragSlotUI.gameObject.SetActive(false);
    }

    private void ClearSlotData(ShopItemSlotViewModel slotVm)
    {
        slotVm.IsSlotEmpty = true;
        slotVm.ItemDataId = string.Empty;
        slotVm.ItemStackCount = 0;
        slotVm.ItemSellingPrice = 0;
    }
}
