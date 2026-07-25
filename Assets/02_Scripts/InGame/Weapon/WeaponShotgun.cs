using UnityEngine;

public class WeaponShotgun : TestWeaponBase
{
    private const int PelletCount = 8;
    private const float PelletSpreadAngle = 5f;

    public override void Fire(Vector3 firePosition, Vector3 direction)
    {
        if (_remainBullets <= 0)
            return;

        _remainBullets--;

        if (_weaponModel != null)
            _weaponModel.CurrentAmmo = _remainBullets;

        Vector3 centerDirection = direction.normalized;
        Quaternion fireRotation = Quaternion.LookRotation(centerDirection);
        float pelletDamage = _currentWeaponStat.Damage / PelletCount;

        for (int i = 0; i < PelletCount; i++)
        {
            Vector3 pelletDirection = CreatePelletDirection(fireRotation, i);
            ShotVisualData visualData;

            if (Physics.Raycast(firePosition, pelletDirection, out RaycastHit hit, _currentWeaponStat.Range))
            {
                Debug.DrawRay(firePosition, pelletDirection * hit.distance, Color.red, _currentWeaponStat.Range);

                if (hit.transform.TryGetComponent<IDamageable>(out var damageable))
                    damageable.TakeDamage(pelletDamage);

                visualData = new ShotVisualData
                {
                    HasHit = true,
                    StartPoint = firePosition,
                    EndPoint = hit.point,
                    HitNormal = hit.normal,
                    HitTransform = hit.transform
                };
            }
            else
            {
                Debug.DrawRay(
                    firePosition,
                    pelletDirection * _currentWeaponStat.Range,
                    Color.red,
                    _currentWeaponStat.Range
                );

                visualData = new ShotVisualData
                {
                    HasHit = false,
                    StartPoint = firePosition,
                    EndPoint = firePosition + pelletDirection * _currentWeaponStat.Range,
                    HitNormal = Vector3.zero,
                    HitTransform = null
                };
            }

            InvokeShotFired(visualData);
        }
    }

    private Vector3 CreatePelletDirection(Quaternion fireRotation, int pelletIndex)
    {
        if (pelletIndex == 0)
            return fireRotation * Vector3.forward;

        Vector2 spread = Random.insideUnitCircle * PelletSpreadAngle;
        Quaternion spreadRotation = Quaternion.Euler(spread.y, spread.x, 0f);

        return fireRotation * spreadRotation * Vector3.forward;
    }
}
