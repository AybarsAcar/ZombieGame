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

    private bool _isCrawling = false;
    private int _seeking = 0;
    private bool _isFeeding = false;
    private int _attackType = 0;

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
      get => _navMeshAgent != null ? _navMeshAgent.speed : 0f;
      set
      {
        if (_navMeshAgent != null)
        {
          _navMeshAgent.speed = value;
        }
      }
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
        _animator.SetFloat(_speedHash, _navMeshAgent.speed);
        animator.SetInteger(_seekingHash, _seeking);
        animator.SetBool(_isFeedingHash, _isFeeding);
        animator.SetInteger(_attackHash, _attackType);
      }
    }
  }
}