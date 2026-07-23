using UnityEngine;

public class FieldItem : MonoBehaviour
{
    [SerializeReference] private ItemModel _itemModel = new ItemModel
    {
        CurrentStackCount = 1
    };

    public ItemModel ItemModel => _itemModel;

    private void Start()
    {
        InitializePlacedItemModel();
    }

    private void InitializePlacedItemModel()
    {
        if (_itemModel == null || DataManager.Instance == null)
            return;

        ItemData itemData = DataManager.Instance.GetItemData(_itemModel.ItemId);

        if (itemData is not WeaponData weaponData || _itemModel is WeaponModel)
            return;

        _itemModel = new WeaponModel
        {
            InstanceId = gameObject.GetInstanceID().ToString(),
            ItemId = _itemModel.ItemId,
            CurrentStackCount = Mathf.Max(1, _itemModel.CurrentStackCount),
            CurrentAmmo = weaponData.MagazineSize,
            CurrentDurability = weaponData.MaxDurability,
            AttachedParts = new System.Collections.Generic.List<ItemModel>()
        };
    }

    public bool TryPickup()
    {
        if (InventoryManager.Instance == null || DataManager.Instance == null)
            return false;

        if (_itemModel == null)
            return false;

        ItemData itemData = DataManager.Instance.GetItemData(_itemModel.ItemId);

        if (itemData == null)
            return false;

        if (itemData is WeaponData)
            return TryPickupWeapon();

        int previousCount = _itemModel.CurrentStackCount;
        int remainingCount = InventoryManager.Instance.TryAddItem(itemData, previousCount);
        bool pickedUpAny = remainingCount < previousCount;
        _itemModel.CurrentStackCount = remainingCount;

        if (_itemModel.CurrentStackCount <= 0)
            Destroy(gameObject);

        return pickedUpAny;
    }

    private bool TryPickupWeapon()
    {
        if (_itemModel is not WeaponModel weaponModel)
            return false;

        if (weaponModel.CurrentStackCount != 1)
            return false;

        string instanceId = gameObject.GetInstanceID().ToString();
        weaponModel.InstanceId = instanceId;

        if (!InventoryManager.Instance.TryAddWeapon(weaponModel))
            return false;

        Destroy(gameObject);
        return true;
    }
}
