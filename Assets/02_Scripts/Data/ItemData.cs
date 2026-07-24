using NUnit.Framework.Interfaces;
using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public enum WeaponPartsType
{
    None,       // 없음
    Muzzle,     // 총구
    Scope,      // 조준경
    Magazine,   // 탄창
    Grip,       // 손잡이
    Stock       // 개머리판
}

public enum WeaponStatType
{
    Damage,
    AttackInterval,
    MagazineSize,
    Accuracy,
    Range,
    ReloadTime
}

public enum WeaponStatModifierType
{
    Add,        // 합연산
    Multiply,   // 곱연산
    Override    // 덮어쓰기
}

public struct WeaponStat
{
    public float Damage;
    public float AttackInterval;
    public int MagazineSize;
    public float Accuracy;
    public float Range;
    public float ReloadTime;
}

[System.Serializable]
public class ItemData : BaseData
{
    public string Name;
    public string ItemDescription;
    public string ItemType;
    public string Grade;
    public int MaxStackCount;
    public int SellingPrice;

    public string IconPath;
    public string PrefabPath;

    public string UseItemType;
    public string UseItemParameterList;
    // public string[] UseItemParameters;

    // 파싱된 키-값 데이터를 보관할 딕셔너리
    public Dictionary<string, float> ItemParameters = new Dictionary<string, float>();

    public void ParseUseItemParameters( )
    {
        ItemParameters.Clear();
        if (string.IsNullOrWhiteSpace(UseItemParameterList))
        {
            return;
        }

        string[] pairs = UseItemParameterList.Split(',');
        for (int i = 0; i < pairs.Length; i++)
        {
            string[] keyValue = pairs[i].Split(':');
            if (keyValue.Length == 2)
            {
                string key = keyValue[0].Trim();
                if (float.TryParse(keyValue[1].Trim(), out float value))
                {
                    ItemParameters[key] = value;
                }
            }
        }
    }
   
    public bool TryGetParameter( string key, out float value )
    {
        if (ItemParameters.Count == 0 && !string.IsNullOrWhiteSpace(UseItemParameterList))
        {
            ParseUseItemParameters();
        }

        return ItemParameters.TryGetValue(key, out value);
    }
}




[System.Serializable]
public class WeaponData : ItemData
{
    public float Damage;
    public int MagazineSize;
    public string AmmoType;
    public float AttackInterval;
    public float Accuracy;
    public float Range;
    public float ReloadTime;
    public float MaxDurability;
}

[System.Serializable]
public class WeaponPartsData : ItemData
{
    public WeaponPartsType PartsType;
    public WeaponStatType StatType;
    public WeaponStatModifierType ModifierType;
    public float Value;
}

[System.Serializable]
public class ItemModel
{
    public string InstanceId;      // 생성될 때마다 발급받는 고유 ID
    public string ItemId;          // DataManager에서 원본 ItemData를 찾기 위한 Key
    public int CurrentStackCount;  // 현재 겹쳐진 개수
}

[System.Serializable]
public class WeaponModel : ItemModel
{
    public int CurrentAmmo;                 // 현재 장전된 총알 수
    public float CurrentDurability;         // 현재 내구도
    public List<ItemModel> AttachedParts;   // 장착된 파츠들
}

[System.Serializable]
public class ShopItemData : BaseData 
{
    public string ItemId;        
    public int StockCount;       
}