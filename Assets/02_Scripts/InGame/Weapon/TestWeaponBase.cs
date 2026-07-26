using System.Collections.Generic;
using UnityEngine;

public struct ShotVisualData
{
    public bool HasHit;
    public Vector3 StartPoint;
    public Vector3 EndPoint;
    public Vector3 HitNormal;
    public Transform HitTransform;
}

public interface IDamageable
{
    void TakeDamage(float damage);//DamageInfo구조체를 만들어 전달하면 더 많은 정보를 전달할수 있음
}
public interface IBattleAgent
{
    BattleAgentTeamType Team { get; }
    Transform Transform { get; }
    void UseWeapon();//무기를 사용할때 공격자를 전달할수있음

}

public class TestWeaponBase : MonoBehaviour
{
    public event System.Action<ShotVisualData> ShotFired;

    protected WeaponData _weaponData;
    protected WeaponModel _weaponModel;

    protected WeaponStat _baseWeaponStat = new WeaponStat();
    protected WeaponStat _currentWeaponStat = new WeaponStat();
    protected int _remainBullets = 0;
    public float ReloadTime => _currentWeaponStat.ReloadTime;
    public int MagazineSize => _currentWeaponStat.MagazineSize;
    public int RemainBullets => _remainBullets;
    public float AttackInterval => _currentWeaponStat.AttackInterval;
    public float Accuracy => _currentWeaponStat.Accuracy;
    public float Range=>_currentWeaponStat.Range;

    protected Dictionary<WeaponPartsType, WeaponPartsData> _weaponPartsDic = new Dictionary<WeaponPartsType, WeaponPartsData>();

    // TODO[안우재](7/22) : Awake() 및 Initialize() 메서드는 호환성 문제로 무기 구축에 어느정도
    //                      무기 구축에 어느정도 틀이 잡히면 삭제 또는 수정해야함.
    //public void Awake()
    //{
    //    Initialize();
    //}
    //public virtual void Initialize()
    //{
    //    _baseWeaponStat.Damage = 10f;
    //    _baseWeaponStat.AttackInterval = 1f;
    //    _baseWeaponStat.MagazineSize = 10;
    //    _baseWeaponStat.Accuracy = 100f;
    //    _baseWeaponStat.Range = 10f;
    //    _baseWeaponStat.ReloadTime = 5f;
    //    _currentWeaponStat = _baseWeaponStat;
    //    _remainBullets = 10;
    //}

    public virtual void Initialize(WeaponData weaponData, WeaponModel weaponModel)
    {
        if (weaponData == null)
        {
            Debug.LogError("TestWeaponBase: WeaponData가 없습니다.");
            return;
        }

        _weaponData = weaponData;
        _weaponModel = weaponModel;
        _baseWeaponStat.Damage = weaponData.Damage;
        _baseWeaponStat.AttackInterval = weaponData.AttackInterval;
        _baseWeaponStat.MagazineSize = weaponData.MagazineSize;
        _baseWeaponStat.Accuracy = weaponData.Accuracy;
        _baseWeaponStat.Range = weaponData.Range;
        _baseWeaponStat.ReloadTime = weaponData.ReloadTime;

        _weaponPartsDic.Clear();

        if (weaponModel?.AttachedParts != null)
        {
            foreach (ItemModel partModel in weaponModel.AttachedParts)
            {
                if (partModel == null)
                    continue;

                ItemData itemData = DataManager.Instance.GetItemData(partModel.ItemId);

                if (itemData is not WeaponPartsData weaponPartData)
                    continue;

                _weaponPartsDic[weaponPartData.PartsType] = weaponPartData;
            }
        }

        CalculateCurrentWeaponStat();


        if (weaponModel == null)
            _remainBullets = 0;
        else
            _remainBullets = weaponModel.CurrentAmmo;

        if (_weaponModel != null)
            _weaponModel.CurrentAmmo = _remainBullets;
    }

    //public abstract bool CanFire { get; }

    public virtual void Fire(Vector3 firePosition, Vector3 direction)
    {
        if (_remainBullets <= 0)
        {
            return;
        }
        _remainBullets--;

        if (_weaponModel != null)
            _weaponModel.CurrentAmmo = _remainBullets;

        Vector3 fireDirection = direction.normalized;

        if (Physics.Raycast(firePosition, fireDirection, out RaycastHit hit, _currentWeaponStat.Range))
        {
            Debug.DrawRay(firePosition, fireDirection * hit.distance, Color.red, _currentWeaponStat.Range);

            Debug.Log(
                $"총알 충돌 대상: {hit.transform.name}, " +
                $"Root: {hit.transform.root.name}, " +
                $"Layer: {LayerMask.LayerToName(hit.transform.gameObject.layer)}, " +
                $"HitPoint: {hit.point}"
            );

            if (hit.transform.TryGetComponent<IDamageable>(out var damageable))
            { 
                Debug.Log($"명중{_remainBullets}발 남음");
                damageable.TakeDamage(_currentWeaponStat.Damage);
            }

            else
            {
                Debug.Log($"빗나감{_remainBullets}발 남음");
            }

            ShotVisualData visualData = new ShotVisualData
            {
                HasHit = true,
                StartPoint = firePosition,
                EndPoint = hit.point,
                HitNormal = hit.normal,
                HitTransform = hit.transform
            };

            InvokeShotFired(visualData);
        }
        else
        {
            Debug.Log($"빗나감{_remainBullets}발 남음");

            ShotVisualData visualData = new ShotVisualData
            {
                HasHit = false,
                StartPoint = firePosition,
                EndPoint = firePosition + fireDirection * _currentWeaponStat.Range,
                HitNormal = Vector3.zero,
                HitTransform = null
            };

            InvokeShotFired(visualData);
        }

    }

    protected void InvokeShotFired(ShotVisualData visualData)
    {
        ShotFired?.Invoke(visualData);
    }

    public virtual int Reload(int bulletAmount)
    {
        int newBulletAmonut = _remainBullets + bulletAmount;

        if (bulletAmount <= 0)
        {
            return bulletAmount;
        }

        if (_remainBullets >= _currentWeaponStat.MagazineSize)
        {
            return bulletAmount;
        }

        if (newBulletAmonut >= _currentWeaponStat.MagazineSize)
        {
            _remainBullets = _currentWeaponStat.MagazineSize;

            if (_weaponModel != null)
                _weaponModel.CurrentAmmo = _remainBullets;

            return newBulletAmonut - _currentWeaponStat.MagazineSize;
        }
        else
        {
            _remainBullets = newBulletAmonut;

            if (_weaponModel != null)
                _weaponModel.CurrentAmmo = _remainBullets;

            return 0;
        }

    }
    public virtual void CalculateCurrentWeaponStat()
    {
        _currentWeaponStat = WeaponStatCalculator.CalculateWeaponStat(_baseWeaponStat, _weaponPartsDic);
    }
    public virtual WeaponPartsData EquipWeaponPart(WeaponPartsData newPart)
    {
        WeaponPartsData oldPart = null;

        if (_weaponPartsDic.TryGetValue(newPart.PartsType, out oldPart))
        {
            _weaponPartsDic[newPart.PartsType] = newPart;
        }
        else
        {
            _weaponPartsDic.Add(newPart.PartsType, newPart);
        }

        CalculateCurrentWeaponStat();

        return oldPart;
    }
}
