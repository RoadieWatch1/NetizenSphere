using UnityEngine;
using UnityEngine.InputSystem;

namespace NetizenSphere.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
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

        private void OnEnable()
        {
            _playerControls.Enable();
            _playerControls.Player.Move.performed += OnMovePerformed;
            _playerControls.Player.Move.canceled += OnMoveCanceled;
            _playerControls.Player.Jump.performed += OnJumpPerformed;
        }

        private void OnDisable()
        {
            _playerControls.Player.Move.performed -= OnMovePerformed;
            _playerControls.Player.Move.canceled -= OnMoveCanceled;
            _playerControls.Player.Jump.performed -= OnJumpPerformed;
            _playerControls.Disable();
        }

        private void Update()
        {
            HandleGroundCheck();
            HandleMovement();
            HandleGravity();
        }

        private void HandleGroundCheck()
        {
            if (groundCheck == null)
            {
                Debug.LogWarning("Ground Check is not assigned on PlayerMovement.", this);
                return;
            }

            _isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

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
            _moveInput = context.ReadValue<Vector2>();
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            _moveInput = Vector2.zero;
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            if (!_isGrounded)
            {
                return;
            }

            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }
}
