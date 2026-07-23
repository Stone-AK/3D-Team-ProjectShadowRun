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

    [Header("Item Info")]
    [SerializeField] private ItemInfoUI ItemInfo;

    private PlayerStatusViewModel _viewModel;

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
        if (_viewModel == null)
            return;

        _viewModel.PropertyChanged -= OnPropertyChanged;
    }
}
