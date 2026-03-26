using UnityEngine;

namespace NetizenSphere.Player
{
    // Reads movement state from PlayerMovement and drives the AvatarVisual Animator.
    // Works for both owner (uses CharacterController velocity) and non-owners
    // (derives speed from position delta, since CharacterController is disabled for them).
    [RequireComponent(typeof(PlayerMovement))]
    public class PlayerAnimator : MonoBehaviour
    {
        private PlayerMovement _movement;
        private PlayerAvatar _avatar;
        private Vector3 _lastPosition;

        private void Awake()
        {
            _movement = GetComponent<PlayerMovement>();
            _avatar = GetComponentInChildren<PlayerAvatar>();
            _lastPosition = transform.position;
        }

        private void Update()
        {
            if (_movement == null || _avatar == null)
                return;

            Vector3 velocity;

            if (_movement.IsOwner)
            {
                // Owner: CharacterController.velocity is authoritative
                velocity = _movement.GetVelocity();
            }
            else
            {
                // Non-owner: CharacterController is disabled; derive from NetworkTransform position change
                velocity = (transform.position - _lastPosition) / Time.deltaTime;
                _lastPosition = transform.position;
            }

            float speed = new Vector3(velocity.x, 0f, velocity.z).magnitude;

            _avatar.SetSpeed(speed);
            _avatar.SetGrounded(_movement.IsGrounded());
            _avatar.SetFacingDirection(velocity);
        }
    }
}
