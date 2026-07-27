using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuffSlotUI : MonoBehaviour
{
    [SerializeField] private Image BuffIcon;
    [SerializeField] private TMP_Text TextRemainValue;

    public void Setup(Sprite icon, float remainValue)
    {
        if (BuffIcon != null)
            BuffIcon.sprite = icon;

        UpdateRemainValue(remainValue);
    }

    public void UpdateRemainValue(float remainValue)
    {
        if (TextRemainValue == null)
            return;

        int displayValue = Mathf.CeilToInt(Mathf.Max(0f, remainValue));
        TextRemainValue.text = displayValue.ToString();
    }
}
