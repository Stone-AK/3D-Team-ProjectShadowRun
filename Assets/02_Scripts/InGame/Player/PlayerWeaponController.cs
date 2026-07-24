using UnityEngine;

public class PlayerWeaponController : MonoBehaviour
{
    public event System.Action<int, int> OnAmmoChanged;
    public event System.Action<bool> OnReloadStateChanged;

    public int CurrentAmmo => _currentWeapon == null ? 0 : _currentWeapon.RemainBullets;
    public int CurrentReserveAmmo => GetCurrentReserveAmmo();
    public bool IsReloading => _isReloading;

    [SerializeField] private Transform PlayerWeaponSocket;
    [SerializeField] private LayerMask AimLayerMask = Physics.AllLayers;
    [SerializeField, Min(0f)] private float MaxSpreadAngle = 10f;

    private Camera MainCamera;
    private PlayerInputHandler InputHandler;
    private PlayerAnimeController AnimeController;
    private GameObject _currentWeaponObject;
    private ItemModel _currentWeaponModel;
    private WeaponData _currentWeaponData;
    private TestWeaponBase _currentWeapon;
    private Transform _currentMuzzle;
    private Coroutine _reloadCoroutine;
    private bool _isReloading;
    private float _nextFireTime;
    private bool _isFirePressed;

    private void Start()
    {
        if (MainCamera == null)
            MainCamera = Camera.main;

        if (InputHandler == null)
            InputHandler = GetComponent<PlayerInputHandler>();

        if (AnimeController == null)
            AnimeController = GetComponent<PlayerAnimeController>();

        if (MainCamera == null)
            Debug.LogError("PlayerWeaponController: MainCamera를 찾을 수 없습니다.");

        if (InputHandler == null)
        {
            Debug.LogError("PlayerWeaponController: PlayerInputHandler를 찾을 수 없습니다.");
        }
        else
        {
            InputHandler.FirePerformed += StartFiring;
            InputHandler.FireCanceled += StopFiring;
            InputHandler.ReloadPerformed += ReloadCurrentWeapon;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogError("PlayerWeaponController: InventoryManager가 없습니다.");
            return;
        }

        InventoryManager.Instance.OnSelectedQuickSlotChanged += RefreshWeapon;
        InventoryManager.Instance.OnInventoryChanged += NotifyAmmoChanged;
        RefreshWeapon();
    }

