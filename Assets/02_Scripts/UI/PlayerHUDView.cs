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

    private PlayerStatusViewModel _viewModel;
    private PlayerWeaponController _weaponController;

    public void BindViewModel(PlayerStatusViewModel viewModel)
    {
        _viewModel = viewModel;

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

    private void OnDestroy()
    {
        if (_viewModel != null)
            _viewModel.PropertyChanged -= OnPropertyChanged;

        if (_weaponController != null)
        {
            _weaponController.OnAmmoChanged -= UpdateAmmoText;
            _weaponController.OnReloadStateChanged -= UpdateReloadState;
        }
    }
}
