using UnityEngine;

namespace NetizenSphere.Player
{
    public class PlayerAvatar : MonoBehaviour
    {
        private Animator _animator;

        private void Awake()
        {
            // Search self first, then children — supports Animator on AvatarVisual
            // OR on the humanoid model root when it's parented under AvatarVisual.
            _animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>(true);
        }

        public void SetSpeed(float speed)
        {
            if (_animator != null)
                _animator.SetFloat("Speed", speed);
        }

        public void SetGrounded(bool grounded)
        {
            if (_animator != null)
                _animator.SetBool("IsGrounded", grounded);
        }

        // Rotates AvatarVisual to face the direction of travel.
        // Only the child visual rotates — root transform is left alone for NetworkTransform.
        public void SetFacingDirection(Vector3 velocity)
        {
            Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
            if (horizontal.sqrMagnitude < 0.01f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(horizontal);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
        }
    }
}
