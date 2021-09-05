using System.Collections.Generic;
using Dead_Earth.Scripts.AI.StateMachineBehaviours;
using Dead_Earth.Scripts.FPS;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Dead_Earth.Scripts.AI
{
  /// <summary>
  /// States that AI can take
  /// also passed down to the Animator as an int
  /// </summary>
  public enum AIStateType
  {
    None, // 0
    Idle, // 1
    Alerted, // 2
    Attack, // 3
    Feeding, // 4
    Pursuit, // 5
    Dead, // 6
    Patrol // 7
  }

  /// <summary>
  /// Aggro enter the Zombie's sensor they are notified
  /// contains the threats and aggro that triggers the AIs
  /// </summary>
  public enum AITargetType
  {
    None,
    Waypoint,
    VisualPlayer,
    VisualLight,
    VisualFood,
    Audio
  };

  public enum AITriggerEventType
  {
    Enter,
    Stay,
    Exit
  }

  /// <summary>
  /// describes the direction of the forward vector of the hip bone
  /// Z Axis is hte most common case and should be case for all the models but sometimes it is not
  /// </summary>
  public enum AIBoneAlignmentType
  {
    XAxis,
    YAxis,
    ZAxis,
    XAxisInverted,
    YAxisInverted,
    ZAxisInverted
  }

  /// <summary>
  /// Describes a potential target to the AI System
  /// </summary>
  public struct AITarget
  {
    private AITargetType _type; // the type of target
    private Collider _collider; // the collider
    private Vector3 _position; // current position in the world
    private float _distance; // Distance from player
    private float _time; // time the target was last pinged

    public AITargetType type => _type;
    public Collider collider => _collider;
    public Vector3 position => _position;

    public float distance
    {
      get => _distance;
      set => _distance = value;
    }

    public float time => _time;

    public void Set(AITargetType t, Collider c, Vector3 p, float d)
    {
      _type = t;
      _collider = c;
      _position = p;
      _distance = d;
      _time = Time.time;
    }

    public void Clear()
    {
      _type = AITargetType.None;
      _collider = null;
      _position = Vector3.zero;
      _distance = Mathf.Infinity;
      _time = 0f;
    }
  }

  /// <summary>
  /// base AI State Machine class
  /// </summary>
  public abstract class AIStateMachine : MonoBehaviour
  {
    public AITarget visualThreat = new AITarget();
    public AITarget audioThreat = new AITarget();

    protected Dictionary<AIStateType, AIState> _states = new Dictionary<AIStateType, AIState>();
    protected AITarget _currentTarget = new AITarget();
    protected AIState _currentState;

    // Root Motion Reference Counts
    // used to set whether we want to play the root motion or not in the Animator
    // used as counter integer since there are more than 1 layer of Animations
    protected int _rootPositionRefCount = 0;
    protected int _rootRotationRefCount = 0;

    protected bool _isTargetReached;

    protected List<Rigidbody> _bodyParts = new List<Rigidbody>();
    protected int _aiBodyPartLayer = -1;

    protected bool _cinematicEnabled = false;

    // idle is its default state
    [SerializeField] protected AIStateType currentStateType = AIStateType.Idle;
    [SerializeField] protected Transform rootBone; // this is the hip bone of the AI
    [SerializeField] protected AIBoneAlignmentType rootBoneAlignment = AIBoneAlignmentType.ZAxis;
    [SerializeField] protected SphereCollider targetTrigger;
    [SerializeField] protected SphereCollider sensorTrigger;
    [SerializeField] [Range(0, 15)] protected float stoppingDistance = 1f;

    [SerializeField] protected AIWaypointNetwork waypointNetwork;

    [Tooltip("Determines whether the Zombie patrols the waypoints in random")] [SerializeField]
    private bool randomPatrol;

    protected int _currentWaypoint = -1;

    // Component Cache
    protected Animator _animator;
    protected NavMeshAgent _navMeshAgent;
    protected Collider _collider;
    protected Transform _transform;

    // Component Cache Accessors & Public Properties
    public bool IsInMeleeRange { get; set; }
    public bool IsTargetReached => _isTargetReached;
    public Animator AIAnimator => _animator;
    public NavMeshAgent AINavMeshAgent => _navMeshAgent;
    public AITargetType CurrentTargetType => _currentTarget.type;
    public Vector3 CurrentTargetPosition => _currentTarget.position;

    public int CurrentTargetColliderId =>
      _currentTarget.collider != null ? _currentTarget.collider.GetInstanceID() : -1;

    public Vector3 SensorPosition
    {
      get
      {
        if (sensorTrigger == null)
        {
          return Vector3.zero;
        }

        var point = sensorTrigger.transform.position;

        point.x += sensorTrigger.center.x * sensorTrigger.transform.lossyScale.x;
        point.y += sensorTrigger.center.y * sensorTrigger.transform.lossyScale.y;
        point.z += sensorTrigger.center.z * sensorTrigger.transform.lossyScale.z;

        return point;
      }
    }

    public float SensorRadius
    {
      get
      {
        if (sensorTrigger == null)
        {
          return 0f;
        }

        var radius = Mathf.Max(sensorTrigger.radius * sensorTrigger.transform.lossyScale.x,
          sensorTrigger.radius * sensorTrigger.transform.lossyScale.y);

        return Mathf.Max(radius, sensorTrigger.radius * sensorTrigger.transform.lossyScale.z);
      }
    }

    public bool CinematicEnabled
    {
      get => _cinematicEnabled;
      set => _cinematicEnabled = value;
    }


    /// <summary>
    /// whether to use Root Motion Position
    /// </summary>
    public bool useRootMotionPosition => _rootPositionRefCount > 0;

    /// <summary>
    /// whether to use Root Motion Rotation
    /// </summary>
    public bool useRootMotionRotation => _rootRotationRefCount > 0;

    protected void Awake()
    {
      // cache components
      _transform = transform;
      _animator = GetComponent<Animator>();
      _navMeshAgent = GetComponent<NavMeshAgent>();
      _collider = GetComponent<Collider>();

      // get BodyPart Layer
      _aiBodyPartLayer = LayerMask.NameToLayer("AI Body Part");

      if (GameSceneManager.Instance != null)
      {
        // Register State Machines to Game Scene Cache
        if (_collider)
        {
          GameSceneManager.Instance.RegisterAIStateMachine(_collider.GetInstanceID(), this);
        }

        if (sensorTrigger)
        {
          GameSceneManager.Instance.RegisterAIStateMachine(sensorTrigger.GetInstanceID(), this);
        }
      }

      if (rootBone != null)
      {
        // cache the bones
        var bodies = rootBone.GetComponentsInChildren<Rigidbody>();

        foreach (var bodyPart in bodies)
        {
          if (bodyPart != null && bodyPart.gameObject.layer == _aiBodyPartLayer)
          {
            bodyPart.isKinematic = true;

            // add to the list of our rag doll list
            _bodyParts.Add(bodyPart);

            // store the body parts
            GameSceneManager.Instance.RegisterAIStateMachine(bodyPart.GetInstanceID(), this);
          }
        }
      }
    }

    protected virtual void Start()
    {
      if (sensorTrigger != null)
      {
        if (sensorTrigger.TryGetComponent<AISensor>(out var aiSensor))
        {
          aiSensor.ParentStateMachine = this;
        }
      }

      var states = GetComponents<AIState>();

      foreach (var state in states)
      {
        if (state != null && !_states.ContainsKey(state.GetStateType()))
        {
          // set the local dictionary if not already in the dictionary
          _states[state.GetStateType()] = state;

          // send this as a parameter
          state.SetStateMachine(this);
        }
      }

      // get the initial state from the dictionary to use as the current state
      if (_states.ContainsKey(currentStateType))
      {
        _currentState = _states[currentStateType];
        _currentState.OnEnterState();
      }
      else
      {
        // fallback case of error
        _currentState = null;
      }

      if (_animator != null)
      {
        // configure the AnimatorController Behaviours
        // start is recommended for configuration
        var aiStateMachineLinks = _animator.GetBehaviours<AIStateMachineLink>();

        foreach (var aiStateMachineLink in aiStateMachineLinks)
        {
          // setup the relationship
          aiStateMachineLink.StateMachine = this;
        }
      }
    }

    /// <summary>
    /// Gives the current state a chance to update itself and perform transitions
    /// </summary>
    protected virtual void Update()
    {
      if (!_currentState) return;

      var newStateType = _currentState.OnUpdate();

      if (newStateType != currentStateType)
      {
        // trigger state change
        if (_states.TryGetValue(newStateType, out var newState))
        {
          // change the state
          _currentState.OnExitState();
          newState.OnEnterState();
          _currentState = newState;
        }
        else if (_states.TryGetValue(AIStateType.Idle, out var fallbackState))
        {
          // fallback - set to Idle State
          _currentState.OnExitState();
          fallbackState.OnEnterState();
          _currentState = fallbackState;
        }

        // set the new state type
        currentStateType = newStateType;
      }
    }

    /// <summary>
    /// called at each tick of the Physics system
    /// 
    /// Keeps track of the current target and the distance from it
    /// makes sure audio and visual threats are cleared at each physics tick
    /// </summary>
    protected virtual void FixedUpdate()
    {
      visualThreat.Clear();
      audioThreat.Clear();

      if (_currentTarget.type != AITargetType.None)
      {
        _currentTarget.distance = Vector3.Distance(_transform.position, _currentTarget.position);
      }

      // set it to false at each physics update
      _isTargetReached = false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="t">ai target</param>
    /// <param name="c">collider</param>
    /// <param name="p">position</param>
    /// <param name="d">distance to target</param>
    public void SetTarget(AITargetType t, Collider c, Vector3 p, float d)
    {
      _currentTarget.Set(t, c, p, d);

      if (targetTrigger != null)
      {
        targetTrigger.radius = stoppingDistance;
        targetTrigger.transform.position = _currentTarget.position;
        targetTrigger.enabled = true;
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="aiTarget"></param>
    public void SetTarget(AITarget aiTarget)
    {
      _currentTarget = aiTarget;

      if (targetTrigger != null)
      {
        targetTrigger.radius = stoppingDistance;
        targetTrigger.transform.position = _currentTarget.position;
        targetTrigger.enabled = true;
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="t"></param>
    /// <param name="c"></param>
    /// <param name="p"></param>
    /// <param name="d"></param>
    /// <param name="s">Stopping Distance</param>
    public void SetTarget(AITargetType t, Collider c, Vector3 p, float d, float s)
    {
      _currentTarget.Set(t, c, p, d);

      if (targetTrigger != null)
      {
        targetTrigger.radius = s;
        targetTrigger.transform.position = _currentTarget.position;
        targetTrigger.enabled = true;
      }
    }

    /// <summary>
    /// Clears the Current Target
    /// </summary>
    public void ClearTarget()
    {
      _currentTarget.Clear();

      if (targetTrigger != null)
      {
        targetTrigger.enabled = false;
      }
    }

    /// <summary>
    /// called by the physics system when AI's main collider enters its trigger
    /// This allows the child state to know when it has entered the sphere of influence of a waypoint
    /// or last player sighted position
    /// </summary>
    /// <param name="other"></param>
    protected void OnTriggerEnter(Collider other)
    {
      if (!other.CompareTag("Target Trigger")) return;

      _isTargetReached = true;

      if (_currentState != null)
      {
        // notify child state
        _currentState.OnDestinationReached(true);
      }
    }

    /// <summary>
    /// Doesn't inform the child state only set the target reached if the AI is in the destination
    /// </summary>
    /// <param name="other"></param>
    protected void OnTriggerStay(Collider other)
    {
      if (targetTrigger == null || other != targetTrigger) return;

      _isTargetReached = true;
    }

    /// <summary>
    /// Informs the child state that AI entity is no longer at its destination
    /// typically true when a new target has been set by the child
    /// </summary>
    /// <param name="other"></param>
    protected void OnTriggerExit(Collider other)
    {
      _isTargetReached = false;

      if (!other.CompareTag("Target Trigger")) return;

      if (_currentState != null)
      {
        // notify child state
        _currentState.OnDestinationReached(false);
      }
    }

    /// <summary>
    /// will be called by the Sensor Manager
    /// </summary>
    /// <param name="type"></param>
    /// <param name="other"></param>
    public virtual void OnTriggerEvent(AITriggerEventType type, Collider other)
    {
      if (_currentState != null)
      {
        _currentState.OnTriggerEvent(type, other);
      }
    }

    /// <summary>
    /// we don't want to do anything at the state machine level
    /// because we don't know what the currentState is so just pass down to AIState
    /// </summary>
    protected virtual void OnAnimatorMove()
    {
      if (_currentState == null) return;

      // root motion of the current state
      _currentState.OnAnimatorUpdated();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="layerIndex"></param>
    protected void OnAnimatorIK(int layerIndex)
    {
      if (_currentState == null) return;

      _currentState.OnAnimatorIKUpdated();
    }

    /// <summary>
    /// Method to talk to our NavMeshAgent in our AI State Machine
    /// we can set the NavMeshAgent's behaviour on state basis
    /// </summary>
    /// <param name="positionUpdate"></param>
    /// <param name="rotationUpdate"></param>
    public void NavMeshAgentControl(bool positionUpdate, bool rotationUpdate)
    {
      if (_navMeshAgent == null) return;

      _navMeshAgent.updatePosition = positionUpdate;
      _navMeshAgent.updateRotation = rotationUpdate;
    }

    /// <summary>
    /// Allows us to set the root position and root rotation ref counts
    /// which decides on the Root Motion Parameters to activate
    /// 
    /// Called by the State Machine Behaviours to Enable / Disable root Motion
    /// </summary>
    /// <param name="rootPosition">-1 or 1</param>
    /// <param name="rootRotation">-1 or 1</param>
    public void AddRootMotionRequest(int rootPosition, int rootRotation)
    {
      _rootPositionRefCount += rootPosition;
      _rootRotationRefCount += rootRotation;
    }

    /// <summary>
    /// Fetches the world space position of the state machine's currently set
    /// waypoint with optional increment
    /// </summary>
    /// <param name="increment">whether to increment to the next waypoint</param>
    /// <returns></returns>
    public Vector3 GetWaypointPosition(bool increment)
    {
      if (_currentWaypoint == -1)
      {
        // initialisation stage, it is the first time it is called
        _currentWaypoint = randomPatrol ? Random.Range(0, waypointNetwork.Waypoints.Count) : 0;
      }

      else if (increment)
      {
        // increment the waypoint index
        NextWaypoint();
      }

      if (waypointNetwork.Waypoints[_currentWaypoint] != null)
      {
        var newWaypoint = waypointNetwork.Waypoints[_currentWaypoint];

        // this is our new target position
        // sets the Target trigger's position
        SetTarget(AITargetType.Waypoint, null, newWaypoint.position,
          Vector3.Distance(newWaypoint.position, transform.position));

        return newWaypoint.position;
      }

      return Vector3.zero;
    }


    /// <summary>
    /// sets the next waypoint
    /// </summary>
    private void NextWaypoint()
    {
      if (randomPatrol && waypointNetwork.Waypoints.Count > 1)
      {
        var oldWaypoint = _currentWaypoint;

        while (_currentWaypoint == oldWaypoint)
        {
          _currentWaypoint = Random.Range(0, waypointNetwork.Waypoints.Count);
        }
      }
      else
      {
        _currentWaypoint = _currentWaypoint == waypointNetwork.Waypoints.Count - 1 ? 0 : _currentWaypoint + 1;
      }
    }

    /// <summary>
    /// Handles when the AI takes damage
    /// </summary>
    /// <param name="position">where the AI is hit</param>
    /// <param name="force">force of the hit</param>
    /// <param name="damage">amount of damage</param>
    /// <param name="bodyPart">which body part is hit</param>
    /// <param name="characterManager">which player hit the AI</param>
    /// <param name="hitDirection">Used for hit animations but doesn't want to rag doll, it is the hitType in Animator</param>
    public virtual void TakeDamage(Vector3 position, Vector3 force, int damage, Rigidbody bodyPart,
      CharacterManager characterManager, int hitDirection = 0)
    {
    }
  }
}