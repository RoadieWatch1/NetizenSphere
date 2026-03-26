using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

namespace NetizenSphere.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : NetworkBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float gravity = -9.81f;
        [SerializeField] private float jumpHeight = 1.2f;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundDistance = 0.2f;
        [SerializeField] private LayerMask groundMask;

        private CharacterController _characterController;
        private PlayerControls _playerControls;

        private Vector2 _moveInput;
        private Vector3 _velocity;
        private bool _isGrounded;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _playerControls = new PlayerControls();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                // Non-owners are positioned by NetworkTransform — CharacterController
                // would resist those position updates, so disable it.
                _characterController.enabled = false;
                return;
            }

            _playerControls.Enable();
            _playerControls.Player.Move.performed += OnMovePerformed;
            _playerControls.Player.Move.canceled += OnMoveCanceled;
            _playerControls.Player.Jump.performed += OnJumpPerformed;

            if (Camera.main != null)
            {
                CameraFollow cameraFollow = Camera.main.GetComponent<CameraFollow>();
                if (cameraFollow == null)
                    cameraFollow = Camera.main.gameObject.AddComponent<CameraFollow>();
                cameraFollow.SetTarget(transform);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                _playerControls.Player.Move.performed -= OnMovePerformed;
                _playerControls.Player.Move.canceled -= OnMoveCanceled;
                _playerControls.Player.Jump.performed -= OnJumpPerformed;
            }
        }

        public override void OnDestroy()
        {
            _playerControls?.Disable();
            _playerControls?.Dispose();
            base.OnDestroy();
        }

        public Vector3 GetVelocity() => _characterController.velocity;

        public bool IsGrounded() => _isGrounded;

        private void Update()
        {
            if (!IsSpawned || !IsOwner)
            {
                return;
            }

            HandleGroundCheck();
            HandleMovement();
            HandleGravity();
        }

        private void HandleGroundCheck()
        {
            if (groundCheck == null)
            {
                // Fall back to CharacterController built-in grounding when the
                // child transform isn't wired.
                _isGrounded = _characterController.isGrounded;
            }
            else if (groundMask == 0)
            {
                // groundMask left at default Nothing — Physics.CheckSphere would
                // never hit any layer. Use CharacterController.isGrounded instead.
                _isGrounded = _characterController.isGrounded;
            }
            else
            {
                _isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
            }

            if (_isGrounded && _velocity.y < 0f)
            {
                _velocity.y = -2f;
            }
        }

        private void HandleMovement()
        {
            Vector3 move = transform.right * _moveInput.x + transform.forward * _moveInput.y;
            _characterController.Move(move * moveSpeed * Time.deltaTime);
        }

        private void HandleGravity()
        {
            _velocity.y += gravity * Time.deltaTime;
            _characterController.Move(_velocity * Time.deltaTime);
        }

        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            if (!IsOwner)
            {
                return;
            }

            _moveInput = context.ReadValue<Vector2>();
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            if (!IsOwner)
            {
                return;
            }

            _moveInput = Vector2.zero;
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            if (!IsOwner || !_isGrounded)
            {
                return;
            }

            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }
}
