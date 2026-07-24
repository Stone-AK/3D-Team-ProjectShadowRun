using UnityEngine;

public enum  WeaponType
{
    None,
    Rifle,
    Pistol
}

public class PlayerAnimeController : MonoBehaviour
{
    [SerializeField] private Animator _playerAnimator;

    public void SetRun(bool isRunning)
    {
        _playerAnimator.SetBool("isRun", isRunning);
    }

    public void Fire()
    {
        AnimatorStateInfo currentState = _playerAnimator.GetCurrentAnimatorStateInfo(0);

        Debug.Log(
            $"PlayerAnimeController Fire: " +
            $"RiflePose={currentState.IsName("FPSArms|RiflePose")}, " +
            $"GunPose={currentState.IsName("FPSArms|GunPose")}, " +
            $"Run={currentState.IsName("FPSArms|Run")}, " +
            $"isRifle={_playerAnimator.GetBool("isRifle")}, " +
            $"isPistol={_playerAnimator.GetBool("isPistol")}"
        );

        _playerAnimator.SetTrigger("Fire");
        StartCoroutine(LogFireStateNextFrame());
    }

    private System.Collections.IEnumerator LogFireStateNextFrame()
    {
        yield return null;

        AnimatorStateInfo currentState = _playerAnimator.GetCurrentAnimatorStateInfo(0);
        AnimatorStateInfo nextState = _playerAnimator.GetNextAnimatorStateInfo(0);

        Debug.Log(
            $"PlayerAnimeController Fire Result: " +
            $"RifleShot={currentState.IsName("FPSArms|RifleShot")}, " +
            $"GunShot={currentState.IsName("FPSArms|GunShot")}, " +
            $"IsInTransition={_playerAnimator.IsInTransition(0)}, " +
            $"NextRifleShot={nextState.IsName("FPSArms|RifleShot")}, " +
            $"NextGunShot={nextState.IsName("FPSArms|GunShot")}"
        );
    }

    public void SwapWeaponPosture()
    {
        WeaponType weaponType = InventoryManager.Instance.ReturnWeaponTypeFromQuickSlotID();

        switch(weaponType)
        {
            case WeaponType.Rifle:
                _playerAnimator.SetBool("isRifle", true);
                _playerAnimator.SetBool("isPistol", false);
                _playerAnimator.Play("Base Layer.FPSArms|RiflePose", 0, 0f);
                break;
            case WeaponType.Pistol:
                _playerAnimator.SetBool("isRifle", false);
                _playerAnimator.SetBool("isPistol", true);
                _playerAnimator.Play("Base Layer.FPSArms|GunPose", 0, 0f);
                break;
            default:
                _playerAnimator.SetBool("isRifle", false);
                _playerAnimator.SetBool("isPistol", false);
                _playerAnimator.Play("Base Layer.NoWeapon Idle", 0, 0f);
                break;
        }
    }
}
