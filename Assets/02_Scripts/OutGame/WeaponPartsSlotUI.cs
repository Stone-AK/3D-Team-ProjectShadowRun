using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WeaponPartsSlotUI : UIBase, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private Image Image_ItemIcon;
    [SerializeField] private TMP_Text Text_PartsTypeName; 

    private WeaponPartsSlotViewModel _slotVm;
    private Action<string> _onHoverEnter;
    private Action _onHoverExit;
    private Action<WeaponPartsSlotViewModel, PointerEventData.InputButton> _onClickSlot;

    public void Bind(
        WeaponPartsSlotViewModel slotVm,
        Action<string> onHoverEnter,
        Action onHoverExit,
        Action<WeaponPartsSlotViewModel, PointerEventData.InputButton> onClickSlot)
    {
        if (_slotVm != null)
        {
            _slotVm.PropertyChanged -= OnSlotPropertyChanged;
        }

        _slotVm = slotVm;
        _onHoverEnter = onHoverEnter;
        _onHoverExit = onHoverExit;
        _onClickSlot = onClickSlot;

        if (Text_PartsTypeName != null)
        {
            Text_PartsTypeName.text = _slotVm.RequiredPartsType.ToString();
        }

        _slotVm.PropertyChanged += OnSlotPropertyChanged;
        UpdateSlotUI();
    }

    private void OnSlotPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        UpdateSlotUI();
    }

    private void UpdateSlotUI()
    {
        if (DataManager.Instance == null || Image_ItemIcon == null) return;

        if (_slotVm.IsSlotEmpty)
        {
            Image_ItemIcon.enabled = false;
            return;
        }

        Image_ItemIcon.enabled = true;
        var itemData = DataManager.Instance.GetItemData(_slotVm.ItemDataId);
        if (itemData != null)
        {
            Image_ItemIcon.sprite = ItemIconLoader.LoadIcon(itemData);
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
