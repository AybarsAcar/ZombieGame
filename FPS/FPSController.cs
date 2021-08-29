using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

namespace Dead_Earth.Scripts.FPS
{
  public enum PlayerMoveStatus
  {
    NotMoving,
    Walking,
    Running,
    NotGrounded,
    Landing
  }

  /// <summary>
  /// First Person Controller
  /// </summary>
  [RequireComponent(typeof(CharacterController))]
  public class FPSController : MonoBehaviour
  {
    [SerializeField] private float walkSpeed = 1f;
    [SerializeField] private float runSpeed = 4.5f;
    [SerializeField] private float jumpSpeed = 7.5f;
    [SerializeField] private float stickToGroundForce = 5.5f;
    [SerializeField] private float gravityMultiplier = 2.5f;

    [SerializeField] private MouseLook mouseLook;

    // cached object references
    private Camera _camera;
    private CharacterController _characterController;

    // cached state variables
    private bool _jumpButtonPressed = false;
    private Vector2 _inputVector = Vector2.zero; // horizontal and vertical axis inputs
    private Vector3 _moveDirection = Vector3.zero;
    private bool _previouslyGrounded = false;
    private bool _isWalking = true;
    private bool _isJumping = false;
    private PlayerMoveStatus _movementStatus = PlayerMoveStatus.NotMoving;

    // timers
    private float _fallingTimer = 0f;

    // public accessor
    public PlayerMoveStatus MovementStatus => _movementStatus;
    public float WalkSpeed => walkSpeed;
    public float RunSpeed => runSpeed;

    private void Start()
    {
      // cache the game objects
      _characterController = GetComponent<CharacterController>();
      _camera = Camera.main;

      // set the initial state
      _movementStatus = PlayerMoveStatus.NotMoving;

      // reset timers
      _fallingTimer = 0f;

      // initialise the MouseLook
      mouseLook.Init(transform, _camera.transform);
    }

    private void Update()
    {
      // if we are falling increment timer
      if (_characterController.isGrounded)
      {
        _fallingTimer = 0f;
      }
      else
      {
        _fallingTimer += Time.deltaTime;
      }

      // allow mouse look a chance to process mouse and rotate camera
      // make sure we have not paused the game
      if (Time.timeScale > Mathf.Epsilon)
      {
        mouseLook.LookRotation(transform, _camera.transform);
      }

      // process the jump
      // the jump state needs to read here to make sure it is not missed
      if (!_jumpButtonPressed)
      {
        _jumpButtonPressed = Input.GetButtonDown("Jump");
      }

      // player was in the air but now grounded
      if (!_previouslyGrounded && _characterController.isGrounded)
      {
        // we have just landed
        if (_fallingTimer > 0.5f)
        {
          // TODO: play landing sound and animation
        }

        // set the state for landing
        _moveDirection.y = 0f;
        _isJumping = false;
        _movementStatus = PlayerMoveStatus.Landing;
      }
      else if (!_characterController.isGrounded)
      {
        _movementStatus = PlayerMoveStatus.NotGrounded;
      }
      else if (_characterController.velocity.sqrMagnitude < 0.01f)
      {
        _movementStatus = PlayerMoveStatus.NotMoving;
      }
      else if (_isWalking)
      {
        _movementStatus = PlayerMoveStatus.Walking;
      }
      else
      {
        _movementStatus = PlayerMoveStatus.Running;
      }

      _previouslyGrounded = _characterController.isGrounded;
    }

    private void FixedUpdate()
    {
      // read the inputs
      var horizontal = Input.GetAxis("Horizontal");
      var vertical = Input.GetAxis("Vertical");

      var wasWalking = _isWalking;
      _isWalking = !Input.GetKey(KeyCode.LeftShift);

      var speed = _isWalking ? walkSpeed : runSpeed;

      _inputVector = new Vector2(horizontal, vertical);

      if (_inputVector.sqrMagnitude > 1)
      {
        // normalise it, this runs when we are hitting both keys at the same time
        // to avoid moving faster diagonally
        _inputVector.Normalize();
      }

      // always move along the camera forward as it is the direction that it being aimed at
      var desiredMove = transform.forward * _inputVector.y + transform.right * _inputVector.x;

      // get a normal for hte surface that is being touched to move along
      // so the character doesnt cut through the geometry
      if (Physics.SphereCast(transform.position, _characterController.radius, Vector3.down, out var hitInfo,
        _characterController.height / 2f, 1))
      {
        // update the desired move so we can properly move up / down hills, etc
        desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;
      }

      // scale movement by our current speed
      _moveDirection.x = desiredMove.x * speed; // sideways movement
      _moveDirection.z = desiredMove.z * speed; // forward backward movement

      if (_characterController.isGrounded)
      {
        // apply severe down force to keep control sticking to floor
        _moveDirection.y = -stickToGroundForce;

        // if the hump button was pressed then apply speed in up direction
        // and set isJumping to true. Also, reset jump button status
        if (_jumpButtonPressed)
        {
          _moveDirection.y = jumpSpeed;
          _jumpButtonPressed = false;
          _isJumping = true;
          // TODO: play jumping sound
        }
      }
      else
      {
        // we are in the air and falling
        _moveDirection += Physics.gravity * gravityMultiplier * Time.fixedDeltaTime;
      }

      // move character controller
      _characterController.Move(_moveDirection * Time.fixedDeltaTime);
    }
  }
}