using UnityEngine;

namespace NetizenSphere.Player
{
    // Run well before NGO components (default order 0) so the controller
    // is assigned before NetworkAnimator.Awake() calls m_Animator.layerCount.
    [DefaultExecutionOrder(-500)]
    public class PlayerAvatar : MonoBehaviour
    {
        [SerializeField] private RuntimeAnimatorController _animatorController;

        private Animator _animator;

        private void Awake()
        {
            // Look on this GO first, then fall back to any child (handles unusual prefab layouts).
            _animator = GetComponent<Animator>();
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>(true);

            if (_animator == null)
            {
                Debug.LogError("[PlayerAvatar] No Animator found on this GameObject or its children.", this);
                return;
            }

            // Prefer the explicitly-assigned controller; if none, keep whatever
            // was wired at edit time (AvatarSetupHelper sets it on the Animator directly).
            if (_animatorController != null)
            {
                _animator.runtimeAnimatorController = _animatorController;
            }
            else if (_animator.runtimeAnimatorController == null)
            {
                Debug.LogError("[PlayerAvatar] No AnimatorController available. " +
                    "Assign _animatorController in the prefab or run NetizenSphere > Setup Avatar.", this);
            }

            Debug.Log($"[PlayerAvatar] Animator on '{_animator.gameObject.name}' | " +
                $"Controller: {_animator.runtimeAnimatorController?.name ?? "NONE"} | " +
                $"Avatar: {_animator.avatar?.name ?? "NONE"}", this);

            // FBX models ship with their own controllerless Animator that fights ours.
            // Disable every child Animator — only this GO's Animator drives the rig.
            foreach (var child in GetComponentsInChildren<Animator>(true))
            {
                if (child.gameObject != gameObject)
                    child.enabled = false;
            }
        }

        private bool HasValidAnimator()
        {
            return _animator != null && _animator.runtimeAnimatorController != null;
        }

        public void SetSpeed(float speed)
        {
            if (!HasValidAnimator()) return;
            _animator.SetFloat("Speed", speed);
        }

        public void SetGrounded(bool grounded)
        {
            if (!HasValidAnimator()) return;
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
