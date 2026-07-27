using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StashUI : UIBase
{
    [SerializeField] private TMP_Text Text_CurPlayerCredit;
    [SerializeField] private Button Button_CloseSelf;

    [SerializeField] private StashItemSlotUI Prefab_StashItemSlotUI;
    [SerializeField] private Transform Transform_InventoryContent;
    [SerializeField] private Transform Transform_StashContent;

    private StashItemSlotUI DragSlotUI;
    private StashItemSlotViewModel _dragSlotVm;

    private ItemModel _cachedHoldingItemModel;
    private ShopItemSlotType _originSlotType; 

    private List<StashItemSlotUI> _stashSlotUIList = new List<StashItemSlotUI>();
    private List<StashItemSlotUI> _invenSlotUIList = new List<StashItemSlotUI>();

    private StashViewModel _stashVm;

    private void OnEnable()
    {
        Button_CloseSelf.onClick.RemoveAllListeners();
        Button_CloseSelf.onClick.AddListener(OnClick_CloseButton);
        BindViewModel();

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += RefreshAllUI;
            RefreshAllUI();
        }

        if (NetworkManager.Inst != null && NetworkManager.Inst.StashService != null)
        {
            NetworkManager.Inst.StashService.InitStashAndInventoryData();
        }
    }

    private void OnDisable()
    {
        if (_stashVm != null)
        {
            _stashVm.HoveredItemId = null;
            _stashVm.PropertyChanged -= OnPropChanged_View;
        }

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= RefreshAllUI;
        }

        if (UIManager.Instance != null)
        { 
            UIManager.Instance.ClosePopupUI(UIType.ShopItemPopupUI); 
        }

        if (NetworkManager.Inst != null && NetworkManager.Inst.StashService != null)
        {
            NetworkManager.Inst.StashService.SyncDataOnClose();
        }
    }

    private void Update()
    {
        if (_dragSlotVm != null && !_dragSlotVm.IsSlotEmpty)
        {
            DragSlotUI.transform.position = Input.mousePosition;
        }
    }

    private void RefreshAllUI()
    {
        RefreshInventoryUI();
        RefreshStashUI();
    }

    private void RefreshInventoryUI()
    {
        if (_stashVm == null || InventoryManager.Instance == null) return;

        var inventoryItems = InventoryManager.Instance.ItemList;
        var slotVms = _stashVm.InventorySlots;

        for (int i = 0; i < slotVms.Length; i++)
        {
            if (i < inventoryItems.Count)
            {
                var item = inventoryItems[i];
                slotVms[i].ItemUniqueId = item.InstanceId;
                slotVms[i].ItemDataId = item.ItemId;
                slotVms[i].ItemStackCount = item.CurrentStackCount;
                slotVms[i].IsSlotEmpty = false;
            }
            else
            {
                ClearSlotData(slotVms[i]);
            }
        }
    }

    private void RefreshStashUI()
    {
        if (_stashVm == null || PlayerStatus.Instance == null) return;

        var stashItems = PlayerStatus.Instance.Model.StashItems;
        if (stashItems == null)
        {
            stashItems = new List<ItemModel>();
        }

        var slotVms = _stashVm.StashSlots;

        for (int i = 0; i < slotVms.Length; i++)
        {
            if (i < stashItems.Count)
            {
                var item = stashItems[i];
                slotVms[i].ItemUniqueId = item.InstanceId;
                slotVms[i].ItemDataId = item.ItemId;
                slotVms[i].ItemStackCount = item.CurrentStackCount;
                slotVms[i].IsSlotEmpty = false;
            }
            else
            {
                ClearSlotData(slotVms[i]);
            }
        }
    }

    private void BindViewModel()
    {
        _stashVm = NetworkManager.Inst.StashService.GetStashViewModel();
        _stashVm.PropertyChanged += OnPropChanged_View;
        _stashVm.InvokeOnceOnInit();

        if (DragSlotUI == null)
        {
            DragSlotUI = Instantiate(Prefab_StashItemSlotUI, this.transform);
            DragSlotUI.gameObject.name = "DragSlotUI_Dynamic";
            DragSlotUI.gameObject.SetActive(false);

            CanvasGroup canvasGroup = DragSlotUI.gameObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = DragSlotUI.gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (_dragSlotVm == null)
        {
            _dragSlotVm = new StashItemSlotViewModel { IsSlotEmpty = true };
            DragSlotUI.Bind(_dragSlotVm, null, null, null);
        }

        InitStashSlotUIs();
    }

    private void InitStashSlotUIs()
    {
        if (_stashSlotUIList.Count == 0)
        {
            foreach (var slotVm in _stashVm.StashSlots)
            {
                var slotUI = Instantiate(Prefab_StashItemSlotUI, Transform_StashContent);
                slotUI.Bind(slotVm, OnSlotHoverEnter, OnSlotHoverExit, OnSlotClicked);
                _stashSlotUIList.Add(slotUI);
            }
        }

        if (_invenSlotUIList.Count == 0)
        {
            foreach (var slotVm in _stashVm.InventorySlots)
            {
                var slotUI = Instantiate(Prefab_StashItemSlotUI, Transform_InventoryContent);
                slotUI.Bind(slotVm, OnSlotHoverEnter, OnSlotHoverExit, OnSlotClicked);
                _invenSlotUIList.Add(slotUI);
            }
        }
    }

    private void OnPropChanged_View(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(StashViewModel.CurPlayerCredit):
                Text_CurPlayerCredit.text = $"Player Credit : {_stashVm.CurPlayerCredit}";
                break;
            case nameof(StashViewModel.HoveredItemId):
                if (_stashVm.HoveredItemId != null && _cachedHoldingItemModel == null)
                {
                    var popupUI = UIManager.Instance.OpenPopupUI(UIType.ShopItemPopupUI) as ShopItemPopupUI;
                    if (popupUI != null)
                    {
                        popupUI.SetItemData(_stashVm.HoveredItemId);
                    }
                }
                else
                {
                    UIManager.Instance.ClosePopupUI(UIType.ShopItemPopupUI);
                }

                break;
        }
    }

    private void OnClick_CloseButton()
    {
        if (_cachedHoldingItemModel != null)
        {
            NetworkManager.Inst.TransferService.PlaceItemSafely(_cachedHoldingItemModel, _originSlotType);
            ClearCursorItem();
        }
        CloseStashUI();
    }

    public void CloseStashUI()
    {
        if (Lobby.Instance != null)
        {
            Lobby.Instance.CloseCurrentTargetUI();
        }
        else
        {
            UIManager.Instance.CloseContentUI(UIType.StashUI);
        }
    }

    private void OnSlotHoverEnter(string dataId) => _stashVm.HoveredItemId = dataId;
    private void OnSlotHoverExit() => _stashVm.HoveredItemId = null;

    private void OnSlotClicked(StashItemSlotViewModel clickedSlotVm, PointerEventData.InputButton button)
    {
        if (button == PointerEventData.InputButton.Left)
        {
            HandleLeftClick(clickedSlotVm);
        }

        if (_cachedHoldingItemModel != null)
        {
            UIManager.Instance.ClosePopupUI(UIType.ShopItemPopupUI);
        }
    }

    private void HandleLeftClick(StashItemSlotViewModel clickedSlot)
    {
        bool isCtrlInput = (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
        bool isShiftInput = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

        if (_cachedHoldingItemModel == null && !clickedSlot.IsSlotEmpty)
        {
            int pickupAmount = -1; 

            if (isCtrlInput) pickupAmount = 1;
            else if (isShiftInput) pickupAmount = Mathf.CeilToInt(clickedSlot.ItemStackCount / 2.0f);

            _cachedHoldingItemModel = NetworkManager.Inst.TransferService.PickupItemSafely(clickedSlot.ItemUniqueId, clickedSlot.ItemDataId, clickedSlot.SlotType, pickupAmount);

            if (_cachedHoldingItemModel != null)
            {
                _originSlotType = clickedSlot.SlotType;
                UpdateDragCursor();
            }
        }
        else if (_cachedHoldingItemModel != null)
        {
            if (clickedSlot.IsSlotEmpty || clickedSlot.ItemDataId == _cachedHoldingItemModel.ItemId)
            {
                int leftover = NetworkManager.Inst.TransferService.PlaceItemSafely(_cachedHoldingItemModel, clickedSlot.SlotType);

                if (leftover <= 0)
                {
                    ClearCursorItem();
                }
                else
                {
                    UpdateDragCursor();
                }
            }
            else
            {
                Debug.LogWarning("빈 공간을 이용해 주세요.");
                NetworkManager.Inst.TransferService.PlaceItemSafely(_cachedHoldingItemModel, _originSlotType);
                ClearCursorItem();
            }
        }

        RefreshAllUI(); // 데이터 조작이 끝난 후 UI는 한 번에 갱신
    }

    private void UpdateDragCursor()
    {
        if (_cachedHoldingItemModel == null) return;

        DragSlotUI.gameObject.SetActive(true);
        _dragSlotVm.ItemDataId = _cachedHoldingItemModel.ItemId;
        _dragSlotVm.ItemUniqueId = _cachedHoldingItemModel.InstanceId;
        _dragSlotVm.ItemStackCount = _cachedHoldingItemModel.CurrentStackCount;
        _dragSlotVm.IsSlotEmpty = false;
    }

    private void ClearCursorItem()
    {
        _cachedHoldingItemModel = null;
        ClearSlotData(_dragSlotVm);
        DragSlotUI.gameObject.SetActive(false);
    }

    private void ClearSlotData(StashItemSlotViewModel slotVm)
    {
        slotVm.IsSlotEmpty = true;
        slotVm.ItemDataId = string.Empty;
        slotVm.ItemUniqueId = string.Empty;
        slotVm.ItemStackCount = 0;
    }
}