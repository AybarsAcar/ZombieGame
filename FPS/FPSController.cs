using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

namespace Dead_Earth.Scripts.FPS
{
  public enum PlayerMoveStatus
  {
    NotMoving,
    Walking,
    Running,
    Crouching,
    NotGrounded,
    Landing,
  }

  /// <summary>
  /// describes whether it should be processed by xPlayHead or yPlayHead
  /// </summary>
  public enum CurveControllerBobCallbackType
  {
    Horizontal,
    Vertical
  }

  // delegates
  public delegate void CurveControlledBobCallback();


  /// <summary>
  /// First Person Controller
  /// </summary>
  [RequireComponent(typeof(CharacterController))]
  public class FPSController : MonoBehaviour
  {
    [SerializeField] private float walkSpeed = 2.4f;
    [SerializeField] private float runSpeed = 4.5f;
    [SerializeField] private float jumpSpeed = 7.5f;
    [SerializeField] private float crouchSpeed = 1f;
    [SerializeField] private float stickToGroundForce = 5.5f;
    [SerializeField] private float gravityMultiplier = 2.5f;

    [SerializeField] private GameObject flashLight;

    [SerializeField] private MouseLook mouseLook;

    [SerializeField] private CurveControlledBob headBob = new CurveControlledBob();

    // used so when we run our head bob doesn't increase linearly 
    [SerializeField] private float runSpeedHeadBobFactor = 0.7f;


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
    private bool _isCrouching = false;
    private PlayerMoveStatus _movementStatus = PlayerMoveStatus.NotMoving;
    private Vector3 _localSpaceCameraPos = Vector3.zero;
    private float _controllerHeight = 0f; // initial height of the controller standing up position

    // test for footsteps
    private readonly List<AudioSource> _audioSources = new List<AudioSource>();
    private int _audioToUse = 0;


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
      _controllerHeight = _characterController.height; // cache the height

      _camera = Camera.main;

      // fill audio sources
      foreach (var audioSource in GetComponents<AudioSource>())
      {
        _audioSources.Add(audioSource);
      }

      // cache the local position of the camera
      _localSpaceCameraPos = _camera.transform.localPosition;

      // set the initial state
      _movementStatus = PlayerMoveStatus.NotMoving;

      // reset timers
      _fallingTimer = 0f;

      // initialise the MouseLook
      mouseLook.Init(transform, _camera.transform);

      // initialise the Head Bob Animation
      headBob.Init();

      headBob.RegisterEventCallback(1.5f, PlayFootStepSound, CurveControllerBobCallbackType.Vertical);

      if (flashLight)
      {
        flashLight.SetActive(false);
      }
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
      if (!_jumpButtonPressed && !_isCrouching)
      {
        _jumpButtonPressed = Input.GetButtonDown("Jump");
      }

      // if crouch button is pressed - update the local state
      if (Input.GetButtonDown("Crouch"))
      {
        _isCrouching = !_isCrouching;

        // set the height
        _characterController.height = _isCrouching ? _controllerHeight / 2 : _controllerHeight;
      }

      if (Input.GetButtonDown("Flashlight"))
      {
        flashLight.SetActive(!flashLight.activeSelf);
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
      else if (_isCrouching)
      {
        _movementStatus = PlayerMoveStatus.Crouching;
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

      var speed = _isCrouching ? crouchSpeed : _isWalking ? walkSpeed : runSpeed;

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

      // move our camera position based on the head bob
      // make sure the character is moving
      var horizontalSpeed = new Vector3(_characterController.velocity.x, 0f, _characterController.velocity.z);
      if (horizontalSpeed.magnitude > 0.01f)
      {
        var multiplier = _isCrouching || _isWalking ? 1 : runSpeedHeadBobFactor;

        // animate the camera
        _camera.transform.localPosition =
          _localSpaceCameraPos + headBob.GetVectorOffset(horizontalSpeed.magnitude * multiplier);
      }
      else
      {
        _camera.transform.localPosition = _localSpaceCameraPos;
      }
    }

    private void PlayFootStepSound()
    {
      if (_isCrouching) return;

      _audioSources[_audioToUse].Play();

      // alternate
      _audioToUse = _audioToUse == 0 ? 1 : 0;
    }
  }
}