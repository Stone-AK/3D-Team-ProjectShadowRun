using UnityEngine;

public class PlayerStatus : MonoBehaviour, IDamageable
{
    public static PlayerStatus Instance { get; set; }

    [Header("Initial Status")]
    [SerializeField] private float MaxHP = 100f;
    [SerializeField] private float MaxStamina = 100f;

    public PlayerModel Model { get; set; }
    public PlayerStatusViewModel ViewModel { get; set; }

    public event System.Action<float> HealthChanged;
    public event System.Action<float> StaminaChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitPlayerStatus();
    }

    private void InitPlayerStatus()
    {
        Model = SaveManager.Instance.LoadPlayerData();

        if (Model == null)
        {
            Model = new PlayerModel();
        }

        NormalizeLoadedInventory();

        if (Model.MaxHP <= 0f)
        {
            Model.MaxHP = MaxHP;
        }

        if (Model.MaxStamina <= 0f)
        {
            Model.MaxStamina = MaxStamina;
        }

        ViewModel = new PlayerStatusViewModel();
        ViewModel.InitPlayerViewModel(Model);
    }

    private void NormalizeLoadedInventory()
    {
        if (Model.InventoryItems == null)
            Model.InventoryItems = new System.Collections.Generic.List<ItemModel>();

        foreach (ItemModel inventoryItem in Model.InventoryItems)
        {
            if (inventoryItem == null)
                continue;

            if (string.IsNullOrWhiteSpace(inventoryItem.InstanceId))
                inventoryItem.InstanceId = System.Guid.NewGuid().ToString();
        }

        System.Collections.Generic.HashSet<string> connectedInstanceIds = new System.Collections.Generic.HashSet<string>();

        Model.QuickSlotOne = FindInventoryItem(Model.QuickSlotOne, connectedInstanceIds);
        Model.QuickSlotTwo = FindInventoryItem(Model.QuickSlotTwo, connectedInstanceIds);
        Model.QuickSlotThree = FindInventoryItem(Model.QuickSlotThree, connectedInstanceIds);
    }

    private ItemModel FindInventoryItem(ItemModel quickSlotItem, System.Collections.Generic.HashSet<string> connectedInstanceIds)
    {
        if (quickSlotItem == null)
            return null;

        if (!string.IsNullOrWhiteSpace(quickSlotItem.InstanceId))
        {
            foreach (ItemModel inventoryItem in Model.InventoryItems)
            {
                if (inventoryItem == null || inventoryItem.InstanceId != quickSlotItem.InstanceId)
                    continue;

                if (!connectedInstanceIds.Add(inventoryItem.InstanceId))
                    return null;

                return inventoryItem;
            }
        }

        foreach (ItemModel inventoryItem in Model.InventoryItems)
        {
            if (inventoryItem == null || inventoryItem.ItemId != quickSlotItem.ItemId)
                continue;

            if (!connectedInstanceIds.Add(inventoryItem.InstanceId))
                continue;

            return inventoryItem;
        }

        return null;
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0f || Model.CurrentHP <= 0f)
            return;

        Debug.Log($"[PlayerStatus 진단] 피해 적용 전 - Damage: {damage}, CurrentHP: {Model.CurrentHP}, InventoryCount: {Model.InventoryItems.Count}");

        Model.CurrentHP = Mathf.Clamp(Model.CurrentHP - damage, 0f, Model.MaxHP);
        HealthChanged?.Invoke(Model.CurrentHP);

        Debug.Log(
            $"[PlayerStatus 진단] 피해 적용 후 - CurrentHP: {Model.CurrentHP}, " +
            $"InventoryCount: {Model.InventoryItems.Count}"
        );

        if (Model.CurrentHP <= 0f)
            Die();
    }

    private void Die()
    {
        Debug.LogError($"[PlayerStatus 진단] Die 호출 - CurrentHP: {Model.CurrentHP}, InventoryCount: {Model.InventoryItems.Count} {System.Environment.StackTrace}");

        InventoryManager.Instance.ClearInventory();

        RestoreStatus();

        GameManager.Instance.ReturnToOutGame();
    }

    public void RecoverHP(float healAmount)
    {
        if (healAmount <= 0f || Model.CurrentHP >= Model.MaxHP)
            return;

        Model.CurrentHP = Mathf.Clamp(Model.CurrentHP + healAmount, 0f, Model.MaxHP);
        HealthChanged?.Invoke(Model.CurrentHP);
    }

    public void UseStamina(float amount)
    {
        if (amount <= 0f || Model.CurrentStamina <= 0f)
            return;

        Model.CurrentStamina = Mathf.Clamp(Model.CurrentStamina - amount, 0f, Model.MaxStamina);
        StaminaChanged?.Invoke(Model.CurrentStamina);
    }

    public void RecoverStamina(float amount)
    {
        if (amount <= 0f || Model.CurrentStamina >= Model.MaxStamina)
            return;

        Model.CurrentStamina = Mathf.Clamp(Model.CurrentStamina + amount, 0f, Model.MaxStamina);
        StaminaChanged?.Invoke(Model.CurrentStamina);
    }

    private void RestoreStatus()
    {
        if (Model == null)
            return;

        Model.CurrentHP = Model.MaxHP;
        Model.CurrentStamina = Model.MaxStamina;

        HealthChanged?.Invoke(Model.CurrentHP);
        StaminaChanged?.Invoke(Model.CurrentStamina);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
