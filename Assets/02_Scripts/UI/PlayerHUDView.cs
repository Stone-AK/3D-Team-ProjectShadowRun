using System.ComponentModel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUDView : UIBase
{
    [Header("HP")]
    [SerializeField] private Slider HPSlider;
    [SerializeField] private TMP_Text Text_HP;

    [Header("Stamina")]
    [SerializeField] private Slider StaminaSlider;

    [Header("Ammo")]
    [SerializeField] private TMP_Text Text_Ammo;

    [Header("Item Info")]
    [SerializeField] private ItemInfoUI ItemInfo;

    [Header("Buff")]
    [SerializeField] private Transform BuffSlotContainer;
    [SerializeField] private BuffSlotUI BuffSlotPrefab;
    [SerializeField] private Sprite IconDotHeal;
    [SerializeField] private Sprite IconShield;
    [SerializeField] private Sprite IconSpeed;

    private PlayerStatusViewModel _viewModel;
    private PlayerWeaponController _weaponController;
    private PlayerStatus _playerStatus;
    private ActivateMedicine _activateMedicine;
    private BuffSlotUI _dotHealBuffSlot;
    private BuffSlotUI _temporaryHPBuffSlot;
    private BuffSlotUI _speedBuffSlot;

    public void BindViewModel(PlayerStatusViewModel viewModel)
    {
        if (_viewModel != null)
            _viewModel.PropertyChanged -= OnPropertyChanged;

        _viewModel = viewModel;

        if (_viewModel == null)
            return;

        _viewModel.PropertyChanged += OnPropertyChanged;

        _viewModel.InvokeOnceOnInit();
    }

    public void BindItemInfoUI(PlayerItemInteractor itemInteractor)
    {
        if (ItemInfo == null)
        {
            Debug.LogWarning("HudUI에 ItemInfoUI가 연결되지 않았습니다.");
            return;
        }

        ItemInfo.BindItemInteractor(itemInteractor);
    }

    public void BindWeaponController(PlayerWeaponController weaponController)
    {
        if (_weaponController != null)
        {
            _weaponController.OnAmmoChanged -= UpdateAmmoText;
            _weaponController.OnReloadStateChanged -= UpdateReloadState;
        }

        _weaponController = weaponController;

        if (_weaponController == null)
            return;

        _weaponController.OnAmmoChanged += UpdateAmmoText;
        _weaponController.OnReloadStateChanged += UpdateReloadState;

        if (_weaponController.IsReloading)
            UpdateReloadState(true);
        else
            UpdateAmmoText(_weaponController.CurrentAmmo, _weaponController.CurrentReserveAmmo);
    }

    public void BindBuffStatus(PlayerStatus playerStatus, ActivateMedicine activateMedicine)
    {
        if (_playerStatus != null)
            _playerStatus.TemporaryHealthChanged -= UpdateTemporaryHPBuff;

        if (_activateMedicine != null)
        {
            _activateMedicine.DotHealBuffChanged -= UpdateDotHealBuff;
            _activateMedicine.SpeedBuffChanged -= UpdateSpeedBuff;
        }

        _playerStatus = playerStatus;
        _activateMedicine = activateMedicine;

        UpdateDotHealBuff(0f);
        UpdateTemporaryHPBuff(0f);
        UpdateSpeedBuff(0f);

        if (_playerStatus != null)
        {
            _playerStatus.TemporaryHealthChanged += UpdateTemporaryHPBuff;
            UpdateTemporaryHPBuff(_playerStatus.TemporaryHP);
        }

        if (_activateMedicine != null)
        {
            _activateMedicine.DotHealBuffChanged += UpdateDotHealBuff;
            _activateMedicine.SpeedBuffChanged += UpdateSpeedBuff;
        }
    }

    private void UpdateAmmoText(int currentAmmo, int reserveAmmo)
    {
        if (Text_Ammo == null)
            return;

        Text_Ammo.text = $"{currentAmmo} / {reserveAmmo}";
    }

    private void UpdateReloadState(bool isReloading)
    {
        if (Text_Ammo == null)
            return;

        if (isReloading)
            Text_Ammo.text = "Reloading...";
        else if (_weaponController != null)
            UpdateAmmoText(_weaponController.CurrentAmmo, _weaponController.CurrentReserveAmmo);
    }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs eventChangeProperty)
    {
        switch (eventChangeProperty.PropertyName)
        {
            case nameof(PlayerStatusViewModel.HPRatio):
                HPSlider.value = _viewModel.HPRatio;
                break;

            case nameof(PlayerStatusViewModel.CurrentHP):
                UpdateHPText();
                break;

            case nameof(PlayerStatusViewModel.MaxHP):
                UpdateHPText();
                break;

            case nameof(PlayerStatusViewModel.StaminaRatio):
                StaminaSlider.value = _viewModel.StaminaRatio;
                break;
        }
    }

    private void UpdateHPText()
    {
        Text_HP.text = $"{_viewModel.CurrentHP:0} / {_viewModel.MaxHP:0}";
    }

    public void UpdateDotHealBuff(float remainTime)
    {
        UpdateBuffSlot(ref _dotHealBuffSlot, IconDotHeal, remainTime);
    }

    public void UpdateTemporaryHPBuff(float temporaryHP)
    {
        UpdateBuffSlot(ref _temporaryHPBuffSlot, IconShield, temporaryHP);
    }

    public void UpdateSpeedBuff(float remainTime)
    {
        UpdateBuffSlot(ref _speedBuffSlot, IconSpeed, remainTime);
    }

    private void UpdateBuffSlot(ref BuffSlotUI buffSlot, Sprite icon, float remainValue)
    {
        if (remainValue <= 0f)
        {
            if (buffSlot != null)
                Destroy(buffSlot.gameObject);

            buffSlot = null;
            return;
        }

        if (buffSlot == null)
        {
            if (BuffSlotPrefab == null || BuffSlotContainer == null)
                return;

            buffSlot = Instantiate(BuffSlotPrefab, BuffSlotContainer);
        }

        buffSlot.Setup(icon, remainValue);
    }

    private void OnDestroy()
    {
        if (_viewModel != null)
            _viewModel.PropertyChanged -= OnPropertyChanged;

        if (_weaponController != null)
        {
            _weaponController.OnAmmoChanged -= UpdateAmmoText;
            _weaponController.OnReloadStateChanged -= UpdateReloadState;
        }

        if (_playerStatus != null)
            _playerStatus.TemporaryHealthChanged -= UpdateTemporaryHPBuff;

        if (_activateMedicine != null)
        {
            _activateMedicine.DotHealBuffChanged -= UpdateDotHealBuff;
            _activateMedicine.SpeedBuffChanged -= UpdateSpeedBuff;
        }
    }
}
