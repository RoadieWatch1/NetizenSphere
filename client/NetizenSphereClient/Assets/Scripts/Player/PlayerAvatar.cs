using UnityEngine;

namespace NetizenSphere.Player
{
    // Run well before NGO components (default order 0) so the controller
    // is assigned before NetworkAnimator.Awake() calls m_Animator.layerCount.
    [DefaultExecutionOrder(-500)]
    public class PlayerAvatar : MonoBehaviour
    {
        // Pre-wire both fields via AvatarSetupHelper (NetizenSphere > Setup Avatar).
        // If left null they fall back to auto-discovery so the component still works
        // even when the prefab hasn't been through setup yet.
        [SerializeField] private Animator _animator;
        [SerializeField] private RuntimeAnimatorController _animatorController;

        private bool _reportedInvalid;

        private void Awake()
        {
            // ── 1. Find the Animator ─────────────────────────────────────────
            if (_animator == null)
                _animator = GetComponent<Animator>();

            if (_animator == null)
                _animator = GetComponentInChildren<Animator>(true);

            if (_animator == null)
            {
                Debug.LogError("[PlayerAvatar] No Animator found. " +
                    "Run NetizenSphere > Setup Avatar to wire the prefab.", this);
                return;
            }

            // ── 2. Assign controller ──────────────────────────────────────────
            // Always apply the serialized _animatorController when set — this
            // overrides whatever m_Controller resolved to (or didn't) at load time,
            // which guards against stale deserialization after an FBX re-import.
            if (_animatorController != null)
                _animator.runtimeAnimatorController = _animatorController;

#if UNITY_EDITOR
            // Editor-only safety net: if both the serialized field AND the Animator's
            // own m_Controller reference are null (e.g., a failed auto-setup pass),
            // load the controller directly from its known asset path so Play mode works.
            if (_animator.runtimeAnimatorController == null)
            {
                const string ControllerPath = "Assets/Animations/PlayerAnimatorController.controller";
                var fallback = UnityEditor.AssetDatabase
                    .LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
                if (fallback != null)
                {
                    _animator.runtimeAnimatorController = fallback;
                    Debug.LogWarning("[PlayerAvatar] Controller loaded via AssetDatabase fallback. " +
                        "Run NetizenSphere > Setup Avatar to permanently wire the prefab.", this);
                }
            }
#endif

            // ── 3. Validate ──────────────────────────────────────────────────
            if (_animator.runtimeAnimatorController == null && !_reportedInvalid)
            {
                _reportedInvalid = true;
                Debug.LogError("[PlayerAvatar] Animator has no controller. " +
                    "Run NetizenSphere > Setup Avatar to wire the prefab.", this);
            }
            else
            {
                Debug.Log($"[PlayerAvatar] Ready — Animator: '{_animator.gameObject.name}' | " +
                    $"Controller: '{_animator.runtimeAnimatorController?.name}' | " +
                    $"Avatar: '{_animator.avatar?.name}'", this);
            }

            // ── 4. Silence competing child Animators ─────────────────────────
            // FBX models ship with their own controller-less Animator that fights ours.
            foreach (var child in GetComponentsInChildren<Animator>(true))
            {
                if (child != _animator)
                    child.enabled = false;
            }
        }

        private bool IsReady() =>
            _animator != null && _animator.runtimeAnimatorController != null;

        public void SetSpeed(float speed)
        {
            if (!IsReady()) return;
            _animator.SetFloat("Speed", speed);
        }

        public void SetGrounded(bool grounded)
        {
            if (!IsReady()) return;
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
