using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class PlayerInventoryPopUpUI : UIBase
{
    [Header("Player Credit")]
    [SerializeField] private TMP_Text TextHaveCreditCount;

    [Header("Slot")]
    [SerializeField] private Transform SlotParent;

    [Header("Context Menu")]
    [SerializeField] private GameObject ContextMenuPanel;
    [SerializeField] private RectTransform ContextMenuRect;
    [SerializeField] private Button ButtonUse;
    [SerializeField] private Button ButtonDrop;
    [SerializeField] private Button ButtonRegisterQuickSlot;
    [SerializeField] private Vector2 ContextMenuOffset = new Vector2(8f, -8f);

    [Header("Drag Icon")]
    [SerializeField] private GameObject DragIconObject;
    [SerializeField] private RectTransform DragIconRect;
    [SerializeField] private Image DragIconImage;
    [SerializeField] private TMP_Text TextDragIconCount;

    [Header("Tooltip")]
    [SerializeField] private PlayerInventoryTooltipController ItemTooltip;

    private readonly List<PlayerInventorySlotUI> _slotUIList = new();

    private Canvas _rootCanvas;
    private RectTransform _canvasRect;

    private PlayerInventorySlotUI _selectedSlot;
    private PlayerInventorySlotUI _draggingSlot;
    private PlayerInventorySlotUI _contextTargetSlot;

    private void Awake()
    {
        _rootCanvas = GetComponentInParent<Canvas>();
        _canvasRect = _rootCanvas != null ? _rootCanvas.transform as RectTransform : null;

        InitSlots();
        InitContextButtons();

        CloseContextMenu();
        HideDragSlot ();
    }

    private void OnEnable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += RefreshInventory;
            InventoryManager.Instance.OnCreditChanged += RefreshPlayerCredit;
        }

        RefreshInventory();
        RefreshPlayerCredit();

        CloseContextMenu();
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= RefreshInventory;
            InventoryManager.Instance.OnCreditChanged -= RefreshPlayerCredit;
        }

        CloseContextMenu();
    }

    private void Update()
    {
        if (ContextMenuPanel != null && ContextMenuPanel.activeSelf)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (!IsPointerOverObject(ContextMenuPanel))
                    CloseContextMenu();
            }
        }
    }

    private void InitSlots()
    {
        _slotUIList.Clear();

        PlayerInventorySlotUI[] slots = SlotParent.GetComponentsInChildren<PlayerInventorySlotUI>(true);

        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].Init(i, this);
            _slotUIList.Add(slots[i]);
        }
    }

    private void InitContextButtons()
    {
        if (ButtonUse != null)
        {
            ButtonUse.onClick.RemoveAllListeners();
            ButtonUse.onClick.AddListener(OnClickUse);
        }

        if (ButtonDrop != null)
        {
            ButtonDrop.onClick.RemoveAllListeners();
            ButtonDrop.onClick.AddListener(OnClickDrop);
        }

        if (ButtonRegisterQuickSlot != null)
        {
            ButtonRegisterQuickSlot.onClick.RemoveAllListeners();
            ButtonRegisterQuickSlot.onClick.AddListener(OnClickRegisterQuickSlot);
        }
    }

    private void RefreshInventory()
    {
        IReadOnlyList<ItemModel> itemModels = PlayerStatus.Instance.Model.InventoryItems;

        for (int i = 0; i < _slotUIList.Count; i++)
        {
            if (i < itemModels.Count)
            {
                ItemModel itemModel = itemModels[i];
                _slotUIList[i].SetItem(itemModel);
            }
            else
                _slotUIList[i].Clear();
        }

        RefreshSelectedSlot();
    }

    private void RefreshPlayerCredit()
    {
        if (TextHaveCreditCount == null)
            return;

        int playerCredit = PlayerStatus.Instance.Model.CurrentCredit;

        TextHaveCreditCount.text = $"{playerCredit:N0}";
    }

    private void RefreshSelectedSlot()
    {
        if (_selectedSlot == null)
            return;

        if (!_selectedSlot.HasItem)
        {
            _selectedSlot.SetSelected(false);
            _selectedSlot = null;
        }
    }

    public void SelectSlot(PlayerInventorySlotUI slot)
    {
        if (slot == null || !slot.HasItem)
            return;

        if (_selectedSlot != null && _selectedSlot != slot)
            _selectedSlot.SetSelected(false);

        _selectedSlot = slot;
        _selectedSlot.SetSelected(true);
    }

    public void TryUseItem(PlayerInventorySlotUI slot)
    {
        if (slot == null || !slot.HasItem)
            return;

        if (InventoryManager.Instance == null)
            return;

        SelectSlot(slot);

        InventoryManager.Instance.TryUseItem(slot.SlotIndex);

        CloseContextMenu();
    }

    public void OpenContextMenu(PlayerInventorySlotUI slot, Vector2 mousePosition)
    {
        if (slot == null || !slot.HasItem)
            return;

        _contextTargetSlot = slot;
        SelectSlot(slot);

        if (ContextMenuPanel == null || ContextMenuRect == null)
            return;

        RefreshContextMenuButtons(slot);

        ContextMenuPanel.SetActive(true);

        LayoutRebuilder.ForceRebuildLayoutImmediate(ContextMenuRect);

        SetPanelPositionClamped(ContextMenuRect, mousePosition + ContextMenuOffset);
    }

    private void CloseContextMenu()
    {
        _contextTargetSlot = null;

        if (ContextMenuPanel != null)
            ContextMenuPanel.SetActive(false);
    }

    private void SetPanelPositionClamped(RectTransform panelRect, Vector2 screenPosition)
    {
        if (panelRect == null || _canvasRect == null || _rootCanvas == null)
            return;

        Camera uiCamera = null;

        if (_rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = _rootCanvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect,
            screenPosition,
            uiCamera,
            out Vector2 localPoint
        );

        panelRect.anchoredPosition = localPoint;

        Vector2 panelSize = panelRect.rect.size;
        Vector2 canvasSize = _canvasRect.rect.size;

        Vector2 clamped = panelRect.anchoredPosition;

        float minX = -canvasSize.x * 0.5f;
        float maxX = canvasSize.x * 0.5f - panelSize.x;

        float minY = -canvasSize.y * 0.5f + panelSize.y;
        float maxY = canvasSize.y * 0.5f;

        clamped.x = Mathf.Clamp(clamped.x, minX, maxX);
        clamped.y = Mathf.Clamp(clamped.y, minY, maxY);

        panelRect.anchoredPosition = clamped;
    }

    private bool IsPointerOverObject(GameObject targetObject)
    {
        if (EventSystem.current == null || targetObject == null)
            return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject == targetObject ||
                result.gameObject.transform.IsChildOf(targetObject.transform))
            {
                return true;
            }
        }

        return false;
    }

    public void BeginDragSlot(PlayerInventorySlotUI slot, PointerEventData eventData)
    {
        if (slot == null || !slot.HasItem)
            return;

        _draggingSlot = slot;

        CloseContextMenu();

        ShowDragSlot(slot.ItemModel, eventData.position);
    }

    public void DragSlot(PlayerInventorySlotUI slot, PointerEventData eventData)
    {
        if (_draggingSlot == null)
            return;

        MoveDragSlot (eventData.position);
    }

    public void EndDragSlot(PlayerInventorySlotUI slot, PointerEventData eventData)
    {
        if (_draggingSlot == null)
            return;

        PlayerEquipmentSlotUI equipmentSlotUI = GetEquipmentSlotUnderPointer(eventData);

        if (equipmentSlotUI != null && InventoryManager.Instance != null)
        {
            bool equipped = InventoryManager.Instance.TryEquipItem(
                _draggingSlot.SlotIndex,
                equipmentSlotUI.EquipmentType
            );
        }
        else
        {
            PlayerQuickSlotUI quickSlotUI = GetQuickSlotUnderPointer(eventData);

            if (quickSlotUI != null && InventoryManager.Instance != null)
            {
                InventoryManager.Instance.TryRegisterQuickSlot(
                    _draggingSlot.SlotIndex,
                    quickSlotUI.SlotIndex
                );
            }
        }

        HideDragSlot();

        _draggingSlot = null;
    }

    private PlayerEquipmentSlotUI GetEquipmentSlotUnderPointer(PointerEventData eventData)
    {
        if (EventSystem.current == null)
            return null;

        List<RaycastResult> results = new();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
            PlayerEquipmentSlotUI equipmentSlotUI = result.gameObject.GetComponentInParent<PlayerEquipmentSlotUI>();

            if (equipmentSlotUI != null)
                return equipmentSlotUI;
        }

        return null;
    }

    private PlayerQuickSlotUI GetQuickSlotUnderPointer(PointerEventData eventData)
    {
        if (EventSystem.current == null)
            return null;

        List<RaycastResult> results = new();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
            PlayerQuickSlotUI quickSlotUI = result.gameObject.GetComponentInParent<PlayerQuickSlotUI>();

            if (quickSlotUI != null)
                return quickSlotUI;
        }

        return null;
    }

    private void OnClickUse()
    {
        if (_contextTargetSlot == null || !_contextTargetSlot.HasItem)
            return;

        TryUseItem(_contextTargetSlot);
    }

    private void OnClickDrop()
    {
        if (_contextTargetSlot == null || !_contextTargetSlot.HasItem)
            return;

        if (InventoryManager.Instance == null)
            return;
        int dropCount = _contextTargetSlot.ItemModel.CurrentStackCount;
        InventoryManager.Instance.TryDropItem(_contextTargetSlot.SlotIndex, dropCount);

        CloseContextMenu();
    }

    private void OnClickRegisterQuickSlot()
    {
        if (_contextTargetSlot == null || !_contextTargetSlot.HasItem)
            return;

        if (InventoryManager.Instance == null)
            return;

        InventoryManager.Instance.TryRegisterQuickSlot(_contextTargetSlot.SlotIndex);

        CloseContextMenu();
    }

    private void RefreshContextMenuButtons(PlayerInventorySlotUI slot)
    {
        if (slot == null || !slot.HasItem)
            return;

        ItemData item = DataManager.Instance.GetItemData(slot.ItemModel.ItemId);

        bool canUse = item.ItemType == "Consumable";
        bool canRegisterQuickSlot = (item.ItemType == "Weapon") || (item.ItemType == "Consumable");

        if (ButtonUse != null)
            ButtonUse.gameObject.SetActive(canUse);

        if (ButtonRegisterQuickSlot != null)
            ButtonRegisterQuickSlot.gameObject.SetActive(canRegisterQuickSlot);

        if (ButtonDrop != null)
            ButtonDrop.gameObject.SetActive(true);
    }

    private void ShowDragSlot(ItemModel stack, Vector2 screenPosition)
    {
        if (stack == null)
            return;

        if (DragIconObject == null || DragIconRect == null || DragIconImage == null)
            return;

        DragIconObject.SetActive(true);

        Sprite icon = ItemIconLoader.LoadIcon(DataManager.Instance.GetItemData(stack.ItemId));

        DragIconImage.sprite = icon;
        DragIconImage.gameObject.SetActive(icon != null);

        if (TextDragIconCount != null)
        {
            bool showCount = stack.CurrentStackCount > 1;
            TextDragIconCount.text = showCount ? stack.CurrentStackCount.ToString() : string.Empty;
        }

        SetPanelPositionClamped(DragIconRect, screenPosition);
    }

    private void MoveDragSlot(Vector2 screenPosition)
    {
        if (DragIconObject == null || !DragIconObject.activeSelf)
            return;

        SetPanelPositionClamped(DragIconRect, screenPosition);
    }

    private void HideDragSlot()
    {
        if (DragIconObject != null)
            DragIconObject.SetActive(false);

        if (DragIconImage != null)
            DragIconImage.sprite = null;

        if (TextDragIconCount != null)
            TextDragIconCount.text = string.Empty;
    }

    public void ShowItemTooltip(PlayerInventorySlotUI slot, Vector2 mousePosition)
    {
        if (slot == null || !slot.HasItem)
            return;

        if (ItemTooltip == null)
            return;

        ItemTooltip.Show(slot.ItemModel, mousePosition);
    }

    public void HideItemTooltip(PlayerInventorySlotUI slot)
    {
        if (ItemTooltip != null)
            ItemTooltip.Hide();
    }
}
