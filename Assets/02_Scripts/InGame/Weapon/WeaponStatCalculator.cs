
using System.Collections.Generic;
using UnityEngine;

public static class WeaponStatCalculator
{
    public static WeaponStat CalculateWeaponStat(WeaponStat currentStat, Dictionary<WeaponPartsType, WeaponPartsData> weaponPartsDataDic) 
    {
        WeaponStat finalStat = currentStat;

        if (weaponPartsDataDic == null)
            return finalStat;

        finalStat.Damage = CalculateValue(finalStat.Damage, WeaponStatType.Damage, weaponPartsDataDic);
        finalStat.AttackInterval = CalculateValue(finalStat.AttackInterval, WeaponStatType.AttackInterval, weaponPartsDataDic);
        finalStat.MagazineSize = Mathf.RoundToInt(CalculateValue(finalStat.MagazineSize, WeaponStatType.MagazineSize, weaponPartsDataDic));
        finalStat.Accuracy = CalculateValue(finalStat.Accuracy, WeaponStatType.Accuracy, weaponPartsDataDic);
        finalStat.Range = CalculateValue(finalStat.Range, WeaponStatType.Range, weaponPartsDataDic);
        finalStat.ReloadTime = CalculateValue(finalStat.ReloadTime, WeaponStatType.ReloadTime, weaponPartsDataDic);

        return finalStat;
    }

    private static float CalculateValue(float baseValue, WeaponStatType statType, Dictionary<WeaponPartsType, WeaponPartsData> weaponPartsDataDic)
    {
        float result = baseValue;
        bool hasOverride = false;
        float highestOverrideValue = 0f;

        foreach (WeaponPartsData weaponPartData in weaponPartsDataDic.Values)
        {
            if (weaponPartData == null || weaponPartData.StatType != statType ||
                weaponPartData.ModifierType != WeaponStatModifierType.Override)
                continue;

            if (!hasOverride || weaponPartData.Value > highestOverrideValue)
            {
                highestOverrideValue = weaponPartData.Value;
                hasOverride = true;
            }
        }

        if (hasOverride)
            result = highestOverrideValue;

        foreach (WeaponPartsData weaponPartData in weaponPartsDataDic.Values)
        {
            if (weaponPartData == null || weaponPartData.StatType != statType || weaponPartData.ModifierType != WeaponStatModifierType.Add)
                continue;

            result += weaponPartData.Value;
        }

        foreach (WeaponPartsData weaponPartData in weaponPartsDataDic.Values)
        {
            if (weaponPartData == null || weaponPartData.StatType != statType || weaponPartData.ModifierType != WeaponStatModifierType.Multiply)
                continue;

            result *= weaponPartData.Value;
        }

        return result;
    }
}
