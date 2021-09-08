using Dead_Earth.Scripts.AI;
using Dead_Earth.Scripts.ImageEffects;
using UnityEngine;

namespace Dead_Earth.Scripts.FPS
{
  public class CharacterManager : MonoBehaviour
  {
    [SerializeField] private CapsuleCollider meleeTrigger;
    [SerializeField] private CameraBloodEffect cameraBloodEffect;

    [SerializeField] private float health = 100f;

    [Header("Player Sound Triggers")] [SerializeField]
    private AISoundEmitter soundEmitter;

    [SerializeField] private float walkSoundRadius = 2f;
    [SerializeField] private float runSoundRadius = 7f;
    [SerializeField] private float landSoundRadius = 12f;
    [SerializeField] private float bloodRadiusScale = 6f;

    private Camera _camera;

    private Collider _collider;
    private FPSController _fpsController;
    private CharacterController _characterController;
    private GameSceneManager _gameSceneManager;
    private int _aiBodyPartLayer = -1;

    private void Start()
    {
      _collider = GetComponent<Collider>();
      _fpsController = GetComponent<FPSController>();
      _characterController = GetComponent<CharacterController>();

      _gameSceneManager = GameSceneManager.Instance;

      _camera = Camera.main;

      _aiBodyPartLayer = LayerMask.NameToLayer("AI Body Part");

      if (_gameSceneManager != null)
      {
        // register
        var info = new PlayerInfo
        {
          camera = _camera,
          characterManager = this,
          collider = _collider,
          meleeTrigger = meleeTrigger
        };

        _gameSceneManager.RegisterPlayerInfo(_collider.GetInstanceID(), info);
      }
    }

    private void Update()
    {
      if (Input.GetMouseButtonDown(0))
      {
        DoDamage();
      }

      if (_fpsController != null && soundEmitter != null)
      {
        var newRadius = 0f; // 0 if standing or crouching

        // imitate the damaged (smell of blood) using the sound system
        // if we are very damaged we will trigger zombies even if not moving
        newRadius = Mathf.Max(newRadius, (100f - health) / bloodRadiusScale);

        newRadius = _fpsController.MovementStatus switch
        {
          PlayerMoveStatus.Landing => Mathf.Max(newRadius, landSoundRadius),
          PlayerMoveStatus.Walking => Mathf.Max(newRadius, walkSoundRadius),
          PlayerMoveStatus.Running => Mathf.Max(newRadius, runSoundRadius),
          _ => newRadius
        };

        soundEmitter.SetRadius(newRadius);

        _fpsController.DragMultiplierLimit = Mathf.Max(health / 100f, 0.25f);
      }
    }

    /// <summary>
    /// Handles player doing damage on the AI
    /// +1 -> left to right
    /// -1 -> right to left
    /// </summary>
    /// <param name="hitDirection">to tell if going from right to left or left to right</param>
    public void DoDamage(int hitDirection = 0)
    {
      if (_camera == null) return;
      if (_gameSceneManager == null) return;

      Ray ray;
      RaycastHit hit;
      var isSomethingHit = false;

      // cast a ray from the centre of the screen
      ray = _camera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));

      isSomethingHit = Physics.Raycast(ray, out hit, 1000f, 1 << _aiBodyPartLayer);

      if (isSomethingHit)
      {
        // we have hit something
        // get the AiStateMachine from the GameSceneManger
        var aiStateMachine = _gameSceneManager.GetAIStateMachine(hit.rigidbody.GetInstanceID());

        if (aiStateMachine != null)
        {
          aiStateMachine.TakeDamage(hit.point, ray.direction * 25f, 25, hit.rigidbody, this, 0);
        }
      }
    }

    /// <summary>
    /// Decreases the Player Health based on the damage taken
    /// </summary>
    /// <param name="damageAmount"></param>
    public void TakeDamage(float damageAmount)
    {
      health = Mathf.Max(0, health - damageAmount * Time.deltaTime);

      // when we take damage we will stop for a split second
      _fpsController.DragMultiplier = 0f;

      if (cameraBloodEffect != null)
      {
        cameraBloodEffect.MinBloodAmount = (1f - health / 100f) / 1.5f;
        cameraBloodEffect.BloodAmount = Mathf.Min(cameraBloodEffect.MinBloodAmount + 0.3f, 1f);
      }
    }
  }
}