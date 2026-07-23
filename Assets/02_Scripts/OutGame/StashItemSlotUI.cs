using Cysharp.Threading.Tasks;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StashItemSlotUI : UIBase, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private Image Image_ItemIcon;
    [SerializeField] private TMP_Text Text_StackCount;

    private StashItemSlotViewModel _slotVm;
    public Action<string> _onHoverEnter;
    public Action _onHoverExit;

    public Action<StashItemSlotViewModel, PointerEventData.InputButton> _onClickSlot;

    public void Bind(StashItemSlotViewModel slotVm, Action<string> onHoverEnter, Action onHoverExit, Action<StashItemSlotViewModel, PointerEventData.InputButton> onClickSlot)
    {
        if (_slotVm != null)
        {
            _slotVm.PropertyChanged -= OnSlotPropertyChanged;
        }

        _slotVm = slotVm;
        _onHoverEnter = onHoverEnter;
        _onHoverExit = onHoverExit;
        _onClickSlot = onClickSlot;

        _slotVm.PropertyChanged += OnSlotPropertyChanged;
        UpdateSlotUI();
    }

    private void OnSlotPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        UpdateSlotUI();
    }

    private void UpdateSlotUI()
    {
        if (_slotVm.IsSlotEmpty == true)
        {
            Image_ItemIcon.enabled = false;
            Text_StackCount.text = "";
            return;
        }

        Image_ItemIcon.enabled = true;

        var itemData = DataManager.Instance.GetItemData(_slotVm.ItemDataId);
        if (itemData != null)
        {
            Image_ItemIcon.sprite = ItemIconLoader.LoadIcon(DataManager.Instance.GetItemData(_slotVm.ItemDataId));
        }

        if (_slotVm.ItemStackCount <= 1)
        {
            Text_StackCount.text = ""; 
        }
        else
        {
            Text_StackCount.text = _slotVm.ItemStackCount.ToString(); 
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _onClickSlot?.Invoke(_slotVm, eventData.button);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_slotVm != null && !_slotVm.IsSlotEmpty)
        {
            _onHoverEnter?.Invoke(_slotVm.ItemDataId);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _onHoverExit?.Invoke();
    }

    private void OnDestroy()
    {
        if (_slotVm != null) _slotVm.PropertyChanged -= OnSlotPropertyChanged;
    }
}