    private void OnDestroy()
    {
        if (InputHandler != null)
        {
            InputHandler.FirePerformed -= StartFiring;
            InputHandler.FireCanceled -= StopFiring;
            InputHandler.ReloadPerformed -= ReloadCurrentWeapon;
        }

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnSelectedQuickSlotChanged -= RefreshWeapon;
            InventoryManager.Instance.OnInventoryChanged -= NotifyAmmoChanged;
        }
    }

    private void Update()
    {
        if (!_isFirePressed || !IsAutomaticWeapon())
            return;

        FireCurrentWeapon();
    }

    private void StartFiring()
    {
        _isFirePressed = true;
        FireCurrentWeapon();
    }

    private void StopFiring()
    {
        _isFirePressed = false;
    }

    private bool IsAutomaticWeapon()
    {
        if (_currentWeaponData == null || string.IsNullOrEmpty(_currentWeaponData.Id))
            return false;

        // TODO[안우재](7/22): WeaponData에 발사 방식이 추가되면 ID 판별을 FireMode 판별로 교체 필요
        return _currentWeaponData.Id.Contains("_AR_") || _currentWeaponData.Id.Contains("_SMG_");
    }

    private void FireCurrentWeapon()
    {
        if (_isReloading || _currentWeapon == null || _currentMuzzle == null || MainCamera == null)
            return;

        if (_currentWeapon.RemainBullets <= 0 || Time.time < _nextFireTime)
            return;

        Ray aimRay = MainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 aimPoint;

        if (Physics.Raycast(aimRay, out RaycastHit hit, _currentWeapon.Range, AimLayerMask))
            aimPoint = hit.point;
        else
            aimPoint = aimRay.origin + aimRay.direction * _currentWeapon.Range;

        Vector3 fireDirection = (aimPoint - _currentMuzzle.position).normalized;
        fireDirection = ApplyAccuracy(fireDirection);
        _currentWeapon.Fire(_currentMuzzle.position, fireDirection);

        if (AnimeController != null)
            AnimeController.Fire();

        _nextFireTime = Time.time + _currentWeapon.AttackInterval;
        NotifyAmmoChanged();
    }

    private Vector3 ApplyAccuracy(Vector3 fireDirection)
    {
        float inaccuracyRatio = (100f - _currentWeapon.Accuracy) / 100f;

        if (inaccuracyRatio <= 0f)
            return fireDirection;

        if (inaccuracyRatio > 1f)
            inaccuracyRatio = 1f;

        Vector2 randomSpread = Random.insideUnitCircle * MaxSpreadAngle * inaccuracyRatio;
        Quaternion fireRotation = Quaternion.LookRotation(fireDirection);

        return fireRotation * Quaternion.Euler(randomSpread.y, randomSpread.x, 0f) * Vector3.forward;
    }

    private void ReloadCurrentWeapon()
    {
        if (_isReloading || _currentWeapon == null || _currentWeaponData == null || InventoryManager.Instance == null)
            return;

        int requiredAmmo = _currentWeapon.MagazineSize - _currentWeapon.RemainBullets;

        if (requiredAmmo <= 0)
            return;

        ItemData ammoData = DataManager.Instance.GetItemData(_currentWeaponData.AmmoType);

        if (ammoData == null)
            return;

        int inventoryAmmo = InventoryManager.Instance.GetItemCount(_currentWeaponData.AmmoType);

        if (inventoryAmmo <= 0)
            return;

        _isReloading = true;
        OnReloadStateChanged?.Invoke(true);
        _reloadCoroutine = StartCoroutine(ReloadRoutine());
    }

    private System.Collections.IEnumerator ReloadRoutine()
    {
        yield return new WaitForSeconds(_currentWeapon.ReloadTime);

        CompleteReload();
        _isReloading = false;
        _reloadCoroutine = null;
        OnReloadStateChanged?.Invoke(false);
        NotifyAmmoChanged();
    }

    private void CompleteReload()
    {
        if (_currentWeapon == null || _currentWeaponData == null || InventoryManager.Instance == null)
            return;

        ItemData ammoData = DataManager.Instance.GetItemData(_currentWeaponData.AmmoType);

        if (ammoData == null)
            return;

        int requiredAmmo = _currentWeapon.MagazineSize - _currentWeapon.RemainBullets;
        int inventoryAmmo = InventoryManager.Instance.GetItemCount(_currentWeaponData.AmmoType);
        int reloadAmmo = Mathf.Min(requiredAmmo, inventoryAmmo);

        if (reloadAmmo <= 0)
            return;

        if (!InventoryManager.Instance.TryRemoveItem(_currentWeaponData.AmmoType, reloadAmmo))
            return;

        int remainingAmmo = _currentWeapon.Reload(reloadAmmo);

        if (remainingAmmo <= 0)
            return;

        InventoryManager.Instance.TryAddItem(ammoData, remainingAmmo);
    }

    private void RefreshWeapon()
    {
        ItemModel selectedItem = InventoryManager.Instance.GetSelectedQuickSlotStack();

        if (selectedItem == null)
        {
            RemoveCurrentWeapon();
            return;
        }

        WeaponData weaponData = DataManager.Instance.GetItemData(selectedItem.ItemId) as WeaponData;

        if (weaponData == null)
        {
            RemoveCurrentWeapon();
            return;
        }

        if (_currentWeaponModel != null && _currentWeaponModel.InstanceId == selectedItem.InstanceId)
        {
            return;
        }

        EquipWeapon(selectedItem, weaponData);
    }

    private void EquipWeapon(ItemModel weaponModel, WeaponData weaponData)
    {
        RemoveCurrentWeapon();

        if (PlayerWeaponSocket == null)
        {
            Debug.LogError("PlayerWeaponController: PlayerWeaponSocket이 연결되지 않았습니다.");
            return;
        }

        GameObject weaponPrefab = Resources.Load<GameObject>(weaponData.PrefabPath);

        if (weaponPrefab == null)
        {
            Debug.LogError($"PlayerWeaponController: 무기 프리팹을 찾을 수 없습니다. Path: {weaponData.PrefabPath}");
            return;
        }

        _currentWeaponObject = Instantiate(weaponPrefab, PlayerWeaponSocket);
        _currentWeaponObject.transform.localPosition = Vector3.zero;
        _currentWeaponObject.transform.localScale = Vector3.one;
        _currentWeaponModel = weaponModel;
        _currentWeaponData = weaponData;

        DisableWorldItemComponents(_currentWeaponObject);
        FindWeaponComponents(weaponData);
        AnimeController?.SwapWeaponPosture();
    }

    private void FindWeaponComponents(WeaponData weaponData)
    {
        _currentWeapon = _currentWeaponObject.GetComponent<TestWeaponBase>();

        if (_currentWeapon == null)
        {
            Debug.LogError($"PlayerWeaponController: 생성된 무기에 TestWeaponBase가 없습니다. Weapon: {_currentWeaponObject.name}");
        }
        else
        {
            _currentWeapon.Initialize(weaponData, _currentWeaponModel as WeaponModel);
            NotifyAmmoChanged();
        }

        Transform[] childTransforms = _currentWeaponObject.GetComponentsInChildren<Transform>(true);

        foreach (Transform childTransform in childTransforms)
        {
            if (childTransform.name != "Muzzle")
                continue;

            _currentMuzzle = childTransform;
            break;
        }

        if (_currentMuzzle == null)
        {
            Debug.LogError($"PlayerWeaponController: 생성된 무기에 Muzzle이 없습니다. Weapon: {_currentWeaponObject.name}");
        }
    }

    private void RemoveCurrentWeapon()
    {
        bool wasReloading = _isReloading;

        if (_reloadCoroutine != null)
            StopCoroutine(_reloadCoroutine);

        _reloadCoroutine = null;
        _isReloading = false;
        _nextFireTime = 0f;
        _isFirePressed = false;

        if (_currentWeaponObject != null)
            Destroy(_currentWeaponObject);

        _currentWeaponObject = null;
        _currentWeaponModel = null;
        _currentWeaponData = null;
        _currentWeapon = null;
        _currentMuzzle = null;

        if (wasReloading)
            OnReloadStateChanged?.Invoke(false);

        OnAmmoChanged?.Invoke(0, 0);
    }

    private void NotifyAmmoChanged()
    {
        if (_currentWeapon == null)
        {
            OnAmmoChanged?.Invoke(0, 0);
            return;
        }

        OnAmmoChanged?.Invoke(_currentWeapon.RemainBullets, GetCurrentReserveAmmo());
    }

    private int GetCurrentReserveAmmo()
    {
        if (_currentWeaponData == null || InventoryManager.Instance == null)
            return 0;

        return InventoryManager.Instance.GetItemCount(_currentWeaponData.AmmoType);
    }

    private void DisableWorldItemComponents(GameObject weaponObject)
    {
        FieldItem fieldItem = weaponObject.GetComponent<FieldItem>();

        if (fieldItem != null)
            fieldItem.enabled = false;

        Rigidbody rigidbody = weaponObject.GetComponent<Rigidbody>();

        if (rigidbody != null)
        {
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
        }

        Collider[] colliders = weaponObject.GetComponentsInChildren<Collider>();

        foreach (Collider weaponCollider in colliders)
            weaponCollider.enabled = false;
    }
}
