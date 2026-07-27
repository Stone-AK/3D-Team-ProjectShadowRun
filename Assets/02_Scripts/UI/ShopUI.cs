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

    private ShopItemSlotUI DragSlotUI;
    private ShopItemSlotViewModel _dragSlotVm;

    private ItemModel _cachedHoldingItemModel;
    private ShopItemSlotType _originSlotType; 

    private ShopViewModel _shopVm;
    private bool _isInitialized = false;

    private void OnEnable()
    {
        Button_CloseSelf.onClick.RemoveAllListeners();
        Button_CloseSelf.onClick.AddListener(OnClick_CloseButton);

        if (InventoryManager.Instance != null) InventoryManager.Instance.OnInventoryChanged += RefreshAllZones;
        if (UIManager.Instance != null) UIManager.Instance.ClosePopupUI(UIType.ShopItemPopupUI);

        if (NetworkManager.Inst != null && NetworkManager.Inst.ShopService != null)
            NetworkManager.Inst.ShopService.RefreshStashData();
    }

    private void OnDisable()
    {
        if (_shopVm != null)
        {
            _shopVm.HoveredItemId = null;
            _shopVm.PropertyChanged -= OnPropertyChanged_View;
        }

        if (InventoryManager.Instance != null) InventoryManager.Instance.OnInventoryChanged -= RefreshAllZones;
        if (UIManager.Instance != null) UIManager.Instance.ClosePopupUI(UIType.ShopItemPopupUI);

        if (NetworkManager.Inst != null && NetworkManager.Inst.ShopService != null)
            NetworkManager.Inst.ShopService.SyncDataOnClose();
    }

    private void RefreshAllZones()
    {
        if (_shopVm == null || InventoryManager.Instance == null) return;

        var inventoryItems = InventoryManager.Instance.ItemList;
        LoadToSlots(inventoryItems, _shopVm.InventoryItemSlotList);

        if (PlayerStatus.Instance != null)
            LoadToSlots(PlayerStatus.Instance.Model.StashItems, _shopVm.StashItemSlotList);
    }

    private void LoadToSlots(IReadOnlyList<ItemModel> sourceList, List<ShopItemSlotViewModel> targetSlots)
    {
        for (int i = 0; i < targetSlots.Count; i++)
        {
            if (i < sourceList.Count)
            {
                var item = sourceList[i];
                var itemData = DataManager.Instance.GetItemData(item.ItemId);

                targetSlots[i].ItemUniqueId = item.InstanceId;
                targetSlots[i].ItemDataId = item.ItemId;
                targetSlots[i].ItemStackCount = item.CurrentStackCount;
                targetSlots[i].ItemSellingPrice = itemData != null ? itemData.SellingPrice : 0;
                targetSlots[i].IsSlotEmpty = false;
            }
            else
            {
                targetSlots[i].IsSlotEmpty = true;
                targetSlots[i].ItemDataId = string.Empty;
            }
        }
    }

    private void Update()
    {
        if (_dragSlotVm != null && !_dragSlotVm.IsSlotEmpty)
            DragSlotUI.transform.position = Input.mousePosition;
    }

    private void OnPropertyChanged_View(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ShopViewModel.CurPlayerCredit):
                Text_CurPlayerCredit.text = $"Player Credit : {_shopVm.CurPlayerCredit}";
                break;
            case nameof(ShopViewModel.HoveredItemId):
                if (_shopVm.HoveredItemId != null && _cachedHoldingItemModel == null)
                {
                    var popupUI = UIManager.Instance.OpenPopupUI(UIType.ShopItemPopupUI) as ShopItemPopupUI;
                    if (popupUI != null) popupUI.SetItemData(_shopVm.HoveredItemId);
                }
                else UIManager.Instance.ClosePopupUI(UIType.ShopItemPopupUI);
                break;
        }
    }

    public void BindViewModel(ShopViewModel vm)
    {
        if (_shopVm != null) _shopVm.PropertyChanged -= OnPropertyChanged_View;

        _shopVm = vm;
        _shopVm.PropertyChanged += OnPropertyChanged_View;

        if (DragSlotUI == null)
        {
            DragSlotUI = Instantiate(Prefab_ShopItemSlotUI, this.transform);
            DragSlotUI.gameObject.SetActive(false);
            var cg = DragSlotUI.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }

        if (_dragSlotVm == null)
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
        RefreshAllZones();
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
        if (_cachedHoldingItemModel != null && _originSlotType != ShopItemSlotType.Shop)
        {
            NetworkManager.Inst.TransferService.PlaceItemSafely(_cachedHoldingItemModel, _originSlotType);
        }

        if (Lobby.Instance != null) Lobby.Instance.CloseCurrentTargetUI();
        else UIManager.Instance.CloseContentUI(UIType.ShopUI);
    }

    private void OnSlotClicked(ShopItemSlotViewModel clickedSlotVm, PointerEventData.InputButton button)
    {
        if (button == PointerEventData.InputButton.Left) HandleLeftClick(clickedSlotVm);

        if (_cachedHoldingItemModel != null)
        {
            DragSlotUI.UpdatePriceDisplay(_dragSlotVm.ItemSellingPrice, _cachedHoldingItemModel.CurrentStackCount);
            UIManager.Instance.ClosePopupUI(UIType.ShopItemPopupUI);
        }
    }

    private void HandleLeftClick(ShopItemSlotViewModel clickedSlot)
    {
        bool isCtrlInput = (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
        bool isShiftInput = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

        if ((_cachedHoldingItemModel == null) && (clickedSlot.IsSlotEmpty == false))
        {
            int pickupAmount = -1;
            if (isCtrlInput)
            {
                pickupAmount = 1;
            }
            else if (isShiftInput)
            {
                pickupAmount = clickedSlot.ItemStackCount == -1 ? 1 : Mathf.CeilToInt(clickedSlot.ItemStackCount / 2.0f);
            }

            _originSlotType = clickedSlot.SlotType;
            int cachedPrice = clickedSlot.ItemSellingPrice;

            if (_originSlotType == ShopItemSlotType.Shop)
            {
                int stock = clickedSlot.ItemStackCount;
                if (pickupAmount == -1)
                {
                    pickupAmount = stock == -1 ? DataManager.Instance.GetItemData(clickedSlot.ItemDataId).MaxStackCount : stock;
                }
                    
                _cachedHoldingItemModel = new ItemModel {
                    ItemId = clickedSlot.ItemDataId,
                    CurrentStackCount = pickupAmount == -1 ? DataManager.Instance.GetItemData(clickedSlot.ItemDataId).MaxStackCount : pickupAmount
                };

                if (stock != -1)
                {
                    clickedSlot.ItemStackCount -= pickupAmount;
                    if (clickedSlot.ItemStackCount <= 0)
                    {
                        ClearSlotData(clickedSlot);
                    }
                }
            }
            else
            {
                _cachedHoldingItemModel = NetworkManager.Inst.TransferService.PickupItemSafely(
                    clickedSlot.ItemUniqueId, clickedSlot.ItemDataId, clickedSlot.SlotType, pickupAmount);
            }

            if (_cachedHoldingItemModel != null)
            {
                UpdateDragCursor(cachedPrice);
            }
        }
        else if (_cachedHoldingItemModel != null)
        {
            if (clickedSlot.SlotType == ShopItemSlotType.Shop)
            {
                if (_originSlotType != ShopItemSlotType.Shop)
                {
                    NetworkManager.Inst.ShopService.RequestSellItem(_cachedHoldingItemModel.ItemId, _cachedHoldingItemModel.CurrentStackCount);
                }
                else
                {
                    if ((clickedSlot.IsSlotEmpty == false) && (clickedSlot.ItemDataId == _cachedHoldingItemModel.ItemId))
                    {
                        if (clickedSlot.ItemStackCount != -1)
                        {
                            clickedSlot.ItemStackCount += _cachedHoldingItemModel.CurrentStackCount;
                        }
                    }
                    else if (clickedSlot.IsSlotEmpty)
                    {
                        clickedSlot.ItemDataId = _cachedHoldingItemModel.ItemId;
                        clickedSlot.ItemStackCount = _cachedHoldingItemModel.CurrentStackCount;
                        clickedSlot.ItemSellingPrice = _dragSlotVm.ItemSellingPrice;
                        clickedSlot.IsSlotEmpty = false;
                    }
                }

                ClearCursorItem();
            }
            else
            {
                if (_originSlotType == ShopItemSlotType.Shop)
                {
                    int bought = NetworkManager.Inst.ShopService.RequestBuyItem(_cachedHoldingItemModel.ItemId, _cachedHoldingItemModel.CurrentStackCount, clickedSlot.SlotType);
                    _cachedHoldingItemModel.CurrentStackCount -= bought;

                    if (_cachedHoldingItemModel.CurrentStackCount <= 0)
                    {
                        ClearCursorItem();
                    }
                    else
                    {
                        UpdateDragCursor(_dragSlotVm.ItemSellingPrice);
                    }
                }
                else
                {
                    int leftover = NetworkManager.Inst.TransferService.PlaceItemSafely(_cachedHoldingItemModel, clickedSlot.SlotType);
                    if (leftover <= 0)
                    {
                        ClearCursorItem();
                    }
                    else
                    {
                        UpdateDragCursor(_dragSlotVm.ItemSellingPrice);
                    }

                    if (leftover <= 0)
                    {
                        ClearCursorItem();
                    }
                    else
                    {
                        UpdateDragCursor(_dragSlotVm.ItemSellingPrice);
                    }
                }
            }
        }
        RefreshAllZones();
    }

    private void UpdateDragCursor(int price)
    {
        DragSlotUI.gameObject.SetActive(true);
        _dragSlotVm.ItemDataId = _cachedHoldingItemModel.ItemId;
        _dragSlotVm.ItemStackCount = _cachedHoldingItemModel.CurrentStackCount;
        _dragSlotVm.ItemSellingPrice = price;
        _dragSlotVm.IsSlotEmpty = false;
    }

    private void ClearCursorItem()
    {
        _cachedHoldingItemModel = null;
        _dragSlotVm.IsSlotEmpty = true;
        DragSlotUI.gameObject.SetActive(false);
    }

    private void ClearSlotData(ShopItemSlotViewModel slotVm)
    {
        slotVm.IsSlotEmpty = true;
        slotVm.ItemDataId = string.Empty;
        slotVm.ItemUniqueId = string.Empty;
        slotVm.ItemStackCount = 0;
        slotVm.ItemSellingPrice = 0;
    }
}