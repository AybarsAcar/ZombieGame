using Dead_Earth.Scripts.FPS;
using UnityEngine;

namespace Dead_Earth.Scripts.AI
{
  /// <summary>
  /// State Machine for the Zombie NPCs
  /// </summary>
  public class AIZombieStateMachine : AIStateMachine
  {
    [SerializeField] [Range(10f, 360f)] private float fieldOfView = 50f;

    // if 1 then can see all the way to the trigger volume radius
    // if 0 then blind
    [SerializeField] [Range(0f, 1f)] private float sight = 0.5f;

    // if 1 then can hear all the way in the sensor trigger radius
    // if 0 then deaf
    [SerializeField] [Range(0f, 1f)] private float hearing = 1f;

    [SerializeField] [Range(0f, 1f)] private float aggression = 0.5f;

    [SerializeField] [Range(0, 100)] private int health = 100;

    [SerializeField] [Range(0f, 1f)] private float intelligence = 0.5f;

    // similar to hunger, but more general
    // if below a certain threshold then feeds
    [SerializeField] [Range(0f, 1f)] private float satisfaction = 1f;

    // replenish rate of the zombie while feeding
    // in Percentage - divided by 100f in calculation
    [Tooltip("Satisfaction replenish rate in Percentage while feeding")] [SerializeField]
    private float replenishRate = 2f;

    // how quickly satisfaction depletes, multiplied with the speed
    // in Percentage - divided by 100f in calculation
    [Tooltip("Satisfaction depleting rate in Percentage while moving")] [SerializeField]
    private float depletionRate = 0.4f;


    private bool _isCrawling = false;
    private int _seeking = 0;
    private bool _isFeeding = false;
    private int _attackType = 0;
    private float _speed = 0f;

    // Animator Hashes
    private readonly int _speedHash = Animator.StringToHash("speed");
    private readonly int _seekingHash = Animator.StringToHash("seeking");
    private readonly int _isFeedingHash = Animator.StringToHash("isFeeding");
    private readonly int _isCrawlingHash = Animator.StringToHash("isCrawling");
    private readonly int _attackHash = Animator.StringToHash("attack");
    private readonly int _hitHash = Animator.StringToHash("hit");
    private readonly int _hitTypeHash = Animator.StringToHash("hitType");

    // Public Getters / Setters
    public float FieldOfView => fieldOfView;
    public float Sight => sight;
    public float Hearing => hearing;
    public float ReplenishRate => replenishRate;

    public float Aggression
    {
      get => aggression;
      set => aggression = value;
    }

    public int Health
    {
      get => health;
      set => health = value;
    }

    public float Intelligence => intelligence;

    public float Satisfaction
    {
      get => satisfaction;
      set => satisfaction = value;
    }

    public int Seeking
    {
      get => _seeking;
      set => _seeking = value;
    }

    public bool IsFeeding
    {
      get => _isFeeding;
      set => _isFeeding = value;
    }

    public bool IsCrawling => _isCrawling;

    public int AttackType
    {
      get => _attackType;
      set => _attackType = value;
    }

    public float Speed
    {
      get => _speed;
      set => _speed = value;
    }

    /// <summary>
    /// Overriding the Update in our Base class
    /// to add more functionality
    /// </summary>
    protected override void Update()
    {
      base.Update();

      if (_animator != null)
      {
        // pass in the animator states
        _animator.SetFloat(_speedHash, _speed);
        _animator.SetInteger(_seekingHash, _seeking);
        _animator.SetBool(_isFeedingHash, _isFeeding);
        _animator.SetInteger(_attackHash, _attackType);
      }

      // reduce the satisfaction of the zombie so it gets hungry
      satisfaction = Mathf.Max(0, satisfaction - ((depletionRate * Time.deltaTime) / 100f) * Mathf.Pow(_speed, 2));
    }

    /// <summary>
    /// Override the Take damage from the base class
    /// </summary>
    /// <param name="position"></param>
    /// <param name="force"></param>
    /// <param name="damage"></param>
    /// <param name="bodyPart"></param>
    /// <param name="characterManager"></param>
    /// <param name="hitDirection"></param>
    public override void TakeDamage(Vector3 position, Vector3 force, int damage, Rigidbody bodyPart,
      CharacterManager characterManager, int hitDirection = 0)
    {
      base.TakeDamage(position, force, damage, bodyPart, characterManager, hitDirection);

      if (GameSceneManager.Instance != null && GameSceneManager.Instance.BloodParticleSystem != null)
      {
        var bloodSystem = GameSceneManager.Instance.BloodParticleSystem;

        bloodSystem.transform.position = position;
        var mainModule = bloodSystem.main;
        mainModule.simulationSpace = ParticleSystemSimulationSpace.World;
        bloodSystem.Emit(60);
      }

      health -= damage;

      // rag doll the AIZombie
      var hitStrength = force.magnitude;
      var shouldRagDoll = hitStrength > 1f || health <= 0;

      if (shouldRagDoll)
      {
        // completely clear the current state in the state machine
        if (_currentState != null)
        {
          _currentState.OnExitState();
          _currentState = null;
          currentStateType = AIStateType.None;
        }
        
        // turn off the following properties
        _navMeshAgent.enabled = false;
        _animator.enabled = false;
        _collider.enabled = false; // disable the main collider as well

        IsInMeleeRange = false;
        
        

        foreach (var body in _bodyParts)
        {
          if (body != null)
          {
            // allow physics system to interact with the body parts
            body.isKinematic = false;
          }
        }

        if (hitStrength > 1f)
        {
          bodyPart.AddForce(force, ForceMode.Impulse);
        }
      }
    }
  }
}