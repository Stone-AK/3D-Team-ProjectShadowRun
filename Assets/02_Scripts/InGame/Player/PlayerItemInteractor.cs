using UnityEngine;
using System;

public class PlayerItemInteractor : MonoBehaviour
{
    [SerializeField] private PlayerInputHandler InputHandler;
    [SerializeField] private PlayerSight PlayerSight;
    [SerializeField] private float InteractionDistance = 3f;
    [SerializeField] private LayerMask ItemLayerMask;

    private FieldItem _currentTarget;

    public FieldItem CurrentTarget => _currentTarget;
    public event Action<FieldItem> OnTargetChanged;

    private void OnEnable()
    {
        InputHandler.GetItemPerformed += TryPickupLookedAtItem;
    }

    private void OnDisable()
    {
        InputHandler.GetItemPerformed -= TryPickupLookedAtItem;
        SetCurrentTarget(null);
    }

    private void Update()
    {
        if (InputHandler.IsGameplayInputBlocked)
        {
            SetCurrentTarget(null);
            return;
        }

        SetCurrentTarget(FindLookedAtItem());
    }

    private FieldItem FindLookedAtItem()
    {
        Transform sight = PlayerSight.GetPlayerSightTransform();

        if (!Physics.Raycast(
                sight.position,
                sight.forward,
                out RaycastHit hit,
                InteractionDistance,
                ItemLayerMask,
                QueryTriggerInteraction.Collide))
        {
            return null;
        }

        return hit.collider.GetComponentInParent<FieldItem>();
    }

    private void SetCurrentTarget(FieldItem target)
    {
        if (ReferenceEquals(_currentTarget, target))
            return;

        _currentTarget = target;
        OnTargetChanged?.Invoke(_currentTarget);
    }

    private void TryPickupLookedAtItem()
    {
        if (_currentTarget == null)
            return;

        _currentTarget.TryPickup();
    }
}
