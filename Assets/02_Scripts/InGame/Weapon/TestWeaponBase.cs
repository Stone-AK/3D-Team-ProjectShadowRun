using System.Collections.Generic;
using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float damage);//DamageInfo구조체를 만들어 전달하면 더 많은 정보를 전달할수 있음
}
public interface IBattleAgent
{
    BattleAgentTeamType Team { get; }
    Transform Transform { get; }

    bool IsDead { get; }
    void UseWeapon();//무기를 사용할때 공격자를 전달할수있음

}

public class TestWeaponBase : MonoBehaviour
{
    protected WeaponData _weaponData;

    protected WeaponStat _baseWeaponStat = new WeaponStat();
    protected WeaponStat _currentWeaponStat = new WeaponStat();
    protected int _remainBullets = 0;
    public float ReloadTime => _currentWeaponStat.ReloadTime;
    public int MagazineSize => _currentWeaponStat.MagazineSize;
    public int RemainBullets => _remainBullets;
    public float AttackInterval => _currentWeaponStat.AttackInterval;
    public float Range=>_currentWeaponStat.Range;

    protected Dictionary<WeaponPartsType, WeaponPartsData> _weaponPartsDic = new Dictionary<WeaponPartsType, WeaponPartsData>();
    public void Awake()
    {
        Initialize();
    }
    public virtual void Initialize()
    {
        _baseWeaponStat.Damage = 10f;
        _baseWeaponStat.AttackInterval = 1f;
        _baseWeaponStat.MagazineSize = 10;
        _baseWeaponStat.Accuracy = 100f;
        _baseWeaponStat.Range = 10f;
        _baseWeaponStat.ReloadTime = 5f;
        _currentWeaponStat = _baseWeaponStat;
        _remainBullets = 10;
    }

    //public abstract bool CanFire { get; }

    public virtual void Fire(Vector3 firePosition, Vector3 direction)
    {
        if (_remainBullets <= 0)
        {
            return;
        }
        _remainBullets--;

        //Vector3 direction = (targetPosition - firePosition).normalized;
        if (Physics.Raycast(firePosition, direction.normalized, out RaycastHit hit, _currentWeaponStat.Range))
        {
            Debug.DrawRay(firePosition, direction * hit.distance, Color.red, _currentWeaponStat.Range);
           
            if (hit.transform.TryGetComponent<IDamageable>(out var damageable))
            { 
                Debug.Log($"명중{_remainBullets}발 남음");
                damageable.TakeDamage(_currentWeaponStat.Damage);
            }

            else
            {
                Debug.Log($"빗나감{_remainBullets}발 남음");
            }
        }
        else
        {
            Debug.Log($"빗나감{_remainBullets}발 남음");
        }

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
            return newBulletAmonut - _currentWeaponStat.MagazineSize;
        }
        else
        {
            _remainBullets = newBulletAmonut;
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
