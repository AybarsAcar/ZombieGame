using System.Collections;
using System.Collections.Generic;
using Dead_Earth.Scripts.FPS;
using UnityEngine;
using UnityEngine.AI;

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
  /// to decide whether to spawn the sound emitter at the AI Zombie
  /// or at the Player object, game design choice
  /// </summary>
  public enum AIScreamPosition
  {
    Entity, // Spawns at the AI Zombie Game Object's position
    Player, // Spawns at the Player Game Object's position
  }

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

    [Header("Zombie Scream Sound Configuration")]
    [Tooltip("The probability of a Zombie can scream when spotting the player")]
    [SerializeField]
    [Range(0f, 1f)]
    private float screamChance = 1f;

    [Tooltip("The range of the scream, correlates to the strength of the scream")] [SerializeField]
    private float screamRadius = 20f;

    [SerializeField] private AIScreamPosition screamPosition = AIScreamPosition.Player;
    [SerializeField] private AISoundEmitter screamEmitterPrefab;

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

    [Header("Animation")] [Tooltip("Blend time from the rag doll position to animated state")] [SerializeField]
    private float reanimationBlendTime = 1.5f;

    [SerializeField] [Tooltip("Time from we rag dolled the AI to reanimate")]
    private float reanimationWaitTime = 3f;

    // set it to default if we dont have specialised layers for geometry
    [Tooltip("What layer we wish it to consider as geometry for raycast")] [SerializeField]
    private LayerMask geometryLayers;


    private int _seeking = 0;
    private bool _isFeeding = false;
    private int _attackType = 0;
    private float _speed = 0f;
    private float _screaming = 0f;

    // RagDoll members
    private AIBoneControlType _boneControlType = AIBoneControlType.Animated;
    private List<BodyPartSnapshot> _bodyPartSnapshots = new List<BodyPartSnapshot>();
    private float _ragDollEndTime = float.MinValue;
    private Vector3 _ragDollHipPosition;
    private Vector3 _ragDollFeetPosition;
    private Vector3 _ragDollHeadPosition;
    private IEnumerator _reanimationCoroutine = null;

    // delay variable for the late update, to bias the t value when lerping
    private float _mechanimTransitionTime = 0.1f;


    // Animator Hashes
    private readonly int _speedHash = Animator.StringToHash("speed");
    private readonly int _seekingHash = Animator.StringToHash("seeking");
    private readonly int _isFeedingHash = Animator.StringToHash("isFeeding");
    private readonly int _isCrawlingHash = Animator.StringToHash("isCrawling");
    private readonly int _attackHash = Animator.StringToHash("attack");
    private readonly int _hitHash = Animator.StringToHash("hit");
    private readonly int _hitTypeHash = Animator.StringToHash("hitType");
    private readonly int _reanimateFromFrontHash = Animator.StringToHash("reanimateFromFront");
    private readonly int _reanimateFromBackHash = Animator.StringToHash("reanimateFromBack");
    private readonly int _upperBodyDamageHash = Animator.StringToHash("upperBodyDamage");
    private readonly int _lowerBodyDamageHash = Animator.StringToHash("lowerBodyDamage");
    private readonly int _stateHash = Animator.StringToHash("state");
    private readonly int _screamingHash = Animator.StringToHash("screaming");
    private readonly int _screamHash = Animator.StringToHash("scream");

    // Animator Layers
    private int _upperBodyLayer = -1;
    private int _lowerBodyLayer = -1;

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

    public bool IsScreaming => _screaming > 0.1f;

    public float ScreamChance => screamChance;


    protected override void Start()
    {
      base.Start();

      // cache the Animator layer indices
      _lowerBodyLayer = _animator.GetLayerIndex("Lower Body");
      _upperBodyLayer = _animator.GetLayerIndex("Upper Body");

      // take a snapshot of the bones of the root bone
      if (rootBone != null)
      {
        var transforms = rootBone.GetComponentsInChildren<Transform>();
        foreach (var t in transforms)
        {
          var snapShot = new BodyPartSnapshot { transform = t };
          _bodyPartSnapshots.Add(snapShot);
        }
      }

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
        _animator.SetInteger(_stateHash, (int)currentStateType);

        // are we screaming or not
        _screaming = IsLayerActive("Cinematic") ? 0f : _animator.GetFloat(_screamingHash);
      }

      // reduce the satisfaction of the zombie so it gets hungry
      satisfaction = Mathf.Max(0, satisfaction - ((depletionRate * Time.deltaTime) / 100f) * Mathf.Pow(_speed, 2));
    }

    /// <summary>
    /// updates the animator state based on the damage taken
    /// </summary>
    protected void UpdateAnimatorDamage()
    {
      if (_animator == null) return;

      if (_lowerBodyLayer != -1)
      {
        // make sure do not activate this layer when crawling
        var weight = lowerBodyDamage > limpThreshold && lowerBodyDamage < crawlThreshold ? 1f : 0f;
        _animator.SetLayerWeight(_lowerBodyLayer, weight);
      }

      if (_upperBodyLayer != -1)
      {
        // make sure do not activate this layer when crawling
        var weight = upperBodyDamage > upperBodyThreshold && lowerBodyDamage < crawlThreshold ? 1f : 0f;
        _animator.SetLayerWeight(_upperBodyLayer, weight);
      }

      _animator.SetBool(_isCrawlingHash, IsCrawling);

      _animator.SetInteger(_lowerBodyDamageHash, lowerBodyDamage);
      _animator.SetInteger(_upperBodyDamageHash, upperBodyDamage);

      // set the active layers in the animation state because we set this layer active
      // from changing its layer weight
      if (lowerBodyDamage > limpThreshold && lowerBodyDamage < crawlThreshold)
      {
        // set it from code because we don't have an empty state in the layer
        SetLayerActive("Lower Body", true);
      }
      else
      {
        SetLayerActive("Lower Body", false);
      }

      if (upperBodyDamage > upperBodyThreshold && lowerBodyDamage < crawlThreshold)
      {
        // set it from code because we don't have an empty state in the layer
        SetLayerActive("Upper Body", true);
      }
      else
      {
        SetLayerActive("Upper Body", false);
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
            if (_reanimationCoroutine != null)
            {
              StopCoroutine(_reanimationCoroutine);
            }

            // should re-animate
            _reanimationCoroutine = ReanimateCoroutine();
            StartCoroutine(_reanimationCoroutine);
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

      if (_boneControlType != AIBoneControlType.Animated || IsCrawling || IsLayerActive("Cinematic") ||
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
        if (_reanimationCoroutine != null)
        {
          StopCoroutine(_reanimationCoroutine);
        }

        // should re-animate
        _reanimationCoroutine = ReanimateCoroutine();
        StartCoroutine(_reanimationCoroutine);
      }
    }

    /// <summary>
    /// Starts the Reanimation Routine from the RagDoll state to Animated State
    /// this reanimation system works with only humanoid avatars
    /// </summary>
    /// <returns></returns>
    protected IEnumerator ReanimateCoroutine()
    {
      if (_boneControlType != AIBoneControlType.RagDoll || _animator == null) yield break;

      yield return new WaitForSeconds(reanimationWaitTime);

      _ragDollEndTime = Time.time;

      // make the rigidbodies kinematic
      foreach (var body in _bodyParts)
      {
        // to prevent the physics system to change its position and rotation
        body.isKinematic = true;
      }

      // start the bone blending
      _boneControlType = AIBoneControlType.RagDollToAnim;

      foreach (var snapshot in _bodyPartSnapshots)
      {
        // store a reference of the transform properties of the bone snapshots before the animation starts
        // Positions and the rotations
        // the state of the rag doll is snapshot
        snapshot.position = snapshot.transform.position;
        snapshot.rotation = snapshot.transform.rotation;
      }

      // store the rag doll's head and feet position
      _ragDollHeadPosition = _animator.GetBoneTransform(HumanBodyBones.Head).position;
      _ragDollFeetPosition = (_animator.GetBoneTransform(HumanBodyBones.LeftFoot).position +
                              _animator.GetBoneTransform(HumanBodyBones.RightFoot).position) * 0.5f;
      _ragDollHipPosition = rootBone.position;

      // Enable Animator
      _animator.enabled = true;

      // find the way the AI is facing
      // get the vector based on where the hip forward vector is facing
      var forwardTest = rootBoneAlignment switch
      {
        AIBoneAlignmentType.ZAxis => rootBone.forward.y,
        AIBoneAlignmentType.ZAxisInverted => -rootBone.forward.y,
        AIBoneAlignmentType.YAxis => rootBone.up.y,
        AIBoneAlignmentType.YAxisInverted => -rootBone.up.y,
        AIBoneAlignmentType.XAxis => rootBone.right.y,
        AIBoneAlignmentType.XAxisInverted => -rootBone.right.y,
        _ => rootBone.forward.y
      };

      if (forwardTest >= 0)
      {
        _animator.SetTrigger(_reanimateFromBackHash);
      }
      else
      {
        _animator.SetTrigger(_reanimateFromFrontHash);
      }
    }

    /// <summary>
    /// Every frame after the Updates finish executing
    /// </summary>
    protected virtual void LateUpdate()
    {
      if (_boneControlType == AIBoneControlType.RagDollToAnim)
      {
        if (Time.time <= _ragDollEndTime + _mechanimTransitionTime)
        {
          // wait state
          var animatedToRagDoll = _ragDollHipPosition - rootBone.position;

          // calculate the new position of the AI Entity's position
          // how we wish to position
          var newRootPos = transform.position + animatedToRagDoll;

          var hits = Physics.RaycastAll(newRootPos + Vector3.up * 0.5f, Vector3.down, float.MaxValue, geometryLayers);

          // make sure to choose the highest surface
          newRootPos.y = float.MinValue;
          foreach (var hit in hits)
          {
            if (!hit.transform.IsChildOf(transform))
            {
              newRootPos.y = Mathf.Max(hit.point.y, newRootPos.y);
            }
          }

          // factor in the base offset of the NavMeshAgent
          var baseOffset = Vector3.zero;
          if (_navMeshAgent != null)
          {
            baseOffset.y = _navMeshAgent.baseOffset;
          }

          // snap to the nav mesh
          if (NavMesh.SamplePosition(newRootPos, out var navMeshHit, 2f, NavMesh.AllAreas))
          {
            transform.position = navMeshHit.position + baseOffset;
          }
          else
          {
            transform.position = newRootPos + baseOffset;
          }

          // orient the game object so it faces the same direction as the rag doll
          var ragDollDirection = _ragDollHeadPosition - _ragDollFeetPosition;
          ragDollDirection.y = 0f; // we only want to rotate in the x-z plane

          var meanFeetPos = 0.5f * (_animator.GetBoneTransform(HumanBodyBones.LeftFoot).position +
                                    _animator.GetBoneTransform(HumanBodyBones.RightFoot).position);

          var animatedDirection = _animator.GetBoneTransform(HumanBodyBones.Head).position - meanFeetPos;
          animatedDirection.y = 0; // we only want to rotate in the x-z plane

          // Try to match the rotations. Note that we can only rotate around y axis as the animated character
          // must stay upright, hence setting the y components of the vectors to zero
          transform.rotation *= Quaternion.FromToRotation(animatedDirection.normalized, ragDollDirection.normalized);
        }

        // start the blending
        // calculate interpolation value
        // time passed since rag doll state ended / reanimation blend time
        // we also need to bias this
        var blendAmount = Mathf.Clamp01((Time.time - _ragDollEndTime - _mechanimTransitionTime) / reanimationBlendTime);

        foreach (var snapshot in _bodyPartSnapshots)
        {
          if (snapshot.transform == rootBone)
          {
            // transition from the rag doll positions and the rotations to the position and rotation
            // described by the animator
            snapshot.transform.position = Vector3.Lerp(snapshot.position, snapshot.transform.position, blendAmount);
          }

          // it is not the root bone so we will only rotate it to the rotate to the rotations described
          // by the animator
          snapshot.transform.rotation = Quaternion.Slerp(snapshot.rotation, snapshot.transform.rotation, blendAmount);
        }

        // detect fully give control to the Animator and 0 Rag doll
        // exits reanimation mode
        if (blendAmount >= 1f)
        {
          _boneControlType = AIBoneControlType.Animated;
          _navMeshAgent.enabled = true;
          _collider.enabled = true;

          // put the zombie back into a valid state
          if (_states.TryGetValue(AIStateType.Alerted, out var newState))
          {
            if (_currentState != null)
            {
              _currentState.OnExitState();
            }

            newState.OnEnterState();
            _currentState = newState;
            currentStateType = AIStateType.Alerted;
          }
        }
      }
    }

    public bool Scream()
    {
      if (_screaming > 0.1f) return true;

      if (_animator == null || IsLayerActive("Cinematic") || screamEmitterPrefab == null)
      {
        return false;
      }

      _animator.SetTrigger(_screamHash);

      var spawnPos = screamPosition == AIScreamPosition.Player ? visualThreat.position : transform.position;

      var emitter = Instantiate(screamEmitterPrefab, spawnPos, Quaternion.identity);
      emitter.SetRadius(screamRadius);

      return true;
    }
  }
}