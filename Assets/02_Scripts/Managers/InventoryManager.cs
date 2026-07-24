using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [SerializeField] private int MaxSlotCount = 30;
    private const int QuickSlotCount = 3;

    public int PlayerCredit
    {
        get => PlayerStatus.Instance.Model.CurrentCredit;
        set
        {
            if (PlayerStatus.Instance.Model.CurrentCredit == value)
                return;

            PlayerStatus.Instance.Model.CurrentCredit = value;
            OnCreditChanged?.Invoke();
        }
    }
    public event Action OnCreditChanged;

    public IReadOnlyList<ItemModel> QuickSlotList => new ItemModel[]
    {
        PlayerStatus.Instance.Model.QuickSlotOne,
        PlayerStatus.Instance.Model.QuickSlotTwo,
        PlayerStatus.Instance.Model.QuickSlotThree
    };

    public event Action OnQuickSlotChanged;

    private List<ItemModel> InventoryItems => PlayerStatus.Instance.Model.InventoryItems;

    public ItemModel EquippedHead => PlayerStatus.Instance.Model.EquippedHelmet;
    public ItemModel EquippedBody => PlayerStatus.Instance.Model.EquippedArmor;
    public event Action OnEquipmentChanged;

    public IReadOnlyList<ItemModel> ItemList => InventoryItems;
    public event Action OnInventoryChanged;

    private int _selectedQuickSlotIndex = -1;
    public int SelectedQuickSlotIndex => _selectedQuickSlotIndex;
    public event Action OnSelectedQuickSlotChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public int TryAddItem(ItemData item, int count)
    {
        if (item == null)
            return count;

        if (count <= 0)
            return count;

        if (item.MaxStackCount <= 0)
            return count;

        int remainCount = count;

        for (int i = 0; i < InventoryItems.Count; i++)
        {
            ItemModel stack = InventoryItems[i];

            if (stack.ItemId != item.Id)
                continue;

            if (stack.CurrentStackCount >= item.MaxStackCount)
                continue;

            int addableCount = item.MaxStackCount - stack.CurrentStackCount;
            int addCount = Mathf.Min(addableCount, remainCount);

            stack.CurrentStackCount += addCount;
            remainCount -= addCount;

            if (remainCount <= 0)
            {
                OnInventoryChanged?.Invoke();
                return 0;
            }
        }

        while (remainCount > 0)
        {
            if (InventoryItems.Count >= MaxSlotCount)
            {
                OnInventoryChanged?.Invoke();
                return remainCount;
            }

            int addCount = Mathf.Min(item.MaxStackCount, remainCount);

            InventoryItems.Add(new ItemModel{ItemId = item.Id,CurrentStackCount = addCount});

            remainCount -= addCount;
        }

        OnInventoryChanged?.Invoke();
        return 0;
    }

    public bool TryRemoveItem(string itemId, int count)
    {
        if (string.IsNullOrEmpty(itemId))
            return false;

        if (count <= 0)
            return false;

        if (!HasItem(itemId, count))
            return false;

        int remainCount = count;

        for (int i = InventoryItems.Count - 1; i >= 0; i--)
        {
            ItemModel stack = InventoryItems[i];

            if (stack.ItemId != itemId)
                continue;

            int removeCount = Mathf.Min(stack.CurrentStackCount, remainCount);

            stack.CurrentStackCount -= removeCount;
            remainCount -= removeCount;

            if (stack.CurrentStackCount <= 0)
            {
                InventoryItems.RemoveAt(i);

                UnregisterItemFromQuickSlots(stack);
                UnregisterItemFromEquipmentSlots(stack);
            }

            if (remainCount <= 0)
            {
                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool HasItem(string itemId, int count)
    {
        if (string.IsNullOrEmpty(itemId))
            return false;

        if (count <= 0)
            return false;

        int totalCount = 0;

        foreach (ItemModel stack in InventoryItems)
        {
            if (stack.ItemId != itemId)
                continue;

            totalCount += stack.CurrentStackCount;

            if (totalCount >= count)
                return true;
        }

        return false;
    }

    public int GetItemCount(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return 0;

        int totalCount = 0;

        foreach (ItemModel stack in InventoryItems)
        {
            if (stack.ItemId != itemId)
                continue;

            totalCount += stack.CurrentStackCount;
        }

        return totalCount;
    }

    public ItemModel GetItemModel(int index)
    {
        if (index < 0 || index >= InventoryItems.Count)
            return null;

        return InventoryItems[index];
    }

    public ItemModel GetEquippedItem(EquipmentSlotType slotType)
    {
        switch (slotType)
        {
            case EquipmentSlotType.Head:
                return EquippedHead;

            case EquipmentSlotType.Body:
                return EquippedBody;

            default:
                return null;
        }
    }

    public bool TryUseItem(int slotIndex)
    {
        ItemModel stack = GetItemModel(slotIndex);

        if (!IsValidStack(stack))
        {
            Debug.LogWarning($"사용할 수 없는 슬롯입니다. Index: {slotIndex}");
            return false;
        }

        string itemType = DataManager.Instance.GetItemData(stack.ItemId).ItemType;

        switch (itemType)
        {
            case "Consumable":
                return TryUseConsumable(stack);

            case "Equipment":
                return TryEquipItem(stack);

            default:
                Debug.LogWarning($"사용할 수 없는 아이템 타입입니다. Item: {DataManager.Instance.GetItemData(stack.ItemId).Name}, Type: {itemType}");
                return false;
        }
    }

    public bool TryDropItem(int slotIndex, int count = 1)
    {
        ItemModel itemModel = GetItemModel(slotIndex);

        if (!IsValidStack(itemModel))
            return false;

        if (count <= 0)
            return false;

        if (itemModel.CurrentStackCount < count)
            return false;

        bool wasRegistered = IsRegisteredInQuickSlot(itemModel);

        bool wasEquipped = IsEquippedItem(itemModel);

        itemModel.CurrentStackCount -= count;

        if (itemModel.CurrentStackCount <= 0)
        {
            InventoryItems.Remove(itemModel);

            UnregisterItemFromQuickSlots(itemModel);
            UnregisterItemFromEquipmentSlots(itemModel);
        }
        else
        {
            if (wasRegistered)
                OnQuickSlotChanged?.Invoke();

            if (wasEquipped)
                OnEquipmentChanged?.Invoke();
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool TryRegisterQuickSlot(int inventorySlotIndex, int quickSlotIndex)
    {
        if (quickSlotIndex < 0 || quickSlotIndex >= QuickSlotCount)
        {
            Debug.LogWarning($"잘못된 퀵슬롯 인덱스입니다. Index: {quickSlotIndex}");
            return false;
        }

        ItemModel itemModel = GetItemModel(inventorySlotIndex);

        if (!IsValidStack(itemModel))
        {
            Debug.LogWarning($"퀵슬롯에 등록할 수 없는 인벤토리 슬롯입니다. Index: {inventorySlotIndex}");
            return false;
        }

        if (!CanRegisterQuickSlot(itemModel))
        {
            Debug.LogWarning($"퀵슬롯 등록 불가 아이템입니다. Item: {DataManager.Instance.GetItemData(itemModel.ItemId).Name}, Type: {DataManager.Instance.GetItemData(itemModel.ItemId).ItemType}");
            return false;
        }

        bool selectedItemMoved = false;

        for (int i = 0; i < QuickSlotCount; i++)
        {
            if (i == quickSlotIndex)
                continue;

            if (!IsSameItemModel(GetQuickSlot(i), itemModel))
                continue;

            SetQuickSlot(i, null);

            if (_selectedQuickSlotIndex == i)
            {
                _selectedQuickSlotIndex = quickSlotIndex;
                selectedItemMoved = true;
            }
        }

        bool selectedSlotReplaced = _selectedQuickSlotIndex == quickSlotIndex;

        SetQuickSlot(quickSlotIndex, itemModel);

        OnQuickSlotChanged?.Invoke();

        if (selectedItemMoved || selectedSlotReplaced)
            OnSelectedQuickSlotChanged?.Invoke();

        Debug.Log($"퀵슬롯 등록 완료: QuickSlot {quickSlotIndex}, ItemId: {itemModel.ItemId}");

        return true;
    }

    public bool TryRegisterQuickSlot(int inventorySlotIndex)
    {
        ItemModel itemModel = GetItemModel(inventorySlotIndex);

        if (!IsValidStack(itemModel))
            return false;

        if (!CanRegisterQuickSlot(itemModel))
            return false;

        for (int i = 0; i < QuickSlotCount; i++)
        {
            if (!IsSameItemModel(GetQuickSlot(i), itemModel))
                continue;
            return true;
        }

        for (int i = 0; i < QuickSlotCount; i++)
        {
            if (IsValidStack(GetQuickSlot(i)))
                continue;

            return TryRegisterQuickSlot(inventorySlotIndex, i);
        }

        return false;
    }

    /*private bool TryUseConsumable( ItemModel stack )
    {
        Debug.Log($"소모품 사용 요청: {DataManager.Instance.GetItemData(stack.ItemId).Name}");

        // TODO: UseItemType / UseItemParameterList 기준으로 효과 적용
        bool removed = TryRemoveItem(stack.ItemId, 1);

        if (removed)
            OnQuickSlotChanged?.Invoke();

        return removed;
    }*/

    public event Action<ItemData> OnConsumableUsed;
    private bool TryUseConsumable( ItemModel stack )
    {
        if (!IsValidStack(stack))
        {
            return false;
        }

        ItemData itemData = DataManager.Instance.GetItemData(stack.ItemId);
        if (itemData == null)
        {
            return false;
        }

        Debug.Log("소모품 사용 요청: " + itemData.Name);

        // 파라미터 파싱 (배열 분할)
        itemData.ParseUseItemParameters();

        // 구독자들에게 ItemData 전체 전달
        /*if (OnConsumableUsed != null)
        {
            OnConsumableUsed(itemData);
        }*/
        OnConsumableUsed?.Invoke(itemData);

        // 인벤토리 수량 차감
        bool removed = TryRemoveItem(stack.ItemId, 1);

        if (removed)
        {
            /*if (OnQuickSlotChanged != null)
            {
                OnQuickSlotChanged();
            }*/
            OnQuickSlotChanged?.Invoke();
        }

        return removed;
    }

    public bool TryEquipItem(int inventorySlotIndex)
    {
        ItemModel itemModel = GetItemModel(inventorySlotIndex);
        return TryEquipItem(itemModel);
    }

    public bool TryEquipItem(int inventorySlotIndex, EquipmentSlotType targetSlotType)
    {
        ItemModel itemModel = GetItemModel(inventorySlotIndex);

        if (!IsValidStack(itemModel))
            return false;

        EquipmentSlotType itemSlotType = GetEquipmentSlotType(itemModel);

        if (itemSlotType != targetSlotType)
        {
            Debug.LogWarning(
                $"장비 부위가 맞지 않습니다. " +
                $"ItemId: {itemModel.ItemId}, " +
                $"ItemSlot: {itemSlotType}, " +
                $"TargetSlot: {targetSlotType}"
            );
            return false;
        }

        return TryEquipItem(itemModel);
    }

    private bool TryEquipItem(ItemModel itemModel)
    {
        if (!IsValidStack(itemModel))
            return false;

        if (!InventoryItems.Contains(itemModel))
            return false;

        if (!CanEquipItem(itemModel))
        {
            Debug.LogWarning($"장착할 수 없는 아이템입니다. ItemId: {itemModel.ItemId}");
            return false;
        }

        if (itemModel.CurrentStackCount != 1)
        {
            Debug.LogWarning($"장비 아이템의 수량은 1이어야 합니다. ItemId: {itemModel.ItemId}, Count: {itemModel.CurrentStackCount}");
            return false;
        }

        EquipmentSlotType slotType = GetEquipmentSlotType(itemModel);

        switch (slotType)
        {
            case EquipmentSlotType.Head:
                PlayerStatus.Instance.Model.EquippedHelmet = itemModel;
                break;

            case EquipmentSlotType.Body:
                PlayerStatus.Instance.Model.EquippedArmor = itemModel;
                break;

            default:
                Debug.LogWarning($"장비 슬롯을 판별할 수 없는 아이템입니다. ItemId: {itemModel.ItemId}");
                return false;
        }

        OnEquipmentChanged?.Invoke();

        Debug.Log($"장비 장착 완료: {DataManager.Instance.GetItemData(itemModel.ItemId).Name}");
        return true;
    }

    public bool TryUnequipHead()
    {
        return TryUnequipItem(EquipmentSlotType.Head);
    }

    public bool TryUnequipBody()
    {
        return TryUnequipItem(EquipmentSlotType.Body);
    }

    public bool TryUnequipItem(EquipmentSlotType slotType)
    {
        switch (slotType)
        {
            case EquipmentSlotType.Head:
                return TryUnequipSlot(slotType);
            case EquipmentSlotType.Body:
                return TryUnequipSlot(slotType);

            default:
                return false;
        }
    }

    private bool TryUnequipSlot(EquipmentSlotType slotType)
    {
        ItemModel equippedItem = GetEquippedItem(slotType);

        if (!IsValidStack(equippedItem))
            return false;

        ItemModel itemToUnequip = equippedItem;

        if (slotType == EquipmentSlotType.Head)
            PlayerStatus.Instance.Model.EquippedHelmet = null;
        else
            PlayerStatus.Instance.Model.EquippedArmor = null;

        OnEquipmentChanged?.Invoke();

        Debug.Log(
            $"장비 해제 완료: " +
            $"{DataManager.Instance.GetItemData(itemToUnequip.ItemId).Name}"
        );

        return true;
    }

    private bool CanRegisterQuickSlot(ItemModel item)
    {
        if (item == null)
            return false;

        return DataManager.Instance.GetItemData(item.ItemId).ItemType == "Weapon" || 
            DataManager.Instance.GetItemData(item.ItemId).ItemType == "Consumable";
    }

    private bool CanEquipItem(ItemModel item)
    {
        if (item == null)
            return false;

        ItemData itemData = DataManager.Instance.GetItemData(item.ItemId);
        return itemData != null && itemData.ItemType == "Equipment";
    }

    private EquipmentSlotType GetEquipmentSlotType(ItemModel item)
    {
        if (item == null || string.IsNullOrEmpty(item.ItemId))
            return EquipmentSlotType.None;

        if (item.ItemId.Contains("Equip_Helmet"))
            return EquipmentSlotType.Head;

        if (item.ItemId.Contains("Equip_Armor"))
            return EquipmentSlotType.Body;

        return EquipmentSlotType.None;
    }

    private bool IsValidStack(ItemModel stack)
    {
        return stack != null && stack.CurrentStackCount > 0;
    }

    public bool TrySelectQuickSlot(int quickSlotIndex)
    {
        if (quickSlotIndex < 0 || quickSlotIndex >= QuickSlotCount)
            return false;

        ItemModel stack = GetQuickSlot(quickSlotIndex);

        if (!IsValidStack(stack))
            return false;

        if (_selectedQuickSlotIndex == quickSlotIndex)
        {
            _selectedQuickSlotIndex = -1;
        }
        else
        {
            _selectedQuickSlotIndex = quickSlotIndex;
            Debug.Log($"퀵슬롯 선택: {quickSlotIndex}, Item: {DataManager.Instance.GetItemData(stack.ItemId).Name}");
        }

        OnSelectedQuickSlotChanged?.Invoke();

        return true;
    }

    public bool TryUseSelectedQuickSlotItem()
    {
        if (_selectedQuickSlotIndex < 0 || _selectedQuickSlotIndex >= QuickSlotCount)
            return false;

        ItemModel stack = GetQuickSlot(_selectedQuickSlotIndex);

        if (!IsValidStack(stack))
            return false;

        switch (DataManager.Instance.GetItemData(stack.ItemId).ItemType)
        {
            case "Weapon":
                Debug.Log($"선택 무기 사용 요청: {DataManager.Instance.GetItemData(stack.ItemId).Name}");
                // TODO: 장착/발사 요청 연결
                return true;

            case "Consumable":
                Debug.Log($"선택 소모품 사용 요청: {DataManager.Instance.GetItemData(stack.ItemId).Name}");
                return TryUseConsumable(stack);

            default:
                Debug.LogWarning($"퀵슬롯에서 사용할 수 없는 아이템 타입입니다. Type: {DataManager.Instance.GetItemData(stack.ItemId).ItemType}");
                return false;
        }
    }

    public bool TryAddWeapon(WeaponData weaponData)
    {
        return TryAddWeapon(weaponData, GetInstanceID().ToString());
    }

    public bool TryAddWeapon(WeaponData weaponData, string instanceId)
    {
        if (weaponData == null)
            return false;

        if (weaponData.ItemType != "Weapon")
            return false;

        if (string.IsNullOrWhiteSpace(instanceId))
        {
            Debug.LogWarning("무기 InstanceId가 없습니다.");
            return false;
        }

        WeaponModel weaponModel = new WeaponModel
        {
            InstanceId = instanceId,
            ItemId = weaponData.Id,
            CurrentStackCount = 1,
            CurrentAmmo = 0,
            CurrentDurability = weaponData.MaxDurability,
            AttachedParts = new List<ItemModel>()
        };

        return TryAddWeapon(weaponModel);
    }

    public bool TryAddWeapon(WeaponModel weaponModel)
    {
        if (weaponModel == null)
            return false;

        if (string.IsNullOrWhiteSpace(weaponModel.InstanceId))
            return false;

        if (weaponModel.CurrentStackCount != 1)
            return false;

        ItemData itemData = DataManager.Instance.GetItemData(weaponModel.ItemId);

        if (itemData is not WeaponData)
            return false;

        if (InventoryItems.Count >= MaxSlotCount)
            return false;

        InventoryItems.Add(weaponModel);

        TryRegisterWeaponToEmptyQuickSlot(weaponModel);

        OnInventoryChanged?.Invoke();

        return true;
    }

    private bool TryRegisterWeaponToEmptyQuickSlot(ItemModel weaponStack)
    {
        if (!IsValidStack(weaponStack))
            return false;

        if (DataManager.Instance.GetItemData(weaponStack.ItemId).ItemType != "Weapon")
            return false;

        for (int i = 0; i < QuickSlotCount; i++)
        {
            if (IsValidStack(GetQuickSlot(i)))
                continue;

            SetQuickSlot(i, weaponStack);

            OnQuickSlotChanged?.Invoke();

            if (_selectedQuickSlotIndex < 0)
            {
                _selectedQuickSlotIndex = i;
                OnSelectedQuickSlotChanged?.Invoke();
            }

            Debug.Log($"무기 자동 퀵슬롯 등록: QuickSlot {i}, Item: {DataManager.Instance.GetItemData(weaponStack.ItemId).Name}");

            return true;
        }

        return false;
    }

    public ItemModel GetSelectedQuickSlotStack()
    {
        if (_selectedQuickSlotIndex < 0 || _selectedQuickSlotIndex >= QuickSlotCount)
        {
            return null;
        }

        ItemModel stack = GetQuickSlot(_selectedQuickSlotIndex);

        if (!IsValidStack(stack))
            return null;

        return stack;
    }

    private bool IsSameItemModel(ItemModel first, ItemModel second)
    {
        if (ReferenceEquals(first, second))
            return true;

        if (first == null || second == null)
            return false;

        if (string.IsNullOrWhiteSpace(first.InstanceId) || string.IsNullOrWhiteSpace(second.InstanceId))
            return false;

        return first.InstanceId == second.InstanceId;
    }

    private bool IsRegisteredInQuickSlot(ItemModel itemModel)
    {
        for (int i = 0; i < QuickSlotCount; i++)
        {
            if (IsSameItemModel(GetQuickSlot(i), itemModel))
                return true;
        }

        return false;
    }

    private bool IsEquippedItem(ItemModel itemModel)
    {
        return IsSameItemModel(EquippedHead, itemModel) || IsSameItemModel(EquippedBody, itemModel);
    }

    private void UnregisterItemFromEquipmentSlots(ItemModel itemModel)
    {
        bool equipmentChanged = false;

        if (IsSameItemModel(EquippedHead, itemModel))
        {
            PlayerStatus.Instance.Model.EquippedHelmet = null;
            equipmentChanged = true;
        }

        if (IsSameItemModel(EquippedBody, itemModel))
        {
            PlayerStatus.Instance.Model.EquippedArmor = null;
            equipmentChanged = true;
        }

        if (equipmentChanged)
            OnEquipmentChanged?.Invoke();
    }

    private void UnregisterItemFromQuickSlots(ItemModel itemModel)
    {
        bool quickSlotChanged = false;
        bool selectedQuickSlotChanged = false;

        for (int i = 0; i < QuickSlotCount; i++)
        {
            if (!IsSameItemModel(GetQuickSlot(i), itemModel))
                continue;

            SetQuickSlot(i, null);
            quickSlotChanged = true;

            if (_selectedQuickSlotIndex == i)
            {
                _selectedQuickSlotIndex = -1;
                selectedQuickSlotChanged = true;
            }
        }

        if (quickSlotChanged)
            OnQuickSlotChanged?.Invoke();

        if (selectedQuickSlotChanged)
            OnSelectedQuickSlotChanged?.Invoke();
    }

    public bool TryUnregisterQuickSlot(int quickSlotIndex)
    {
        if (quickSlotIndex < 0 || quickSlotIndex >= QuickSlotCount)
            return false;

        ItemModel itemModel = GetQuickSlot(quickSlotIndex);

        if (!IsValidStack(itemModel))
            return false;

        SetQuickSlot(quickSlotIndex, null);

        bool selectedSlotUnregistered = (_selectedQuickSlotIndex == quickSlotIndex);

        if (selectedSlotUnregistered)
            _selectedQuickSlotIndex = -1;

        OnQuickSlotChanged?.Invoke();

        if (selectedSlotUnregistered)
            OnSelectedQuickSlotChanged?.Invoke();

        return true;
    }

    private ItemModel GetQuickSlot(int quickSlotIndex)
    {
        switch(quickSlotIndex)
        {
            case 0:
                return PlayerStatus.Instance.Model.QuickSlotOne;
            case 1:
                return PlayerStatus.Instance.Model.QuickSlotTwo;
            case 2:
                return PlayerStatus.Instance.Model.QuickSlotThree;
        }
        return null;
    }

    private void SetQuickSlot(int quickSlotIndex, ItemModel itemModel)
    {
        switch (quickSlotIndex)
        {
            case 0:
                PlayerStatus.Instance.Model.QuickSlotOne = itemModel;
                break;

            case 1:
                PlayerStatus.Instance.Model.QuickSlotTwo = itemModel;
                break;

            case 2:
                PlayerStatus.Instance.Model.QuickSlotThree = itemModel;
                break;
        }
    }

    public WeaponType ReturnWeaponTypeFromQuickSlotID()
    {
        if (SelectedQuickSlotIndex > -1)
        {
            switch (GetQuickSlot(SelectedQuickSlotIndex).ItemId)
            {
                case string value when value.Contains("Weapon_AR"):
                    return WeaponType.Rifle;
                case string value when value.Contains("Weapon_Pistol"):
                    return WeaponType.Pistol;
                default:
                    return WeaponType.None;
            }
        }

        return WeaponType.None;
    }
}
