using UnityEngine;

namespace NetizenSphere.Player
{
    public class PlayerAvatar : MonoBehaviour
    {
        [SerializeField] private RuntimeAnimatorController _animatorController;

        private Animator _animator;

        private void Awake()
        {
            _animator = GetComponent<Animator>();

            // Assign controller via code — more reliable than the serialized YAML
            // reference on the Animator component itself.
            if (_animatorController != null)
                _animator.runtimeAnimatorController = _animatorController;

            // Ch20 (and any imported FBX model) ships with its own Animator component.
            // That child Animator has no controller and takes priority over AvatarVisual's
            // Animator for its own bones, causing a permanent T-pose. Disable all child
            // Animators so only the one on this GameObject (AvatarVisual) drives the rig.
            foreach (var child in GetComponentsInChildren<Animator>(true))
            {
                if (child.gameObject != gameObject)
                    child.enabled = false;
            }
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
