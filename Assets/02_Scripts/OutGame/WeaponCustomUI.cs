using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class WeaponCustomUI : UIBase
{
    [SerializeField] private Button Button_CloseSelf;

    [Header("Prefabs")]
    [SerializeField] private StashItemSlotUI Prefab_StashItemSlotUI;
    [SerializeField] private WeaponPartsSlotUI Prefab_WeaponPartsSlotUI;

    [Header("Scroll View Contents")]
    [SerializeField] private Transform Transform_InventoryContent;
    [SerializeField] private Transform Transform_StashContent;
    [SerializeField] private Transform Transform_PartsContent;

    [Header("Main Weapon Area")]
    [SerializeField] private Button Button_MainWeaponSlot;
    [SerializeField] private Image Image_MainWeapon;

    private StashItemSlotUI DragSlotUI;
    private StashItemSlotViewModel _dragSlotVm;
    private ItemModel _cachedHoldingItemModel;
    private int _heldStackCount = 0;

    private WeaponCustomViewModel _customVm;

    private List<StashItemSlotUI> _invenSlotUIList = new List<StashItemSlotUI>();
    private List<StashItemSlotUI> _stashSlotUIList = new List<StashItemSlotUI>();
    private List<WeaponPartsSlotUI> _spawnedPartsSlotUIList = new List<WeaponPartsSlotUI>();

    private void OnEnable()
    {
        Button_CloseSelf.onClick.RemoveAllListeners();
        Button_CloseSelf.onClick.AddListener(OnClick_CloseButton);

        Button_MainWeaponSlot.onClick.RemoveAllListeners();
        Button_MainWeaponSlot.onClick.AddListener(OnClick_MainWeaponSlot);

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += RefreshInventoryUI;
        }

        BindViewModel();
        RefreshInventoryUI();
        RefreshStashUI();

        UpdateMainWeaponImage();
        RefreshDynamicPartsSlots();
    }

    private void OnDisable()
    {
        if (_customVm != null)
        {
            _customVm.HoveredItemId = null;
            _customVm.PropertyChanged -= OnPropChanged_View;
        }
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= RefreshInventoryUI;
        }
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ClosePopupUI(UIType.ShopItemPopupUI);
        }
    }

    private void Update()
    {
        if (_dragSlotVm != null && !_dragSlotVm.IsSlotEmpty)
        {
            DragSlotUI.transform.position = Input.mousePosition;
        }
    }

    private void BindViewModel()
    {
        _customVm = NetworkManager.Inst.WeaponCustomService.GetWeaponCustomViewModel();
        _customVm.PropertyChanged += OnPropChanged_View;

        InitDragCursor();
        InitLeftSlots();

        _customVm.InvokeOnceOnInit();
    }

    private void InitDragCursor()
    {
        if (DragSlotUI == null)
        {
            DragSlotUI = Instantiate(Prefab_StashItemSlotUI, this.transform);
            DragSlotUI.gameObject.name = "DragSlotUI_WeaponCustom";
            DragSlotUI.gameObject.SetActive(false);

            CanvasGroup canvasGroup = DragSlotUI.gameObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = DragSlotUI.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (_dragSlotVm == null)
        {
            _dragSlotVm = new StashItemSlotViewModel { IsSlotEmpty = true };
            DragSlotUI.Bind(_dragSlotVm, null, null, null);
        }
    }

    private void InitLeftSlots()
    {
        if (_invenSlotUIList.Count == 0)
        {
            foreach (var slotVm in _customVm.InventorySlots)
            {
                var slotUI = Instantiate(Prefab_StashItemSlotUI, Transform_InventoryContent);
                slotUI.Bind(slotVm, _customVm.OnSlotPointerEnter, _customVm.OnSlotPointerExit, OnClick_LeftSlot);
                _invenSlotUIList.Add(slotUI);
            }
        }

        if (_stashSlotUIList.Count == 0)
        {
            foreach (var slotVm in _customVm.StashSlots)
            {
                var slotUI = Instantiate(Prefab_StashItemSlotUI, Transform_StashContent);
                slotUI.Bind(slotVm, _customVm.OnSlotPointerEnter, _customVm.OnSlotPointerExit, OnClick_LeftSlot);
                _stashSlotUIList.Add(slotUI);
            }
        }
    }

    private void RefreshInventoryUI()
    {
        if (_customVm == null || InventoryManager.Instance == null) return;

        var allItems = InventoryManager.Instance.ItemList;
        var filteredItems = new List<ItemModel>();

        foreach (var item in allItems)
        {
            var itemData = DataManager.Instance.GetItemData(item.ItemId);
            if (itemData != null && (itemData.ItemType == "Weapon" || itemData is WeaponPartsData))
            {
                filteredItems.Add(item);
            }
        }

        var slotVms = _customVm.InventorySlots;
        for (int i = 0; i < slotVms.Length; i++)
        {
            if (i < filteredItems.Count)
            {
                var item = filteredItems[i];
                slotVms[i].ItemUniqueId = item.InstanceId;
                slotVms[i].ItemDataId = item.ItemId;
                slotVms[i].ItemStackCount = item.CurrentStackCount;
                slotVms[i].IsSlotEmpty = false;
            }
            else
            {
                slotVms[i].IsSlotEmpty = true;
                slotVms[i].ItemDataId = string.Empty;
                slotVms[i].ItemUniqueId = string.Empty;
            }
        }
    }

    private void RefreshStashUI()
    {
        if (_customVm == null || PlayerStatus.Instance == null) return;
        var stashItems = PlayerStatus.Instance.Model.StashItems;
        if (stashItems == null) stashItems = new List<ItemModel>();

        var filteredItems = new List<ItemModel>();

        foreach (var item in stashItems)
        {
            var itemData = DataManager.Instance.GetItemData(item.ItemId);
            if (itemData != null && (itemData.ItemType == "Weapon" || itemData is WeaponPartsData))
            {
                filteredItems.Add(item);
            }
        }

        var slotVms = _customVm.StashSlots;
        for (int i = 0; i < slotVms.Length; i++)
        {
            if (i < filteredItems.Count)
            {
                var item = filteredItems[i];
                slotVms[i].ItemUniqueId = item.InstanceId;
                slotVms[i].ItemDataId = item.ItemId;
                slotVms[i].ItemStackCount = item.CurrentStackCount;
                slotVms[i].IsSlotEmpty = false;
            }
            else
            {
                slotVms[i].IsSlotEmpty = true;
                slotVms[i].ItemDataId = string.Empty;
                slotVms[i].ItemUniqueId = string.Empty;
            }
        }
    }

    private void RefreshDynamicPartsSlots()
    {
        foreach (var slot in _spawnedPartsSlotUIList)
        {
            if (slot != null) Destroy(slot.gameObject);
        }
        _spawnedPartsSlotUIList.Clear();

        foreach (var slotVm in _customVm.PartsSlotList)
        {
            var slotUI = Instantiate(Prefab_WeaponPartsSlotUI, Transform_PartsContent);
            slotUI.Bind(slotVm, _customVm.OnSlotPointerEnter, _customVm.OnSlotPointerExit, OnClick_RightPartSlot);
            _spawnedPartsSlotUIList.Add(slotUI);
        }
    }

    private void OnPropChanged_View(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(WeaponCustomViewModel.TargetWeaponDataId):
                UpdateMainWeaponImage();
                break;
            case nameof(WeaponCustomViewModel.HoveredItemId):
                if (_customVm.HoveredItemId != null && _heldStackCount == 0)
                {
                    var popupUI = UIManager.Instance.OpenPopupUI(UIType.ShopItemPopupUI) as ShopItemPopupUI;
                    if (popupUI != null) popupUI.SetItemData(_customVm.HoveredItemId);
                }
                else
                {
                    UIManager.Instance.ClosePopupUI(UIType.ShopItemPopupUI);
                }
                break;
        }
    }

    private void UpdateMainWeaponImage()
    {
        if (string.IsNullOrEmpty(_customVm.TargetWeaponDataId))
        {
            Image_MainWeapon.enabled = false;
        }
        else
        {
            var itemData = DataManager.Instance.GetItemData(_customVm.TargetWeaponDataId);
            if (itemData != null)
            {
                Image_MainWeapon.sprite = ItemIconLoader.LoadIcon(itemData);
                Image_MainWeapon.enabled = true;
            }
        }
    }

    private void OnClick_LeftSlot(StashItemSlotViewModel clickedSlotVm, PointerEventData.InputButton button)
    {
        if (button != PointerEventData.InputButton.Left) return;

        if (_heldStackCount == 0 && !clickedSlotVm.IsSlotEmpty)
        {
            _cachedHoldingItemModel = NetworkManager.Inst.WeaponCustomService.PickupItemSafely(clickedSlotVm.ItemDataId, clickedSlotVm.ItemUniqueId, clickedSlotVm.SlotType);

            if (_cachedHoldingItemModel != null)
            {
                _heldStackCount = _cachedHoldingItemModel.CurrentStackCount;
                DragSlotUI.gameObject.SetActive(true);
                _dragSlotVm.ItemDataId = _cachedHoldingItemModel.ItemId;
                _dragSlotVm.ItemUniqueId = _cachedHoldingItemModel.InstanceId;
                _dragSlotVm.ItemStackCount = _heldStackCount;
                _dragSlotVm.IsSlotEmpty = false;

                RefreshInventoryUI();
                RefreshStashUI();
            }
        }
        else if (_heldStackCount > 0)
        {
            NetworkManager.Inst.WeaponCustomService.PlaceItemSafely(_cachedHoldingItemModel, clickedSlotVm.SlotType);
            ClearCursorItem();
            RefreshInventoryUI();
            RefreshStashUI();
        }

        UIManager.Instance.ClosePopupUI(UIType.ShopItemPopupUI);
    }

    private void OnClick_MainWeaponSlot()
    {
        if (_heldStackCount > 0)
        {
            var itemData = DataManager.Instance.GetItemData(_dragSlotVm.ItemDataId);
            if (itemData != null && itemData.ItemType == "Weapon")
            {
                NetworkManager.Inst.WeaponCustomService.SetTargetWeapon(_cachedHoldingItemModel);
                ClearCursorItem();
                RefreshDynamicPartsSlots();
            }
            else
            {
                Debug.LogWarning("무기만 개조대에 올릴 수 있습니다.");
            }
        }
        else if (_heldStackCount == 0 && !string.IsNullOrEmpty(_customVm.TargetWeaponDataId))
        {
            NetworkManager.Inst.WeaponCustomService.RestoreTargetWeaponToInventory();
            RefreshInventoryUI();
            RefreshStashUI();
            RefreshDynamicPartsSlots();
        }
    }

    private void OnClick_RightPartSlot(WeaponPartsSlotViewModel clickedSlotVm, PointerEventData.InputButton button)
    {
        if (button != PointerEventData.InputButton.Left) return;

        if (_heldStackCount > 0)
        {
            bool success = NetworkManager.Inst.WeaponCustomService.TryEquipPart(clickedSlotVm, _cachedHoldingItemModel);
            if (success) ClearCursorItem();
        }
        else if (_heldStackCount == 0 && !clickedSlotVm.IsSlotEmpty)
        {
            NetworkManager.Inst.WeaponCustomService.UnequipPart(clickedSlotVm);
            RefreshInventoryUI();
            RefreshStashUI();
        }
    }

    private void ClearCursorItem()
    {
        _heldStackCount = 0;
        _cachedHoldingItemModel = null;
        _dragSlotVm.ItemDataId = string.Empty;
        _dragSlotVm.ItemUniqueId = string.Empty;
        _dragSlotVm.IsSlotEmpty = true;
        DragSlotUI.gameObject.SetActive(false);
    }

    private void OnClick_CloseButton()
    {
        try
        {
            // 커서에 쥐고 있는 아이템만 안전하게 반환하고, 실제 창고/무기 동기화는 Obj의 OnCancel()에 맡깁니다.
            if (_heldStackCount > 0 && _cachedHoldingItemModel != null)
            {
                NetworkManager.Inst.WeaponCustomService.PlaceItemSafely(_cachedHoldingItemModel, ShopItemSlotType.Inventory);
                ClearCursorItem();
            }

            if (Lobby.Instance != null)
                Lobby.Instance.CloseCurrentTargetUI();
            else
                UIManager.Instance.CloseContentUI(UIType.WeaponCustomUI);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"WeaponCustomUI 닫기 버튼 예외 발생: {e.Message}");
            gameObject.SetActive(false);
        }
    }
}