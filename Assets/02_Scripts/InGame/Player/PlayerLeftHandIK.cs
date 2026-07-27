using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerLeftHandIK : MonoBehaviour
{
    private PlayerWeaponController _weaponController;
    private TwoBoneIKConstraint _leftHandConstraint;
    private MultiParentConstraint _leftHandFollowConstraint;
    private Transform _leftHandTarget;
    private Transform _playerLeftGrip;
    private Transform _leftHand;
    private Vector3 _handToGripPosition;
    private Quaternion _handToGripRotation;

    private void Start()
    {
        _weaponController = GetComponent<PlayerWeaponController>();
        _leftHandConstraint = GetComponentInChildren<TwoBoneIKConstraint>(true);
        _leftHandFollowConstraint = GetComponentInChildren<MultiParentConstraint>(true);
        _leftHandTarget = FindChildTransform("LeftHandTarget");
        _playerLeftGrip = FindChildTransform("PlayerLeftGrip");
        _leftHand = FindChildTransform("HandIK.L");

        if (_weaponController == null || _leftHandConstraint == null || _leftHandFollowConstraint == null || _leftHandTarget == null || _playerLeftGrip == null || _leftHand == null)
        {
            Debug.LogError("PlayerLeftHandIK: 왼손 IK 구성 요소를 찾을 수 없습니다.");
            enabled = false;
            return;
        }

        _handToGripPosition = _leftHand.InverseTransformPoint(_playerLeftGrip.position);
        _handToGripRotation = Quaternion.Inverse(_leftHand.rotation) * _playerLeftGrip.rotation;
        SetIKWeight(0f);
    }

    private void Update()
    {
        Transform weaponLeftHandGrip = _weaponController.CurrentLeftHandGrip;

        if (weaponLeftHandGrip == null)
        {
            SetIKWeight(0f);
            return;
        }

        Quaternion targetRotation = weaponLeftHandGrip.rotation * Quaternion.Inverse(_handToGripRotation);
        Vector3 targetPosition = weaponLeftHandGrip.position - targetRotation * _handToGripPosition;

        _leftHandTarget.SetPositionAndRotation(targetPosition, targetRotation);
        SetIKWeight(1f);
    }

    private void SetIKWeight(float weight)
    {
        _leftHandConstraint.weight = weight;
        _leftHandFollowConstraint.weight = weight;
    }

    private Transform FindChildTransform(string childName)
    {
        Transform[] childTransforms = GetComponentsInChildren<Transform>(true);

        foreach (Transform childTransform in childTransforms)
        {
            if (childTransform.name == childName)
                return childTransform;
        }

        return null;
    }
}
