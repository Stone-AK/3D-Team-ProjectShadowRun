using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class PlayerInventorySlotUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("UI")]
    [SerializeField] private Image ImageItemIcon;
    [SerializeField] private TMP_Text TextItemCount;
    [SerializeField] private GameObject SelectedFrame;

    private PlayerInventoryPopUpUI _owner;
    private int _slotIndex;
    private ItemModel _ItemModel;

    public int SlotIndex => _slotIndex;
    public ItemModel ItemModel => _ItemModel;

    public bool HasItem =>
        _ItemModel != null &&
        _ItemModel.CurrentStackCount > 0;

    public void Init(int slotIndex, PlayerInventoryPopUpUI owner)
    {
        _slotIndex = slotIndex;
        _owner = owner;

        Clear();
    }

    public void SetItem(ItemModel stack)
    {
        _ItemModel = stack;

        if (!HasItem)
        {
            Clear();
            return;
        }

        if (ImageItemIcon != null)
        {
            ImageItemIcon.sprite = ItemIconLoader.LoadIcon(DataManager.Instance.GetItemData(stack.ItemId));
            ImageItemIcon.enabled = ImageItemIcon.sprite != null;
        }

        if (TextItemCount != null)
        {
            bool showCount = _ItemModel.CurrentStackCount > 1;

            TextItemCount.text = showCount
                ? _ItemModel.CurrentStackCount.ToString()
                : string.Empty;

            TextItemCount.enabled = showCount;
        }
    }

    public void Clear()
    {
        _ItemModel = null;

        if (ImageItemIcon != null)
        {
            ImageItemIcon.sprite = null;
            ImageItemIcon.enabled = false;
        }

        if (TextItemCount != null)
        {
            TextItemCount.text = string.Empty;
            TextItemCount.enabled = false;
        }

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (SelectedFrame == null)
            return;

        SelectedFrame.SetActive(selected);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!HasItem)
            return;

        _owner.ShowItemTooltip(this, eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _owner.HideItemTooltip(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!HasItem)
            return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (eventData.clickCount >= 2)
                _owner.TryUseItem(this);
            else
                _owner.SelectSlot(this);
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            _owner.SelectSlot(this);
            _owner.OpenContextMenu(this, eventData.position);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!HasItem)
            return;

        _owner.BeginDragSlot(this, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!HasItem)
            return;

        _owner.DragSlot(this, eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_owner == null)
            return;

        _owner.EndDragSlot(this, eventData);
    }
}