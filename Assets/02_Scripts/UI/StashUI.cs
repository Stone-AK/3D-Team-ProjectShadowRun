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

    private StashItemSlotViewModel _originSlotVm;
    private StashItemSlotViewModel _dragSlotVm;
    private int _heldStackCount = 0;

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
            InventoryManager.Instance.OnInventoryChanged += RefreshInventoryUI;
            RefreshInventoryUI();
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
            InventoryManager.Instance.OnInventoryChanged -= RefreshInventoryUI;
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

    private void BindViewModel()
    {
        var stashVm = NetworkManager.Inst.StashService.GetStashViewModel();
        _stashVm = stashVm;
        _stashVm.PropertyChanged += OnPropChanged_View;
        _stashVm.InvokeOnceOnInit();

        if (DragSlotUI == null)
        {
            // this.transform (ShopUI 최상위 객체)의 자식으로 생성
            // 이렇게 하면 ShopUI 패널 내에서 가장 나중에 그려지므로(Z-Order 최상단) 
            // 다른 슬롯이나 배경에 가려지지 않습니다.
            DragSlotUI = Instantiate(Prefab_StashItemSlotUI, this.transform);
            DragSlotUI.gameObject.name = "DragSlotUI_Dynamic";
            DragSlotUI.gameObject.SetActive(false);

            // (매우 중요) 마우스를 따라다니는 이미지가 클릭을 막지 않도록 통과시켜 줍니다.
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
                if (_stashVm.HoveredItemId != null && _heldStackCount == 0)
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
        CloseStashUI();
    }

    public void CloseStashUI()
    {
        UIManager.Instance.CloseContentUI(UIType.StashUI);
    }

    private void OnSlotHoverEnter(string dataId)
    {
        _stashVm.HoveredItemId = dataId;
    }

    private void OnSlotHoverExit()
    {
        _stashVm.HoveredItemId = null;
    }

    private void OnSlotClicked(StashItemSlotViewModel clickedSlotVm, PointerEventData.InputButton button)
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
            // 아이템을 하나라도 집었다면 팝업을 즉시 꺼서 마우스 커서를 가리지 않게 합니다.
            UIManager.Instance.ClosePopupUI(UIType.ShopItemPopupUI);
        }
    }

    private void HandleLeftClick(StashItemSlotViewModel clickedSlot)
    {
        bool isCtrlInput = (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
        bool isShiftInput = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

        if (_heldStackCount == 0)
        {
            if (isCtrlInput)
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
            if (isCtrlInput && (clickedSlot.ItemDataId == _dragSlotVm.ItemDataId))
            {
                PickupOne(clickedSlot);
            }
            else if (clickedSlot.IsSlotEmpty)
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

    private void HandleRightClick(StashItemSlotViewModel clickedSlot)
    {
        if (_heldStackCount == 0) return;

        if (clickedSlot.IsSlotEmpty || clickedSlot.ItemDataId == _dragSlotVm.ItemDataId)
        {
            PlaceOne(clickedSlot);
        }
    }

    // ==========================================
    // 헬퍼 메서드 (데이터 백업 및 MaxStackCount 검증 포함)
    // ==========================================

    private void PickupOne(StashItemSlotViewModel slotVm)
    {
        if (slotVm == null || slotVm.IsSlotEmpty) return;

        var itemData = DataManager.Instance.GetItemData(slotVm.ItemDataId);
        if (_heldStackCount >= itemData.MaxStackCount) return;

        // ⭐ 1. 이벤트 순서 문제 예방을 위한 슬롯 데이터 사전 백업
        string backupDataId = slotVm.ItemDataId;
        string backupUniqueId = slotVm.ItemUniqueId;

        // ⭐ 2. 인벤토리 출신일 경우 먼저 매니저에서 삭제
        if (slotVm.SlotType == ShopItemSlotType.Inventory)
        {
            if (!InventoryManager.Instance.TryRemoveItem(backupDataId, 1)) return;
        }

        // ⭐ 3. 커서에 백업 데이터 세팅
        if (_heldStackCount == 0)
        {
            _originSlotVm = slotVm;
            DragSlotUI.gameObject.SetActive(true);
            _dragSlotVm.ItemDataId = backupDataId;
            _dragSlotVm.ItemUniqueId = backupUniqueId;
            _dragSlotVm.IsSlotEmpty = false;
        }

        _heldStackCount++;
        _dragSlotVm.ItemStackCount = _heldStackCount;

        if (slotVm.SlotType != ShopItemSlotType.Inventory)
        {
            slotVm.ItemStackCount--;
            if (slotVm.ItemStackCount == 0) ClearSlotData(slotVm);
        }
    }

    private void PickupHalf(StashItemSlotViewModel slotVm)
    {
        if (slotVm == null || slotVm.IsSlotEmpty) return;

        string backupDataId = slotVm.ItemDataId;
        string backupUniqueId = slotVm.ItemUniqueId;

        int halfAmount = Mathf.CeilToInt(slotVm.ItemStackCount / 2.0f);

        if (slotVm.SlotType == ShopItemSlotType.Inventory)
        {
            if (!InventoryManager.Instance.TryRemoveItem(backupDataId, halfAmount)) return;
        }

        _heldStackCount = halfAmount;
        _originSlotVm = slotVm;

        DragSlotUI.gameObject.SetActive(true);
        _dragSlotVm.ItemDataId = backupDataId;
        _dragSlotVm.ItemUniqueId = backupUniqueId;
        _dragSlotVm.ItemStackCount = _heldStackCount;
        _dragSlotVm.IsSlotEmpty = false;

        if (slotVm.SlotType != ShopItemSlotType.Inventory)
        {
            slotVm.ItemStackCount -= halfAmount;
            if (slotVm.ItemStackCount == 0) ClearSlotData(slotVm);
        }
    }

    private void PickupAll(StashItemSlotViewModel slotVm)
    {
        if (slotVm == null || slotVm.IsSlotEmpty) return;

        string backupDataId = slotVm.ItemDataId;
        string backupUniqueId = slotVm.ItemUniqueId;

        var itemData = DataManager.Instance.GetItemData(backupDataId);
        int pickupAmount = Mathf.Min(slotVm.ItemStackCount, itemData.MaxStackCount);

        if (slotVm.SlotType == ShopItemSlotType.Inventory)
        {
            if (!InventoryManager.Instance.TryRemoveItem(backupDataId, pickupAmount)) return;
        }

        _heldStackCount = pickupAmount;
        _originSlotVm = slotVm;

        DragSlotUI.gameObject.SetActive(true);
        _dragSlotVm.ItemDataId = backupDataId;
        _dragSlotVm.ItemUniqueId = backupUniqueId;
        _dragSlotVm.ItemStackCount = _heldStackCount;
        _dragSlotVm.IsSlotEmpty = false;

        if (slotVm.SlotType != ShopItemSlotType.Inventory)
        {
            slotVm.ItemStackCount -= pickupAmount;
            if (slotVm.ItemStackCount <= 0) ClearSlotData(slotVm);
        }
    }

    private void DropAllIntoInventory()
    {
        var itemData = DataManager.Instance.GetItemData(_dragSlotVm.ItemDataId);
        int remain = InventoryManager.Instance.TryAddItem(itemData, _heldStackCount);
        _heldStackCount = remain;

        if (_heldStackCount > 0) RestoreItemToOrigin();
        else ClearCursorItem();
    }

    private void PlaceAll(StashItemSlotViewModel targetSlot)
    {
        if (targetSlot.SlotType == ShopItemSlotType.Inventory)
        {
            DropAllIntoInventory();
            return;
        }

        targetSlot.ItemDataId = _dragSlotVm.ItemDataId;
        targetSlot.ItemUniqueId = _dragSlotVm.ItemUniqueId;
        targetSlot.ItemStackCount = _heldStackCount;
        targetSlot.IsSlotEmpty = false;

        ClearCursorItem();
    }

    private void PlaceOne(StashItemSlotViewModel targetSlot)
    {
        var itemData = DataManager.Instance.GetItemData(_dragSlotVm.ItemDataId);

        if (targetSlot.SlotType == ShopItemSlotType.Inventory)
        {
            int remain = InventoryManager.Instance.TryAddItem(itemData, 1);
            if (remain == 0) _heldStackCount--;

            if (_heldStackCount == 0) ClearCursorItem();
            else _dragSlotVm.ItemStackCount = _heldStackCount;
            return;
        }

        if (!targetSlot.IsSlotEmpty && targetSlot.ItemStackCount >= itemData.MaxStackCount) return;

        if (targetSlot.IsSlotEmpty)
        {
            targetSlot.ItemDataId = _dragSlotVm.ItemDataId;
            targetSlot.ItemUniqueId = _dragSlotVm.ItemUniqueId;
            targetSlot.ItemStackCount = 0;
            targetSlot.IsSlotEmpty = false;
        }

        targetSlot.ItemStackCount++;
        _heldStackCount--;

        if (_heldStackCount == 0) ClearCursorItem();
        else _dragSlotVm.ItemStackCount = _heldStackCount;
    }

    private void MergeAll(StashItemSlotViewModel targetSlot)
    {
        if (targetSlot.SlotType == ShopItemSlotType.Inventory)
        {
            DropAllIntoInventory();
            return;
        }

        var itemData = DataManager.Instance.GetItemData(_dragSlotVm.ItemDataId);
        int maxCanAdd = itemData.MaxStackCount - targetSlot.ItemStackCount;

        if (maxCanAdd <= 0) return;

        int amountToAdd = Mathf.Min(_heldStackCount, maxCanAdd);
        targetSlot.ItemStackCount += amountToAdd;
        _heldStackCount -= amountToAdd;

        if (_heldStackCount <= 0) ClearCursorItem();
        else _dragSlotVm.ItemStackCount = _heldStackCount;
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
            _originSlotVm.ItemStackCount += _heldStackCount;
            _originSlotVm.ItemDataId = _dragSlotVm.ItemDataId;
            _originSlotVm.ItemUniqueId = _dragSlotVm.ItemUniqueId;
        }

        ClearCursorItem();
    }

    private void SwapItems(StashItemSlotViewModel targetSlot)
    {
        if (targetSlot.SlotType == ShopItemSlotType.Inventory || _originSlotVm.SlotType == ShopItemSlotType.Inventory)
        {
            Debug.LogWarning("인벤토리는 자동 정렬되므로 맞바꾸기(스왑)를 지원하지 않습니다. 빈 공간을 이용해 주세요.");
            RestoreItemToOrigin();
            return;
        }

        string tempId = targetSlot.ItemDataId;
        string tempUniqueId = targetSlot.ItemUniqueId;
        int tempCount = targetSlot.ItemStackCount;

        targetSlot.ItemDataId = _dragSlotVm.ItemDataId;
        targetSlot.ItemUniqueId = _dragSlotVm.ItemUniqueId;
        targetSlot.ItemStackCount = _heldStackCount;

        _dragSlotVm.ItemDataId = tempId;
        _dragSlotVm.ItemUniqueId = tempUniqueId;
        _heldStackCount = tempCount;
        _dragSlotVm.ItemStackCount = _heldStackCount;

        _originSlotVm = targetSlot;
    }

    private void ClearCursorItem()
    {
        _heldStackCount = 0;
        _originSlotVm = null;
        _dragSlotVm.ItemDataId = string.Empty;
        _dragSlotVm.ItemUniqueId = string.Empty;
        _dragSlotVm.ItemStackCount = 0;
        _dragSlotVm.IsSlotEmpty = true;
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