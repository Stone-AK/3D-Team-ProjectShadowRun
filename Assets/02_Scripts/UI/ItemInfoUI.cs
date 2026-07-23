using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class ItemInfoUI : MonoBehaviour
{
    [SerializeField] private GameObject PanelItemInfo;
    [SerializeField] private TMP_Text TextItemName;
    [SerializeField] private TMP_Text TextItemInfo;
    [SerializeField] private TMP_Text TextItemInteractKey;

    private PlayerItemInteractor _itemInteractor;

    public void BindItemInteractor(PlayerItemInteractor itemInteractor)
    {
        Unbind();

        _itemInteractor = itemInteractor;

        if (_itemInteractor == null)
        {
            Hide();
            return;
        }

        _itemInteractor.OnTargetChanged += Refresh;
        Refresh(_itemInteractor.CurrentTarget);
    }

    private void OnDestroy()
    {
        Unbind();
    }

    private void Unbind()
    {
        if (_itemInteractor != null)
            _itemInteractor.OnTargetChanged -= Refresh;

        _itemInteractor = null;
    }

    private void Refresh(FieldItem fieldItem)
    {
        if (fieldItem == null || fieldItem.ItemModel == null || DataManager.Instance == null)
        {
            Hide();
            return;
        }

        ItemModel itemModel = fieldItem.ItemModel;
        ItemData itemData = DataManager.Instance.GetItemData(itemModel.ItemId);

        if (itemData == null)
        {
            Hide();
            return;
        }

        TextItemName.text = itemData.Name;

        if(itemModel is WeaponModel weaponModel)
        {
            TextItemInfo.text = BuildWeaponInfo(itemData, weaponModel);
        }
        else
        {
            TextItemInfo.text = BuildItemInfo(itemData, itemModel);
        }
        TextItemInteractKey.text = "[F] 줍기";

        PanelItemInfo.SetActive(true);
    }

    private string BuildItemInfo(ItemData itemData, ItemModel itemModel)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine(itemData.ItemDescription);
        builder.Append($"수량: {itemModel.CurrentStackCount}");
        return builder.ToString();
    }

    private string BuildWeaponInfo(ItemData itemData, WeaponModel weaponModel)
    {
        Dictionary<WeaponPartsType, string> partNames = GetAttachedPartNames(weaponModel);
        StringBuilder builder = new StringBuilder();

        builder.AppendLine(itemData.ItemDescription);
        builder.AppendLine();
        builder.AppendLine($"총구: {GetPartName(partNames, WeaponPartsType.Muzzle)}");
        builder.AppendLine($"조준경: {GetPartName(partNames, WeaponPartsType.Scope)}");
        builder.AppendLine($"탄창: {GetPartName(partNames, WeaponPartsType.Magazine)}");
        builder.AppendLine($"손잡이: {GetPartName(partNames, WeaponPartsType.Grip)}");
        builder.Append($"개머리판: {GetPartName(partNames, WeaponPartsType.Stock)}");

        return builder.ToString();
    }

    private Dictionary<WeaponPartsType, string> GetAttachedPartNames(WeaponModel weaponModel)
    {
        Dictionary<WeaponPartsType, string> partNames = new();

        if (weaponModel.AttachedParts == null)
            return partNames;

        foreach (ItemModel partModel in weaponModel.AttachedParts)
        {
            if (partModel == null)
                continue;

            ItemData partData = DataManager.Instance.GetItemData(partModel.ItemId);

            if (partData is not WeaponPartsData weaponPartData)
                continue;

            partNames[weaponPartData.PartsType] = weaponPartData.Name;
        }

        return partNames;
    }

    private string GetPartName(IReadOnlyDictionary<WeaponPartsType, string> partNames, WeaponPartsType partType)
    {
        if(partNames.TryGetValue(partType, out string partName))
        {
            return partName;
        }

        return "없음";
    }

    private void Hide()
    {
        if (PanelItemInfo != null)
            PanelItemInfo.SetActive(false);
    }
}
