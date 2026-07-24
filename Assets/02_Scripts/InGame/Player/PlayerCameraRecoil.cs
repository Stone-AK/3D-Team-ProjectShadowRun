using UnityEngine;

public class PlayerCameraRecoil : MonoBehaviour
{
    private PlayerSight PlayerSight;
    private Transform RecoilTarget;
    [SerializeField, Min(0f)] private float VerticalRecoilAngle = 1.5f;
    [SerializeField, Min(0f)] private float HorizontalRecoilAngle = 0.5f;
    [SerializeField, Min(0f)] private float KickSpeed = 25f;
    [SerializeField, Min(0f)] private float ReturnSpeed = 10f;

    private Quaternion _initialLocalRotation;
    private Vector2 _currentRecoil;
    private Vector2 _targetRecoil;
    private bool _isInitialized;

    private void Start()
    {
        if (PlayerSight == null)
            PlayerSight = GetComponent<PlayerSight>();

        if (RecoilTarget == null && PlayerSight != null)
            RecoilTarget = PlayerSight.GetPlayerSightTransform();

        if (RecoilTarget == null)
        {
            Debug.LogError("PlayerCameraRecoil: RecoilTarget이 연결되지 않았습니다.");
            return;
        }

        _initialLocalRotation = RecoilTarget.localRotation;
        _isInitialized = true;
    }

    private void Update()
    {
        if (!_isInitialized)
            return;

        _currentRecoil = Vector2.Lerp(
            _currentRecoil,
            _targetRecoil,
            KickSpeed * Time.deltaTime
        );

        _targetRecoil = Vector2.Lerp(
            _targetRecoil,
            Vector2.zero,
            ReturnSpeed * Time.deltaTime
        );

        RecoilTarget.localRotation = _initialLocalRotation * Quaternion.Euler(
            _currentRecoil.x,
            _currentRecoil.y,
            0f
        );
    }

    public void ApplyRecoil()
    {
        if (!_isInitialized)
            return;

        float horizontalAngle = Random.Range(-HorizontalRecoilAngle, HorizontalRecoilAngle);
        _targetRecoil = new Vector2(-VerticalRecoilAngle, horizontalAngle);
    }

    private void OnDisable()
    {
        if (!_isInitialized)
            return;

        RecoilTarget.localRotation = _initialLocalRotation;
        _currentRecoil = Vector2.zero;
        _targetRecoil = Vector2.zero;
    }
}
