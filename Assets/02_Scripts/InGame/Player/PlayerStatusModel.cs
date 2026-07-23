using System;
using UnityEngine;

public class PlayerStatusModel
{
    public float MaxHP { get; private set; }
    public float CurrentHP { get; private set; }

    public float MaxStamina { get; private set; }
    public float CurrentStamina { get; private set; }

    public event Action<float> HealthChanged;
    public event Action<float> StaminaChanged;

    public void InitPlayer(float maxHP, float maxStamina)
    {
        MaxHP = Mathf.Max(1f, maxHP);
        CurrentHP = MaxHP;

        MaxStamina = Mathf.Max(1f, maxStamina);
        CurrentStamina = MaxStamina;
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0f || CurrentHP <= 0f)
            return;

        CurrentHP = Mathf.Clamp(CurrentHP - damage, 0f, MaxHP);

        HealthChanged?.Invoke(CurrentHP);
    }

    public void RecoverHP( float healAmount )
    {
        if (healAmount <= 0f || CurrentHP >= MaxHP)
            return;

        CurrentHP = Mathf.Clamp(CurrentHP + healAmount, 0f, MaxHP);

        HealthChanged?.Invoke(CurrentHP);
    }

    public void UseStamina(float amount)
    {
        if (amount <= 0f || CurrentStamina <= 0f)
            return;

        CurrentStamina = Mathf.Clamp(CurrentStamina - amount, 0f, MaxStamina);

        StaminaChanged?.Invoke(CurrentStamina);
    }

    public void RecoverStamina(float amount)
    {
        if (amount <= 0f || CurrentStamina >= MaxStamina)
            return;

        CurrentStamina = Mathf.Clamp(CurrentStamina + amount, 0f, MaxStamina);

        StaminaChanged?.Invoke(CurrentStamina);
    }
}
