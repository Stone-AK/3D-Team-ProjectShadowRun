using UnityEngine;
using System.Collections;

public class ActivateGrenade : MonoBehaviour
{
    [SerializeField] private GameObject _explosionEffect; // 폭발 이펙트 프리팹 (선택)

    private string _poolAddress;

    // 생성 직후 투척력 전달 및 폭발 코루틴 시작
    public void InitGrenade( ItemData itemData, Vector3 throwDirection, float throwForce )
    {
        _poolAddress = itemData.PrefabPath;

        StopAllCoroutines();

        Rigidbody rigidBody = GetComponent<Rigidbody>();
        if (rigidBody != null)
        {
            // 재사용 시 이전 물리 속도 초기화
            rigidBody.linearVelocity = Vector3.zero;
            rigidBody.angularVelocity = Vector3.zero;
            rigidBody.AddForce(throwDirection * throwForce, ForceMode.Impulse);
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

        //  폭발 처리
        // UseItemType에 따른 분기 처리
        if (itemData.UseItemType == "Explosion")
        {
            itemData.TryGetParameter("Damage", out float damage);
            itemData.TryGetParameter("Radius", out float radius);
            ProcessExplosion(damage, radius);
        }
        else if (itemData.UseItemType == "EMP")
        {
            itemData.TryGetParameter("Duration", out float duration);
            itemData.TryGetParameter("Radius", out float radius);
            // TODO: EMP 효과 처리
        }
        else if (itemData.UseItemType == "Smoke")
        {
            itemData.TryGetParameter("Duration", out float duration);
            itemData.TryGetParameter("Radius", out float radius);
            // TODO: 연막 효과 처리
        }

        ObjectPoolManager.Instance.ReturnToPool(_poolAddress, gameObject);
    }

    private void ProcessExplosion( float damage, float radius )
    {
        // 폭발 이펙트 생성
        if (_explosionEffect != null)
        {
            GameObject effect = Instantiate(_explosionEffect, transform.position, Quaternion.identity);
            Destroy(effect, 3f); // 3초 후 자동 삭제
        }

        // 구체(Sphere) 범위 안의 모든 콜라이더 감지
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius);

        for (int i = 0; i < hitColliders.Length; i++)
        {
            // TODO: 적(Enemy)이나 플레이어 등 피해 대상에게 데미지 전달
            // 예시: 
            // EnemyHealth enemy = hitColliders[i].GetComponent<EnemyHealth>();
            // if (enemy != null) { enemy.TakeDamage(damage); }
        }

        
    }
}
    