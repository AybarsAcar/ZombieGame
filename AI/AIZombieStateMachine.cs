using Dead_Earth.Scripts.FPS;
using UnityEngine;

namespace Dead_Earth.Scripts.AI
{
  /// <summary>
  /// too keep track of the Zombie whether the bones of the Zombie
  /// controlled by the Animator or the Physics System
  /// </summary>
  public enum AIBoneControlType
  {
    Animated, // the default type
    RagDoll,
    RagDollToAnim
  };

  /// <summary>
  /// State Machine for the Zombie NPCs
  /// </summary>
  public class AIZombieStateMachine : AIStateMachine
  {
    [Header("Zombie Stats")] [SerializeField] [Range(10f, 360f)]
    private float fieldOfView = 50f;

    // if 1 then can see all the way to the trigger volume radius
    // if 0 then blind
    [SerializeField] [Range(0f, 1f)] private float sight = 0.5f;

    // if 1 then can hear all the way in the sensor trigger radius
    // if 0 then deaf
    [SerializeField] [Range(0f, 1f)] private float hearing = 1f;

    [SerializeField] [Range(0f, 1f)] private float aggression = 0.5f;

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

    [Header("Zombie Health & Damage Stats")]
    // health only decreases when damage to the head
    [SerializeField]
    [Range(0, 100)]
    private int health = 100;

    [Tooltip("When damage is 100 zombie can't use its lower body")] [SerializeField] [Range(0, 100)]
    private int lowerBodyDamage = 0;

    [Tooltip("When damage is 100 zombie can't use its upper body")] [SerializeField] [Range(0, 100)]
    private int upperBodyDamage = 0;

    // as the damage passes the threshold we will activate the upper body layer in the Animator
    [Tooltip("Threshold that affects the Animator")] [SerializeField] [Range(0, 100)]
    private int upperBodyThreshold = 30;

    // as the damage passes the threshold we will activate the lower body layer in the Animator
    [Tooltip("Threshold that affects the Animator")] [SerializeField] [Range(0, 100)]
    private int limpThreshold = 30;

    [SerializeField] [Range(0, 100)] private int crawlThreshold = 90;


    private int _seeking = 0;
    private bool _isFeeding = false;
    private int _attackType = 0;
    private float _speed = 0f;

    // RagDoll members
    private AIBoneControlType _boneControlType = AIBoneControlType.Animated;

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

    public bool IsCrawling => lowerBodyDamage >= crawlThreshold;

    protected override void Start()
    {
      base.Start();

      UpdateAnimatorDamage();
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
    /// updates the animator state based on the damage taken
    /// </summary>
    protected void UpdateAnimatorDamage()
    {
      if (_animator != null)
      {
        _animator.SetBool(_isCrawlingHash, IsCrawling);
      }
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

      // rag doll the AIZombie
      var hitStrength = force.magnitude;

      // if the zombie is previously rag dolled and currently in the rag doll state
      if (_boneControlType == AIBoneControlType.RagDoll)
      {
        if (bodyPart != null)
        {
          if (hitStrength > 1f)
          {
            bodyPart.AddForce(Vector3.forward, ForceMode.Impulse);
          }

          if (bodyPart.CompareTag("Head"))
          {
            // head is hit
            health = Mathf.Max(health - damage, 0);
          }
          else if (bodyPart.CompareTag("Upper Body"))
          {
            upperBodyDamage += damage;
          }
          else if (bodyPart.CompareTag("Lower Body"))
          {
            lowerBodyDamage += damage;
          }

          // make changes to the Animator
          UpdateAnimatorDamage();

          if (health > 0)
          {
            // Re-animate Zombie
          }
        }

        return;
      }

      // relative character manager position relative to the zombie's position 
      var attackerLocalPosition = transform.InverseTransformPoint(characterManager.transform.position);

      // get local space position of hit
      var hitLocalPosition = transform.InverseTransformPoint(position);

      var shouldRagDoll = hitStrength > 50f;


      if (bodyPart != null)
      {
        if (bodyPart.CompareTag("Head"))
        {
          // head is hit
          health = Mathf.Max(health - damage, 0);

          if (health <= 0)
          {
            shouldRagDoll = true;
          }
        }
        else if (bodyPart.CompareTag("Upper Body"))
        {
          upperBodyDamage += damage;

          // make changes to the Animator
          UpdateAnimatorDamage();
        }
        else if (bodyPart.CompareTag("Lower Body"))
        {
          lowerBodyDamage += damage;

          // make changes to the Animator
          UpdateAnimatorDamage();

          // we will always rag doll the zombie when hit on the legs
          shouldRagDoll = true;
        }
      }

      if (_boneControlType != AIBoneControlType.Animated || IsCrawling || _cinematicEnabled ||
          attackerLocalPosition.z < 0)
      {
        shouldRagDoll = true;
      }

      if (!shouldRagDoll)
      {
        var angle = 0f;

        if (hitDirection == 0)
        {
          var vecToHit = (position - transform.position).normalized;

          angle = AIState.FindSignedAngle(vecToHit, transform.forward);
        }

        // decide which of the hit animation to play
        var hitType = 0;
        if (bodyPart.gameObject.CompareTag("Head"))
        {
          if (angle < -10 || hitDirection == -1)
          {
            hitType = 1;
          }
          else if (angle > 10 || hitDirection == 1)
          {
            hitType = 3;
          }
          else
          {
            hitType = 2;
          }
        }
        else if (bodyPart.gameObject.CompareTag("Upper Body"))
        {
          if (angle < -20 || hitDirection == -1)
          {
            hitType = 4;
          }
          else if (angle > 20 || hitDirection == 1)
          {
            hitType = 6;
          }
          else
          {
            hitType = 5;
          }
        }

        // set the hit type and the hit trigger in the Zombie's animator
        _animator.SetTrigger(_hitHash);
        _animator.SetInteger(_hitTypeHash, hitType);

        return;
      }

      // handle when should rag doll TRANSITION TO RAG DOLL NOW
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

      _boneControlType = AIBoneControlType.RagDoll;

      if (health > 0)
      {
        // should re-animate
      }
    }
  }
}