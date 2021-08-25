using System.Collections.Generic;
using Dead_Earth.Scripts.AI.StateMachineBehaviours;
using UnityEngine;
using UnityEngine.AI;

namespace Dead_Earth.Scripts.AI
{
  /// <summary>
  /// States that AI can take
  /// </summary>
  public enum AIStateType
  {
    None,
    Idle,
    Alerted,
    Attack,
    Feeding,
    Pursuit,
    Dead,
    Patrol
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
    protected AITarget _target = new AITarget();
    protected AIState _currentState;

    // Root Motion Reference Counts
    // used to set whether we want to play the root motion or not in the Animator
    // used as counter integer since there are more than 1 layer of Animations
    protected int _rootPositionRefCount = 0;
    protected int _rootRotationRefCount = 0;


    // idle is its default state
    [SerializeField] protected AIStateType currentStateType = AIStateType.Idle;
    [SerializeField] protected SphereCollider targetTrigger;
    [SerializeField] protected SphereCollider sensorTrigger;
    [SerializeField] [Range(0, 15)] protected float stoppingDistance = 1f;

    // Component Cache
    protected Animator _animator;
    protected NavMeshAgent _navMeshAgent;
    protected Collider _collider;
    protected Transform _transform;

    // Component Cache Accessors
    public Animator animator => _animator;
    public NavMeshAgent navMeshAgent => _navMeshAgent;

    public Vector3 sensorPosition
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

    public float sensorRadius
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

      if (_target.type != AITargetType.None)
      {
        _target.distance = Vector3.Distance(_transform.position, _target.position);
      }
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
      _target.Set(t, c, p, d);

      if (targetTrigger != null)
      {
        targetTrigger.radius = stoppingDistance;
        targetTrigger.transform.position = _target.position;
        targetTrigger.enabled = true;
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="aiTarget"></param>
    public void SetTarget(AITarget aiTarget)
    {
      _target = aiTarget;

      if (targetTrigger != null)
      {
        targetTrigger.radius = stoppingDistance;
        targetTrigger.transform.position = _target.position;
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
      _target.Set(t, c, p, d);

      if (targetTrigger != null)
      {
        targetTrigger.radius = s;
        targetTrigger.transform.position = _target.position;
        targetTrigger.enabled = true;
      }
    }

    /// <summary>
    /// Clears the Current Target
    /// </summary>
    public void ClearTarget()
    {
      _target.Clear();

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
    public void OnTriggerEnter(Collider other)
    {
      if (targetTrigger == null || other != targetTrigger) return;

      if (_currentState != null)
      {
        // notify child state
        _currentState.OnDestinationReached(true);
      }
    }

    /// <summary>
    /// Informs the child state that AI entity is no longer at its destionation
    /// typically true when a new target has been set by the child
    /// </summary>
    /// <param name="other"></param>
    public void OnTriggerExit(Collider other)
    {
      if (targetTrigger == null || other != targetTrigger) return;

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
  }
}