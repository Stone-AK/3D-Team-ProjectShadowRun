using UnityEngine;
using System.Collections;

public class ActivateGrenade : MonoBehaviour
{
    [Header("Visual Effects (VFX)")]
    [SerializeField] private GameObject ExplosionEffect;
    [Header("Sound Effects (SFX)")]
    [SerializeField] private AudioClip ExplosionSfx;
    [SerializeField] private float SoundVolume = 1.0f;
    [Header("Targeting Settings")]
    [SerializeField] private LayerMask DamageableLayer; // 피격 대상 레이어 지정

    private Rigidbody _rigidBody;
    private string _poolAddress;
    public bool IsActivated { get; private set; }

    private void Awake( )
    {
        _rigidBody = GetComponent<Rigidbody>();
    }

    private void OnDisable( )
    {
        StopAllCoroutines();
        IsActivated = false;
    }

    // 생성 직후 투척력 전달 및 폭발 코루틴 시작
    public void InitGrenade( ItemData itemData, Vector3 throwDirection, float throwForce )
    {
        IsActivated = true;
        _poolAddress = itemData.PrefabPath;

        StopAllCoroutines();

        // Awake에서 미리 받아둔 _rigidBody 활용
        if (_rigidBody != null)
        {
            // 재사용 시 이전 물리 속도 초기화
            _rigidBody.linearVelocity = Vector3.zero;
            _rigidBody.angularVelocity = Vector3.zero;
            _rigidBody.AddForce(throwDirection * throwForce, ForceMode.Impulse);
        }

        StartCoroutine(ExplodeRoutine(itemData));
    }

    private IEnumerator ExplodeRoutine( ItemData itemData )
    {
        itemData.TryGetParameter("Fuse", out float fuseTime);

        //  신관 타이머 대기
        if (fuseTime > 0f)
        {
            yield return new WaitForSeconds(fuseTime);
        }

        itemData.TryGetParameter("Damage", out float damage);
        itemData.TryGetParameter("Radius", out float radius);
        itemData.TryGetParameter("Duration", out float duration);

        //  폭발 처리
        // UseItemType에 따른 분기 처리
        // switch 문으로 타입별 분기 처리
        switch (itemData.UseItemType)
        {
            case "Explosion":
                ProcessExplosion(damage, radius, duration);
                break;

            case "EMP":
                ProcessExplosion(damage, radius, duration);
                // TODO: EMP 전용 효과 처리
                break;

            case "Smoke":
                ProcessExplosion(damage, radius, duration);
                // TODO: 연막 전용 효과 처리
                break;

            default:
                Debug.LogWarning($"정의되지 않은 UseItemType: {itemData.UseItemType}");
                break;
        }

        IsActivated = false;

        ObjectPoolManager.Instance.ReturnToPool(_poolAddress, gameObject);
    }

    private void ProcessExplosion( float damage, float radius, float duration )
    {
        // 이펙트 및 사운드 재생
        SpawnExplosionEffect(ExplosionEffect, 3.0f);
        PlaySound(ExplosionSfx);

        // 구체(Sphere) 범위 안의 모든 콜라이더 감지
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius, DamageableLayer);

        for (int i = 0; i < hitColliders.Length; i++)
        {
            // IDamageable 인터페이스가 있는지 확인 후 데미지 전달
            if (hitColliders[i].TryGetComponent<IDamageable>(out var damageable))
            {
                if (damage > 0f)
                {
                    damageable.TakeDamage(damage);
                }
            }
        }


        
    }

    // 이펙트 생성 공통 메서드
    private void SpawnExplosionEffect( GameObject effectPrefab, float destroyDelay )
    {
        if (effectPrefab != null)
        {
            GameObject effect = Instantiate(effectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, destroyDelay);
        }
    }

    // 3D sound 재생 공통 메서드 (오브젝트 풀 반환 시 소리 끊김 방지)
    private void PlaySound( AudioClip clip )
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, transform.position, SoundVolume);
        }
    }
}
    