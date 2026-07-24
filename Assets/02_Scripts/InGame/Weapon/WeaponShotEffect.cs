using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(AudioSource))]
public class WeaponShotEffect : MonoBehaviour
{
    [SerializeField, Min(0f)] private float TrailDuration = 0.05f;
    [SerializeField, Min(0f)] private float TrailWidth = 0.006f;
    [SerializeField] private ParticleSystem MuzzleFlash;
    [SerializeField] private AudioClip FireSound;
    [SerializeField, Range(0f, 1f)] private float FireSoundVolume = 1f;

    private TestWeaponBase _weapon;
    private LineRenderer _lineRenderer;
    private AudioSource _audioSource;
    private float _hideTrailTime;

    private void Awake()
    {
        _weapon = GetComponent<TestWeaponBase>();
        _lineRenderer = GetComponent<LineRenderer>();
        _audioSource = GetComponent<AudioSource>();

        _lineRenderer.useWorldSpace = true;
        _lineRenderer.positionCount = 2;
        _lineRenderer.startWidth = TrailWidth;
        _lineRenderer.endWidth = TrailWidth;
        _lineRenderer.enabled = false;

        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 1f;
    }

    private void OnEnable()
    {
        if (_weapon != null)
            _weapon.ShotFired += ShowBulletTrail;
    }

    private void OnDisable()
    {
        if (_weapon != null)
            _weapon.ShotFired -= ShowBulletTrail;

        if (_lineRenderer != null)
            _lineRenderer.enabled = false;
    }

    private void Update()
    {
        if (!_lineRenderer.enabled)
            return;

        if (Time.time < _hideTrailTime)
            return;

        _lineRenderer.enabled = false;
    }

    private void ShowBulletTrail(ShotVisualData visualData)
    {
        if (MuzzleFlash != null)
            MuzzleFlash.Play(true);

        if (FireSound != null)
            _audioSource.PlayOneShot(FireSound, FireSoundVolume);

        _lineRenderer.SetPosition(0, visualData.StartPoint);
        _lineRenderer.SetPosition(1, visualData.EndPoint);
        _lineRenderer.enabled = true;
        _hideTrailTime = Time.time + TrailDuration;
    }
}
