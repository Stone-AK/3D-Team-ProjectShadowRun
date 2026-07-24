using UnityEngine;

public class FieldItem : MonoBehaviour
{
    public ItemModel ItemModel { get; private set; }
    public ItemData ItemData { get; private set; }

    public void Initialize(ItemModel generatedModel, ItemData itemData)
    {
        ItemModel = generatedModel;
        ItemData = itemData;
    }

    public bool TryPickup()
    {
        if (InventoryManager.Instance == null || ItemModel == null || ItemData == null)
            return false;

        if (ItemData is WeaponData)
            return TryPickupWeapon();

        int previousCount = ItemModel.CurrentStackCount;
        int remainingCount = InventoryManager.Instance.TryAddItem(ItemData, previousCount);
        bool pickedUpAny = remainingCount < previousCount;
        ItemModel.CurrentStackCount = remainingCount;

        if (ItemModel.CurrentStackCount <= 0)
            Destroy(gameObject);

        return pickedUpAny;
    }

    private bool TryPickupWeapon()
    {
        if (ItemModel is not WeaponModel weaponModel)
            return false;

        if (weaponModel.CurrentStackCount != 1)
            return false;

        if (!InventoryManager.Instance.TryAddWeapon(weaponModel))
            return false;

        Destroy(gameObject);
        return true;
    }
}
