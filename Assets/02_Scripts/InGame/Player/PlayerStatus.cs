using UnityEngine;

public class PlayerStatus : MonoBehaviour
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

    public void TakeDamage(float damage)
    {
        if (damage <= 0f || Model.CurrentHP <= 0f)
            return;

        Model.CurrentHP = Mathf.Clamp(Model.CurrentHP - damage, 0f, Model.MaxHP);
        HealthChanged?.Invoke(Model.CurrentHP);
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

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
