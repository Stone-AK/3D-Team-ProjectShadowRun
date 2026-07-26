using System.Collections.Generic;
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
    private readonly List<LineRenderer> _trailRenderers = new List<LineRenderer>();
    private AudioSource _audioSource;
    private float _hideTrailTime;
    private int _trailIndex;
    private int _lastShotFrame = -1;

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
        _trailRenderers.Add(_lineRenderer);

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

        foreach (LineRenderer trailRenderer in _trailRenderers)
        {
            if (trailRenderer != null)
                trailRenderer.enabled = false;
        }
    }

    private void Update()
    {
        if (Time.time < _hideTrailTime)
            return;

        foreach (LineRenderer trailRenderer in _trailRenderers)
            trailRenderer.enabled = false;
    }

    private void ShowBulletTrail(ShotVisualData visualData)
    {
        if (_lastShotFrame != Time.frameCount)
        {
            _lastShotFrame = Time.frameCount;
            _trailIndex = 0;

            if (MuzzleFlash != null)
                MuzzleFlash.Play(true);

            if (FireSound != null)
                _audioSource.PlayOneShot(FireSound, FireSoundVolume);
        }

        LineRenderer trailRenderer = GetTrailRenderer(_trailIndex);
        _trailIndex++;

        trailRenderer.SetPosition(0, visualData.StartPoint);
        trailRenderer.SetPosition(1, visualData.EndPoint);
        trailRenderer.enabled = true;
        _hideTrailTime = Time.time + TrailDuration;
    }

    private LineRenderer GetTrailRenderer(int index)
    {
        if (index < _trailRenderers.Count)
            return _trailRenderers[index];

        GameObject trailObject = new GameObject($"BulletTrail_{index + 1}");
        trailObject.transform.SetParent(transform, false);

        LineRenderer trailRenderer = trailObject.AddComponent<LineRenderer>();
        trailRenderer.sharedMaterial = _lineRenderer.sharedMaterial;
        trailRenderer.useWorldSpace = true;
        trailRenderer.positionCount = 2;
        trailRenderer.startWidth = TrailWidth;
        trailRenderer.endWidth = TrailWidth;
        trailRenderer.startColor = _lineRenderer.startColor;
        trailRenderer.endColor = _lineRenderer.endColor;
        trailRenderer.textureMode = _lineRenderer.textureMode;
        trailRenderer.alignment = _lineRenderer.alignment;
        trailRenderer.numCapVertices = _lineRenderer.numCapVertices;
        trailRenderer.numCornerVertices = _lineRenderer.numCornerVertices;
        trailRenderer.enabled = false;
        _trailRenderers.Add(trailRenderer);

        return trailRenderer;
    }
}
