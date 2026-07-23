
public class PlayerStatusViewModel : ViewModelBase
{
    private PlayerModel _model;
    private PlayerStatus _playerStatus;

    public float MaxHP => _model.MaxHP;
    public float CurrentHP => _model.CurrentHP;

    public float MaxStamina => _model.MaxStamina;
    public float CurrentStamina => _model.CurrentStamina;

    public float HPRatio
    {
        get
        {
            if (MaxHP <= 0f)
                return 0f;

            return CurrentHP / MaxHP;
        }
    }

    public float StaminaRatio
    {
        get
        {
            if (MaxStamina <= 0f)
                return 0f;

            return CurrentStamina / MaxStamina;
        }
    }

    public void InitPlayerViewModel(PlayerModel model)
    {
        if (_playerStatus != null)
        {
            _playerStatus.HealthChanged -= OnHealthChanged;
            _playerStatus.StaminaChanged -= OnStaminaChanged;
        }

        _model = model;
        _playerStatus = PlayerStatus.Instance;

        if (_playerStatus != null)
        {
            _playerStatus.HealthChanged += OnHealthChanged;
            _playerStatus.StaminaChanged += OnStaminaChanged;
        }
    }

    private void OnHealthChanged(float currentHP)
    {
        OnPropertyChanged(nameof(CurrentHP));
        OnPropertyChanged(nameof(HPRatio));
    }

    private void OnStaminaChanged(float currentStamina)
    {
        OnPropertyChanged(nameof(CurrentStamina));
        OnPropertyChanged(nameof(StaminaRatio));
    }

    public void InvokeOnceOnInit()
    {
        OnPropertyChanged(nameof(MaxHP));
        OnPropertyChanged(nameof(CurrentHP));
        OnPropertyChanged(nameof(HPRatio));

        OnPropertyChanged(nameof(MaxStamina));
        OnPropertyChanged(nameof(CurrentStamina));
        OnPropertyChanged(nameof(StaminaRatio));
    }

}
